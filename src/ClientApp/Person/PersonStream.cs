using Pars.Messaging;
using static Pars.Messaging.SyncMqGateway;

namespace ClientApp.Person;

public static class PersonStream
{
    public static void RegisterPerson()
    {
        RegisterJsonType<Person>(new[] { typeof(SoftwareDeveloper), typeof(SoftwareArchitech) });
    }

    public static SubscriptionStream<Person> CreatePersonSubscription(this SyncMqGatewayClient client)
    {
        return client.CreateSubscriptionStream<Person>("subscriber", new[] { "/person_create", "/person_update" });
    }

    public static async Task WritePersonCreateAsync(this SyncMqGatewayClient client, params Person[] persons)
    {
        using var publisher = client.CreatePublicationStream();
        foreach (var person in persons)
        {
            await publisher.WriteAsync("/person_create", person);
        }        
        await publisher.CompleteAsync();
    }
}