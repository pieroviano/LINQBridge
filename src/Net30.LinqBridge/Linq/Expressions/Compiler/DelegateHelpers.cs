#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Dynamic.Utils;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Compiler;

internal static class DelegateHelpers
{
    private const MethodAttributes CtorAttributes =
        MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName;

    private const MethodImplAttributes ImplAttributes = MethodImplAttributes.CodeTypeMask;

    private const MethodAttributes InvokeAttributes = MethodAttributes.Public | MethodAttributes.Virtual |
                                                      MethodAttributes.HideBySig | MethodAttributes.VtableLayoutMask;

    private const int MaximumArity = 17;

    private static readonly Type[] _DelegateCtorSignature = new Type[2]
    {
        typeof(object),
        typeof(IntPtr)
    };

    private static readonly TypeInfo _DelegateCache = new();

    internal static Type GetActionType(Type[] types)
    {
        switch (types.Length)
        {
            case 0:
                return typeof(Action);
            case 1:
                return typeof(Action<>).MakeGenericType(types);
            case 2:
                return typeof(Action<,>).MakeGenericType(types);
            case 3:
                return typeof(Action<,,>).MakeGenericType(types);
            case 4:
                return typeof(Action<,,,>).MakeGenericType(types);
            case 5:
                return typeof(Action<,,,,>).MakeGenericType(types);
            case 6:
                return typeof(Action<,,,,,>).MakeGenericType(types);
            case 7:
                return typeof(Action<,,,,,,>).MakeGenericType(types);
            case 8:
                return typeof(Action<,,,,,,,>).MakeGenericType(types);
            case 9:
                return typeof(Action<,,,,,,,,>).MakeGenericType(types);
            case 10:
                return typeof(Action<,,,,,,,,,>).MakeGenericType(types);
            case 11:
                return typeof(Action<,,,,,,,,,,>).MakeGenericType(types);
            case 12:
                return typeof(Action<,,,,,,,,,,,>).MakeGenericType(types);
            case 13:
                return typeof(Action<,,,,,,,,,,,,>).MakeGenericType(types);
            case 14:
                return typeof(Action<,,,,,,,,,,,,,>).MakeGenericType(types);
            case 15:
                return typeof(Action<,,,,,,,,,,,,,,>).MakeGenericType(types);
            case 16 /*0x10*/:
                return typeof(Action<,,,,,,,,,,,,,,,>).MakeGenericType(types);
            default:
                return null;
        }
    }

    internal static Type GetFuncType(Type[] types)
    {
        switch (types.Length)
        {
            case 1:
                return typeof(Func<>).MakeGenericType(types);
            case 2:
                return typeof(Func<,>).MakeGenericType(types);
            case 3:
                return typeof(Func<,,>).MakeGenericType(types);
            case 4:
                return typeof(Func<,,,>).MakeGenericType(types);
            case 5:
                return typeof(Func<,,,,>).MakeGenericType(types);
            case 6:
                return typeof(Func<,,,,,>).MakeGenericType(types);
            case 7:
                return typeof(Func<,,,,,,>).MakeGenericType(types);
            case 8:
                return typeof(Func<,,,,,,,>).MakeGenericType(types);
            case 9:
                return typeof(Func<,,,,,,,,>).MakeGenericType(types);
            case 10:
                return typeof(Func<,,,,,,,,,>).MakeGenericType(types);
            case 11:
                return typeof(Func<,,,,,,,,,,>).MakeGenericType(types);
            case 12:
                return typeof(Func<,,,,,,,,,,,>).MakeGenericType(types);
            case 13:
                return typeof(Func<,,,,,,,,,,,,>).MakeGenericType(types);
            case 14:
                return typeof(Func<,,,,,,,,,,,,,>).MakeGenericType(types);
            case 15:
                return typeof(Func<,,,,,,,,,,,,,,>).MakeGenericType(types);
            case 16 /*0x10*/:
                return typeof(Func<,,,,,,,,,,,,,,,>).MakeGenericType(types);
            case 17:
                return typeof(Func<,,,,,,,,,,,,,,,,>).MakeGenericType(types);
            default:
                return null;
        }
    }

    internal static TypeInfo GetNextTypeInfo(
        Type initialArg,
        TypeInfo curTypeInfo)
    {
        lock (_DelegateCache)
        {
            return NextTypeInfo(initialArg, curTypeInfo);
        }
    }

    internal static Type MakeCallSiteDelegate(ReadOnlyCollection<Expression> types, Type returnType)
    {
        lock (_DelegateCache)
        {
            var curTypeInfo = NextTypeInfo(typeof(CallSite), _DelegateCache);
            for (var index = 0; index < types.Count; ++index)
            {
                curTypeInfo = NextTypeInfo(types[index].Type, curTypeInfo);
            }

            var typeInfo = NextTypeInfo(returnType, curTypeInfo);
            if (typeInfo.DelegateType == null)
            {
                typeInfo.MakeDelegateType(returnType, types);
            }

            return typeInfo.DelegateType;
        }
    }

    internal static Type MakeDeferredSiteDelegate(DynamicMetaObject[] args, Type returnType)
    {
        lock (_DelegateCache)
        {
            var curTypeInfo = NextTypeInfo(typeof(CallSite), _DelegateCache);
            for (var index = 0; index < args.Length; ++index)
            {
                var mo = args[index];
                var initialArg = mo.Expression.Type;
                if (IsByRef(mo))
                {
                    initialArg = initialArg.MakeByRefType();
                }

                curTypeInfo = NextTypeInfo(initialArg, curTypeInfo);
            }

            var typeInfo = NextTypeInfo(returnType, curTypeInfo);
            if (typeInfo.DelegateType == null)
            {
                var types = new Type[args.Length + 2];
                types[0] = typeof(CallSite);
                types[types.Length - 1] = returnType;
                for (var index = 0; index < args.Length; ++index)
                {
                    var mo = args[index];
                    var type = mo.Expression.Type;
                    if (IsByRef(mo))
                    {
                        type = type.MakeByRefType();
                    }

                    types[index + 1] = type;
                }

                typeInfo.DelegateType = MakeNewDelegate(types);
            }

            return typeInfo.DelegateType;
        }
    }

    internal static Type MakeDelegateType(Type[] types)
    {
        lock (_DelegateCache)
        {
            var curTypeInfo = _DelegateCache;
            for (var index = 0; index < types.Length; ++index)
            {
                curTypeInfo = NextTypeInfo(types[index], curTypeInfo);
            }

            if (curTypeInfo.DelegateType == null)
            {
                curTypeInfo.DelegateType = MakeNewDelegate((Type[])types.Clone());
            }

            return curTypeInfo.DelegateType;
        }
    }

    internal static TypeInfo NextTypeInfo(Type initialArg)
    {
        lock (_DelegateCache)
        {
            return NextTypeInfo(initialArg, _DelegateCache);
        }
    }

    private static bool IsByRef(DynamicMetaObject mo)
    {
        return mo.Expression is ParameterExpression expression && expression.IsByRef;
    }

    private static Type MakeNewCustomDelegate(Type[] types)
    {
        var type = types[types.Length - 1];
        var parameterTypes = types.RemoveLast();
        var typeBuilder = AssemblyGen.DefineDelegateType("Delegate" + types.Length);
        typeBuilder
            .DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName,
                CallingConventions.Standard, _DelegateCtorSignature)
            .SetImplementationFlags(MethodImplAttributes.CodeTypeMask);
        typeBuilder.DefineMethod("Invoke",
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig |
                MethodAttributes.VtableLayoutMask, type, parameterTypes)
            .SetImplementationFlags(MethodImplAttributes.CodeTypeMask);
        return typeBuilder.CreateType();
    }

    private static Type MakeNewDelegate(Type[] types)
    {
        return types.Length > 17 || types.Any(t => t.IsByRef) ? MakeNewCustomDelegate(types) :
            !(types[types.Length - 1] == typeof(void)) ? GetFuncType(types) : GetActionType(types.RemoveLast());
    }

    private static TypeInfo NextTypeInfo(
        Type initialArg,
        TypeInfo curTypeInfo)
    {
        var type = initialArg;
        if (curTypeInfo.TypeChain == null)
        {
            curTypeInfo.TypeChain = new Dictionary<Type, TypeInfo>();
        }

        TypeInfo typeInfo;
        if (!curTypeInfo.TypeChain.TryGetValue(type, out typeInfo))
        {
            typeInfo = new TypeInfo();
            if (type.CanCache())
            {
                curTypeInfo.TypeChain[type] = typeInfo;
            }
        }

        return typeInfo;
    }

    internal class TypeInfo
    {
        public Type DelegateType;
        public Dictionary<Type, TypeInfo> TypeChain;

        public Type MakeDelegateType(Type retType, params Expression[] args)
        {
            return MakeDelegateType(retType, (IList<Expression>)args);
        }

        public Type MakeDelegateType(Type retType, IList<Expression> args)
        {
            var types = new Type[args.Count + 2];
            types[0] = typeof(CallSite);
            types[types.Length - 1] = retType;
            for (var index = 0; index < args.Count; ++index)
            {
                types[index + 1] = args[index].Type;
            }

            return DelegateType = MakeNewDelegate(types);
        }
    }
}