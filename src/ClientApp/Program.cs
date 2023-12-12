using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using ServiceApp;

using var channel = GrpcChannel.ForAddress("http://localhost:5121"
    , new()
    {
        HttpHandler = new HttpClientHandler()
        {
            UseProxy = false,
        }
    }
);

SyncMq.SyncMqClient client = new SyncMq.SyncMqClient(channel);

Console.WriteLine("Subsriber begin");
var metadata = new Metadata
{
    new Metadata.Entry("subscriber", "/subscriber"),
    new Metadata.Entry("topic1", "/topic"),
    new Metadata.Entry("topic2", "/topicx"),
};

using var receive = client.Subscribe(metadata);
while (await receive.ResponseStream.MoveNext() && receive.ResponseStream.Current is not null)
{
    string message_id = receive.ResponseStream.Current.MessageId;
    string topic = receive.ResponseStream.Current.Topic;
    Console.WriteLine("{0} {1} begin", topic, message_id);

    using MemoryStream data = new();
    do
    {
        data.Write(receive.ResponseStream.Current.Data.Span);
    } while (!(receive.ResponseStream.Current?.MessageEof ?? true) && await receive.ResponseStream.MoveNext());

    await receive.RequestStream.WriteAsync(new Request() { Commit = true });
    Console.WriteLine("{0} {1} end received bytes {2:N0}", topic, message_id, data.Length);
}
Console.WriteLine("Subsriber end");

Console.ReadLine();

const int ChunkSize = 1024 * 64; // 64 KB

using var publisher = client.Publish();
foreach (var file in new[] {
    @"C:\Users\tmand\Pictures\Ekran görüntüsü 2023-06-15 141121.png",
    @"C:\Users\tmand\Pictures\Ekran görüntüsü 2023-09-22 101652.png",
    @"C:\Users\tmand\Pictures\Ekran görüntüsü 2023-09-22 101712.png" })
{
    var buffer = new byte[ChunkSize];
    var message = new MessageBroker() { MessageId = Guid.NewGuid().ToString(), Topic = "/topic" };

    await using var readStream = File.OpenRead(file);
    var count = await readStream.ReadAsync(buffer);
    while (count > 0)
    {
        message.Data = UnsafeByteOperations.UnsafeWrap(buffer.AsMemory(0, count));
        count = await readStream.ReadAsync(buffer);
        message.MessageEof = count == 0;
        await publisher.RequestStream.WriteAsync(message);

        if (message.MessageEof)
            if (await publisher.ResponseStream.MoveNext())
                Console.WriteLine("published {0}", publisher.ResponseStream.Current.EventId);
    }
}
await publisher.RequestStream.CompleteAsync();

Console.WriteLine("Shutting down");
Console.WriteLine("Press any key to exit...");
Console.ReadKey();

//static void TestGrpcNetClient(GrpcChannel channel, string testName)
//{
//    var callInvoker = channel.CreateCallInvoker();
//    var marshaller = new Marshaller<string>(Encoding.UTF8.GetBytes, Encoding.UTF8.GetString);
//    var method = new Method<string, string>(MethodType.Unary,
//        "test-service", "test-method",
//        marshaller, marshaller);
//    try
//    {
//        Console.WriteLine($"Starting request for {testName}.");
//        var response = callInvoker.BlockingUnaryCall(method, null,
//            default, "test-request");
//        Console.WriteLine($"Got response {response} for {testName}.");
//    }
//    catch (Exception e)
//    {
//        Console.WriteLine($"{testName} failed.");
//        Console.WriteLine(e);
//    }
//}