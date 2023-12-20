using ConsoleAppFramework.Person;
using Grpc.Net.Client;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ConsoleAppFramework
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            using (var channel = GrpcChannel.ForAddress("https://grpcerptest.azurewebsites.net/"
                , new GrpcChannelOptions()
                {
                    HttpHandler = new HttpClientHandler()
                    {
                        UseProxy = false,
                    }
                }
            ))
            {
                var personStream = new PersonStream(channel);
                await personStream.WritePersonCreateAsync(
                    new SoftwareArchitech() { Id = 1, Name = "Timur" },
                    new SoftwareDeveloper() { Id = 1, Name = "Mehmet" });

                var subscriber = personStream.CreateSubscription();
                while (await subscriber.MoveNextAsync())
                {
                    if (subscriber.Current.Value is SoftwareArchitech architech)
                    {
                        Console.WriteLine($"Architech {architech.Name}");
                    }
                    else if (subscriber.Current.Value is SoftwareDeveloper developer)
                    {
                        Console.WriteLine($"Developer {developer.Name}");
                    }
                }
            }
        }
    }
}