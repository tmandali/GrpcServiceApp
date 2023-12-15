using CommunityToolkit.HighPerformance;
using Google.Protobuf;
using Grpc.Core;
using System;
using System.Buffers;
using System.Linq;
using System.Threading.Tasks;

namespace Pars.Messaging;

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
