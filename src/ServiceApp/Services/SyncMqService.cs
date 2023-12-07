using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Pars.Extensions.SyncMq;

namespace ServiceApp.Services;

public class SyncMqService : SyncMq.SyncMqBase
{
    public override async Task SendMessage(IAsyncStreamReader<MessageBroker> requestStream, IServerStreamWriter<Result> responseStream, ServerCallContext context)
    {
        var i = 0;   // begin tran
        await foreach (var request in requestStream.ReadAllAsync())
        {
            i++;
            await responseStream.WriteAsync(new Result() { Topic = request.Topic, MessageId = request.MessageId, Key = request.Key });
        }
        // commit tran
    }
}