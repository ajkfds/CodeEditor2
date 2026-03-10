using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

public class DynamicHierarchyResolver : DefaultJsonTypeInfoResolver
{
    private readonly Type _baseType;
    private readonly Dictionary<string, Type> _derivedTypes;

    public DynamicHierarchyResolver(Type baseType, Dictionary<string, Type> derivedTypes)
    {
        _baseType = baseType;
        _derivedTypes = derivedTypes;
    }

    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        JsonTypeInfo typeInfo = base.GetTypeInfo(type, options);

        // 蟇ｾ雎｡縺ｮ蝓ｺ蠎輔け繝ｩ繧ｹ縺ｮ蝙区ュ蝣ｱ繧偵き繧ｹ繧ｿ繝槭う繧ｺ縺吶ｋ
        if (typeInfo.Type == _baseType)
        {
            typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
            {
                TypeDiscriminatorPropertyName = "$type", // 繝・ぅ繧ｹ繧ｯ繝ｪ繝溘ロ繝ｼ繧ｿ縺ｮ繝励Ο繝代ユ繧｣蜷・
                IgnoreUnrecognizedTypeDiscriminators = true,
                UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType
            };

            // 蜍慕噪縺ｫ豢ｾ逕溘け繝ｩ繧ｹ繧堤匳骭ｲ
            foreach (var pair in _derivedTypes)
            {
                typeInfo.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(pair.Value, pair.Key));
            }
        }

        return typeInfo;
    }
}
