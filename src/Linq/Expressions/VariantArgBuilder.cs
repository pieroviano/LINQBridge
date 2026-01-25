using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System.Linq.Expressions
{
    internal class VariantArgBuilder : SimpleArgBuilder
    {
        private readonly bool _isWrapper;

        internal VariantArgBuilder(Type parameterType) : base(parameterType)
        {
            this._isWrapper = parameterType == typeof(VariantWrapper);
        }

        internal override Expression Marshal(Expression parameter)
        {
            parameter = base.Marshal(parameter);
            if (this._isWrapper)
            {
                parameter = Expression.Property(Helpers.Convert(parameter, typeof(VariantWrapper)), typeof(VariantWrapper).GetProperty("WrappedObject"));
            }
            return Helpers.Convert(parameter, typeof(object));
        }

        internal override Expression MarshalToRef(Expression parameter)
        {
            parameter = this.Marshal(parameter);
            Expression expression = Expression.Call(typeof(UnsafeMethods).GetMethod("GetVariantForObject", BindingFlags.Static | BindingFlags.NonPublic), parameter);
            return expression;
        }

        internal override Expression UnmarshalFromRef(Expression value)
        {
            Expression expression = Expression.Call(typeof(UnsafeMethods).GetMethod("GetObjectForVariant"), value);
            if (this._isWrapper)
            {
                expression = Expression.New(typeof(VariantWrapper).GetConstructor(new Type[] { typeof(object) }), new Expression[] { expression });
            }
            return base.UnmarshalFromRef(expression);
        }
    }
}