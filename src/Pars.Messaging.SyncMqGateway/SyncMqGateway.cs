using Grpc.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Pars.Messaging;

public static partial class SyncMqGateway
{
    private static readonly Dictionary<Type, object> registerStreams = new();

    private static SubscriptionStream<T> CreateSubscriptionStream<T>(this SyncMqGatewayClient client, string name, IEnumerable<string> topics, Func<Stream, ValueTask<T>> deserializer, bool autocommit = true)
    {
        var metadata = new Metadata
        {
            { "subscriber", name }
        };

        foreach (var topic in topics.Select((s, i) => new KeyValuePair<string, string>("topic." + i, s)))
            metadata.Add(topic.Key, topic.Value);

        return new SubscriptionStream<T>(client.Subscribe(metadata), deserializer, autocommit);
    }

    public static SubscriptionStream<T> CreateSubscriptionStream<T>(this SyncMqGatewayClient client, string name, IEnumerable<string> topics, bool autocommit = true)
    {
        if (registerStreams.TryGetValue(typeof(T), out var resolver) && resolver is ISyncMqStream<T> syncMqStream)
        {
            return client.CreateSubscriptionStream(name, topics, syncMqStream.DeserializeAysnc, autocommit);
        }

        throw new InvalidOperationException("Type not be registered");
    }

    public static PublicationStream CreatePublicationStream(this SyncMqGatewayClient client)
    {
        return new PublicationStream(client.Publish());
    }

    public static async Task<long> WriteAsync<T>(this PublicationStream publisher, string topic, T value, string messageId = null, string dataAreaId = null)
    {
        if (registerStreams.TryGetValue(typeof(T), out var resolver) && resolver is ISyncMqStream<T> syncMqStream)
        {
            using var stream = new MemoryStream();
            await syncMqStream.SerializeAsync(stream, value);
            stream.Seek(0, SeekOrigin.Begin);
            return await publisher.WriteAsync(topic, messageId ??= Guid.NewGuid().ToString(), stream, dataAreaId, (int) stream.Length);
        }

        throw new InvalidOperationException("Type not be registered");
    }

    public static async IAsyncEnumerable<T> ReadAllAsync<T>(this IAsyncEnumerator<T> streamReader)
    {
        while (await streamReader.MoveNextAsync().ConfigureAwait(continueOnCapturedContext: false))
        {
            yield return streamReader.Current;
        }
    }
}
