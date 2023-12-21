using ConsoleAppFramework.Person;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using System.Net.Http;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Program
    {
        static async Task Main()
        {
            var channel = GrpcChannel.ForAddress("https://grpcerptest.azurewebsites.net/",
                new GrpcChannelOptions() { HttpHandler = new GrpcWebHandler(new WinHttpHandler())});            
            var personStream = new PersonStream(channel);

            await personStream.WritePersonCreateAsync(
                new SoftwareArchitech() { Id = 1, Name = "Timur" },
                new SoftwareDeveloper() { Id = 1, Name = "Mehmet" });

            var subscriber = personStream.CreateSubscription();
            while (await subscriber.MoveNextAsync())
            {

            }
        }
    }
}