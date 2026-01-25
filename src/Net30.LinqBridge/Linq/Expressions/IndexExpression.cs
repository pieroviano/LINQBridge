#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;

namespace System.Linq.Expressions;

/// <summary>Represents indexing a property or array.</summary>
[DebuggerTypeProxy(typeof(IndexExpressionProxy))]
public sealed class IndexExpression : Expression, IArgumentProvider
{
    private IList<Expression> _arguments;

    internal IndexExpression(Expression instance, PropertyInfo indexer, IList<Expression> arguments)
    {
        var num = indexer == null ? 1 : 0;
        Object = instance;
        Indexer = indexer;
        _arguments = arguments;
    }

    /// <summary>Returns the node type of this <see cref="T:System.Linq.Expressions.Expression" />.</summary>
    /// <returns>The <see cref="T:System.Linq.Expressions.ExpressionType" /> that represents this expression.</returns>
    public override ExpressionType NodeType => ExpressionType.Index;

    /// <summary>
    ///     Gets the static type of the expression that this <see cref="T:System.Linq.Expressions.Expression" />
    ///     represents.
    /// </summary>
    /// <returns>
    ///     The <see cref="P:System.Linq.Expressions.IndexExpression.Type" /> that represents the static type of the
    ///     expression.
    /// </returns>
    public override Type Type => Indexer != null ? Indexer.PropertyType : Object.Type.GetElementType();

    /// <summary>An object to index.</summary>
    /// <returns>The <see cref="T:System.Linq.Expressions.Expression" /> representing the object to index.</returns>
    public Expression Object { get; }

    /// <summary>
    ///     Gets the <see cref="T:System.Reflection.PropertyInfo" /> for the property if the expression represents an
    ///     indexed property, returns null otherwise.
    /// </summary>
    /// <returns>
    ///     The <see cref="T:System.Reflection.PropertyInfo" /> for the property if the expression represents an indexed
    ///     property, otherwise null.
    /// </returns>
    public PropertyInfo Indexer { get; }

    /// <summary>Gets the arguments that will be used to index the property or array.</summary>
    /// <returns>The read-only collection containing the arguments that will be used to index the property or array.</returns>
    public ReadOnlyCollection<Expression> Arguments => ReturnReadOnly(ref _arguments);

    Expression IArgumentProvider.GetArgument(int index)
    {
        return _arguments[index];
    }

    int IArgumentProvider.ArgumentCount => _arguments.Count;

    /// <summary>
    ///     Creates a new expression that is like this one, but using the supplied children. If all of the children are
    ///     the same, it will return this expression.
    /// </summary>
    /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
    /// <param name="object">The <see cref="P:System.Linq.Expressions.IndexExpression.Object" /> property of the result.</param>
    /// <param name="arguments">The <see cref="P:System.Linq.Expressions.IndexExpression.Arguments" /> property of the result.</param>
    public IndexExpression Update(Expression @object, IEnumerable<Expression> arguments)
    {
        return @object == Object && arguments == Arguments ? this : MakeIndex(@object, Indexer, arguments);
    }

    protected internal override Expression Accept(ExpressionVisitor visitor)
    {
        return visitor.VisitIndex(this);
    }

    internal Expression Rewrite(Expression instance, Expression[] arguments)
    {
        return MakeIndex(instance, Indexer, (IList<Expression>)arguments ?? _arguments);
    }
}