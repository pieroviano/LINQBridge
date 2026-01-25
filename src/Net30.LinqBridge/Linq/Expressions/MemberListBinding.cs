#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace System.Linq.Expressions;

/// <summary>Represents initializing the elements of a collection member of a newly created object.</summary>
public sealed class MemberListBinding : MemberBinding
{
    internal MemberListBinding(MemberInfo member, ReadOnlyCollection<ElementInit> initializers)
#pragma warning disable CS0618 // Type or member is obsolete
        : base(MemberBindingType.ListBinding, member)
#pragma warning restore CS0618 // Type or member is obsolete
    {
        Initializers = initializers;
    }

    /// <summary>Gets the element initializers for initializing a collection member of a newly created object.</summary>
    /// <returns>
    ///     A <see cref="T:System.Collections.ObjectModel.ReadOnlyCollection`1" /> of
    ///     <see cref="T:System.Linq.Expressions.ElementInit" /> objects to initialize a collection member with.
    /// </returns>
    public ReadOnlyCollection<ElementInit> Initializers { get; }

    /// <summary>
    ///     Creates a new expression that is like this one, but using the supplied children. If all of the children are
    ///     the same, it will return this expression.
    /// </summary>
    /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
    /// <param name="initializers">
    ///     The <see cref="P:System.Linq.Expressions.MemberListBinding.Initializers" /> property of the
    ///     result.
    /// </param>
    public MemberListBinding Update(IEnumerable<ElementInit> initializers)
    {
        return initializers == Initializers ? this : Expression.ListBind(Member, initializers);
    }
}