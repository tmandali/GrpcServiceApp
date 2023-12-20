namespace Pars.Messaging;

public readonly struct MessageBroker<T>
{
    public MessageBroker(string topic, string messageId, string dataAreaId, T value)
    {
        Topic = topic;
        Value = value;
        MessageId = messageId;
        DataAreaId = dataAreaId;
    }

    public string Topic { get; }
    public string MessageId { get; }
    public string DataAreaId { get; }
    public T Value { get; }
}
