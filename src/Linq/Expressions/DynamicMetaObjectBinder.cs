using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;

namespace System.Linq.Expressions
{
    /// <summary>The dynamic call site binder that participates in the <see cref="T:System.Dynamic.DynamicMetaObject" /> binding protocol.</summary>
    [__DynamicallyInvokable]
    public abstract class DynamicMetaObjectBinder : CallSiteBinder
    {
        private readonly static Type ComObjectType;

        internal virtual bool IsStandardBinder
        {
            get
            {
                return false;
            }
        }

        /// <summary>The result type of the operation.</summary>
        /// <returns>The <see cref="T:System.Type" /> object representing the result type of the operation.</returns>
        [__DynamicallyInvokable]
        public virtual Type ReturnType
        {
            [__DynamicallyInvokable]
            get
            {
                return typeof(object);
            }
        }

        static DynamicMetaObjectBinder()
        {
            DynamicMetaObjectBinder.ComObjectType = typeof(object).Assembly.GetType("System.__ComObject");
        }

        /// <summary>Initializes a new instance of the <see cref="T:System.Dynamic.DynamicMetaObjectBinder" /> class.</summary>
        [__DynamicallyInvokable]
        protected DynamicMetaObjectBinder()
        {
        }

        private static BindingRestrictions AddRemoteObjectRestrictions(BindingRestrictions restrictions, object[] args, ReadOnlyCollection<ParameterExpression> parameters)
        {
            BindingRestrictions bindingRestriction;
            for (int i = 0; i < parameters.Count; i++)
            {
                ParameterExpression item = parameters[i];
                MarshalByRefObject marshalByRefObject = args[i] as MarshalByRefObject;
                if (marshalByRefObject != null && !DynamicMetaObjectBinder.IsComObject(marshalByRefObject))
                {
                    bindingRestriction = (!RemotingServices.IsObjectOutOfAppDomain(marshalByRefObject) ? BindingRestrictions.GetExpressionRestriction(Expression.AndAlso(Expression.NotEqual(item, Expression.Constant(null)), Expression.Not(Expression.Call(typeof(RemotingServices).GetMethod("IsObjectOutOfAppDomain"), item)))) : BindingRestrictions.GetExpressionRestriction(Expression.AndAlso(Expression.NotEqual(item, Expression.Constant(null)), Expression.Call(typeof(RemotingServices).GetMethod("IsObjectOutOfAppDomain"), item))));
                    restrictions = restrictions.Merge(bindingRestriction);
                }
            }
            return restrictions;
        }

        /// <summary>Performs the runtime binding of the dynamic operation on a set of arguments.</summary>
        /// <returns>An Expression that performs tests on the dynamic operation arguments, and performs the dynamic operation if the tests are valid. If the tests fail on subsequent occurrences of the dynamic operation, Bind will be called again to produce a new <see cref="T:System.Linq.Expressions.Expression" /> for the new argument types.</returns>
        /// <param name="args">An array of arguments to the dynamic operation.</param>
        /// <param name="parameters">The array of <see cref="T:System.Linq.Expressions.ParameterExpression" /> instances that represent the parameters of the call site in the binding process.</param>
        /// <param name="returnLabel">A LabelTarget used to return the result of the dynamic binding.</param>
        [__DynamicallyInvokable]
        public override Expression Bind(object[] args, ReadOnlyCollection<ParameterExpression> parameters, LabelTarget returnLabel)
        {
            Type type;
            ContractUtils.RequiresNotNull(args, "args");
            ContractUtils.RequiresNotNull(parameters, "parameters");
            ContractUtils.RequiresNotNull(returnLabel, "returnLabel");
            if (args.Length == 0)
            {
                throw new IndexOutOfRangeException("args.Length");
            }
            if (parameters.Count == 0)
            {
                throw new IndexOutOfRangeException("parameters.Count");
            }
            if ((int)args.Length != parameters.Count)
            {
                throw new ArgumentOutOfRangeException("args");
            }
            if (!this.IsStandardBinder)
            {
                type = returnLabel.Type;
            }
            else
            {
                type = this.ReturnType;
                if (returnLabel.Type != typeof(void) && !TypeUtils.AreReferenceAssignable(returnLabel.Type, type))
                {
                    throw Error.BinderNotCompatibleWithCallSite(type, this, returnLabel.Type);
                }
            }
            DynamicMetaObject dynamicMetaObject = DynamicMetaObject.Create(args[0], parameters[0]);
            DynamicMetaObject[] dynamicMetaObjectArray = DynamicMetaObjectBinder.CreateArgumentMetaObjects(args, parameters);
            DynamicMetaObject dynamicMetaObject1 = this.Bind(dynamicMetaObject, dynamicMetaObjectArray);
            if (dynamicMetaObject1 == null)
            {
                throw Error.BindingCannotBeNull();
            }
            Expression expression = dynamicMetaObject1.Expression;
            BindingRestrictions restrictions = dynamicMetaObject1.Restrictions;
            if (type != typeof(void) && !TypeUtils.AreReferenceAssignable(type, expression.Type))
            {
                if (!(dynamicMetaObject.Value is IDynamicMetaObjectProvider))
                {
                    throw Error.DynamicBinderResultNotAssignable(expression.Type, this, type);
                }
                throw Error.DynamicObjectResultNotAssignable(expression.Type, dynamicMetaObject.Value.GetType(), this, type);
            }
            if (this.IsStandardBinder && args[0] is IDynamicMetaObjectProvider && restrictions == BindingRestrictions.Empty)
            {
                throw Error.DynamicBindingNeedsRestrictions(dynamicMetaObject.Value.GetType(), this);
            }
            restrictions = DynamicMetaObjectBinder.AddRemoteObjectRestrictions(restrictions, args, parameters);
            if (expression.NodeType != ExpressionType.Goto)
            {
                expression = Expression.Return(returnLabel, expression);
            }
            if (restrictions != BindingRestrictions.Empty)
            {
                expression = Expression.IfThen(restrictions.ToExpression(), expression);
            }
            return expression;
        }

        /// <summary>When overridden in the derived class, performs the binding of the dynamic operation.</summary>
        /// <returns>The <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
        /// <param name="target">The target of the dynamic operation.</param>
        /// <param name="args">An array of arguments of the dynamic operation.</param>
        [__DynamicallyInvokable]
        public abstract DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args);

       
        private static DynamicMetaObject[] CreateArgumentMetaObjects(object[] args, ReadOnlyCollection<ParameterExpression> parameters)
        {
            DynamicMetaObject[] emptyMetaObjects;
            if ((int)args.Length == 1)
            {
                emptyMetaObjects = DynamicMetaObject.EmptyMetaObjects;
            }
            else
            {
                emptyMetaObjects = new DynamicMetaObject[(int)args.Length - 1];
                for (int i = 1; i < (int)args.Length; i++)
                {
                    emptyMetaObjects[i - 1] = DynamicMetaObject.Create(args[i], parameters[i]);
                }
            }
            return emptyMetaObjects;
        }

        /// <summary>Defers the binding of the operation until later time when the runtime values of all dynamic operation arguments have been computed.</summary>
        /// <returns>The <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
        /// <param name="target">The target of the dynamic operation.</param>
        /// <param name="args">An array of arguments of the dynamic operation.</param>
        [__DynamicallyInvokable]
        public DynamicMetaObject Defer(DynamicMetaObject target, params DynamicMetaObject[] args)
        {
            ContractUtils.RequiresNotNull(target, "target");
            if (args == null)
            {
                return this.MakeDeferred(target.Restrictions, new DynamicMetaObject[] { target });
            }
            return this.MakeDeferred(target.Restrictions.Merge(BindingRestrictions.Combine(args)), args.AddFirst<DynamicMetaObject>(target));
        }

        /// <summary>Defers the binding of the operation until later time when the runtime values of all dynamic operation arguments have been computed.</summary>
        /// <returns>The <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
        /// <param name="args">An array of arguments of the dynamic operation.</param>
        [__DynamicallyInvokable]
        public DynamicMetaObject Defer(params DynamicMetaObject[] args)
        {
            return this.MakeDeferred(BindingRestrictions.Combine(args), args);
        }

        /// <summary>Gets an expression that will cause the binding to be updated. It indicates that the expression's binding is no longer valid. This is typically used when the "version" of a dynamic object has changed.</summary>
        /// <returns>The update expression.</returns>
        /// <param name="type">The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of the resulting expression; any type is allowed.</param>
        [__DynamicallyInvokable]
        public Expression GetUpdateExpression(Type type)
        {
            return Expression.Goto(CallSiteBinder.UpdateLabel, type);
        }

        private static bool IsComObject(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            return DynamicMetaObjectBinder.ComObjectType.IsAssignableFrom(obj.GetType());
        }

        private DynamicMetaObject MakeDeferred(BindingRestrictions rs, params DynamicMetaObject[] args)
        {
            Expression[] expressions = DynamicMetaObject.GetExpressions(args);
            Type type = DelegateHelpers.MakeDeferredSiteDelegate(args, this.ReturnType);
            return new DynamicMetaObject(DynamicExpression.Make(this.ReturnType, type, this, new TrueReadOnlyCollection<Expression>(expressions)), rs);
        }
    }
}