#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace System.Linq.Expressions;

/// <summary>Represents initializing members of a member of a newly created object.</summary>
public sealed class MemberMemberBinding : MemberBinding
{
    internal MemberMemberBinding(MemberInfo member, ReadOnlyCollection<MemberBinding> bindings)
#pragma warning disable CS0618 // Type or member is obsolete
        : base(MemberBindingType.MemberBinding, member)
#pragma warning restore CS0618 // Type or member is obsolete
    {
        Bindings = bindings;
    }

    /// <summary>Gets the bindings that describe how to initialize the members of a member.</summary>
    /// <returns>
    ///     A <see cref="T:System.Collections.ObjectModel.ReadOnlyCollection`1" /> of
    ///     <see cref="T:System.Linq.Expressions.MemberBinding" /> objects that describe how to initialize the members of the
    ///     member.
    /// </returns>
    public ReadOnlyCollection<MemberBinding> Bindings { get; }

    /// <summary>
    ///     Creates a new expression that is like this one, but using the supplied children. If all of the children are
    ///     the same, it will return this expression.
    /// </summary>
    /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
    /// <param name="bindings">
    ///     The <see cref="P:System.Linq.Expressions.MemberMemberBinding.Bindings" /> property of the
    ///     result.
    /// </param>
    public MemberMemberBinding Update(IEnumerable<MemberBinding> bindings)
    {
        return bindings == Bindings ? this : Expression.MemberBind(Member, bindings);
    }
}