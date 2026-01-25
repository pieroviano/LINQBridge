#nullable disable
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace System.Dynamic;

/// <summary>
///     Represents the binary dynamic operation at the call site, providing the binding semantic and the details about
///     the operation.
/// </summary>
public abstract class BinaryOperationBinder : DynamicMetaObjectBinder
{
    /// <summary>Initializes a new instance of the <see cref="T:System.Dynamic.BinaryOperationBinder" /> class.</summary>
    /// <param name="operation">The binary operation kind.</param>
    protected BinaryOperationBinder(ExpressionType operation)
    {
        ContractUtils.Requires(OperationIsValid(operation), nameof(operation));
        Operation = operation;
    }

    /// <summary>The result type of the operation.</summary>
    /// <returns>The result type of the operation.</returns>
    public sealed override Type ReturnType => typeof(object);

    /// <summary>The binary operation kind.</summary>
    /// <returns>The <see cref="T:System.Linq.Expressions.ExpressionType" /> object representing the kind of binary operation.</returns>
    public ExpressionType Operation { get; }

    internal sealed override bool IsStandardBinder => true;

    /// <summary>Performs the binding of the dynamic binary operation.</summary>
    /// <returns>The <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="target">The target of the dynamic operation.</param>
    /// <param name="args">An array of arguments of the dynamic operation.</param>
    public sealed override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
    {
        ContractUtils.RequiresNotNull(target, nameof(target));
        ContractUtils.RequiresNotNull(args, nameof(args));
        ContractUtils.Requires(args.Length == 1, nameof(args));
        var dynamicMetaObject = args[0];
        ContractUtils.RequiresNotNull(dynamicMetaObject, nameof(args));
        return target.BindBinaryOperation(this, dynamicMetaObject);
    }

    /// <summary>Performs the binding of the binary dynamic operation if the target dynamic object cannot bind.</summary>
    /// <returns>The <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="target">The target of the dynamic binary operation.</param>
    /// <param name="arg">The right hand side operand of the dynamic binary operation.</param>
    public DynamicMetaObject FallbackBinaryOperation(DynamicMetaObject target, DynamicMetaObject arg)
    {
        return FallbackBinaryOperation(target, arg, null);
    }

    /// <summary>
    ///     When overridden in the derived class, performs the binding of the binary dynamic operation if the target
    ///     dynamic object cannot bind.
    /// </summary>
    /// <returns>The <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="target">The target of the dynamic binary operation.</param>
    /// <param name="arg">The right hand side operand of the dynamic binary operation.</param>
    /// <param name="errorSuggestion">The binding result if the binding fails, or null.</param>
    public abstract DynamicMetaObject FallbackBinaryOperation(
        DynamicMetaObject target,
        DynamicMetaObject arg,
        DynamicMetaObject errorSuggestion);

    internal static bool OperationIsValid(ExpressionType operation)
    {
        switch (operation)
        {
            case ExpressionType.Add:
            case ExpressionType.And:
            case ExpressionType.Divide:
            case ExpressionType.Equal:
            case ExpressionType.ExclusiveOr:
            case ExpressionType.GreaterThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.LeftShift:
            case ExpressionType.LessThan:
            case ExpressionType.LessThanOrEqual:
            case ExpressionType.Modulo:
            case ExpressionType.Multiply:
            case ExpressionType.NotEqual:
            case ExpressionType.Or:
            case ExpressionType.Power:
            case ExpressionType.RightShift:
            case ExpressionType.Subtract:
            case ExpressionType.Extension:
            case ExpressionType.AddAssign:
            case ExpressionType.AndAssign:
            case ExpressionType.DivideAssign:
            case ExpressionType.ExclusiveOrAssign:
            case ExpressionType.LeftShiftAssign:
            case ExpressionType.ModuloAssign:
            case ExpressionType.MultiplyAssign:
            case ExpressionType.OrAssign:
            case ExpressionType.PowerAssign:
            case ExpressionType.RightShiftAssign:
            case ExpressionType.SubtractAssign:
                return true;
            default:
                return false;
        }
    }
}