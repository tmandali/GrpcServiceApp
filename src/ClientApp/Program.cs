using Grpc.Core;
using Grpc.Net.Client;
using Pars.Messaging;

using var channel = GrpcChannel.ForAddress("https://grpcerptest.azurewebsites.net/" //"http://localhost:5121"
    , new() 
    {
        HttpHandler = new HttpClientHandler()
        {
            UseProxy = false,
        }
    }
);

var client = new SyncMqGateway.SyncMqGatewayClient(channel);
var subscriber = client.CreateSubscriptionJsonStream<Person>("subscriber", new[] { "/person" });
await foreach (var message in subscriber.ReadAllAsync())
{

}

Console.ReadLine();

using var publisher = client.CreatePublicationStream();
await publisher.WriteJsonAsync("/person", Guid.NewGuid().ToString(), new Person() { Id = 1, Name = "Timur" });
await publisher.CompleteAsync();


//Console.WriteLine("Subsriber begin"); 

//var client = new SyncMqGateway.SyncMqGatewayClient(channel);
//var subscriber =  client.CreateSubscriptionStream("subscriber", new[]{ "/topic", "/topic1" });
//await foreach (var message in subscriber.ReadAllAsync())
//{
//    Console.WriteLine("{0} {1} message received, bytes {2:N0}", message.Topic, message.MessageId, message.Value.Length);
//}

//Console.WriteLine("Subsriber end");
//Console.ReadLine();

//using var publisher = client.CreatePublicationStream();
//foreach (var file in new[] {
//    @"C:\Users\tmand\Pictures\Ekran görüntüsü 2023-06-15 141121.png",
//    @"C:\Users\tmand\Pictures\Ekran görüntüsü 2023-09-22 101652.png",
//    @"C:\Users\tmand\Pictures\Ekran görüntüsü 2023-09-22 101712.png" })
//{
//    await using var readStream = File.OpenRead(file);
//    var id = await publisher.WriteAsync("/topic", Guid.NewGuid().ToString(), readStream);
//}
//await publisher.CompleteAsync();

Console.WriteLine("Shutting down");
Console.WriteLine("Press any key to exit...");
Console.ReadKey();


public class Person
{
    public int Id { get; set; }
    public string Name { get; set; }
}