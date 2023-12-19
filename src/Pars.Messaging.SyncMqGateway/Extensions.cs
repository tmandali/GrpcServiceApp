using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Pars.Messaging;

public static partial class Extensions
{
    public static SubscriptionStream<byte[]> CreateSubscriptionStream(this SyncMqGateway.SyncMqGatewayClient client, string name, IEnumerable<string> topics, bool autocommit = true)
    {
        return client.CreateSubscriptionStream(name, topics, data => Task.FromResult(data), autocommit);
    }

    public static SubscriptionStream<T> CreateSubscriptionJson<T>(this SyncMqGateway.SyncMqGatewayClient client, string name, IEnumerable<string> topics, JsonSerializerOptions options = null, bool autocommit = true)
    {
        return client.CreateSubscriptionStream(name, topics, data => 
            Task.FromResult(JsonSerializer.Deserialize<T>(data, options)), autocommit);
    }

    private static SubscriptionStream<T> CreateSubscriptionStream<T>(this SyncMqGateway.SyncMqGatewayClient client, string name, IEnumerable<string> topics, Func<byte[], Task<T>> serializer, bool autocommit = true)
    {
        //JsonSerializer.Deserialize<T>(message.Data.ToByteArray(), options
        var metadata = new Metadata
        {
            { "subscriber", name }
        };

        foreach (var topic in topics.Select((s, i) => new KeyValuePair<string, string>("topic." + i, s)))
            metadata.Add(topic.Key, topic.Value);

        return new SubscriptionStream<T>(client.Subscribe(metadata), serializer, autocommit);
    }

    public static PublicationStream CreatePublicationStream(this SyncMqGateway.SyncMqGatewayClient client)
    {
        return new PublicationStream(client.Publish());
    }
}