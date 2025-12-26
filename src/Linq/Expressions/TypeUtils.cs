using System.Reflection;

namespace System.Linq.Expressions;

internal static class TypeUtils
{
    private const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    internal const MethodAttributes PublicStatic = MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Public | MethodAttributes.Static;

    private readonly static Assembly _mscorlib;

    private readonly static Assembly _systemCore;

    static TypeUtils()
    {
        _mscorlib = typeof(object).Assembly;
        _systemCore = typeof(Expression).Assembly;
    }

    internal static bool AreEquivalent(Type t1, Type t2)
    {
        if (t1 == t2)
        {
            return true;
        }
        return t1==(t2);
    }

    internal static bool AreReferenceAssignable(Type dest, Type src)
    {
        if (AreEquivalent(dest, src))
        {
            return true;
        }
        if (!dest.IsValueType && !src.IsValueType && dest.IsAssignableFrom(src))
        {
            return true;
        }
        return false;
    }

    internal static bool CanCache(this Type t)
    {
        var assembly = t.Assembly;
        if (assembly != _mscorlib && assembly != _systemCore)
        {
            return false;
        }
        if (t.IsGenericType)
        {
            var genericArguments = t.GetGenericArguments();
            for (var i = 0; i < genericArguments.Length; i++)
            {
                if (!genericArguments[i].CanCache())
                {
                    return false;
                }
            }
        }
        return true;
    }

    internal static MethodInfo FindConversionOperator(MethodInfo[] methods, Type typeFrom, Type typeTo, bool implicitOnly)
    {
        var methodInfoArray = methods;
        for (var i = 0; i < methodInfoArray.Length; i++)
        {
            var methodInfo = methodInfoArray[i];
            if ((!(methodInfo.Name != "op_Implicit") || !implicitOnly && !(methodInfo.Name != "op_Explicit")) && AreEquivalent(methodInfo.ReturnType, typeTo) && AreEquivalent(methodInfo.GetParameters()[0].ParameterType, typeFrom))
            {
                return methodInfo;
            }
        }
        return null;
    }

    internal static Type FindGenericType(Type definition, Type type)
    {
        while (type != null && type != typeof(object))
        {
            if (type.IsGenericType && AreEquivalent(type.GetGenericTypeDefinition(), definition))
            {
                return type;
            }
            if (definition.IsInterface)
            {
                var interfaces = type.GetInterfaces();
                for (var i = 0; i < interfaces.Length; i++)
                {
                    var type1 = FindGenericType(definition, interfaces[i]);
                    if (type1 != null)
                    {
                        return type1;
                    }
                }
            }
            type = type.BaseType;
        }
        return null;
    }

    internal static MethodInfo GetBooleanOperator(Type type, string name)
    {
        do
        {
            var methodValidated = type.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { type }, null);
            if (methodValidated != null && methodValidated.IsSpecialName && !methodValidated.ContainsGenericParameters)
            {
                return methodValidated;
            }
            type = type.BaseType;
        }
        while (type != null);
        return null;
    }

    internal static Type GetNonNullableType(this Type type)
    {
        if (!type.IsNullableType())
        {
            return type;
        }
        return type.GetGenericArguments()[0];
    }

    internal static Type GetNonRefType(this Type type)
    {
        if (!type.IsByRef)
        {
            return type;
        }
        return type.GetElementType();
    }

    internal static Type GetNullableType(Type type)
    {
        if (!type.IsValueType || type.IsNullableType())
        {
            return type;
        }
        return typeof(Nullable<>).MakeGenericType(new Type[] { type });
    }

    internal static MethodInfo GetUserDefinedCoercionMethod(Type convertFrom, Type convertToType, bool implicitOnly)
    {
        var nonNullableType = convertFrom.GetNonNullableType();
        var type = convertToType.GetNonNullableType();
        var methods = nonNullableType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        var methodInfo = FindConversionOperator(methods, convertFrom, convertToType, implicitOnly);
        if (methodInfo != null)
        {
            return methodInfo;
        }
        var methodInfoArray = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        methodInfo = FindConversionOperator(methodInfoArray, convertFrom, convertToType, implicitOnly);
        if (methodInfo != null)
        {
            return methodInfo;
        }
        if (!AreEquivalent(nonNullableType, convertFrom) || !AreEquivalent(type, convertToType))
        {
            methodInfo = FindConversionOperator(methods, nonNullableType, type, implicitOnly);
            if (methodInfo == null)
            {
                methodInfo = FindConversionOperator(methodInfoArray, nonNullableType, type, implicitOnly);
            }
            if (methodInfo != null)
            {
                return methodInfo;
            }
        }
        return null;
    }

    internal static bool HasBuiltInEqualityOperator(Type left, Type right)
    {
        if (left.IsInterface && !right.IsValueType)
        {
            return true;
        }
        if (right.IsInterface && !left.IsValueType)
        {
            return true;
        }
        if (!left.IsValueType && !right.IsValueType && (AreReferenceAssignable(left, right) || AreReferenceAssignable(right, left)))
        {
            return true;
        }
        if (!AreEquivalent(left, right))
        {
            return false;
        }
        var nonNullableType = left.GetNonNullableType();
        if (!(nonNullableType == typeof(bool)) && !IsNumeric(nonNullableType) && !nonNullableType.IsEnum)
        {
            return false;
        }
        return true;
    }

    internal static bool HasIdentityPrimitiveOrNullableConversion(Type source, Type dest)
    {
        if (AreEquivalent(source, dest))
        {
            return true;
        }
        if (source.IsNullableType() && AreEquivalent(dest, source.GetNonNullableType()))
        {
            return true;
        }
        if (dest.IsNullableType() && AreEquivalent(source, dest.GetNonNullableType()))
        {
            return true;
        }
        if (IsConvertible(source) && IsConvertible(dest) && dest.GetNonNullableType() != typeof(bool))
        {
            return true;
        }
        return false;
    }

    internal static bool HasReferenceConversion(Type source, Type dest)
    {
        if (source == typeof(void) || dest == typeof(void))
        {
            return false;
        }
        var nonNullableType = source.GetNonNullableType();
        var type = dest.GetNonNullableType();
        if (nonNullableType.IsAssignableFrom(type))
        {
            return true;
        }
        if (type.IsAssignableFrom(nonNullableType))
        {
            return true;
        }
        if (source.IsInterface || dest.IsInterface)
        {
            return true;
        }
        if (IsLegalExplicitVariantDelegateConversion(source, dest))
        {
            return true;
        }
        if (!(source == typeof(object)) && !(dest == typeof(object)))
        {
            return false;
        }
        return true;
    }

    internal static bool HasReferenceEquality(Type left, Type right)
    {
        if (left.IsValueType || right.IsValueType)
        {
            return false;
        }
        if (left.IsInterface || right.IsInterface || AreReferenceAssignable(left, right))
        {
            return true;
        }
        return AreReferenceAssignable(right, left);
    }

    internal static bool IsArithmetic(Type type)
    {
        type = type.GetNonNullableType();
        if (!type.IsEnum && (int)Type.GetTypeCode(type) - (int)TypeCode.Int16 <= (int)TypeCode.Int16)
        {
            return true;
        }
        return false;
    }

    internal static bool IsBool(Type type)
    {
        return type.GetNonNullableType() == typeof(bool);
    }

    private static bool IsContravariant(Type t)
    {
        return (t.GenericParameterAttributes & GenericParameterAttributes.Contravariant) != GenericParameterAttributes.None;
    }

    internal static bool IsConvertible(Type type)
    {
        type = type.GetNonNullableType();
        if (type.IsEnum)
        {
            return true;
        }
        if ((int)Type.GetTypeCode(type) - (int)TypeCode.Boolean <= (int)TypeCode.Int64)
        {
            return true;
        }
        return false;
    }

    private static bool IsCovariant(Type t)
    {
        return (t.GenericParameterAttributes & GenericParameterAttributes.Covariant) != GenericParameterAttributes.None;
    }

    private static bool IsDelegate(Type t)
    {
        return t.IsSubclassOf(typeof(MulticastDelegate));
    }

    internal static bool IsFloatingPoint(Type type)
    {
        type = type.GetNonNullableType();
        if ((int)Type.GetTypeCode(type) - (int)TypeCode.Single <= (int)TypeCode.Object)
        {
            return true;
        }
        return false;
    }

    private static bool IsImplicitBoxingConversion(Type source, Type destination)
    {
        if (source.IsValueType && (destination == typeof(object) || destination == typeof(ValueType)))
        {
            return true;
        }
        if (source.IsEnum && destination == typeof(Enum))
        {
            return true;
        }
        return false;
    }

    internal static bool IsImplicitlyConvertible(Type source, Type destination)
    {
        if (AreEquivalent(source, destination) || IsImplicitNumericConversion(source, destination) || IsImplicitReferenceConversion(source, destination) || IsImplicitBoxingConversion(source, destination))
        {
            return true;
        }
        return IsImplicitNullableConversion(source, destination);
    }

    private static bool IsImplicitNullableConversion(Type source, Type destination)
    {
        if (!destination.IsNullableType())
        {
            return false;
        }
        return IsImplicitlyConvertible(source.GetNonNullableType(), destination.GetNonNullableType());
    }

    private static bool IsImplicitNumericConversion(Type source, Type destination)
    {
        var typeCode = Type.GetTypeCode(source);
        var typeCode1 = Type.GetTypeCode(destination);
        switch (typeCode)
        {
            case TypeCode.Char:
            {
                if ((int)typeCode1 - (int)TypeCode.UInt16 <= (int)TypeCode.Int16)
                {
                    return true;
                }
                return false;
            }
            case TypeCode.SByte:
            {
                switch (typeCode1)
                {
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Decimal:
                    {
                        return true;
                    }
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                    {
                        return false;
                    }
                    default:
                    {
                        return false;
                    }
                }
                break;
            }
            case TypeCode.Byte:
            {
                if ((int)typeCode1 - (int)TypeCode.Int16 <= (int)TypeCode.UInt16)
                {
                    return true;
                }
                return false;
            }
            case TypeCode.Int16:
            {
                switch (typeCode1)
                {
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Decimal:
                    {
                        return true;
                    }
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                    {
                        return false;
                    }
                    default:
                    {
                        return false;
                    }
                }
                break;
            }
            case TypeCode.UInt16:
            {
                if ((int)typeCode1 - (int)TypeCode.Int32 <= (int)TypeCode.Byte)
                {
                    return true;
                }
                return false;
            }
            case TypeCode.Int32:
            {
                if (typeCode1 != TypeCode.Int64 && (int)typeCode1 - (int)TypeCode.Single > (int)TypeCode.DBNull)
                {
                    return false;
                }
                return true;
            }
            case TypeCode.UInt32:
            {
                if (typeCode1 != TypeCode.UInt32 && (int)typeCode1 - (int)TypeCode.UInt64 > (int)TypeCode.Boolean)
                {
                    return false;
                }
                return true;
            }
            case TypeCode.Int64:
            case TypeCode.UInt64:
            {
                if ((int)typeCode1 - (int)TypeCode.Single <= (int)TypeCode.DBNull)
                {
                    return true;
                }
                return false;
            }
            case TypeCode.Single:
            {
                return typeCode1 == TypeCode.Double;
            }
        }
        return false;
    }

    private static bool IsImplicitReferenceConversion(Type source, Type destination)
    {
        return destination.IsAssignableFrom(source);
    }

    internal static bool IsInteger(Type type)
    {
        type = type.GetNonNullableType();
        if (type.IsEnum)
        {
            return false;
        }
        if ((int)Type.GetTypeCode(type) - (int)TypeCode.SByte <= (int)TypeCode.Int16)
        {
            return true;
        }
        return false;
    }

    internal static bool IsIntegerOrBool(Type type)
    {
        type = type.GetNonNullableType();
        if (!type.IsEnum)
        {
            var typeCode = Type.GetTypeCode(type);
            if (typeCode == TypeCode.Boolean || (int)typeCode - (int)TypeCode.SByte <= (int)TypeCode.Int16)
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsInvariant(Type t)
    {
        return (t.GenericParameterAttributes & GenericParameterAttributes.VarianceMask) == GenericParameterAttributes.None;
    }

    internal static bool IsLegalExplicitVariantDelegateConversion(Type source, Type dest)
    {
        if (!IsDelegate(source) || !IsDelegate(dest) || !source.IsGenericType || !dest.IsGenericType)
        {
            return false;
        }
        var genericTypeDefinition = source.GetGenericTypeDefinition();
        if (dest.GetGenericTypeDefinition() != genericTypeDefinition)
        {
            return false;
        }
        var genericArguments = genericTypeDefinition.GetGenericArguments();
        var typeArray = source.GetGenericArguments();
        var genericArguments1 = dest.GetGenericArguments();
        for (var i = 0; i < genericArguments.Length; i++)
        {
            var type = typeArray[i];
            var type1 = genericArguments1[i];
            if (!AreEquivalent(type, type1))
            {
                var type2 = genericArguments[i];
                if (IsInvariant(type2))
                {
                    return false;
                }
                if (IsCovariant(type2))
                {
                    if (!HasReferenceConversion(type, type1))
                    {
                        return false;
                    }
                }
                else if (IsContravariant(type2) && (type.IsValueType || type1.IsValueType))
                {
                    return false;
                }
            }
        }
        return true;
    }

    internal static bool IsNullableType(this Type type)
    {
        if (!type.IsGenericType)
        {
            return false;
        }
        return type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    internal static bool IsNumeric(Type type)
    {
        type = type.GetNonNullableType();
        if (!type.IsEnum && (int)Type.GetTypeCode(type) - (int)TypeCode.Char <= (int)TypeCode.UInt32)
        {
            return true;
        }
        return false;
    }

    internal static bool IsSameOrSubclass(Type type, Type subType)
    {
        if (AreEquivalent(type, subType))
        {
            return true;
        }
        return subType.IsSubclassOf(type);
    }

    internal static bool IsUnsigned(Type type)
    {
        type = type.GetNonNullableType();
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Char:
            case TypeCode.Byte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            {
                return true;
            }
            case TypeCode.SByte:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            {
                return false;
            }
            default:
            {
                return false;
            }
        }
    }

    internal static bool IsUnsignedInt(Type type)
    {
        type = type.GetNonNullableType();
        if (!type.IsEnum)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                {
                    return true;
                }
            }
        }
        return false;
    }

    internal static bool IsValidInstanceType(MemberInfo member, Type instanceType)
    {
        var declaringType = member.DeclaringType;
        if (AreReferenceAssignable(declaringType, instanceType))
        {
            return true;
        }
        if (instanceType.IsValueType)
        {
            if (AreReferenceAssignable(declaringType, typeof(object)))
            {
                return true;
            }
            if (AreReferenceAssignable(declaringType, typeof(ValueType)))
            {
                return true;
            }
            if (instanceType.IsEnum && AreReferenceAssignable(declaringType, typeof(Enum)))
            {
                return true;
            }
            if (declaringType.IsInterface)
            {
                var interfaces = instanceType.GetInterfaces();
                for (var i = 0; i < interfaces.Length; i++)
                {
                    if (AreReferenceAssignable(declaringType, interfaces[i]))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    internal static void ValidateType(Type type)
    {
        if (type.IsGenericTypeDefinition)
        {
            throw Error.TypeIsGeneric(type);
        }
        if (type.ContainsGenericParameters)
        {
            throw Error.TypeContainsGenericParameters(type);
        }
    }
}