using Grpc.Net.Client;
using Pars.Messaging;
using System.Threading.Tasks;
using static Pars.Messaging.SyncMqGateway;

namespace ConsoleAppFramework.Person
{
    public class PersonStream
    {
        private readonly SyncMqGatewayClient client;

        public PersonStream(GrpcChannel channel)
        {
            client = new SyncMqGatewayClient(channel);
            //RegisterJsonType<Person>(new[] { typeof(SoftwareDeveloper), typeof(SoftwareArchitech) });
            RegisterXmlType<Person>(new[] { typeof(SoftwareDeveloper), typeof(SoftwareArchitech) });
        }

        public SubscriptionStream<Person> CreateSubscription()
        {
            return client.CreateSubscriptionStream<Person>("subscriber", new[] { "/person_create", "/person_update" });
        }

        public Task WritePersonCreateAsync(params Person[] persons)
        {
            return WritePersonAsync("/person_create", persons);
        }

        public Task WritePersonUpdateAsync(params Person[] persons)
        {
            return WritePersonAsync("/person_update", persons);
        }

        private async Task WritePersonAsync(string topic, params Person[] persons)
        {
            using (var publisher = client.CreatePublicationStream())
            {
                foreach (var person in persons)
                {
                    await publisher.WriteAsync<Person>(topic, person, "", null);
                }
                await publisher.CompleteAsync();
            }
        }
    }
}