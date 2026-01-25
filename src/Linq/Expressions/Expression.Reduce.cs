namespace System.Linq.Expressions;

public abstract partial class Expression
{
    /// <summary>Reduces this node to a simpler expression. If CanReduce returns true, this should return a valid expression. This method can return another node which itself must be reduced.</summary>
    /// <returns>The reduced expression.</returns>
    [__DynamicallyInvokable]
    public Expression ReduceAndCheck()
    {
        if (!this.CanReduce)
        {
            throw Error.MustBeReducible();
        }

        Expression expression = this.Reduce();
        if (expression == null || expression == this)
        {
            throw Error.MustReduceToDifferent();
        }

        if (!TypeUtils.AreReferenceAssignable(this.Type, expression.Type))
        {
            throw Error.ReducedNotCompatible();
        }

        return expression;
    }

    /// <summary>Reduces this node to a simpler expression. If CanReduce returns true, this should return a valid expression. This method can return another node which itself must be reduced.</summary>
    /// <returns>The reduced expression.</returns>
    [__DynamicallyInvokable]
    public virtual Expression Reduce()
    {
        if (this.CanReduce)
        {
            throw Error.ReducibleMustOverrideReduce();
        }

        return this;
    }

    /// <summary>Indicates that the node can be reduced to a simpler node. If this returns true, Reduce() can be called to produce the reduced form.</summary>
    /// <returns>True if the node can be reduced, otherwise false.</returns>
    [__DynamicallyInvokable]
    public virtual bool CanReduce
    {
        [__DynamicallyInvokable] get { return false; }
    }
}