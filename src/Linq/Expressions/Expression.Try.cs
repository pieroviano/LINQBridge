namespace System.Linq.Expressions;

public abstract partial class Expression
{
    /// <summary>Creates a Try expression that represents a try/finally construct.</summary>
    public static Expression TryFinally(Expression tryBody, Expression finallyBody)
    {
        if (tryBody == null)
            throw Error.ArgumentNull(nameof(tryBody));
        if (finallyBody == null)
            throw Error.ArgumentNull(nameof(finallyBody));

        // finally body must be void
        if (finallyBody.Type != typeof(void))
            throw new ArgumentException("The finally block must be of type void.", nameof(finallyBody));

        ValidateType(tryBody.Type);
        return new TryExpression(tryBody, finallyBody);
    }
}