#nullable disable
using System.Runtime.CompilerServices;

namespace System.Dynamic;

/// <summary>
///     Represents the dynamic set member operation at the call site, providing the binding semantic and the details
///     about the operation.
/// </summary>
public abstract class SetMemberBinder : DynamicMetaObjectBinder
{
    /// <summary>Initializes a new instance of the <see cref="T:System.Dynamic.SetMemberBinder" />.</summary>
    /// <param name="name">The name of the member to obtain.</param>
    /// <param name="ignoreCase">Is true if the name should be matched ignoring case; false otherwise.</param>
    protected SetMemberBinder(string name, bool ignoreCase)
    {
        ContractUtils.RequiresNotNull(name, nameof(name));
        Name = name;
        IgnoreCase = ignoreCase;
    }

    /// <summary>The result type of the operation.</summary>
    /// <returns>The <see cref="T:System.Type" /> object representing the result type of the operation.</returns>
    public sealed override Type ReturnType => typeof(object);

    /// <summary>Gets the name of the member to obtain.</summary>
    /// <returns>The name of the member to obtain.</returns>
    public string Name { get; }

    /// <summary>Gets the value indicating if the string comparison should ignore the case of the member name.</summary>
    /// <returns>True if the case is ignored, otherwise false.</returns>
    public bool IgnoreCase { get; }

    internal sealed override bool IsStandardBinder => true;

    /// <summary>Performs the binding of the dynamic set member operation.</summary>
    /// <returns>The <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="target">The target of the dynamic set member operation.</param>
    /// <param name="args">An array of arguments of the dynamic set member operation.</param>
    public sealed override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
    {
        ContractUtils.RequiresNotNull(target, nameof(target));
        ContractUtils.RequiresNotNull(args, nameof(args));
        ContractUtils.Requires(args.Length == 1, nameof(args));
        var dynamicMetaObject = args[0];
        ContractUtils.RequiresNotNull(dynamicMetaObject, nameof(args));
        return target.BindSetMember(this, dynamicMetaObject);
    }

    /// <summary>Performs the binding of the dynamic set member operation if the target dynamic object cannot bind.</summary>
    /// <returns>The <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="target">The target of the dynamic set member operation.</param>
    /// <param name="value">The value to set to the member.</param>
    public DynamicMetaObject FallbackSetMember(DynamicMetaObject target, DynamicMetaObject value)
    {
        return FallbackSetMember(target, value, null);
    }

    /// <summary>Performs the binding of the dynamic set member operation if the target dynamic object cannot bind.</summary>
    /// <returns>The <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="target">The target of the dynamic set member operation.</param>
    /// <param name="value">The value to set to the member.</param>
    /// <param name="errorSuggestion">The binding result to use if binding fails, or null.</param>
    public abstract DynamicMetaObject FallbackSetMember(
        DynamicMetaObject target,
        DynamicMetaObject value,
        DynamicMetaObject errorSuggestion);
}