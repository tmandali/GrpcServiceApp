using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Pars.Extensions.SyncMq;

//using var channel = GrpcChannel.ForAddress("https://grpcerptest.azurewebsites.net/");
using var channel = GrpcChannel.ForAddress("https://localhost:7082"); //

var client = new SyncMq.SyncMqClient(channel);

var corelationId = UnsafeByteOperations.UnsafeWrap(Guid.NewGuid().ToByteArray());

var message1 = new MessageBroker() {
    MessageId = Guid.NewGuid().ToString(),
    Topic = "/topic",
    Data = ByteString.Empty, 
    Headers = {
        { "DataAreaId", ByteString.CopyFromUtf8("TRLC") },
        { "CorelationId", corelationId }
    }
};

var message2 = new MessageBroker()
{
    MessageId = Guid.NewGuid().ToString(),
    Topic = "/topic",
    Data = ByteString.Empty,
    Headers = {
        { "DataAreaId", ByteString.CopyFromUtf8("TRLC") },
        { "CorelationId", corelationId }
    }
};

var xx = new HashSet<MessageBroker>
{
    message1,
    message2
};

using var send = client.SendMessage();

await send.RequestStream.WriteAsync(message1);
await send.RequestStream.WriteAsync(message2);

var i = 0;
await foreach (var response in send.ResponseStream.ReadAllAsync())
{
    if (++i == 2)
        await send.RequestStream.CompleteAsync();    
}


Console.WriteLine("Shutting down");
Console.WriteLine("Press any key to exit...");
Console.ReadKey();