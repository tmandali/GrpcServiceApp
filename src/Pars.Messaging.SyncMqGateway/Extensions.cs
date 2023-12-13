using CommunityToolkit.HighPerformance;
using Google.Protobuf;
using Grpc.Core;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Pars.Messaging;

public static partial class Extensions
{
    const int ChunkSize = 1024 * 64; // 64 KB

    public static async Task Publish(this SyncMqGateway.SyncMqGatewayClient client, IEnumerable<MessageBroker> messages, Action<MessageBroker, long> action = null)
    {
        using var publisher = client.Publish();
        foreach (var message in messages)
        {            
            using var readStream = new MemoryStream();
            message.Data.WriteTo(readStream);
            readStream.Seek(0, SeekOrigin.Begin);

            var part = new MessageBroker() { MessageId = message.MessageId, Topic = message.Topic };            
            var buffer = ArrayPool<byte>.Shared.Rent(ChunkSize);
            try
            {
                var count = readStream.Read(buffer);
                while (count > 0)
                {
                    part.Data = UnsafeByteOperations.UnsafeWrap(buffer.AsMemory(0, count));
                    count = await readStream.ReadAsync(buffer);
                    part.MessageEof = count == 0;

                    part.Data.Memory.AsStream().Dispose();

                    await publisher.RequestStream.WriteAsync(part);

                    if (part.MessageEof)
                        if (await publisher.ResponseStream.MoveNext())
                            action?.Invoke(part, publisher.ResponseStream.Current.EventId);
                }
            }
            finally 
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        await publisher.RequestStream.CompleteAsync();
    }

    public static async Task Subscribe(this SyncMqGateway.SyncMqGatewayClient client, string name, IEnumerable<string> topics)
    {
        var metadata = new Metadata
        {
            { "subscriber", name }
        };

        foreach (var topic in topics.Select((s,i) => new KeyValuePair<string,string>("topic."+i, s)))
            metadata.Add(topic.Key, topic.Value);

        using var subscriber = client.Subscribe(metadata);
        while (await subscriber.ResponseStream.MoveNext() && subscriber.ResponseStream.Current is not null)
        {
            string message_id = subscriber.ResponseStream.Current.MessageId;
            string topic = subscriber.ResponseStream.Current.Topic;
            //Console.WriteLine("{0} {1} begin", topic, message_id);            

            using MemoryStream data = new();
            do
            {
                data.Write(subscriber.ResponseStream.Current.Data.Span);
            } while (!(subscriber.ResponseStream.Current?.MessageEof ?? true) && await subscriber.ResponseStream.MoveNext());

            await subscriber.RequestStream.WriteAsync(new Request() { Commit = true });
            //Console.WriteLine("{0} {1} end received bytes {2:N0}", topic, message_id, data.Length);
        }
        //Console.WriteLine("Subsriber end");
    }
}