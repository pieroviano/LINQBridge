using System.Collections.Generic;

namespace System.Linq;

internal static class TypeHelper
{
    internal static bool IsEnumerableType(Type enumerableType) => FindGenericType(typeof(IEnumerable<>), enumerableType) != null;

    internal static bool IsKindOfGeneric(Type type, Type definition) => FindGenericType(definition, type) != null;

    internal static Type GetElementType(Type enumerableType)
    {
        var genericType = FindGenericType(typeof(IEnumerable<>), enumerableType);
        return genericType != null ? genericType.GetGenericArguments()[0] : enumerableType;
    }

    internal static Type FindGenericType(Type definition, Type type)
    {
        for (; type != null && type != typeof(object); type = type.BaseType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == definition)
                return type;
            if (definition.IsInterface)
            {
                foreach (var type1 in type.GetInterfaces())
                {
                    var genericType = FindGenericType(definition, type1);
                    if (genericType != null)
                        return genericType;
                }
            }
        }
        return null;
    }

    internal static bool IsNullableType(Type type) => type != null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

    internal static Type GetNonNullableType(Type type) => IsNullableType(type) ? type.GetGenericArguments()[0] : type;
}