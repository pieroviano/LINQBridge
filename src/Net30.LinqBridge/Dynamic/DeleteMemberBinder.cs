#nullable disable
using System.Runtime.CompilerServices;

namespace System.Dynamic;

/// <summary>
///     Represents the dynamic delete member operation at the call site, providing the binding semantic and the
///     details about the operation.
/// </summary>
public abstract class DeleteMemberBinder : DynamicMetaObjectBinder
{
    /// <summary>Initializes a new instance of the <see cref="T:System.Dynamic.DeleteIndexBinder" />.</summary>
    /// <param name="name">The name of the member to delete.</param>
    /// <param name="ignoreCase">Is true if the name should be matched ignoring case; false otherwise.</param>
    protected DeleteMemberBinder(string name, bool ignoreCase)
    {
        ContractUtils.RequiresNotNull(name, nameof(name));
        Name = name;
        IgnoreCase = ignoreCase;
    }

    /// <summary>Gets the name of the member to delete.</summary>
    /// <returns>The name of the member to delete.</returns>
    public string Name { get; }

    /// <summary>Gets the value indicating if the string comparison should ignore the case of the member name.</summary>
    /// <returns>True if the string comparison should ignore the case, otherwise false.</returns>
    public bool IgnoreCase { get; }

    /// <summary>The result type of the operation.</summary>
    /// <returns>The <see cref="T:System.Type" /> object representing the result type of the operation.</returns>
    public sealed override Type ReturnType => typeof(void);

    internal sealed override bool IsStandardBinder => true;

    /// <summary>Performs the binding of the dynamic delete member operation.</summary>
    /// <returns>The <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="target">The target of the dynamic delete member operation.</param>
    /// <param name="args">An array of arguments of the dynamic delete member operation.</param>
    public sealed override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
    {
        ContractUtils.RequiresNotNull(target, nameof(target));
        ContractUtils.Requires(args == null || args.Length == 0);
        return target.BindDeleteMember(this);
    }

    /// <summary>Performs the binding of the dynamic delete member operation if the target dynamic object cannot bind.</summary>
    /// <returns>The <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="target">The target of the dynamic delete member operation.</param>
    public DynamicMetaObject FallbackDeleteMember(DynamicMetaObject target)
    {
        return FallbackDeleteMember(target, null);
    }

    /// <summary>
    ///     When overridden in the derived class, performs the binding of the dynamic delete member operation if the
    ///     target dynamic object cannot bind.
    /// </summary>
    /// <returns>The <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="target">The target of the dynamic delete member operation.</param>
    /// <param name="errorSuggestion">The binding result to use if binding fails, or null.</param>
    public abstract DynamicMetaObject FallbackDeleteMember(
        DynamicMetaObject target,
        DynamicMetaObject errorSuggestion);
}