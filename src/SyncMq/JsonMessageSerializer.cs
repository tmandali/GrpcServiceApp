using Google.Protobuf;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SyncMq
{
    public sealed class JsonMessageSerializer 
    {
        public static MessageBroker Serialize<T>(MessageBroker<T> message)
        {
            using var ms = new MemoryStream();
            JsonSerializer.Serialize(ms, message.Value);
            var broker = new MessageBroker() {
                Topic = message.Topic,  
                Data = ByteString.FromStream(ms)
            };

            broker.Headers.Add(message.Headers.ToDictionary(k=>k.Key, v => ByteString.CopyFromUtf8(v.Value)));
            return broker;
        }

        public static MessageBroker<T> Deserialize<T>(MessageBroker message, JsonSerializerOptions? options = null)
        {
            return new MessageBroker<T>(
                message.Topic,
                JsonSerializer.Deserialize<T>(message.Data.ToByteArray(), options)!,
                message.Headers.ToDictionary(k => k.Key, v => v.Value.ToStringUtf8()));
        }
    }

   
}