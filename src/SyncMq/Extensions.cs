using Grpc.Core;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SyncMq
{
    public static partial class SyncMq
    {
        public static async Task SendMessages(this SyncMqClient client, params MessageBroker[] values)
        {
            using var send = client.SendMessage();
            foreach (var value in values)
            {
                await send.RequestStream.WriteAsync(value);
            }
            await send.RequestStream.CompleteAsync();
        }

        public static Task SendJsonMessages<T>(this SyncMqClient client, params MessageBroker<T>[] values)
        {
            return client.SendMessages(values.Select(s => JsonMessageSerializer.Serialize(s)).ToArray());
        }

        public static async IAsyncEnumerable<MessageBroker> GetMessages(this SyncMqClient client, string topic, string subscriber, string? scope = null)
        {
            var request = new Request() { Topic = topic, Subscriber = subscriber, Scope = scope ?? string.Empty };

            var messages = client.GetMessages();
            await messages.RequestStream.WriteAsync(request);

            while (await messages.ResponseStream.MoveNext())
            {
                yield return messages.ResponseStream.Current;

                if (!messages.ResponseStream.Current.Next)
                    await messages.RequestStream.CompleteAsync();
                else
                    await messages.RequestStream.WriteAsync(request);
            }
        }

        public static async IAsyncEnumerable<MessageBroker<T>> GetJsonMessages<T>(this SyncMqClient client, string topic, string subscriber, string? scope = null)
        {
            await foreach (var item in client.GetMessages(topic, subscriber, scope))
            {
                yield return JsonMessageSerializer.Deserialize<T>(item);
            }
        }
    }
}