using Grpc.Net.Client;
using Pars.Messaging;


// The port number must match the port of the gRPC server.
using var channel = GrpcChannel.ForAddress("https://grpcerptest.azurewebsites.net/"); 
var client = new Greeter.GreeterClient(channel);
var reply = await client.SayHelloAsync(
                  new HelloRequest { Name = "GreeterClient" });
Console.WriteLine("Greeting: " + reply.Message);


//using var channel = GrpcChannel.ForAddress("https://grpcerptest.azurewebsites.net/" //"http://localhost:5121"
//    , new() 
//    {
//        HttpHandler = new HttpClientHandler()
//        {
//            UseProxy = false,
//        }
//    }
//);

//var personStream = new PersonStream(channel);
//await personStream.WritePersonCreateAsync(
//    new SoftwareArchitech() { Id = 1, Name = "Timur" }, 
//    new SoftwareDeveloper() { Id = 1, Name = "Mehmet" });

//var subscriber = personStream.CreateSubscription();
//while (await subscriber.MoveNextAsync())
//{
//    var result = subscriber.Current.Value switch
//    {
//        SoftwareArchitech architech => $"Architech {architech.Name}",
//        SoftwareDeveloper developer => $"Developer {developer.Name}",
//        _ => string.Empty
//    };
//    Console.WriteLine(result);
//}

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