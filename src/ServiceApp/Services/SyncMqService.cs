using Grpc.Core;
using Pars.Extensions.SyncMq;

namespace ServiceApp.Services;

public class SyncMqService : SyncMq.SyncMqBase
{
    private readonly ILogger<SyncMqService> _logger;

    public SyncMqService(ILogger<SyncMqService> logger)
    {
        _logger = logger;
    }

    public override async Task SendMessage(IAsyncStreamReader<MessageBroker> requestStream, IServerStreamWriter<Result> responseStream, ServerCallContext context)
    {
        await foreach (var request in requestStream.ReadAllAsync())
        {
            await responseStream.WriteAsync(new Result() { Topic = request.Topic, MessageId = request.MessageId, Key = request.Key });
        }
    }

    public override async Task GetMessages(IAsyncStreamReader<Request> request, IServerStreamWriter<MessageBroker> responseStream, ServerCallContext context)
    {
        int i = 0;
        int msg = 0;
        _logger.LogInformation("Begintran {0}",i);
        await foreach (var item in request.ReadAllAsync())
        {
            var message = new MessageBroker() { Topic = item.Topic, Next = ++msg <= 3 };

            await responseStream.WriteAsync(message);
            if (item.Commit)
            {
                _logger.LogInformation("Committran {0}", i);
                _logger.LogInformation("Begintran {0}", ++i);
            }
        }
        _logger.LogInformation("Committran {0}", i);
    }
}