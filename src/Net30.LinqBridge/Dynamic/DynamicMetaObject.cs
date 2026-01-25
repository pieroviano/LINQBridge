#nullable disable
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;

namespace System.Dynamic;

/// <summary>Represents the dynamic binding and a binding logic of an object participating in the dynamic binding.</summary>
public class DynamicMetaObject
{
    /// <summary>Represents an empty array of type <see cref="T:System.Dynamic.DynamicMetaObject" />. This field is read only.</summary>
    public static readonly DynamicMetaObject[] EmptyMetaObjects = new DynamicMetaObject[0];

    /// <summary>Initializes a new instance of the <see cref="T:System.Dynamic.DynamicMetaObject" /> class.</summary>
    /// <param name="expression">
    ///     The expression representing this <see cref="T:System.Dynamic.DynamicMetaObject" /> during the
    ///     dynamic binding process.
    /// </param>
    /// <param name="restrictions">The set of binding restrictions under which the binding is valid.</param>
    public DynamicMetaObject(Expression expression, BindingRestrictions restrictions)
    {
        ContractUtils.RequiresNotNull(expression, nameof(expression));
        ContractUtils.RequiresNotNull(restrictions, nameof(restrictions));
        Expression = expression;
        Restrictions = restrictions;
    }

    /// <summary>Initializes a new instance of the <see cref="T:System.Dynamic.DynamicMetaObject" /> class.</summary>
    /// <param name="expression">
    ///     The expression representing this <see cref="T:System.Dynamic.DynamicMetaObject" /> during the
    ///     dynamic binding process.
    /// </param>
    /// <param name="restrictions">The set of binding restrictions under which the binding is valid.</param>
    /// <param name="value">The runtime value represented by the <see cref="T:System.Dynamic.DynamicMetaObject" />.</param>
    public DynamicMetaObject(Expression expression, BindingRestrictions restrictions, object value)
        : this(expression, restrictions)
    {
        Value = value;
        HasValue = true;
    }

    /// <summary>
    ///     The expression representing the <see cref="T:System.Dynamic.DynamicMetaObject" /> during the dynamic binding
    ///     process.
    /// </summary>
    /// <returns>
    ///     The expression representing the <see cref="T:System.Dynamic.DynamicMetaObject" /> during the dynamic binding
    ///     process.
    /// </returns>
    public Expression Expression { get; }

    /// <summary>The set of binding restrictions under which the binding is valid.</summary>
    /// <returns>The set of binding restrictions.</returns>
    public BindingRestrictions Restrictions { get; }

    /// <summary>The runtime value represented by this <see cref="T:System.Dynamic.DynamicMetaObject" />.</summary>
    /// <returns>The runtime value represented by this <see cref="T:System.Dynamic.DynamicMetaObject" />.</returns>
    public object Value { get; }

    /// <summary>Gets a value indicating whether the <see cref="T:System.Dynamic.DynamicMetaObject" /> has the runtime value.</summary>
    /// <returns>True if the <see cref="T:System.Dynamic.DynamicMetaObject" /> has the runtime value, otherwise false.</returns>
    public bool HasValue { get; }

    /// <summary>
    ///     Gets the <see cref="T:System.Type" /> of the runtime value or null if the
    ///     <see cref="T:System.Dynamic.DynamicMetaObject" /> has no value associated with it.
    /// </summary>
    /// <returns>The <see cref="T:System.Type" /> of the runtime value or null.</returns>
    public Type RuntimeType
    {
        get
        {
            if (!HasValue)
            {
                return null;
            }

            var type = Expression.Type;
            if (type.IsValueType)
            {
                return type;
            }

            return Value != null ? Value.GetType() : null;
        }
    }

    /// <summary>Gets the limit type of the <see cref="T:System.Dynamic.DynamicMetaObject" />.</summary>
    /// <returns>
    ///     <see cref="P:System.Dynamic.DynamicMetaObject.RuntimeType" /> if runtime value is available, a type of the
    ///     <see cref="P:System.Dynamic.DynamicMetaObject.Expression" /> otherwise.
    /// </returns>
    public Type LimitType
    {
        get
        {
            var runtimeType = RuntimeType;
            return runtimeType != null ? runtimeType : Expression.Type;
        }
    }

    /// <summary>Performs the binding of the dynamic binary operation.</summary>
    /// <returns>The new <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="binder">
    ///     An instance of the <see cref="T:System.Dynamic.BinaryOperationBinder" /> that represents the
    ///     details of the dynamic operation.
    /// </param>
    /// <param name="arg">
    ///     An instance of the <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the right hand side
    ///     of the binary operation.
    /// </param>
    public virtual DynamicMetaObject BindBinaryOperation(
        BinaryOperationBinder binder,
        DynamicMetaObject arg)
    {
        ContractUtils.RequiresNotNull(binder, nameof(binder));
        return binder.FallbackBinaryOperation(this, arg);
    }

    /// <summary>Performs the binding of the dynamic conversion operation.</summary>
    /// <returns>The new <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="binder">
    ///     An instance of the <see cref="T:System.Dynamic.ConvertBinder" /> that represents the details of
    ///     the dynamic operation.
    /// </param>
    public virtual DynamicMetaObject BindConvert(ConvertBinder binder)
    {
        ContractUtils.RequiresNotNull(binder, nameof(binder));
        return binder.FallbackConvert(this);
    }

    /// <summary>Performs the binding of the dynamic create instance operation.</summary>
    /// <returns>The new <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="binder">
    ///     An instance of the <see cref="T:System.Dynamic.CreateInstanceBinder" /> that represents the
    ///     details of the dynamic operation.
    /// </param>
    /// <param name="args">
    ///     An array of <see cref="T:System.Dynamic.DynamicMetaObject" /> instances - arguments to the create
    ///     instance operation.
    /// </param>
    public virtual DynamicMetaObject BindCreateInstance(
        CreateInstanceBinder binder,
        DynamicMetaObject[] args)
    {
        ContractUtils.RequiresNotNull(binder, nameof(binder));
        return binder.FallbackCreateInstance(this, args);
    }

    /// <summary>Performs the binding of the dynamic delete index operation.</summary>
    /// <returns>The new <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="binder">
    ///     An instance of the <see cref="T:System.Dynamic.DeleteIndexBinder" /> that represents the details
    ///     of the dynamic operation.
    /// </param>
    /// <param name="indexes">
    ///     An array of <see cref="T:System.Dynamic.DynamicMetaObject" /> instances - indexes for the delete
    ///     index operation.
    /// </param>
    public virtual DynamicMetaObject BindDeleteIndex(
        DeleteIndexBinder binder,
        DynamicMetaObject[] indexes)
    {
        ContractUtils.RequiresNotNull(binder, nameof(binder));
        return binder.FallbackDeleteIndex(this, indexes);
    }

    /// <summary>Performs the binding of the dynamic delete member operation.</summary>
    /// <returns>The new <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="binder">
    ///     An instance of the <see cref="T:System.Dynamic.DeleteMemberBinder" /> that represents the details
    ///     of the dynamic operation.
    /// </param>
    public virtual DynamicMetaObject BindDeleteMember(DeleteMemberBinder binder)
    {
        ContractUtils.RequiresNotNull(binder, nameof(binder));
        return binder.FallbackDeleteMember(this);
    }

    /// <summary>Performs the binding of the dynamic get index operation.</summary>
    /// <returns>The new <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="binder">
    ///     An instance of the <see cref="T:System.Dynamic.GetIndexBinder" /> that represents the details of
    ///     the dynamic operation.
    /// </param>
    /// <param name="indexes">
    ///     An array of <see cref="T:System.Dynamic.DynamicMetaObject" /> instances - indexes for the get
    ///     index operation.
    /// </param>
    public virtual DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
    {
        ContractUtils.RequiresNotNull(binder, nameof(binder));
        return binder.FallbackGetIndex(this, indexes);
    }

    /// <summary>Performs the binding of the dynamic get member operation.</summary>
    /// <returns>The new <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="binder">
    ///     An instance of the <see cref="T:System.Dynamic.GetMemberBinder" /> that represents the details of
    ///     the dynamic operation.
    /// </param>
    public virtual DynamicMetaObject BindGetMember(GetMemberBinder binder)
    {
        ContractUtils.RequiresNotNull(binder, nameof(binder));
        return binder.FallbackGetMember(this);
    }

    /// <summary>Performs the binding of the dynamic invoke operation.</summary>
    /// <returns>The new <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="binder">
    ///     An instance of the <see cref="T:System.Dynamic.InvokeBinder" /> that represents the details of the
    ///     dynamic operation.
    /// </param>
    /// <param name="args">
    ///     An array of <see cref="T:System.Dynamic.DynamicMetaObject" /> instances - arguments to the invoke
    ///     operation.
    /// </param>
    public virtual DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
    {
        ContractUtils.RequiresNotNull(binder, nameof(binder));
        return binder.FallbackInvoke(this, args);
    }

    /// <summary>Performs the binding of the dynamic invoke member operation.</summary>
    /// <returns>The new <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="binder">
    ///     An instance of the <see cref="T:System.Dynamic.InvokeMemberBinder" /> that represents the details
    ///     of the dynamic operation.
    /// </param>
    /// <param name="args">
    ///     An array of <see cref="T:System.Dynamic.DynamicMetaObject" /> instances - arguments to the invoke
    ///     member operation.
    /// </param>
    public virtual DynamicMetaObject BindInvokeMember(
        InvokeMemberBinder binder,
        DynamicMetaObject[] args)
    {
        ContractUtils.RequiresNotNull(binder, nameof(binder));
        return binder.FallbackInvokeMember(this, args);
    }

    /// <summary>Performs the binding of the dynamic set index operation.</summary>
    /// <returns>The new <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="binder">
    ///     An instance of the <see cref="T:System.Dynamic.SetIndexBinder" /> that represents the details of
    ///     the dynamic operation.
    /// </param>
    /// <param name="indexes">
    ///     An array of <see cref="T:System.Dynamic.DynamicMetaObject" /> instances - indexes for the set
    ///     index operation.
    /// </param>
    /// <param name="value">
    ///     The <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the value for the set index
    ///     operation.
    /// </param>
    public virtual DynamicMetaObject BindSetIndex(
        SetIndexBinder binder,
        DynamicMetaObject[] indexes,
        DynamicMetaObject value)
    {
        ContractUtils.RequiresNotNull(binder, nameof(binder));
        return binder.FallbackSetIndex(this, indexes, value);
    }

    /// <summary>Performs the binding of the dynamic set member operation.</summary>
    /// <returns>The new <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="binder">
    ///     An instance of the <see cref="T:System.Dynamic.SetMemberBinder" /> that represents the details of
    ///     the dynamic operation.
    /// </param>
    /// <param name="value">
    ///     The <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the value for the set member
    ///     operation.
    /// </param>
    public virtual DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
    {
        ContractUtils.RequiresNotNull(binder, nameof(binder));
        return binder.FallbackSetMember(this, value);
    }

    /// <summary>Performs the binding of the dynamic unary operation.</summary>
    /// <returns>The new <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="binder">
    ///     An instance of the <see cref="T:System.Dynamic.UnaryOperationBinder" /> that represents the
    ///     details of the dynamic operation.
    /// </param>
    public virtual DynamicMetaObject BindUnaryOperation(UnaryOperationBinder binder)
    {
        ContractUtils.RequiresNotNull(binder, nameof(binder));
        return binder.FallbackUnaryOperation(this);
    }

    /// <summary>Creates a meta-object for the specified object.</summary>
    /// <returns>
    ///     If the given object implements <see cref="T:System.Dynamic.IDynamicMetaObjectProvider" /> and is not a remote
    ///     object from outside the current AppDomain, returns the object's specific meta-object returned by
    ///     <see cref="M:System.Dynamic.IDynamicMetaObjectProvider.GetMetaObject(System.Linq.Expressions.Expression)" />.
    ///     Otherwise a plain new meta-object with no restrictions is created and returned.
    /// </returns>
    /// <param name="value">The object to get a meta-object for.</param>
    /// <param name="expression">
    ///     The expression representing this <see cref="T:System.Dynamic.DynamicMetaObject" /> during the
    ///     dynamic binding process.
    /// </param>
    public static DynamicMetaObject Create(object value, Expression expression)
    {
        ContractUtils.RequiresNotNull(expression, nameof(expression));
        if (!(value is IDynamicMetaObjectProvider metaObjectProvider) || RemotingServices.IsObjectOutOfAppDomain(value))
        {
            return new DynamicMetaObject(expression, BindingRestrictions.Empty, value);
        }

        var metaObject = metaObjectProvider.GetMetaObject(expression);
        if (metaObject == null || !metaObject.HasValue || metaObject.Value == null ||
            metaObject.Expression != expression)
        {
            throw Error.InvalidMetaObjectCreated(metaObjectProvider.GetType());
        }

        return metaObject;
    }

    /// <summary>Returns the enumeration of all dynamic member names.</summary>
    /// <returns>The list of dynamic member names.</returns>
    public virtual IEnumerable<string> GetDynamicMemberNames()
    {
        return new string[0];
    }

    internal static Expression[] GetExpressions(DynamicMetaObject[] objects)
    {
        ContractUtils.RequiresNotNull(objects, nameof(objects));
        var expressions = new Expression[objects.Length];
        for (var index = 0; index < objects.Length; ++index)
        {
            var dynamicMetaObject = objects[index];
            ContractUtils.RequiresNotNull(dynamicMetaObject, nameof(objects));
            var expression = dynamicMetaObject.Expression;
            ContractUtils.RequiresNotNull(expression, nameof(objects));
            expressions[index] = expression;
        }

        return expressions;
    }
}