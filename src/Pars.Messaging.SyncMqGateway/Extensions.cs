using Grpc.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Pars.Messaging;

public static partial class Extensions
{
    private static SubscriptionStream<T> CreateSubscriptionStream<T>(this SyncMqGateway.SyncMqGatewayClient client, string name, IEnumerable<string> topics, Func<byte[], Task<T>> deserializer,  bool autocommit = true)
    {
        var metadata = new Metadata
        {
            { "subscriber", name }
        };

        foreach (var topic in topics.Select((s, i) => new KeyValuePair<string, string>("topic." + i, s)))
            metadata.Add(topic.Key, topic.Value);

        return new SubscriptionStream<T>(client.Subscribe(metadata), deserializer, autocommit);
    }

    public static IAsyncEnumerator<MessageBroker<byte[]>> CreateSubscriptionStream(this SyncMqGateway.SyncMqGatewayClient client, string name, IEnumerable<string> topics, bool autocommit = true)
    {
        return client.CreateSubscriptionStream(name, topics, data => Task.FromResult(data), autocommit);
    }
   
    public static IAsyncEnumerator<MessageBroker<T>> CreateSubscriptionJsonStream<T>(this SyncMqGateway.SyncMqGatewayClient client, string name, IEnumerable<string> topics, JsonSerializerOptions options = null, bool autocommit = true)
    {
        return client.CreateSubscriptionStream<T>(name, topics, data => Task.FromResult(JsonSerializer.Deserialize<T>(data, options)), autocommit);
    }

    public static PublicationStream CreatePublicationStream(this SyncMqGateway.SyncMqGatewayClient client)
    {
        return new PublicationStream(client.Publish());
    }

    public static async Task<long> WriteJsonAsync<T>(this PublicationStream publisher, string topic, string messageId, T value, string dataAreaId = null, JsonSerializerOptions options = null, int size = 1024 * 65)
    {
        using var stream = new MemoryStream();
        JsonSerializer.SerializeToUtf8Bytes(value, options);
        return await publisher.WriteAsync(topic, messageId, stream, dataAreaId, size);
    }

    public static async IAsyncEnumerable<T> ReadAllAsync<T>(this IAsyncEnumerator<T> streamReader)
    {
        while (await streamReader.MoveNextAsync().ConfigureAwait(continueOnCapturedContext: false))
        {
            yield return streamReader.Current;
        }
    }
}