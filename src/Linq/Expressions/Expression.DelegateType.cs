using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace System.Linq.Expressions;

public abstract partial class Expression
{
    // Adds support for delegate types (Func/Action and custom delegates with by-ref params).
    private static readonly object s_delegateTypesLock = new object();
    private static readonly Dictionary<string, Type> s_delegateTypesCache = new Dictionary<string, Type>(StringComparer.Ordinal);

    /// <summary>
    /// Returns a delegate Type for the given signature. The last element is the return type.
    /// Emits runtime delegate types to support by-ref parameter types.
    /// </summary>
    public static Type GetDelegateType(params Type[] typeArgs)
    {
        if (typeArgs == null)
            throw Error.ArgumentNull(nameof(typeArgs));
        if (typeArgs.Length == 0)
            throw Error.IncorrectNumberOfTypeArgsForFunc();

        for (var i = 0; i < typeArgs.Length; i++)
        {
            var t = typeArgs[i];
            if (t == null)
                throw Error.ArgumentNull(nameof(typeArgs));
            // Validate element type for by-ref
            ValidateType(t.IsByRef ? t.GetElementType() : t);
        }

        var returnType = typeArgs[typeArgs.Length - 1];
        var paramCount = typeArgs.Length - 1;
        var paramTypes = new Type[paramCount];
        if (paramCount > 0) Array.Copy(typeArgs, 0, paramTypes, 0, paramCount);

        // If any parameter is by-ref, emit a custom delegate type supporting by-ref parameters.
        var needsByRefDelegate = false;
        for (var i = 0; i < paramTypes.Length; i++)
        {
            if (paramTypes[i].IsByRef)
            {
                needsByRefDelegate = true;
                break;
            }
        }

        if (needsByRefDelegate)
        {
            var keyBuilder = new System.Text.StringBuilder();
            for (var i = 0; i < paramTypes.Length; i++)
            {
                var t = paramTypes[i];
                keyBuilder.Append(t.AssemblyQualifiedName).Append(';');
            }
            keyBuilder.Append("->").Append(returnType.AssemblyQualifiedName);
            var key = keyBuilder.ToString();

            lock (s_delegateTypesLock)
            {
                if (s_delegateTypesCache.TryGetValue(key, out var cached))
                    return cached;
                var created = CreateDelegateTypeWithByRef(paramTypes, returnType, key);
                s_delegateTypesCache[key] = created;
                return created;
            }
        }

        // No by-ref parameters -> use built-in Func/Action from mscorlib/System.Core
        if (returnType == typeof(void))
        {
            if (paramTypes.Length == 0)
                return typeof(Action);
            var actionTypeName = "System.Action`" + paramTypes.Length;
            var actionTypeDef = typeof(Action).Assembly.GetType(actionTypeName, throwOnError: false, ignoreCase: false);
            if (actionTypeDef == null)
                throw new NotSupportedException(Strings.IncorrectNumberOfTypeArgsForAction);
            return actionTypeDef.MakeGenericType(paramTypes);
        }
        else
        {
            var arity = paramTypes.Length + 1;
            var funcTypeName = "System.Func`" + arity;
            var funcTypeDef = typeof(Func<>).Assembly.GetType(funcTypeName, throwOnError: false, ignoreCase: false);
            if (funcTypeDef == null)
                throw new NotSupportedException(Strings.IncorrectNumberOfTypeArgsForFunc);
            var genArgs = new Type[arity];
            if (paramTypes.Length > 0) Array.Copy(paramTypes, genArgs, paramTypes.Length);
            genArgs[genArgs.Length - 1] = returnType;
            return funcTypeDef.MakeGenericType(genArgs);
        }
    }

}