using Google.Protobuf;
using Grpc.Core;
using Pars.Messaging;
using System.Buffers;

namespace ServiceApp.Services;

public class SyncMqService : SyncMqGateway.SyncMqGatewayBase
{
    private readonly ILogger<SyncMqService> _logger;
    private readonly HashSet<MessageBroker> _messages = new();

    public SyncMqService(ILogger<SyncMqService> logger)
    {
        _logger = logger;
    }

    public override async Task Publish(IAsyncStreamReader<MessageBroker> requestStream, IServerStreamWriter<Respose> responseStream, ServerCallContext context)
    {
        _logger.LogInformation("publish begin");
        while (await requestStream.MoveNext() && requestStream.Current is not null) 
        {
            string message_id = requestStream.Current.MessageId;
            string topic = requestStream.Current.Topic;
            _logger.LogInformation("{topic} {message_id} begin", topic, message_id);
            using MemoryStream data = new();
            do
            {
                data.Write(requestStream.Current.Data.Span);
            } while (!(requestStream.Current?.MessageEof ?? true) && await requestStream.MoveNext());                        

            _messages.Add(new MessageBroker()
            {
                MessageId = message_id,
                Topic = topic,
                Data = UnsafeByteOperations.UnsafeWrap(data.ToArray()),
                MessageEof = true
            });
            _logger.LogInformation("{topic} {message_id} end {event_id} received bytes {byte:N0}", topic, message_id, _messages.Count, data.Length);

            await responseStream.WriteAsync(new Respose() { EventId = _messages.Count });
        }
        _logger.LogInformation("publish end");
    }

    const int ChunkSize = 1024 * 64; // 64 KB    

    public override async Task Subscribe(IAsyncStreamReader<Request> requestStream, IServerStreamWriter<MessageBroker> responseStream, ServerCallContext context)
    {
        var subscriber = context.RequestHeaders.Get("subscriber")?.Value;
        var topics = context.RequestHeaders.Where(m => m.Key.StartsWith("topic")).Select(m => m.Value);
        _logger.LogInformation("{subscriber} begin {topic}", subscriber, context.RequestHeaders.Get("topic")?.Value);
        
        foreach (var message in _messages.Where(m => topics.Contains(m.Topic)))        
        {
            var part = new MessageBroker()
            {
                Topic = message.Topic,
                MessageId = message.MessageId
            };
            
            using MemoryStream readStream = new();
            message.Data.WriteTo(readStream);
            readStream.Seek(0, SeekOrigin.Begin);

            var buffer = ArrayPool<byte>.Shared.Rent(ChunkSize);            
            try
            {
                var count = await readStream.ReadAsync(buffer);
                while (count > 0)
                {
                    part.Data = UnsafeByteOperations.UnsafeWrap(buffer.AsMemory(0, count));
                    count = await readStream.ReadAsync(buffer);
                    part.MessageEof = count == 0;
                    await responseStream.WriteAsync(part);
                }
            }
            finally 
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }            

            if (await requestStream.MoveNext())
                _logger.LogInformation("{message id} {commit}", message.MessageId, requestStream.Current.Commit);

        } //while (await requestStream.MoveNext());
        _logger.LogInformation("{subscriber} end", subscriber);
    }
}