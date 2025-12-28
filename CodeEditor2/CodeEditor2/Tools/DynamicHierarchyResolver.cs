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

        // 対象の基底クラスの型情報をカスタマイズする
        if (typeInfo.Type == _baseType)
        {
            typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
            {
                TypeDiscriminatorPropertyName = "$type", // ディスクリミネータのプロパティ名
                IgnoreUnrecognizedTypeDiscriminators = true,
                UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType
            };

            // 動的に派生クラスを登録
            foreach (var pair in _derivedTypes)
            {
                typeInfo.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(pair.Value, pair.Key));
            }
        }

        return typeInfo;
    }
}
