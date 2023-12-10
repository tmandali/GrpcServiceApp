using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncMq
{
    public readonly struct MessageBroker<T>
    {
        public MessageBroker(string topic, T value, IDictionary<string, string> headers)
        {
            Topic = topic;
            Value = value;
            Headers = headers;
        }

        public string Topic { get; }
        public T Value { get; }
        public IDictionary<string, string> Headers { get; }
    }

    public sealed partial class MessageBroker
    {
        public MessageBroker(string topic, string utf8Text, IDictionary<string, string>? headers = null) : this(topic, utf8Text, Encoding.UTF8, headers) { }        

        public MessageBroker(string topic, string text, Encoding encoding, IDictionary<string, string>? headers = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentException($"'{nameof(text)}' cannot be null or empty.", nameof(text));
            }

            if (encoding is null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            Topic = topic;
            Data = ByteString.CopyFrom(text ?? string.Empty, encoding);

            headers ??= new Dictionary<string, string>();
            Headers.Add(headers.ToDictionary(k => k.Key, v => ByteString.CopyFromUtf8(v.Value)));
        }
    }
}