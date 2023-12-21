using ClientFramework.Person;
using Grpc.Net.Client;
using Pars.Messaging;
using System.Net.Http;

using var channel = GrpcChannel.ForAddress("https://grpcerptest.azurewebsites.net/",
    new GrpcChannelOptions() { HttpHandler = new WinHttpHandler() });
var client = new Greeter.GreeterClient(channel);
var reply = await client.SayHelloAsync(
                  new HelloRequest { Name = "GreeterClient" });
Console.WriteLine("Greeting: " + reply.Message);

//using var channel = GrpcChannel.ForAddress("https://grpcerptest.azurewebsites.net/",
//    new GrpcChannelOptions() { HttpHandler = new WinHttpHandler() });

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