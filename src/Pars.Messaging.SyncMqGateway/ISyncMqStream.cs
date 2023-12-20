using System.IO;
using System.Threading.Tasks;

namespace Pars.Messaging;

public interface ISyncMqStream<T>
{
    Task SerializeAsync(Stream output, T value);

    ValueTask<T> DeserializeAysnc(Stream input);
}
