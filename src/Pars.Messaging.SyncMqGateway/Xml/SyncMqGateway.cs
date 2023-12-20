using Pars.Messaging.Xml;
using System;
using System.Xml.Serialization;

namespace Pars.Messaging;

public static partial class SyncMqGateway
{
    public static void RegisterXmlType<T>(XmlAttributeOverrides xmlAttributes)
    {
        registerStreams.Add(typeof(T), new SyncMqXmlStream<T>(xmlAttributes));
    }

    public static void RegisterXmlType<T>(params Type[] derivedTypes)
    {
        registerStreams.Add(typeof(T), new SyncMqXmlStream<T>(derivedTypes));
    }
}