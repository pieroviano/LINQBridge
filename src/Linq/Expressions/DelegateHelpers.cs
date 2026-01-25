using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions
{
    internal static class DelegateHelpers
    {
        private const MethodAttributes CtorAttributes = MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName;

        private const MethodImplAttributes ImplAttributes = MethodImplAttributes.CodeTypeMask;

        private const MethodAttributes InvokeAttributes = MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.VtableLayoutMask | MethodAttributes.NewSlot;

        private readonly static Type[] _DelegateCtorSignature;

        private static DelegateHelpers.TypeInfo _DelegateCache;

        private const int MaximumArity = 17;

        static DelegateHelpers()
        {
            DelegateHelpers._DelegateCtorSignature = new Type[] { typeof(object), typeof(IntPtr) };
            DelegateHelpers._DelegateCache = new DelegateHelpers.TypeInfo();
        }

        internal static Type GetActionType(Type[] types)
        {
            switch ((int)types.Length)
            {
                case 0:
                    {
                        return typeof(Action);
                    }
                case 1:
                    {
                        return typeof(Action<>).MakeGenericType(types);
                    }
                case 2:
                    {
                        return typeof(Action<,>).MakeGenericType(types);
                    }
                case 3:
                    {
                        return typeof(Action<,,>).MakeGenericType(types);
                    }
                case 4:
                    {
                        return typeof(Action<,,,>).MakeGenericType(types);
                    }
                case 5:
                    {
                        return typeof(Action<,,,,>).MakeGenericType(types);
                    }
                case 6:
                    {
                        return typeof(Action<,,,,,>).MakeGenericType(types);
                    }
                case 7:
                    {
                        return typeof(Action<,,,,,,>).MakeGenericType(types);
                    }
                case 8:
                    {
                        return typeof(Action<,,,,,,,>).MakeGenericType(types);
                    }
                case 9:
                    {
                        return typeof(Action<,,,,,,,,>).MakeGenericType(types);
                    }
                case 10:
                    {
                        return typeof(Action<,,,,,,,,,>).MakeGenericType(types);
                    }
                case 11:
                    {
                        return typeof(Action<,,,,,,,,,,>).MakeGenericType(types);
                    }
                case 12:
                    {
                        return typeof(Action<,,,,,,,,,,,>).MakeGenericType(types);
                    }
                case 13:
                    {
                        return typeof(Action<,,,,,,,,,,,,>).MakeGenericType(types);
                    }
                case 14:
                    {
                        return typeof(Action<,,,,,,,,,,,,,>).MakeGenericType(types);
                    }
                case 15:
                    {
                        return typeof(Action<,,,,,,,,,,,,,,>).MakeGenericType(types);
                    }
                case 16:
                    {
                        return typeof(Action<,,,,,,,,,,,,,,,>).MakeGenericType(types);
                    }
            }
            return null;
        }

        internal static Type GetFuncType(Type[] types)
        {
            switch ((int)types.Length)
            {
                case 1:
                    {
                        return typeof(Func<>).MakeGenericType(types);
                    }
                case 2:
                    {
                        return typeof(Func<,>).MakeGenericType(types);
                    }
                case 3:
                    {
                        return typeof(Func<,,>).MakeGenericType(types);
                    }
                case 4:
                    {
                        return typeof(Func<,,,>).MakeGenericType(types);
                    }
                case 5:
                    {
                        return typeof(Func<,,,,>).MakeGenericType(types);
                    }
                case 6:
                    {
                        return typeof(Func<,,,,,>).MakeGenericType(types);
                    }
                case 7:
                    {
                        return typeof(Func<,,,,,,>).MakeGenericType(types);
                    }
                case 8:
                    {
                        return typeof(Func<,,,,,,,>).MakeGenericType(types);
                    }
                case 9:
                    {
                        return typeof(Func<,,,,,,,,>).MakeGenericType(types);
                    }
                case 10:
                    {
                        return typeof(Func<,,,,,,,,,>).MakeGenericType(types);
                    }
                case 11:
                    {
                        return typeof(Func<,,,,,,,,,,>).MakeGenericType(types);
                    }
                case 12:
                    {
                        return typeof(Func<,,,,,,,,,,,>).MakeGenericType(types);
                    }
                case 13:
                    {
                        return typeof(Func<,,,,,,,,,,,,>).MakeGenericType(types);
                    }
                case 14:
                    {
                        return typeof(Func<,,,,,,,,,,,,,>).MakeGenericType(types);
                    }
                case 15:
                    {
                        return typeof(Func<,,,,,,,,,,,,,,>).MakeGenericType(types);
                    }
                case 16:
                    {
                        return typeof(Func<,,,,,,,,,,,,,,,>).MakeGenericType(types);
                    }
                case 17:
                    {
                        return typeof(Func<,,,,,,,,,,,,,,,,>).MakeGenericType(types);
                    }
            }
            return null;
        }

        internal static DelegateHelpers.TypeInfo GetNextTypeInfo(Type initialArg, DelegateHelpers.TypeInfo curTypeInfo)
        {
            DelegateHelpers.TypeInfo typeInfo;
            lock (DelegateHelpers._DelegateCache)
            {
                typeInfo = DelegateHelpers.NextTypeInfo(initialArg, curTypeInfo);
            }
            return typeInfo;
        }

        private static bool IsByRef(DynamicMetaObject mo)
        {
            ParameterExpression expression = mo.Expression as ParameterExpression;
            if (expression == null)
            {
                return false;
            }
            return expression.IsByRef;
        }

        internal static Type MakeCallSiteDelegate(ReadOnlyCollection<Expression> types, Type returnType)
        {
            Type delegateType;
            lock (DelegateHelpers._DelegateCache)
            {
                DelegateHelpers.TypeInfo typeInfo = DelegateHelpers._DelegateCache;
                typeInfo = DelegateHelpers.NextTypeInfo(typeof(CallSite), typeInfo);
                for (int i = 0; i < types.Count; i++)
                {
                    typeInfo = DelegateHelpers.NextTypeInfo(types[i].Type, typeInfo);
                }
                typeInfo = DelegateHelpers.NextTypeInfo(returnType, typeInfo);
                if (typeInfo.DelegateType == null)
                {
                    typeInfo.MakeDelegateType(returnType, types);
                }
                delegateType = typeInfo.DelegateType;
            }
            return delegateType;
        }

        internal static Type MakeDeferredSiteDelegate(DynamicMetaObject[] args, Type returnType)
        {
            Type delegateType;
            lock (DelegateHelpers._DelegateCache)
            {
                DelegateHelpers.TypeInfo typeInfo = DelegateHelpers._DelegateCache;
                typeInfo = DelegateHelpers.NextTypeInfo(typeof(CallSite), typeInfo);
                for (int i = 0; i < (int)args.Length; i++)
                {
                    DynamicMetaObject dynamicMetaObject = args[i];
                    Type type = dynamicMetaObject.Expression.Type;
                    if (DelegateHelpers.IsByRef(dynamicMetaObject))
                    {
                        type = type.MakeByRefType();
                    }
                    typeInfo = DelegateHelpers.NextTypeInfo(type, typeInfo);
                }
                typeInfo = DelegateHelpers.NextTypeInfo(returnType, typeInfo);
                if (typeInfo.DelegateType == null)
                {
                    Type[] typeArray = new Type[(int)args.Length + 2];
                    typeArray[0] = typeof(CallSite);
                    typeArray[(int)typeArray.Length - 1] = returnType;
                    for (int j = 0; j < (int)args.Length; j++)
                    {
                        DynamicMetaObject dynamicMetaObject1 = args[j];
                        Type type1 = dynamicMetaObject1.Expression.Type;
                        if (DelegateHelpers.IsByRef(dynamicMetaObject1))
                        {
                            type1 = type1.MakeByRefType();
                        }
                        typeArray[j + 1] = type1;
                    }
                    typeInfo.DelegateType = DelegateHelpers.MakeNewDelegate(typeArray);
                }
                delegateType = typeInfo.DelegateType;
            }
            return delegateType;
        }

        internal static Type MakeDelegateType(Type[] types)
        {
            Type delegateType;
            lock (DelegateHelpers._DelegateCache)
            {
                DelegateHelpers.TypeInfo typeInfo = DelegateHelpers._DelegateCache;
                for (int i = 0; i < (int)types.Length; i++)
                {
                    typeInfo = DelegateHelpers.NextTypeInfo(types[i], typeInfo);
                }
                if (typeInfo.DelegateType == null)
                {
                    typeInfo.DelegateType = DelegateHelpers.MakeNewDelegate((Type[])types.Clone());
                }
                delegateType = typeInfo.DelegateType;
            }
            return delegateType;
        }

        private static Type MakeNewCustomDelegate(Type[] types)
        {
            Type type = types[(int)types.Length - 1];
            Type[] typeArray = types.RemoveLast<Type>();
            int length = (int)types.Length;
            TypeBuilder typeBuilder = AssemblyGen.DefineDelegateType(string.Concat("Delegate", length.ToString()));
            typeBuilder.DefineConstructor(MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName, CallingConventions.Standard, DelegateHelpers._DelegateCtorSignature).SetImplementationFlags(MethodImplAttributes.CodeTypeMask);
            typeBuilder.DefineMethod("Invoke", MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.VtableLayoutMask | MethodAttributes.NewSlot, type, typeArray).SetImplementationFlags(MethodImplAttributes.CodeTypeMask);
            return typeBuilder.CreateType();
        }

        private static Type MakeNewDelegate(Type[] types)
        {
            Type type;
            if ((int)types.Length <= 17)
            {
                if (!((IEnumerable<Type>)types).Any<Type>((Type t) => t.IsByRef))
                {
                    type = (types[(int)types.Length - 1] != typeof(void) ? DelegateHelpers.GetFuncType(types) : DelegateHelpers.GetActionType(types.RemoveLast<Type>()));
                    return type;
                }
            }
            return DelegateHelpers.MakeNewCustomDelegate(types);
        }

        internal static DelegateHelpers.TypeInfo NextTypeInfo(Type initialArg)
        {
            DelegateHelpers.TypeInfo typeInfo;
            lock (DelegateHelpers._DelegateCache)
            {
                typeInfo = DelegateHelpers.NextTypeInfo(initialArg, DelegateHelpers._DelegateCache);
            }
            return typeInfo;
        }

        private static DelegateHelpers.TypeInfo NextTypeInfo(Type initialArg, DelegateHelpers.TypeInfo curTypeInfo)
        {
            DelegateHelpers.TypeInfo typeInfo;
            Type type = initialArg;
            if (curTypeInfo.TypeChain == null)
            {
                curTypeInfo.TypeChain = new Dictionary<Type, DelegateHelpers.TypeInfo>();
            }
            if (!curTypeInfo.TypeChain.TryGetValue(type, out typeInfo))
            {
                typeInfo = new DelegateHelpers.TypeInfo();
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

            public Dictionary<Type, DelegateHelpers.TypeInfo> TypeChain;

            public TypeInfo()
            {
            }

            public Type MakeDelegateType(Type retType, params Expression[] args)
            {
                return this.MakeDelegateType(retType, (IList<Expression>)args);
            }

            public Type MakeDelegateType(Type retType, IList<Expression> args)
            {
                Type[] type = new Type[args.Count + 2];
                type[0] = typeof(CallSite);
                type[(int)type.Length - 1] = retType;
                for (int i = 0; i < args.Count; i++)
                {
                    type[i + 1] = args[i].Type;
                }
                Type type1 = DelegateHelpers.MakeNewDelegate(type);
                Type type2 = type1;
                this.DelegateType = type1;
                return type2;
            }
        }
    }
}