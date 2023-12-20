using Pars.Messaging.Json;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Pars.Messaging;

public static partial class SyncMqGateway
{
    public static void RegisterJsonType<T>(JsonSerializerOptions options = null)
    {
        registerStreams.Add(typeof(T), new SyncMqJsonStream<T>() { SerializerOptions = options });
    }

    public static void RegisterJsonType<T>(IEnumerable<Type> derivedTypes, string typeDiscriminatorPropertyName = "$type", Func<Type, string> nameResolver = null)
    {
        var options = new JsonSerializerOptions()
        {
            TypeInfoResolver = new PolymorphicTypeResolver<T>(derivedTypes, typeDiscriminatorPropertyName, nameResolver)
        };

        RegisterJsonType<T>(options);
    }
}