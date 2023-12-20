using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Pars.Messaging.Json;

public class SyncMqJsonStream<T> : ISyncMqStream<T>
{
    public JsonSerializerOptions SerializerOptions { get; set; }

    public ValueTask<T> DeserializeAysnc(Stream input)
    {
        return JsonSerializer.DeserializeAsync<T>(input, SerializerOptions);
    }

    public Task SerializeAsync(Stream output, T value)
    {
        return JsonSerializer.SerializeAsync(output, value, SerializerOptions);
    }
}