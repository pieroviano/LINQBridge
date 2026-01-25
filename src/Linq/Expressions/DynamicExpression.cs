using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions
{
    /// <summary>Represents a dynamic operation.</summary>
    [__DynamicallyInvokable]
    public class DynamicExpression : Expression, IDynamicExpression, IArgumentProvider
    {
        private readonly CallSiteBinder _binder;

        private readonly Type _delegateType;

        /// <summary>Gets the arguments to the dynamic operation.</summary>
        /// <returns>The read-only collections containing the arguments to the dynamic operation.</returns>
        [__DynamicallyInvokable]
        public ReadOnlyCollection<Expression> Arguments
        {
            [__DynamicallyInvokable]
            get
            {
                return this.GetOrMakeArguments();
            }
        }

        /// <summary>Gets the <see cref="T:System.Runtime.CompilerServices.CallSiteBinder" />, which determines the runtime behavior of the dynamic site.</summary>
        /// <returns>The <see cref="T:System.Runtime.CompilerServices.CallSiteBinder" />, which determines the runtime behavior of the dynamic site.</returns>
        [__DynamicallyInvokable]
        public CallSiteBinder Binder
        {
            [__DynamicallyInvokable]
            get
            {
                return this._binder;
            }
        }

        /// <summary>Gets the type of the delegate used by the <see cref="T:System.Runtime.CompilerServices.CallSite" />.</summary>
        /// <returns>The <see cref="T:System.Type" /> object representing the type of the delegate used by the <see cref="T:System.Runtime.CompilerServices.CallSite" />.</returns>
        [__DynamicallyInvokable]
        public Type DelegateType
        {
            [__DynamicallyInvokable]
            get
            {
                return this._delegateType;
            }
        }

        /// <summary>Returns the node type of this expression. Extension nodes should return <see cref="F:System.Linq.Expressions.ExpressionType.Extension" /> when overriding this method.</summary>
        /// <returns>The <see cref="T:System.Linq.Expressions.ExpressionType" /> of the expression.</returns>
        [__DynamicallyInvokable]
        public sealed override ExpressionType NodeType
        {
            [__DynamicallyInvokable]
            get
            {
                return ExpressionType.Dynamic;
            }
        }

        [__DynamicallyInvokable]
        int System.Linq.Expressions.IArgumentProvider.ArgumentCount
        {
            [__DynamicallyInvokable]
            get
            {
                throw ContractUtils.Unreachable;
            }
        }

        /// <summary>Gets the static type of the expression that this <see cref="T:System.Linq.Expressions.Expression" /> represents.</summary>
        /// <returns>The <see cref="P:System.Linq.Expressions.DynamicExpression.Type" /> that represents the static type of the expression.</returns>
        [__DynamicallyInvokable]
        public override Type Type
        {
            [__DynamicallyInvokable]
            get
            {
                return typeof(object);
            }
        }

        internal DynamicExpression(Type delegateType, CallSiteBinder binder)
        {
            this._delegateType = delegateType;
            this._binder = binder;
        }

        /// <summary>Dispatches to the specific visit method for this node type. For example, <see cref="T:System.Linq.Expressions.MethodCallExpression" /> calls the <see cref="M:System.Linq.Expressions.ExpressionVisitor.VisitMethodCall(System.Linq.Expressions.MethodCallExpression)" />.</summary>
        /// <returns>The result of visiting this node.</returns>
        /// <param name="visitor">The visitor to visit this node with.</param>
        [__DynamicallyInvokable]
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitDynamic(this);
        }

        [__DynamicallyInvokable]
        public static new DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, params Expression[] arguments)
        {
            return Expression.Dynamic(binder, returnType, arguments);
        }

        [__DynamicallyInvokable]
        public static new DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, IEnumerable<Expression> arguments)
        {
            return Expression.Dynamic(binder, returnType, arguments);
        }

        [__DynamicallyInvokable]
        public static new DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, Expression arg0)
        {
            return Expression.Dynamic(binder, returnType, arg0);
        }

        [__DynamicallyInvokable]
        public static new DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, Expression arg0, Expression arg1)
        {
            return Expression.Dynamic(binder, returnType, arg0, arg1);
        }

        [__DynamicallyInvokable]
        public static new DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, Expression arg0, Expression arg1, Expression arg2)
        {
            return Expression.Dynamic(binder, returnType, arg0, arg1, arg2);
        }

        [__DynamicallyInvokable]
        public static new DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, Expression arg0, Expression arg1, Expression arg2, Expression arg3)
        {
            return Expression.Dynamic(binder, returnType, arg0, arg1, arg2, arg3);
        }

        internal virtual ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            throw ContractUtils.Unreachable;
        }

        internal static DynamicExpression Make(Type returnType, Type delegateType, CallSiteBinder binder, ReadOnlyCollection<Expression> arguments)
        {
            if (returnType == typeof(object))
            {
                return new DynamicExpressionN(delegateType, binder, arguments);
            }
            return new TypedDynamicExpressionN(returnType, delegateType, binder, arguments);
        }

        internal static DynamicExpression Make(Type returnType, Type delegateType, CallSiteBinder binder, Expression arg0)
        {
            if (returnType == typeof(object))
            {
                return new DynamicExpression1(delegateType, binder, arg0);
            }
            return new TypedDynamicExpression1(returnType, delegateType, binder, arg0);
        }

        internal static DynamicExpression Make(Type returnType, Type delegateType, CallSiteBinder binder, Expression arg0, Expression arg1)
        {
            if (returnType == typeof(object))
            {
                return new DynamicExpression2(delegateType, binder, arg0, arg1);
            }
            return new TypedDynamicExpression2(returnType, delegateType, binder, arg0, arg1);
        }

        internal static DynamicExpression Make(Type returnType, Type delegateType, CallSiteBinder binder, Expression arg0, Expression arg1, Expression arg2)
        {
            if (returnType == typeof(object))
            {
                return new DynamicExpression3(delegateType, binder, arg0, arg1, arg2);
            }
            return new TypedDynamicExpression3(returnType, delegateType, binder, arg0, arg1, arg2);
        }

        internal static DynamicExpression Make(Type returnType, Type delegateType, CallSiteBinder binder, Expression arg0, Expression arg1, Expression arg2, Expression arg3)
        {
            if (returnType == typeof(object))
            {
                return new DynamicExpression4(delegateType, binder, arg0, arg1, arg2, arg3);
            }
            return new TypedDynamicExpression4(returnType, delegateType, binder, arg0, arg1, arg2, arg3);
        }

        [__DynamicallyInvokable]
        public static new DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, IEnumerable<Expression> arguments)
        {
            return Expression.MakeDynamic(delegateType, binder, arguments);
        }

        [__DynamicallyInvokable]
        public static new DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, params Expression[] arguments)
        {
            return Expression.MakeDynamic(delegateType, binder, arguments);
        }

        [__DynamicallyInvokable]
        public static new DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, Expression arg0)
        {
            return Expression.MakeDynamic(delegateType, binder, arg0);
        }

        [__DynamicallyInvokable]
        public static new DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, Expression arg0, Expression arg1)
        {
            return Expression.MakeDynamic(delegateType, binder, arg0, arg1);
        }

        [__DynamicallyInvokable]
        public static new DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, Expression arg0, Expression arg1, Expression arg2)
        {
            return Expression.MakeDynamic(delegateType, binder, arg0, arg1, arg2);
        }

        [__DynamicallyInvokable]
        public static new DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, Expression arg0, Expression arg1, Expression arg2, Expression arg3)
        {
            return Expression.MakeDynamic(delegateType, binder, arg0, arg1, arg2, arg3);
        }

        internal virtual DynamicExpression Rewrite(Expression[] args)
        {
            throw ContractUtils.Unreachable;
        }

        [__DynamicallyInvokable]
        Expression System.Linq.Expressions.IArgumentProvider.GetArgument(int index)
        {
            throw ContractUtils.Unreachable;
        }

        [__DynamicallyInvokable]
        object System.Linq.Expressions.IDynamicExpression.CreateCallSite()
        {
            return CallSite.Create(this.DelegateType, this.Binder);
        }

        [__DynamicallyInvokable]
        Expression System.Linq.Expressions.IDynamicExpression.Rewrite(Expression[] args)
        {
            return this.Rewrite(args);
        }

        /// <summary>Creates a new expression that is like this one, but using the supplied children. If all of the children are the same, it will return this expression.</summary>
        /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
        /// <param name="arguments">The <see cref="P:System.Linq.Expressions.DynamicExpression.Arguments" /> property of the result.</param>
        [__DynamicallyInvokable]
        public DynamicExpression Update(IEnumerable<Expression> arguments)
        {
            if (arguments == this.Arguments)
            {
                return this;
            }
            return Expression.MakeDynamic(this.DelegateType, this.Binder, arguments);
        }
    }
}