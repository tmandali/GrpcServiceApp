using Grpc.Net.Client;
using SyncMq;

//using var channel = GrpcChannel.ForAddress("https://grpcerptest.azurewebsites.net/");
using var channel = GrpcChannel.ForAddress("https://localhost:7082"); //  
SyncMq.SyncMq.SyncMqClient client = new SyncMq.SyncMq.SyncMqClient(channel);

await client.SendMessages(new MessageBroker("/topic", "deneme mesaj", null));

var messages = client.GetMessages("/topic", "sub").GetAsyncEnumerator();
while (await messages.MoveNextAsync())
{
    Console.WriteLine(messages.Current.Data.ToStringUtf8());
}

Console.WriteLine("Shutting down");
Console.WriteLine("Press any key to exit...");
Console.ReadKey();