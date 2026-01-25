using System;
using System.Dynamic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;

#nullable disable
namespace System.Linq.Expressions
{
    // Minimal DynamicMetaObject emulation for .NET 3.5
    public class DynamicMetaObject
    {
        private readonly object _value;
        private readonly bool _hasValue;

        public Expression Expression { get; private set; }
        public BindingRestrictions Restrictions { get; private set; }

        // Whether a concrete runtime value was supplied to this meta-object.
        public bool HasValue => _hasValue;

        // The value if one was supplied via the constructor; otherwise null.
        public object Value => _value;

        // The "limit" type the binder can assume for this object.
        // If a value was supplied, prefer its actual runtime type; otherwise use the expression's type.
        public Type LimitType
        {
            get
            {
                if (_hasValue)
                {
                    return _value != null ? _value.GetType() : typeof(object);
                }
                return this.Expression != null ? this.Expression.Type : typeof(object);
            }
        }

        // Empty array convenience used throughout the codebase
        public static readonly DynamicMetaObject[] EmptyMetaObjects = new DynamicMetaObject[0];

        // Primary constructors used in your codebase:
        public DynamicMetaObject(Expression expression, BindingRestrictions restrictions)
        {
            if (expression == null) throw new ArgumentNullException("expression");
            this.Expression = expression;
            this.Restrictions = restrictions ?? BindingRestrictions.Empty;
            this._value = null;
            this._hasValue = false;
        }

        public DynamicMetaObject(Expression expression, BindingRestrictions restrictions, object value)
        {
            if (expression == null) throw new ArgumentNullException("expression");
            this.Expression = expression;
            this.Restrictions = restrictions ?? BindingRestrictions.Empty;
            this._value = value;
            this._hasValue = true;
        }

        // Default base bind implementations delegate to the binder's fallback.
        // Derived types commonly override these to provide specialized behavior.
        public virtual DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            if (binder == null) throw new ArgumentNullException("binder");
            return binder.FallbackGetMember(this, null);
        }

        public virtual DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
        {
            if (binder == null) throw new ArgumentNullException("binder");
            return binder.FallbackSetMember(this, value, null);
        }

        public virtual DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
        {
            if (binder == null) throw new ArgumentNullException("binder");
            return binder.FallbackInvoke(this, args, null);
        }

        public virtual DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
        {
            if (binder == null) throw new ArgumentNullException("binder");
            return binder.FallbackInvokeMember(this, args, null);
        }

        public virtual DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
        {
            if (binder == null) throw new ArgumentNullException("binder");
            return binder.FallbackGetIndex(this, indexes, null);
        }

        public virtual DynamicMetaObject BindSetIndex(SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value)
        {
            if (binder == null) throw new ArgumentNullException("binder");
            return binder.FallbackSetIndex(this, indexes, value, null);
        }

        // Provide a simple ToString useful in debugging
        public override string ToString()
        {
            return string.Format("DynamicMetaObject(Expression: {0}, LimitType: {1}, HasValue: {2})", this.Expression, this.LimitType, this.HasValue);
        }

        /// <summary>Creates a meta-object for the specified object.</summary>
        /// <returns>If the given object implements <see cref="T:IDynamicMetaObjectProvider" /> and is not a remote object from outside the current AppDomain, returns the object's specific meta-object returned by <see cref="M:IDynamicMetaObjectProvider.GetMetaObject(System.Linq.Expressions.Expression)" />. Otherwise a plain new meta-object with no restrictions is created and returned.</returns>
        /// <param name="value">The object to get a meta-object for.</param>
        /// <param name="expression">The expression representing this <see cref="T:DynamicMetaObject" /> during the dynamic binding process.</param>
        [__DynamicallyInvokable]
        public static DynamicMetaObject Create(object value, Expression expression)
        {
            ContractUtils.RequiresNotNull(expression, "expression");
            IDynamicMetaObjectProvider dynamicMetaObjectProvider = value as IDynamicMetaObjectProvider;
            if (dynamicMetaObjectProvider == null || RemotingServices.IsObjectOutOfAppDomain(value))
            {
                return new DynamicMetaObject(expression, BindingRestrictions.Empty, value);
            }
            DynamicMetaObject metaObject = dynamicMetaObjectProvider.GetMetaObject(expression);
            if (metaObject == null || !metaObject.HasValue || metaObject.Value == null || metaObject.Expression != expression)
            {
                throw Error.InvalidMetaObjectCreated(dynamicMetaObjectProvider.GetType());
            }
            return metaObject;
        }

        internal static Expression[] GetExpressions(DynamicMetaObject[] objects)
        {
            ContractUtils.RequiresNotNull(objects, "objects");
            Expression[] expressionArray = new Expression[(int)objects.Length];
            for (int i = 0; i < (int)objects.Length; i++)
            {
                DynamicMetaObject dynamicMetaObject = objects[i];
                ContractUtils.RequiresNotNull(dynamicMetaObject, "objects");
                Expression expression = dynamicMetaObject.Expression;
                ContractUtils.RequiresNotNull(expression, "objects");
                expressionArray[i] = expression;
            }
            return expressionArray;
        }
    }
}