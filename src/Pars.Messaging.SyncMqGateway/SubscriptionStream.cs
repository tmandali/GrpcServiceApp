using Google.Protobuf;
using Grpc.Core;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Pars.Messaging;

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