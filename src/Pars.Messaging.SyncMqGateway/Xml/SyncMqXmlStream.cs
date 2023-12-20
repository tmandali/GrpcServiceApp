using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Pars.Messaging.Xml;

public class SyncMqXmlStream<T> : ISyncMqStream<T>
{
    private static XmlSerializer serializer;

    public SyncMqXmlStream()
    {
        serializer = new(typeof(T));
    }

    public SyncMqXmlStream(Type[] types)
    {
        serializer = new(typeof(T), types);
    }

    public SyncMqXmlStream(XmlAttributeOverrides attributeOverrides)
    {
        serializer = new(typeof(T), attributeOverrides);
    }

    public async ValueTask<T> DeserializeAysnc(Stream input)
    {
        return (T) await Task.FromResult(serializer.Deserialize(input));
    }

    public Task SerializeAsync(Stream output, T value)
    {
        serializer.Serialize(output, value);
        return Task.CompletedTask;        
    }
}
