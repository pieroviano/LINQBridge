// Implementations for Dynamic / MakeDynamic helpers.
// These are intended to match the behavior of System.Linq.Expressions.Expression.Dynamic:
//  - validate inputs
//  - build delegate type when only returnType is supplied
//  - produce a ReadOnlyCollection<Expression> for arguments
//  - dispatch to DynamicExpression factory
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

public partial class Expression
{
    private static ReadOnlyCollection<Expression> ValidateArgumentsEnumerable(IEnumerable<Expression> arguments)
    {
        if (arguments == null)
        {
            return new ReadOnlyCollection<Expression>(new List<Expression>());
        }

        var list = arguments as IList<Expression> ?? arguments.ToList();

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == null)
            {
                throw new ArgumentNullException($"arguments[{i}]");
            }
        }

        return new ReadOnlyCollection<Expression>(list);
    }

    /// <summary>
    /// Creates a dynamic expression. Builds delegate type automatically:
    /// delegate parameters = (CallSite, [object for each argument]) -> returnType
    /// </summary>
    public static DynamicExpression MakeDynamic(CallSiteBinder binder, Type returnType, params Expression[] arguments)
    {
        return MakeDynamic(binder, returnType, (IEnumerable<Expression>)arguments);
    }

    /// <summary>
    /// Creates a dynamic expression. Builds delegate type automatically:
    /// delegate parameters = (CallSite, [object for each argument]) -> returnType
    /// </summary>
    public static DynamicExpression MakeDynamic(CallSiteBinder binder, Type returnType, IEnumerable<Expression> arguments)
    {
        if (binder == null) throw new ArgumentNullException(nameof(binder));
        if (returnType == null) throw new ArgumentNullException(nameof(returnType));

        var argsRO = ValidateArgumentsEnumerable(arguments);

        // Build delegate type: (CallSite, object, object, ...) -> returnType
        var typeArgs = new Type[argsRO.Count + 2];
        typeArgs[0] = typeof(CallSite);
        for (int i = 0; i < argsRO.Count; i++)
        {
            typeArgs[i + 1] = typeof(object);
        }
        typeArgs[typeArgs.Length - 1] = returnType;

        var delegateType = GetDelegateType(typeArgs);
        return DynamicExpression.Make(returnType, delegateType, binder, argsRO);
    }

    /// <summary>
    /// Creates a dynamic expression using a specific delegate type.
    /// </summary>
    public static DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, params Expression[] arguments)
    {
        return MakeDynamic(delegateType, binder, (IEnumerable<Expression>)arguments);
    }

    /// <summary>
    /// Creates a dynamic expression using a specific delegate type.
    /// </summary>
    public static DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, IEnumerable<Expression> arguments)
    {
        if (delegateType == null) throw new ArgumentNullException(nameof(delegateType));
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        // Basic delegate type check
        if (!typeof(Delegate).IsAssignableFrom(delegateType))
        {
            throw new ArgumentException("delegateType must be a delegate type", nameof(delegateType));
        }

        var argsRO = ValidateArgumentsEnumerable(arguments);

        // Determine the delegate's Invoke signature
        var invoke = delegateType.GetMethod("Invoke");
        if (invoke == null)
        {
            throw new ArgumentException("delegateType must have an Invoke method", nameof(delegateType));
        }

        var parameters = invoke.GetParameters();
        // Expecting delegate to be called with CallSite + N arguments where N == argsRO.Count
        if (parameters.Length != argsRO.Count + 1)
        {
            throw new ArgumentException("The number of delegate parameters does not match the number of arguments plus the CallSite parameter.", nameof(delegateType));
        }

        // Optional: could validate parameter types (first is CallSite, others are assignable from object),
        // but to keep compatible with many call-site delegate shapes we avoid strict checks here.

        var returnType = invoke.ReturnType;
        return DynamicExpression.Make(returnType, delegateType, binder, argsRO);
    }

    //
    // Convenience wrappers named `Dynamic` to mirror System.Linq.Expressions API
    // (these simply forward to the MakeDynamic overloads above).
    //
    public static DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, params Expression[] arguments)
    {
        return MakeDynamic(binder, returnType, arguments);
    }

    public static DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, IEnumerable<Expression> arguments)
    {
        return MakeDynamic(binder, returnType, arguments);
    }

    public static DynamicExpression Dynamic(Type delegateType, CallSiteBinder binder, params Expression[] arguments)
    {
        return MakeDynamic(delegateType, binder, arguments);
    }

    public static DynamicExpression Dynamic(Type delegateType, CallSiteBinder binder, IEnumerable<Expression> arguments)
    {
        return MakeDynamic(delegateType, binder, arguments);
    }
}