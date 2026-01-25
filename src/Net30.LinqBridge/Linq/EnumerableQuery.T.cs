#nullable disable
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace System.Linq;

/// <summary>
///     Represents an <see cref="T:System.Collections.Generic.IEnumerable`1" /> collection as an
///     <see cref="T:System.Linq.IQueryable`1" /> data source.
/// </summary>
/// <typeparam name="T">The type of the data in the collection.</typeparam>
public class EnumerableQuery<T> :
    EnumerableQuery,
    IOrderedQueryable<T>,
    IQueryable<T>,
    IEnumerable<T>,
    IEnumerable,
    IQueryable,
    IOrderedQueryable,
    IQueryProvider
{
    private readonly Expression expression;
    private IEnumerable<T> enumerable;

    /// <summary>
    ///     Initializes a new instance of the <see cref="T:System.Linq.EnumerableQuery`1" /> class and associates it with
    ///     an <see cref="T:System.Collections.Generic.IEnumerable`1" /> collection.
    /// </summary>
    /// <param name="enumerable">A collection to associate with the new instance.</param>
    public EnumerableQuery(IEnumerable<T> enumerable)
    {
        this.enumerable = enumerable;
        expression = Expression.Constant(this);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="T:System.Linq.EnumerableQuery`1" /> class and associates the
    ///     instance with an expression tree.
    /// </summary>
    /// <param name="expression">An expression tree to associate with the new instance.</param>
    public EnumerableQuery(Expression expression)
    {
        this.expression = expression;
    }

    internal override Expression Expression => expression;

    internal override IEnumerable Enumerable => enumerable;

    /// <summary>Gets the query provider that is associated with this instance.</summary>
    /// <returns>The query provider that is associated with this instance.</returns>
    IQueryProvider IQueryable.Provider => this;

    /// <summary>Gets the expression tree that is associated with or that represents this instance.</summary>
    /// <returns>The expression tree that is associated with or that represents this instance.</returns>
    Expression IQueryable.Expression => expression;

    /// <summary>Gets the type of the data in the collection that this instance represents.</summary>
    /// <returns>The type of the data in the collection that this instance represents.</returns>
    Type IQueryable.ElementType => typeof(T);

    /// <summary>
    ///     Returns an enumerator that can iterate through the associated
    ///     <see cref="T:System.Collections.Generic.IEnumerable`1" /> collection, or, if it is null, through the collection
    ///     that results from rewriting the associated expression tree as a query on an
    ///     <see cref="T:System.Collections.Generic.IEnumerable`1" /> data source and executing it.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the associated data source.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    ///     Returns an enumerator that can iterate through the associated
    ///     <see cref="T:System.Collections.Generic.IEnumerable`1" /> collection, or, if it is null, through the collection
    ///     that results from rewriting the associated expression tree as a query on an
    ///     <see cref="T:System.Collections.Generic.IEnumerable`1" /> data source and executing it.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the associated data source.</returns>
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    ///     Constructs a new <see cref="T:System.Linq.EnumerableQuery`1" /> object and associates it with a specified
    ///     expression tree that represents an <see cref="T:System.Linq.IQueryable" /> collection of data.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Linq.EnumerableQuery`1" /> object that is associated with
    ///     <paramref name="expression" />.
    /// </returns>
    /// <param name="expression">
    ///     An expression tree that represents an <see cref="T:System.Linq.IQueryable" /> collection of
    ///     data.
    /// </param>
    IQueryable IQueryProvider.CreateQuery(Expression expression)
    {
        var type = expression != null
            ? TypeHelper.FindGenericType(typeof(IQueryable<>), expression.Type)
            : throw Error.ArgumentNull(nameof(expression));
        return !(type == null)
            ? Create(type.GetGenericArguments()[0], expression)
            : throw Error.ArgumentNotValid(nameof(expression));
    }

    /// <summary>
    ///     Constructs a new <see cref="T:System.Linq.EnumerableQuery`1" /> object and associates it with a specified
    ///     expression tree that represents an <see cref="T:System.Linq.IQueryable`1" /> collection of data.
    /// </summary>
    /// <returns>An EnumerableQuery object that is associated with <paramref name="expression" />.</returns>
    /// <param name="expression">An expression tree to execute.</param>
    /// <typeparam name="S">The type of the data in the collection that <paramref name="expression" /> represents.</typeparam>
    IQueryable<S> IQueryProvider.CreateQuery<S>(Expression expression)
    {
        if (expression == null)
        {
            throw Error.ArgumentNull(nameof(expression));
        }

        return typeof(IQueryable<S>).IsAssignableFrom(expression.Type)
            ? (IQueryable<S>)new EnumerableQuery<S>(expression)
            : throw Error.ArgumentNotValid(nameof(expression));
    }

    /// <summary>
    ///     Executes an expression after rewriting it to call <see cref="T:System.Linq.Enumerable" /> methods instead of
    ///     <see cref="T:System.Linq.Queryable" /> methods on any enumerable data sources that cannot be queried by
    ///     <see cref="T:System.Linq.Queryable" /> methods.
    /// </summary>
    /// <returns>The value that results from executing <paramref name="expression" />.</returns>
    /// <param name="expression">An expression tree to execute.</param>
    object IQueryProvider.Execute(Expression expression)
    {
        if (expression == null)
        {
            throw Error.ArgumentNull(nameof(expression));
        }

        typeof(EnumerableExecutor<>).MakeGenericType(expression.Type);
        return EnumerableExecutor.Create(expression).ExecuteBoxed();
    }

    /// <summary>
    ///     Executes an expression after rewriting it to call <see cref="T:System.Linq.Enumerable" /> methods instead of
    ///     <see cref="T:System.Linq.Queryable" /> methods on any enumerable data sources that cannot be queried by
    ///     <see cref="T:System.Linq.Queryable" /> methods.
    /// </summary>
    /// <returns>The value that results from executing <paramref name="expression" />.</returns>
    /// <param name="expression">An expression tree to execute.</param>
    /// <typeparam name="S">The type of the data in the collection that <paramref name="expression" /> represents.</typeparam>
    S IQueryProvider.Execute<S>(Expression expression)
    {
        if (expression == null)
        {
            throw Error.ArgumentNull(nameof(expression));
        }

        return typeof(S).IsAssignableFrom(expression.Type)
            ? new EnumerableExecutor<S>(expression).Execute()
            : throw Error.ArgumentNotValid(nameof(expression));
    }

    /// <summary>
    ///     Returns a textual representation of the enumerable collection or, if it is null, of the expression tree that
    ///     is associated with this instance.
    /// </summary>
    /// <returns>
    ///     A textual representation of the enumerable collection or, if it is null, of the expression tree that is
    ///     associated with this instance.
    /// </returns>
    public override string ToString()
    {
        if (!(this.expression is ConstantExpression expression) || expression.Value != this)
        {
            return this.expression.ToString();
        }

        return enumerable != null ? enumerable.ToString() : "null";
    }

    private IEnumerator<T> GetEnumerator()
    {
        if (enumerable == null)
        {
            enumerable = Expression.Lambda<Func<IEnumerable<T>>>(new EnumerableRewriter().Visit(expression),
                (IEnumerable<ParameterExpression>)null).Compile()();
        }

        return enumerable.GetEnumerator();
    }
}