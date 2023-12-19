using Grpc.Core;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Pars.Messaging;

public static partial class SyncMqGateway
{
    private static readonly Dictionary<Type, object> registerTypes = new();

    public static void RegisterJsonType<T>(JsonSerializerOptions options = null)
    {
        registerTypes.Add(typeof(T), new Marshaller<T>(value => JsonSerializer.SerializeToUtf8Bytes(value, options), data => JsonSerializer.Deserialize<T>(data, options)));
    }

    public static void RegisterJsonType<T>(IEnumerable<Type> derivedTypes, string typeDiscriminatorPropertyName = "$type", Func<Type, string> nameResolver = null)
    {
        var options = new JsonSerializerOptions()
        {
            TypeInfoResolver = new PolymorphicTypeResolver(typeof(T), derivedTypes, typeDiscriminatorPropertyName, nameResolver)
        };

        RegisterJsonType<T>(options);
    }

    private static SubscriptionStream<T> CreateSubscriptionStream<T>(this SyncMqGateway.SyncMqGatewayClient client, string name, IEnumerable<string> topics, Func<byte[], T> deserializer, bool autocommit = true)
    {
        var metadata = new Metadata
        {
            { "subscriber", name }
        };

        foreach (var topic in topics.Select((s, i) => new KeyValuePair<string, string>("topic." + i, s)))
            metadata.Add(topic.Key, topic.Value);

        return new SubscriptionStream<T>(client.Subscribe(metadata), deserializer, autocommit);
    }

    public static SubscriptionStream<byte[]> CreateSubscriptionStream(this SyncMqGateway.SyncMqGatewayClient client, string name, IEnumerable<string> topics, bool autocommit = true)
    {
        return client.CreateSubscriptionStream(name, topics, data => data, autocommit);
    }

    public static SubscriptionStream<T> CreateSubscriptionStream<T>(this SyncMqGateway.SyncMqGatewayClient client, string name, IEnumerable<string> topics, bool autocommit = true)
    {
        if (registerTypes.TryGetValue(typeof(T), out var resolver) && resolver is Marshaller<T> marshaller)
        {
            return client.CreateSubscriptionStream(name, topics, marshaller.Deserializer, autocommit);
        }

        return null;
    }

    public static PublicationStream CreatePublicationStream(this SyncMqGateway.SyncMqGatewayClient client)
    {
        return new PublicationStream(client.Publish());
    }

    public static async Task<long> WriteJsonAsync<T>(this PublicationStream publisher, string topic, string messageId, T value, string dataAreaId = null, JsonSerializerOptions options = null, int size = 1024 * 65)
    {
        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, value, options);
        stream.Seek(0, SeekOrigin.Begin);
        return await publisher.WriteAsync(topic, messageId, stream, dataAreaId, (int)stream.Length);
    }

    public static async IAsyncEnumerable<T> ReadAllAsync<T>(this IAsyncEnumerator<T> streamReader)
    {
        while (await streamReader.MoveNextAsync().ConfigureAwait(continueOnCapturedContext: false))
        {
            yield return streamReader.Current;
        }
    }
}