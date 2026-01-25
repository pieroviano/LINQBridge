#nullable disable
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace System.Dynamic;

/// <summary>
///     Represents the unary dynamic operation at the call site, providing the binding semantic and the details about
///     the operation.
/// </summary>
public abstract class UnaryOperationBinder : DynamicMetaObjectBinder
{
    /// <summary>Initializes a new instance of the <see cref="T:System.Dynamic.BinaryOperationBinder" /> class.</summary>
    /// <param name="operation">The unary operation kind.</param>
    protected UnaryOperationBinder(ExpressionType operation)
    {
        ContractUtils.Requires(OperationIsValid(operation), nameof(operation));
        Operation = operation;
    }

    /// <summary>The result type of the operation.</summary>
    /// <returns>The <see cref="T:System.Type" /> object representing the result type of the operation.</returns>
    public sealed override Type ReturnType
    {
        get
        {
            switch (Operation)
            {
                case ExpressionType.IsTrue:
                case ExpressionType.IsFalse:
                    return typeof(bool);
                default:
                    return typeof(object);
            }
        }
    }

    /// <summary>The unary operation kind.</summary>
    /// <returns>
    ///     The object of the <see cref="T:System.Linq.Expressions.ExpressionType" /> that represents the unary operation
    ///     kind.
    /// </returns>
    public ExpressionType Operation { get; }

    internal sealed override bool IsStandardBinder => true;

    /// <summary>Performs the binding of the dynamic unary operation.</summary>
    /// <returns>The <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="target">The target of the dynamic operation.</param>
    /// <param name="args">An array of arguments of the dynamic operation.</param>
    public sealed override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
    {
        ContractUtils.RequiresNotNull(target, nameof(target));
        ContractUtils.Requires(args == null || args.Length == 0, nameof(args));
        return target.BindUnaryOperation(this);
    }

    /// <summary>Performs the binding of the unary dynamic operation if the target dynamic object cannot bind.</summary>
    /// <returns>The <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="target">The target of the dynamic unary operation.</param>
    public DynamicMetaObject FallbackUnaryOperation(DynamicMetaObject target)
    {
        return FallbackUnaryOperation(target, null);
    }

    /// <summary>Performs the binding of the unary dynamic operation if the target dynamic object cannot bind.</summary>
    /// <returns>The <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="target">The target of the dynamic unary operation.</param>
    /// <param name="errorSuggestion">The binding result in case the binding fails, or null.</param>
    public abstract DynamicMetaObject FallbackUnaryOperation(
        DynamicMetaObject target,
        DynamicMetaObject errorSuggestion);

    internal static bool OperationIsValid(ExpressionType operation)
    {
        switch (operation)
        {
            case ExpressionType.Negate:
            case ExpressionType.UnaryPlus:
            case ExpressionType.Not:
            case ExpressionType.Decrement:
            case ExpressionType.Extension:
            case ExpressionType.Increment:
            case ExpressionType.OnesComplement:
            case ExpressionType.IsTrue:
            case ExpressionType.IsFalse:
                return true;
            default:
                return false;
        }
    }
}