using Grpc.Core;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Pars.Messaging;

public sealed class SubscriptionStream<T> : IDisposable, IAsyncStreamReader<MessageBroker<T>>
{
    private readonly AsyncDuplexStreamingCall<Request, MessageBroker> _streamingCall;
    private readonly bool _autoCommit;

    private readonly Func<Byte[], Task<T>> _serializer;

    public SubscriptionStream(AsyncDuplexStreamingCall<Request, MessageBroker> streamingCall, Func<Byte[], Task<T>> serializer, bool autoCommit = true)
    {
        _streamingCall = streamingCall;
        _autoCommit = autoCommit;
        _serializer = serializer;
    }

    public MessageBroker<T> Current { get; private set; }

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
            using MemoryStream data = new();
            do
            {
                _streamingCall.ResponseStream.Current.Data.WriteTo(data);   
            } while (!(_streamingCall.ResponseStream.Current?.MessageEof ?? true) && await _streamingCall.ResponseStream.MoveNext(cancellationToken));

            data.Seek(0, SeekOrigin.Begin);

            Current = new MessageBroker<T>(
                _streamingCall.ResponseStream.Current.Topic,                 
                _streamingCall.ResponseStream.Current.MessageId, 
                _streamingCall.ResponseStream.Current.DataAreaId,
                await _serializer(data.ToArray()));

            if (_autoCommit)
                await _streamingCall.RequestStream.WriteAsync(new Request() { Commit = _autoCommit });

            return true;
        }

        return false;
    }
}