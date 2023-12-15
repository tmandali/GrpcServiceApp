using Grpc.Core;
using System.Collections.Generic;
using System.Linq;

namespace Pars.Messaging;

public static partial class Extensions
{
    public static SubscriptionStream CreateSubscriptionStream(this SyncMqGateway.SyncMqGatewayClient client, string name, IEnumerable<string> topics, bool autocommit = true)
    {
        var metadata = new Metadata
        {
            { "subscriber", name }
        };

        foreach (var topic in topics.Select((s, i) => new KeyValuePair<string, string>("topic." + i, s)))
            metadata.Add(topic.Key, topic.Value);

        return new SubscriptionStream(client.Subscribe(metadata), autocommit);        
    }

    public static PublicationStream CreatePublicationStream(this SyncMqGateway.SyncMqGatewayClient client)
    {
        return new PublicationStream(client.Publish());
    }
}
