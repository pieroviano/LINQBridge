#nullable disable
using System.Linq.Expressions;

namespace System.Runtime.CompilerServices;

/// <summary>Represents the runtime state of a dynamically generated method.</summary>
/// <filterpriority>2</filterpriority>
[Obsolete("do not use this type", true)]
public class ExecutionScope
{
    /// <summary>
    ///     Represents the non-trivial constants and locally executable expressions that are referenced by a dynamically
    ///     generated method.
    /// </summary>
    public object[] Globals;

    /// <summary>Represents the hoisted local variables from the parent context.</summary>
    public object[] Locals;

    /// <summary>Represents the execution scope of the calling delegate.</summary>
    public ExecutionScope Parent;

    internal ExecutionScope()
    {
        Parent = null;
        Globals = null;
        Locals = null;
    }

    /// <summary>Creates a delegate that can be used to execute a dynamically generated method.</summary>
    /// <returns>A <see cref="T:System.Delegate" /> that can execute a dynamically generated method.</returns>
    /// <param name="indexLambda">
    ///     The index of the object that stores information about associated lambda expression of the
    ///     dynamic method.
    /// </param>
    /// <param name="locals">An array that contains the hoisted local variables from the parent context.</param>
    public Delegate CreateDelegate(int indexLambda, object[] locals)
    {
        throw new NotSupportedException();
    }

    /// <summary>Creates an array to store the hoisted local variables.</summary>
    /// <returns>An array to store hoisted local variables.</returns>
    public object[] CreateHoistedLocals()
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     Frees a specified expression tree of external parameter references by replacing the parameter with its current
    ///     value.
    /// </summary>
    /// <returns>An expression tree that does not contain external parameter references.</returns>
    /// <param name="expression">An expression tree to free of external parameter references.</param>
    /// <param name="locals">An array that contains the hoisted local variables.</param>
    public Expression IsolateExpression(Expression expression, object[] locals)
    {
        throw new NotSupportedException();
    }
}