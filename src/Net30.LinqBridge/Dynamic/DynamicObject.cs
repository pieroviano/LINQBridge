#nullable disable
using System.Collections.Generic;
using System.Dynamic.Utils;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Dynamic;

/// <summary>
///     Provides a base class for specifying dynamic behavior at run time. This class must be inherited from; you
///     cannot instantiate it directly.
/// </summary>
[Serializable]
public class DynamicObject : IDynamicMetaObjectProvider
{
    /// <summary>Enables derived types to initialize a new instance of the <see cref="T:System.Dynamic.DynamicObject" /> type.</summary>
    protected DynamicObject()
    {
    }

    /// <summary>
    ///     Provides a <see cref="T:System.Dynamic.DynamicMetaObject" /> that dispatches to the dynamic virtual methods.
    ///     The object can be encapsulated inside another <see cref="T:System.Dynamic.DynamicMetaObject" /> to provide custom
    ///     behavior for individual actions. This method supports the Dynamic Language Runtime infrastructure for language
    ///     implementers and it is not intended to be used directly from your code.
    /// </summary>
    /// <returns>An object of the <see cref="T:System.Dynamic.DynamicMetaObject" /> type.</returns>
    /// <param name="parameter">
    ///     The expression that represents <see cref="T:System.Dynamic.DynamicMetaObject" /> to dispatch to
    ///     the dynamic virtual methods.
    /// </param>
    public virtual DynamicMetaObject GetMetaObject(Expression parameter)
    {
        return new MetaDynamic(parameter, this);
    }

    /// <summary>Returns the enumeration of all dynamic member names. </summary>
    /// <returns>A sequence that contains dynamic member names.</returns>
    public virtual IEnumerable<string> GetDynamicMemberNames()
    {
        return new string[0];
    }

    /// <summary>
    ///     Provides implementation for binary operations. Classes derived from the
    ///     <see cref="T:System.Dynamic.DynamicObject" /> class can override this method to specify dynamic behavior for
    ///     operations such as addition and multiplication.
    /// </summary>
    /// <returns>
    ///     true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of
    ///     the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
    /// </returns>
    /// <param name="binder">
    ///     Provides information about the binary operation. The binder.Operation property returns an
    ///     <see cref="T:System.Linq.Expressions.ExpressionType" /> object. For example, for the sum = first + second
    ///     statement, where first and second are derived from the DynamicObject class, binder.Operation returns
    ///     ExpressionType.Add.
    /// </param>
    /// <param name="arg">
    ///     The right operand for the binary operation. For example, for the sum = first + second statement,
    ///     where first and second are derived from the DynamicObject class, <paramref name="arg" /> is equal to second.
    /// </param>
    /// <param name="result">The result of the binary operation.</param>
    public virtual bool TryBinaryOperation(
        BinaryOperationBinder binder,
        object arg,
        out object result)
    {
        result = null;
        return false;
    }

    /// <summary>
    ///     Provides implementation for type conversion operations. Classes derived from the
    ///     <see cref="T:System.Dynamic.DynamicObject" /> class can override this method to specify dynamic behavior for
    ///     operations that convert an object from one type to another.
    /// </summary>
    /// <returns>
    ///     true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of
    ///     the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
    /// </returns>
    /// <param name="binder">
    ///     Provides information about the conversion operation. The binder.Type property provides the type to
    ///     which the object must be converted. For example, for the statement (String)sampleObject in C# (CType(sampleObject,
    ///     Type) in Visual Basic), where sampleObject is an instance of the class derived from the
    ///     <see cref="T:System.Dynamic.DynamicObject" /> class, binder.Type returns the <see cref="T:System.String" /> type.
    ///     The binder.Explicit property provides information about the kind of conversion that occurs. It returns true for
    ///     explicit conversion and false for implicit conversion.
    /// </param>
    /// <param name="result">The result of the type conversion operation.</param>
    public virtual bool TryConvert(ConvertBinder binder, out object result)
    {
        result = null;
        return false;
    }

    /// <summary>
    ///     Provides the implementation for operations that initialize a new instance of a dynamic object. This method is
    ///     not intended for use in C# or Visual Basic.
    /// </summary>
    /// <returns>
    ///     true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of
    ///     the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
    /// </returns>
    /// <param name="binder">Provides information about the initialization operation.</param>
    /// <param name="args">
    ///     The arguments that are passed to the object during initialization. For example, for the new
    ///     SampleType(100) operation, where SampleType is the type derived from the
    ///     <see cref="T:System.Dynamic.DynamicObject" /> class, <paramref name="args[0]" /> is equal to 100.
    /// </param>
    /// <param name="result">The result of the initialization.</param>
    public virtual bool TryCreateInstance(
        CreateInstanceBinder binder,
        object[] args,
        out object result)
    {
        result = null;
        return false;
    }

    /// <summary>
    ///     Provides the implementation for operations that delete an object by index. This method is not intended for use
    ///     in C# or Visual Basic.
    /// </summary>
    /// <returns>
    ///     true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of
    ///     the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
    /// </returns>
    /// <param name="binder">Provides information about the deletion.</param>
    /// <param name="indexes">The indexes to be deleted.</param>
    public virtual bool TryDeleteIndex(DeleteIndexBinder binder, object[] indexes)
    {
        return false;
    }

    /// <summary>
    ///     Provides the implementation for operations that delete an object member. This method is not intended for use
    ///     in C# or Visual Basic.
    /// </summary>
    /// <returns>
    ///     true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of
    ///     the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
    /// </returns>
    /// <param name="binder">Provides information about the deletion.</param>
    public virtual bool TryDeleteMember(DeleteMemberBinder binder)
    {
        return false;
    }

    /// <summary>
    ///     Provides the implementation for operations that get a value by index. Classes derived from the
    ///     <see cref="T:System.Dynamic.DynamicObject" /> class can override this method to specify dynamic behavior for
    ///     indexing operations.
    /// </summary>
    /// <returns>
    ///     true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of
    ///     the language determines the behavior. (In most cases, a run-time exception is thrown.)
    /// </returns>
    /// <param name="binder">Provides information about the operation. </param>
    /// <param name="indexes">
    ///     The indexes that are used in the operation. For example, for the sampleObject[3] operation in C#
    ///     (sampleObject(3) in Visual Basic), where sampleObject is derived from the DynamicObject class,
    ///     <paramref name="indexes[0]" /> is equal to 3.
    /// </param>
    /// <param name="result">The result of the index operation.</param>
    public virtual bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
    {
        result = null;
        return false;
    }

    /// <summary>
    ///     Provides the implementation for operations that get member values. Classes derived from the
    ///     <see cref="T:System.Dynamic.DynamicObject" /> class can override this method to specify dynamic behavior for
    ///     operations such as getting a value for a property.
    /// </summary>
    /// <returns>
    ///     true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of
    ///     the language determines the behavior. (In most cases, a run-time exception is thrown.)
    /// </returns>
    /// <param name="binder">
    ///     Provides information about the object that called the dynamic operation. The binder.Name property
    ///     provides the name of the member on which the dynamic operation is performed. For example, for the
    ///     Console.WriteLine(sampleObject.SampleProperty) statement, where sampleObject is an instance of the class derived
    ///     from the <see cref="T:System.Dynamic.DynamicObject" /> class, binder.Name returns "SampleProperty". The
    ///     binder.IgnoreCase property specifies whether the member name is case-sensitive.
    /// </param>
    /// <param name="result">
    ///     The result of the get operation. For example, if the method is called for a property, you can
    ///     assign the property value to <paramref name="result" />.
    /// </param>
    public virtual bool TryGetMember(GetMemberBinder binder, out object result)
    {
        result = null;
        return false;
    }

    /// <summary>
    ///     Provides the implementation for operations that invoke an object. Classes derived from the
    ///     <see cref="T:System.Dynamic.DynamicObject" /> class can override this method to specify dynamic behavior for
    ///     operations such as invoking an object or a delegate.
    /// </summary>
    /// <returns>
    ///     true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of
    ///     the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.
    /// </returns>
    /// <param name="binder">Provides information about the invoke operation.</param>
    /// <param name="args">
    ///     The arguments that are passed to the object during the invoke operation. For example, for the
    ///     sampleObject(100) operation, where sampleObject is derived from the <see cref="T:System.Dynamic.DynamicObject" />
    ///     class, <paramref name="args[0]" /> is equal to 100.
    /// </param>
    /// <param name="result">The result of the object invocation.</param>
    public virtual bool TryInvoke(InvokeBinder binder, object[] args, out object result)
    {
        result = null;
        return false;
    }

    /// <summary>
    ///     Provides the implementation for operations that invoke a member. Classes derived from the
    ///     <see cref="T:System.Dynamic.DynamicObject" /> class can override this method to specify dynamic behavior for
    ///     operations such as calling a method.
    /// </summary>
    /// <returns>
    ///     true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of
    ///     the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
    /// </returns>
    /// <param name="binder">
    ///     Provides information about the dynamic operation. The binder.Name property provides the name of
    ///     the member on which the dynamic operation is performed. For example, for the statement
    ///     sampleObject.SampleMethod(100), where sampleObject is an instance of the class derived from the
    ///     <see cref="T:System.Dynamic.DynamicObject" /> class, binder.Name returns "SampleMethod". The binder.IgnoreCase
    ///     property specifies whether the member name is case-sensitive.
    /// </param>
    /// <param name="args">
    ///     The arguments that are passed to the object member during the invoke operation. For example, for the
    ///     statement sampleObject.SampleMethod(100), where sampleObject is derived from the
    ///     <see cref="T:System.Dynamic.DynamicObject" /> class, <paramref name="args[0]" /> is equal to 100.
    /// </param>
    /// <param name="result">The result of the member invocation.</param>
    public virtual bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
    {
        result = null;
        return false;
    }

    /// <summary>
    ///     Provides the implementation for operations that set a value by index. Classes derived from the
    ///     <see cref="T:System.Dynamic.DynamicObject" /> class can override this method to specify dynamic behavior for
    ///     operations that access objects by a specified index.
    /// </summary>
    /// <returns>
    ///     true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of
    ///     the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.
    /// </returns>
    /// <param name="binder">Provides information about the operation. </param>
    /// <param name="indexes">
    ///     The indexes that are used in the operation. For example, for the sampleObject[3] = 10 operation
    ///     in C# (sampleObject(3) = 10 in Visual Basic), where sampleObject is derived from the
    ///     <see cref="T:System.Dynamic.DynamicObject" /> class, <paramref name="indexes[0]" /> is equal to 3.
    /// </param>
    /// <param name="value">
    ///     The value to set to the object that has the specified index. For example, for the sampleObject[3] =
    ///     10 operation in C# (sampleObject(3) = 10 in Visual Basic), where sampleObject is derived from the
    ///     <see cref="T:System.Dynamic.DynamicObject" /> class, <paramref name="value" /> is equal to 10.
    /// </param>
    public virtual bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
    {
        return false;
    }

    /// <summary>
    ///     Provides the implementation for operations that set member values. Classes derived from the
    ///     <see cref="T:System.Dynamic.DynamicObject" /> class can override this method to specify dynamic behavior for
    ///     operations such as setting a value for a property.
    /// </summary>
    /// <returns>
    ///     true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of
    ///     the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
    /// </returns>
    /// <param name="binder">
    ///     Provides information about the object that called the dynamic operation. The binder.Name property
    ///     provides the name of the member to which the value is being assigned. For example, for the statement
    ///     sampleObject.SampleProperty = "Test", where sampleObject is an instance of the class derived from the
    ///     <see cref="T:System.Dynamic.DynamicObject" /> class, binder.Name returns "SampleProperty". The binder.IgnoreCase
    ///     property specifies whether the member name is case-sensitive.
    /// </param>
    /// <param name="value">
    ///     The value to set to the member. For example, for sampleObject.SampleProperty = "Test", where
    ///     sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject" /> class, the
    ///     <paramref name="value" /> is "Test".
    /// </param>
    public virtual bool TrySetMember(SetMemberBinder binder, object value)
    {
        return false;
    }

    /// <summary>
    ///     Provides implementation for unary operations. Classes derived from the
    ///     <see cref="T:System.Dynamic.DynamicObject" /> class can override this method to specify dynamic behavior for
    ///     operations such as negation, increment, or decrement.
    /// </summary>
    /// <returns>
    ///     true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of
    ///     the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
    /// </returns>
    /// <param name="binder">
    ///     Provides information about the unary operation. The binder.Operation property returns an
    ///     <see cref="T:System.Linq.Expressions.ExpressionType" /> object. For example, for the negativeNumber = -number
    ///     statement, where number is derived from the DynamicObject class, binder.Operation returns "Negate".
    /// </param>
    /// <param name="result">The result of the unary operation.</param>
    public virtual bool TryUnaryOperation(UnaryOperationBinder binder, out object result)
    {
        result = null;
        return false;
    }

    private sealed class MetaDynamic : DynamicMetaObject
    {
        private static readonly Expression[] NoArgs = new Expression[0];

        internal MetaDynamic(Expression expression, DynamicObject value)
            : base(expression, BindingRestrictions.Empty, value)
        {
        }

        private new DynamicObject Value => (DynamicObject)base.Value;

        public override DynamicMetaObject BindBinaryOperation(
            BinaryOperationBinder binder,
            DynamicMetaObject arg)
        {
            if (!IsOverridden("TryBinaryOperation"))
            {
                return base.BindBinaryOperation(binder, arg);
            }

            return CallMethodWithResult("TryBinaryOperation", binder, GetExpressions(new DynamicMetaObject[1]
            {
                arg
            }), e => binder.FallbackBinaryOperation(this, arg, e));
        }

        public override DynamicMetaObject BindConvert(ConvertBinder binder)
        {
            return IsOverridden("TryConvert")
                ? CallMethodWithResult("TryConvert", binder, NoArgs, e => binder.FallbackConvert(this, e))
                : base.BindConvert(binder);
        }

        public override DynamicMetaObject BindCreateInstance(
            CreateInstanceBinder binder,
            DynamicMetaObject[] args)
        {
            return IsOverridden("TryCreateInstance")
                ? CallMethodWithResult("TryCreateInstance", binder, GetExpressions(args),
                    e => binder.FallbackCreateInstance(this, args, e))
                : base.BindCreateInstance(binder, args);
        }

        public override DynamicMetaObject BindDeleteIndex(
            DeleteIndexBinder binder,
            DynamicMetaObject[] indexes)
        {
            return IsOverridden("TryDeleteIndex")
                ? CallMethodNoResult("TryDeleteIndex", binder, GetExpressions(indexes),
                    e => binder.FallbackDeleteIndex(this, indexes, e))
                : base.BindDeleteIndex(binder, indexes);
        }

        public override DynamicMetaObject BindDeleteMember(DeleteMemberBinder binder)
        {
            return IsOverridden("TryDeleteMember")
                ? CallMethodNoResult("TryDeleteMember", binder, NoArgs, e => binder.FallbackDeleteMember(this, e))
                : base.BindDeleteMember(binder);
        }

        public override DynamicMetaObject BindGetIndex(
            GetIndexBinder binder,
            DynamicMetaObject[] indexes)
        {
            return IsOverridden("TryGetIndex")
                ? CallMethodWithResult("TryGetIndex", binder, GetExpressions(indexes),
                    e => binder.FallbackGetIndex(this, indexes, e))
                : base.BindGetIndex(binder, indexes);
        }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            return IsOverridden("TryGetMember")
                ? CallMethodWithResult("TryGetMember", binder, NoArgs, e => binder.FallbackGetMember(this, e))
                : base.BindGetMember(binder);
        }

        public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
        {
            return IsOverridden("TryInvoke")
                ? CallMethodWithResult("TryInvoke", binder, GetExpressions(args),
                    e => binder.FallbackInvoke(this, args, e))
                : base.BindInvoke(binder, args);
        }

        public override DynamicMetaObject BindInvokeMember(
            InvokeMemberBinder binder,
            DynamicMetaObject[] args)
        {
            var fallback = (Fallback)(e => binder.FallbackInvokeMember(this, args, e));
            var errorSuggestion = BuildCallMethodWithResult("TryInvokeMember", binder, GetExpressions(args),
                BuildCallMethodWithResult("TryGetMember", new GetBinderAdapter(binder), NoArgs, fallback(null),
                    e => binder.FallbackInvoke(e, args, null)), null);
            return fallback(errorSuggestion);
        }

        public override DynamicMetaObject BindSetIndex(
            SetIndexBinder binder,
            DynamicMetaObject[] indexes,
            DynamicMetaObject value)
        {
            return IsOverridden("TrySetIndex")
                ? CallMethodReturnLast("TrySetIndex", binder, GetExpressions(indexes), value.Expression,
                    e => binder.FallbackSetIndex(this, indexes, value, e))
                : base.BindSetIndex(binder, indexes, value);
        }

        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
        {
            return IsOverridden("TrySetMember")
                ? CallMethodReturnLast("TrySetMember", binder, NoArgs, value.Expression,
                    e => binder.FallbackSetMember(this, value, e))
                : base.BindSetMember(binder, value);
        }

        public override DynamicMetaObject BindUnaryOperation(UnaryOperationBinder binder)
        {
            return IsOverridden("TryUnaryOperation")
                ? CallMethodWithResult("TryUnaryOperation", binder, NoArgs, e => binder.FallbackUnaryOperation(this, e))
                : base.BindUnaryOperation(binder);
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return Value.GetDynamicMemberNames();
        }

        private static Expression[] BuildCallArgs(
            DynamicMetaObjectBinder binder,
            Expression[] parameters,
            Expression arg0,
            Expression arg1)
        {
            return parameters != NoArgs ? arg1 == null
                    ? new Expression[2]
                    {
                        Constant(binder),
                        arg0
                    }
                    : new Expression[3]
                    {
                        Constant(binder),
                        arg0,
                        arg1
                    } :
                arg1 == null ? new Expression[1]
                {
                    Constant(binder)
                } : new Expression[2]
                {
                    Constant(binder),
                    arg1
                };
        }

        private DynamicMetaObject BuildCallMethodWithResult(
            string methodName,
            DynamicMetaObjectBinder binder,
            Expression[] args,
            DynamicMetaObject fallbackResult,
            Fallback fallbackInvoke)
        {
            if (!IsOverridden(methodName))
            {
                return fallbackResult;
            }

            var parameterExpression1 = Expression.Parameter(typeof(object), null);
            var parameterExpression2 = methodName != "TryBinaryOperation"
                ? Expression.Parameter(typeof(object[]), null)
                : Expression.Parameter(typeof(object), null);
            var convertedArgs = GetConvertedArgs(args);
            var errorSuggestion = new DynamicMetaObject(parameterExpression1, BindingRestrictions.Empty);
            if (binder.ReturnType != typeof(object))
            {
                var ifTrue = Expression.Convert(errorSuggestion.Expression, binder.ReturnType);
                var str = Strings.DynamicObjectResultNotAssignable("{0}", Value.GetType(), binder.GetType(),
                    binder.ReturnType);
                errorSuggestion = new DynamicMetaObject(Expression.Condition(
                        !binder.ReturnType.IsValueType || !(Nullable.GetUnderlyingType(binder.ReturnType) == null)
                            ? Expression.OrElse(Expression.Equal(errorSuggestion.Expression, Expression.Constant(null)),
                                Expression.TypeIs(errorSuggestion.Expression, binder.ReturnType))
                            : Expression.TypeIs(errorSuggestion.Expression, binder.ReturnType), ifTrue,
                        Expression.Throw(
                            Expression.New(typeof(InvalidCastException).GetConstructor(new Type[1]
                            {
                                typeof(string)
                            }), Expression.Call(typeof(string).GetMethod("Format", new Type[2]
                                {
                                    typeof(string),
                                    typeof(object[])
                                }), Expression.Constant(str),
                                Expression.NewArrayInit(typeof(object),
                                    Expression.Condition(
                                        Expression.Equal(errorSuggestion.Expression, Expression.Constant(null)),
                                        Expression.Constant("null"),
                                        Expression.Call(errorSuggestion.Expression,
                                            typeof(object).GetMethod("GetType")),
                                        typeof(object))))), binder.ReturnType), binder.ReturnType),
                    errorSuggestion.Restrictions);
            }

            if (fallbackInvoke != null)
            {
                errorSuggestion = fallbackInvoke(errorSuggestion);
            }

            return new DynamicMetaObject(Expression.Block(new ParameterExpression[2]
                    {
                        parameterExpression1,
                        parameterExpression2
                    },
                    methodName != "TryBinaryOperation"
                        ? Expression.Assign(parameterExpression2,
                            Expression.NewArrayInit(typeof(object), convertedArgs))
                        : (Expression)Expression.Assign(parameterExpression2, convertedArgs[0]),
                    Expression.Condition(
                        Expression.Call(GetLimitedSelf(), typeof(DynamicObject).GetMethod(methodName),
                            BuildCallArgs(binder, args, parameterExpression2, parameterExpression1)),
                        Expression.Block(
                            methodName != "TryBinaryOperation"
                                ? ReferenceArgAssign(parameterExpression2, args)
                                : Expression.Empty(), errorSuggestion.Expression), fallbackResult.Expression,
                        binder.ReturnType)),
                GetRestrictions().Merge(errorSuggestion.Restrictions).Merge(fallbackResult.Restrictions));
        }

        private DynamicMetaObject CallMethodNoResult(
            string methodName,
            DynamicMetaObjectBinder binder,
            Expression[] args,
            Fallback fallback)
        {
            var dynamicMetaObject = fallback(null);
            var parameterExpression = Expression.Parameter(typeof(object[]), null);
            var convertedArgs = GetConvertedArgs(args);
            var errorSuggestion = new DynamicMetaObject(Expression.Block(new ParameterExpression[1]
                {
                    parameterExpression
                }, Expression.Assign(parameterExpression, Expression.NewArrayInit(typeof(object), convertedArgs)),
                Expression.Condition(
                    Expression.Call(GetLimitedSelf(), typeof(DynamicObject).GetMethod(methodName),
                        BuildCallArgs(binder, args, parameterExpression, null)),
                    Expression.Block(ReferenceArgAssign(parameterExpression, args), Expression.Empty()),
                    dynamicMetaObject.Expression, typeof(void))),
            GetRestrictions().Merge(dynamicMetaObject.Restrictions));
            return fallback(errorSuggestion);
        }

        private DynamicMetaObject CallMethodReturnLast(
            string methodName,
            DynamicMetaObjectBinder binder,
            Expression[] args,
            Expression value,
            Fallback fallback)
        {
            var dynamicMetaObject = fallback(null);
            var left = Expression.Parameter(typeof(object), null);
            var parameterExpression = Expression.Parameter(typeof(object[]), null);
            var convertedArgs = GetConvertedArgs(args);
            var errorSuggestion = new DynamicMetaObject(Expression.Block(new ParameterExpression[2]
                {
                    left,
                    parameterExpression
                }, Expression.Assign(parameterExpression, Expression.NewArrayInit(typeof(object), convertedArgs)),
                Expression.Condition(
                    Expression.Call(GetLimitedSelf(), typeof(DynamicObject).GetMethod(methodName),
                        BuildCallArgs(binder, args, parameterExpression,
                            Expression.Assign(left, Expression.Convert(value, typeof(object))))),
                    Expression.Block(ReferenceArgAssign(parameterExpression, args), left), dynamicMetaObject.Expression,
                    typeof(object))), GetRestrictions().Merge(dynamicMetaObject.Restrictions));
            return fallback(errorSuggestion);
        }

        private DynamicMetaObject CallMethodWithResult(
            string methodName,
            DynamicMetaObjectBinder binder,
            Expression[] args,
            Fallback fallback)
        {
            return CallMethodWithResult(methodName, binder, args, fallback, null);
        }

        private DynamicMetaObject CallMethodWithResult(
            string methodName,
            DynamicMetaObjectBinder binder,
            Expression[] args,
            Fallback fallback,
            Fallback fallbackInvoke)
        {
            var fallbackResult = fallback(null);
            var errorSuggestion = BuildCallMethodWithResult(methodName, binder, args, fallbackResult, fallbackInvoke);
            return fallback(errorSuggestion);
        }

        private static ConstantExpression Constant(DynamicMetaObjectBinder binder)
        {
            var type = binder.GetType();
            while (!type.IsVisible)
            {
                type = type.BaseType;
            }

            return Expression.Constant(binder, type);
        }

        private static Expression[] GetConvertedArgs(params Expression[] args)
        {
            var collectionBuilder = new ReadOnlyCollectionBuilder<Expression>(args.Length);
            for (var index = 0; index < args.Length; ++index)
            {
                collectionBuilder.Add(Expression.Convert(args[index], typeof(object)));
            }

            return collectionBuilder.ToArray();
        }

        private Expression GetLimitedSelf()
        {
            return TypeUtils.AreEquivalent(Expression.Type, typeof(DynamicObject))
                ? Expression
                : Expression.Convert(Expression, typeof(DynamicObject));
        }

        private BindingRestrictions GetRestrictions()
        {
            return BindingRestrictions.GetTypeRestriction(this);
        }

        private bool IsOverridden(string method)
        {
            foreach (MethodInfo methodInfo in Value.GetType()
                         .GetMember(method, MemberTypes.Method, BindingFlags.Instance | BindingFlags.Public))
            {
                if (methodInfo.DeclaringType != typeof(DynamicObject) &&
                    methodInfo.GetBaseDefinition().DeclaringType == typeof(DynamicObject))
                {
                    return true;
                }
            }

            return false;
        }

        private static Expression ReferenceArgAssign(Expression callArgs, Expression[] args)
        {
            var collectionBuilder = (ReadOnlyCollectionBuilder<Expression>)null;
            for (var index = 0; index < args.Length; ++index)
            {
                ContractUtils.Requires(args[index] is ParameterExpression);
                if (((ParameterExpression)args[index]).IsByRef)
                {
                    if (collectionBuilder == null)
                    {
                        collectionBuilder = new ReadOnlyCollectionBuilder<Expression>();
                    }

                    collectionBuilder.Add(Expression.Assign(args[index],
                        Expression.Convert(Expression.ArrayIndex(callArgs, Expression.Constant(index)),
                            args[index].Type)));
                }
            }

            return collectionBuilder != null ? Expression.Block(collectionBuilder) : Expression.Empty();
        }

        private delegate DynamicMetaObject Fallback(DynamicMetaObject errorSuggestion);

        private sealed class GetBinderAdapter : GetMemberBinder
        {
            internal GetBinderAdapter(InvokeMemberBinder binder)
                : base(binder.Name, binder.IgnoreCase)
            {
            }

            public override DynamicMetaObject FallbackGetMember(
                DynamicMetaObject target,
                DynamicMetaObject errorSuggestion)
            {
                throw new NotSupportedException();
            }
        }
    }
}