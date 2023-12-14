using CommunityToolkit.HighPerformance;
using Google.Protobuf;
using Grpc.Core;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pars.Messaging;

public static partial class Extensions
{
    const int ChunkSize = 1024 * 64; // 64 KB

    //public static async Task Publish(this SyncMqGateway.SyncMqGatewayClient client, IEnumerable<MessageBroker> messages, Action<MessageBroker, long> action = null)
    //{
    //    using var publisher = client.Publish();
    //    foreach (var message in messages)
    //    {            
    //        using var readStream = new MemoryStream();
    //        message.Data.WriteTo(readStream);
    //        readStream.Seek(0, SeekOrigin.Begin);

    //        var part = new MessageBroker() { MessageId = message.MessageId, Topic = message.Topic };            
    //        var buffer = ArrayPool<byte>.Shared.Rent(ChunkSize);
    //        try
    //        {
    //            var count = readStream.Read(buffer);
    //            while (count > 0)
    //            {
    //                part.Data = UnsafeByteOperations.UnsafeWrap(buffer.AsMemory(0, count));
    //                count = await readStream.ReadAsync(buffer);
    //                part.MessageEof = count == 0;

    //                part.Data.Memory.AsStream().Dispose();

    //                await publisher.RequestStream.WriteAsync(part);

    //                if (part.MessageEof)
    //                    if (await publisher.ResponseStream.MoveNext())
    //                        action?.Invoke(part, publisher.ResponseStream.Current.EventId);
    //            }
    //        }
    //        finally 
    //        {
    //            ArrayPool<byte>.Shared.Return(buffer);
    //        }
    //    }
    //    await publisher.RequestStream.CompleteAsync();
    //}

    //public static async Task Subscribe(this SyncMqGateway.SyncMqGatewayClient client, string name, IEnumerable<string> topics)
    //{
    //    var metadata = new Metadata
    //    {
    //        { "subscriber", name }
    //    };

    //    foreach (var topic in topics.Select((s,i) => new KeyValuePair<string,string>("topic."+i, s)))
    //        metadata.Add(topic.Key, topic.Value);

    //    using var subscriber = client.Subscribe(metadata);
    //    while (await subscriber.ResponseStream.MoveNext() && subscriber.ResponseStream.Current is not null)
    //    {
    //        string message_id = subscriber.ResponseStream.Current.MessageId;
    //        string topic = subscriber.ResponseStream.Current.Topic;
    //        //Console.WriteLine("{0} {1} begin", topic, message_id);            

    //        using MemoryStream data = new();
    //        do
    //        {
    //            data.Write(subscriber.ResponseStream.Current.Data.Span);
    //        } while (!(subscriber.ResponseStream.Current?.MessageEof ?? true) && await subscriber.ResponseStream.MoveNext());

    //        await subscriber.RequestStream.WriteAsync(new Request() { Commit = true });
    //        //Console.WriteLine("{0} {1} end received bytes {2:N0}", topic, message_id, data.Length);
    //    }
    //    //Console.WriteLine("Subsriber end");
    //}

    public static SubscriptionStream CreateSubscriptionStream(this SyncMqGateway.SyncMqGatewayClient client, string name, IEnumerable<string> topics, bool autocommit = true)
    {
        var metadata = new Metadata
        {
            { "subscriber", name }
        };

        foreach (var topic in topics.Select((s, i) => new KeyValuePair<string, string>("topic." + i, s)))
            metadata.Add(topic.Key, topic.Value);

        return new SubscriptionStream(client.Subscribe(metadata), autocommit);        
    }

    public static PublicationStream CreatePublicationStream(this SyncMqGateway.SyncMqGatewayClient client)
    {
        return new PublicationStream(client.Publish());
    }
}

public sealed class PublicationStream : IDisposable, IAsyncStreamWriter<MessageBroker>
{
    private readonly AsyncDuplexStreamingCall<MessageBroker, Respose> _streamingCall;
    public event EventHandler<long> Commit;

    public PublicationStream(AsyncDuplexStreamingCall<MessageBroker, Respose> streamingCall)
    {
        _streamingCall = streamingCall;
    }

    public WriteOptions WriteOptions { get => _streamingCall.RequestStream.WriteOptions; set => _streamingCall.RequestStream.WriteOptions = value; }

    public Task WriteAsync(MessageBroker message) =>
        WriteAsync(1024 * 64, message);

    public async Task WriteAsync(int chunkSize, params MessageBroker[] messages)
    {
        if (chunkSize < 1024)
            throw new ArgumentOutOfRangeException(nameof(chunkSize));

        await Task.WhenAll(messages.Select(message => WriteAsync(chunkSize, message)));
    }

    private async Task WriteAsync(int chunkSize, MessageBroker message)
    {
        using var readStream = message.Data.Memory.AsStream();
        var buffer = ArrayPool<byte>.Shared.Rent(chunkSize);
        try
        {
            var count = await readStream.ReadAsync(buffer);
            while (count > 0)
            {
                var part = new MessageBroker
                {                    
                    MessageId = message.MessageId,
                    Topic = message.Topic,
                    Data = UnsafeByteOperations.UnsafeWrap(buffer.AsMemory(0, count))
                };

                count = await readStream.ReadAsync(buffer);
                part.MessageEof = count == 0;
                await _streamingCall.RequestStream.WriteAsync(part);

                if (part.MessageEof)
                    if (await _streamingCall.ResponseStream.MoveNext())
                        Commit?.Invoke(this, _streamingCall.ResponseStream.Current.EventId);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public async ValueTask CompleteAsync()
    {    
        await _streamingCall?.RequestStream.CompleteAsync();
    }

    void IDisposable.Dispose()
    {
        _streamingCall?.Dispose();
    }
}

public sealed class SubscriptionStream : IDisposable, IAsyncStreamReader<MessageBroker>
{
    private readonly AsyncDuplexStreamingCall<Request, MessageBroker> _streamingCall;
    private readonly bool _autoCommit;

    public SubscriptionStream(AsyncDuplexStreamingCall<Request, MessageBroker> streamingCall, bool autoCommit = true)
    {
        _streamingCall = streamingCall;
        _autoCommit = autoCommit;
    }

    public MessageBroker Current { get; private set; }

    public void Dispose()
    {
        _streamingCall?.Dispose();
    }

    public async ValueTask CommitAsync()
    {
        if (!_autoCommit)
            await _streamingCall.RequestStream.WriteAsync(new Request() { Commit = true });
    }

    public async Task<bool> MoveNext(CancellationToken cancellationToken)
    {
        if (await _streamingCall.ResponseStream.MoveNext(cancellationToken) && _streamingCall.ResponseStream.Current is not null)
        {
            Current = new MessageBroker()
            {
                MessageId = _streamingCall.ResponseStream.Current.MessageId,
                Topic = _streamingCall.ResponseStream.Current.Topic,
            };

            using MemoryStream data = new();
            do
            {
                _streamingCall.ResponseStream.Current.Data.WriteTo(data);   
            } while (!(_streamingCall.ResponseStream.Current?.MessageEof ?? true) && await _streamingCall.ResponseStream.MoveNext(cancellationToken));

            data.Seek(0, SeekOrigin.Begin);
            Current.MessageEof = true;
            Current.Data = await ByteString.FromStreamAsync(data);

            if (_autoCommit)
                await _streamingCall.RequestStream.WriteAsync(new Request() { Commit = _autoCommit });

            return true;
        }

        return false;
    }
}