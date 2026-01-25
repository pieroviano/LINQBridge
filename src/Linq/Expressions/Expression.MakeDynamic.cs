using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Linq.Expressions;

public abstract partial class Expression
{
    /// <summary>
    /// Creates an expression node that represents a dynamic operation.
    /// Builds a delegate type automatically when delegateType is not supplied:
    /// delegate parameters = (CallSite, [object for each argument]) -> returnType
    /// </summary>
    public static Expression MakeDynamic(CallSiteBinder binder, Type returnType, params Expression[] arguments)
    {
        return MakeDynamic(binder, returnType, (IEnumerable<Expression>)arguments);
    }

    public static Expression MakeDynamic(CallSiteBinder binder, Type returnType, IEnumerable<Expression> arguments)
    {
        if (binder == null) throw Error.ArgumentNull(nameof(binder));
        if (returnType == null) throw Error.ArgumentNull(nameof(returnType));

        var argsList = (arguments == null) ? new Expression[0] : arguments as Expression[] ?? arguments.ToArray();

        // Build delegate type: first parameter is CallSite, then one object parameter per expression argument,
        // last element is the return type.
        var paramCount = argsList.Length;
        var delegateTypeArgs = new Type[paramCount + 2]; // CallSite + args... + return
        delegateTypeArgs[0] = typeof(CallSite);
        for (var i = 0; i < paramCount; i++)
        {
            delegateTypeArgs[i + 1] = typeof(object);
        }
        delegateTypeArgs[delegateTypeArgs.Length - 1] = returnType;

        var delegateType = GetDelegateType(delegateTypeArgs);
        var ro = new ReadOnlyCollection<Expression>(argsList.ToList());
        return DynamicExpression.Make(returnType, delegateType, binder, ro);
    }

    /// <summary>
    /// Creates a dynamic expression using a specific delegate type.
    /// </summary>
    public static Expression MakeDynamic(Type delegateType, CallSiteBinder binder, params Expression[] arguments)
    {
        return MakeDynamic(delegateType, binder, (IEnumerable<Expression>)arguments);
    }

    public static Expression MakeDynamic(Type delegateType, CallSiteBinder binder, IEnumerable<Expression> arguments)
    {
        if (delegateType == null) throw Error.ArgumentNull(nameof(delegateType));
        if (binder == null) throw Error.ArgumentNull(nameof(binder));

        // Validate delegateType is a delegate
        if (!typeof(MulticastDelegate).IsAssignableFrom(delegateType))
        {
            throw Error.TypeParameterIsNotDelegate(delegateType);
        }

        var invoke = delegateType.GetMethod("Invoke");
        if (invoke == null)
        {
            throw Error.LambdaTypeMustBeDerivedFromSystemDelegate();
        }

        var returnType = invoke.ReturnType;
        var argsList = (arguments == null) ? new Expression[0] : arguments as Expression[] ?? arguments.ToArray();
        var ro = new ReadOnlyCollection<Expression>(argsList.ToList());
        return DynamicExpression.Make(returnType, delegateType, binder, ro);
    }

}