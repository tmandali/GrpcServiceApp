using System;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Collections.Generic;

namespace Pars.Messaging;

public class PolymorphicTypeResolver : DefaultJsonTypeInfoResolver
{
    private readonly JsonPolymorphismOptions _options;
    private readonly Type _baseType;

    public PolymorphicTypeResolver(Type baseType, IEnumerable<Type> derivedTypes = null, string typeDiscriminatorPropertyName = "$type", Func<Type, string> nameResolver = null)
    {
        _baseType = baseType;
        _options =  new JsonPolymorphismOptions()
        {
            TypeDiscriminatorPropertyName = typeDiscriminatorPropertyName,
            IgnoreUnrecognizedTypeDiscriminators = true,
            UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization,
        };

        if (derivedTypes is not null)
            foreach (var derivedType in derivedTypes)
            {
                _options.DerivedTypes.Add(
                    nameResolver is null ? new JsonDerivedType(derivedType): new JsonDerivedType(derivedType, nameResolver(derivedType)));
            }        
    }

    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        JsonTypeInfo jsonTypeInfo = base.GetTypeInfo(type, options);
        if (jsonTypeInfo.Type == _baseType)
        {
            jsonTypeInfo.PolymorphismOptions = _options; 
        }

        return jsonTypeInfo;
    }
}