using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;

namespace System.Linq.Expressions;

/// <summary>Represents initializing members of a member of a newly created object.</summary>
public sealed class MemberMemberBinding : MemberBinding
{
    private readonly ReadOnlyCollection<MemberBinding> bindings;

    internal MemberMemberBinding(MemberInfo member, ReadOnlyCollection<MemberBinding> bindings)
        : base(MemberBindingType.MemberBinding, member)
    {
        this.bindings = bindings;
    }

    /// <summary>Gets the bindings that describe how to initialize the members of a member.</summary>
    /// <returns>A <see cref="T:System.Collections.ObjectModel.ReadOnlyCollection`1" /> of <see cref="T:System.Linq.Expressions.MemberBinding" /> objects that describe how to initialize the members of the member.</returns>
    public ReadOnlyCollection<MemberBinding> Bindings => bindings;

    internal override void BuildString(StringBuilder builder)
    {
        builder.Append(Member.Name);
        builder.Append(" = {");
        var index = 0;
        for (var count = bindings.Count; index < count; ++index)
        {
            if (index > 0)
                builder.Append(", ");
            bindings[index].BuildString(builder);
        }
        builder.Append("}");
    }
}