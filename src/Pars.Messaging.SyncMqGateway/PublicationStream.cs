using CommunityToolkit.HighPerformance;
using Google.Protobuf;
using Grpc.Core;
using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;

namespace Pars.Messaging;

public sealed class PublicationStream : IDisposable
{
    private readonly AsyncDuplexStreamingCall<MessageBroker, Respose> _streamingCall;

    public PublicationStream(AsyncDuplexStreamingCall<MessageBroker, Respose> streamingCall)
    {
        _streamingCall = streamingCall;
    }

    public async Task<long> WriteAsync(string topic, string messageId, Stream data, int size = 1024 * 65)
    {     
        var buffer = ArrayPool<byte>.Shared.Rent(size);
        try
        {
            var count = await data.ReadAsync(buffer);
            while (count > 0)
            {
                var part = new MessageBroker
                {                    
                    MessageId = messageId,
                    Topic = topic,
                    Data = UnsafeByteOperations.UnsafeWrap(buffer.AsMemory(0, count)),
                    MessageEof = false
                };

                count = await data.ReadAsync(buffer);
                part.MessageEof = count == 0;
                await _streamingCall.RequestStream.WriteAsync(part);

                if (part.MessageEof)
                    if (await _streamingCall.ResponseStream.MoveNext())
                        return _streamingCall.ResponseStream.Current.EventId;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return 0;
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