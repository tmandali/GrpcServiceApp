using Grpc.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Pars.Messaging;

public sealed class SubscriptionStream<T> : IAsyncEnumerator<MessageBroker<T>>
{
    private readonly AsyncDuplexStreamingCall<Request, MessageBroker> _streamingCall;
    private readonly Func<Stream, ValueTask<T>> _deserializer;
    private readonly bool _autoCommit;
    
    public SubscriptionStream(AsyncDuplexStreamingCall<Request, MessageBroker> streamingCall, Func<Stream, ValueTask<T>> deserializer, bool autoCommit = true)
    {
        _streamingCall = streamingCall;
        _deserializer = deserializer;
        _autoCommit = autoCommit;
    }

    public MessageBroker<T> Current { get; private set; }

    public async ValueTask CommitAsync()
    {
        if (!_autoCommit)
            await _streamingCall.RequestStream.WriteAsync(new Request() { Commit = true });
    }

    public async ValueTask DisposeAsync()
    {
        await Task.Run(() => _streamingCall?.Dispose());        
    }

    public async ValueTask<bool> MoveNextAsync()
    {
        if (await _streamingCall.ResponseStream.MoveNext() && _streamingCall.ResponseStream.Current is not null)
        {
            using MemoryStream data = new();
            do
            {
                _streamingCall.ResponseStream.Current.Data.WriteTo(data);   
            } while (!(_streamingCall.ResponseStream.Current?.MessageEof ?? true) && await _streamingCall.ResponseStream.MoveNext());

            data.Seek(0, SeekOrigin.Begin);

            Current = new MessageBroker<T>(
                _streamingCall.ResponseStream.Current.Topic, 
                _streamingCall.ResponseStream.Current.MessageId, 
                _streamingCall.ResponseStream.Current.DataAreaId, 
                await _deserializer(data));

            if (_autoCommit)
                await _streamingCall.RequestStream.WriteAsync(new Request() { Commit = _autoCommit });

            return true;
        }

        return false;
    }    
}