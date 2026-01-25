using System;
using System.Dynamic;
using System.Runtime.CompilerServices;

#nullable disable
namespace System.Linq.Expressions
{
    // Minimal GetMemberBinder implementation for .NET 3.5 compatibility.
    //
    // Provides:
    // - Name, IgnoreCase
    // - abstract FallbackGetMember for binders to implement
    // - Defer helpers used by the COM shim (simple implementation that calls FallbackGetMember)
    public abstract class GetMemberBinder: DynamicMetaObjectBinder
    {
        protected GetMemberBinder(string name, bool ignoreCase)
        {
            if (name == null) throw new ArgumentNullException("name");
            this.Name = name;
            this.IgnoreCase = ignoreCase;
        }

        public string Name { get; private set; }
        public bool IgnoreCase { get; private set; }

        // Binder implementations must provide this to produce binding result.
        public abstract DynamicMetaObject
            FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion);

        // Defer helpers: minimal behavior used by the COM-related code in this project.
        // They simply delegate to the FallbackGetMember with no error suggestion.
        public virtual DynamicMetaObject Defer(DynamicMetaObject target)
        {
            if (target == null) throw new ArgumentNullException("target");
            return this.FallbackGetMember(target, null);
        }

        /// <summary>Defers the binding of the operation until later time when the runtime values of all dynamic operation arguments have been computed.</summary>
        /// <returns>The <see cref="T:DynamicMetaObject" /> representing the result of the binding.</returns>
        /// <param name="target">The target of the dynamic operation.</param>
        /// <param name="args">An array of arguments of the dynamic operation.</param>
        public DynamicMetaObject Defer(DynamicMetaObject target, params DynamicMetaObject[] args)
        {
            ContractUtils.RequiresNotNull(target, "target");
            if (args == null)
            {
                return this.MakeDeferred(target.Restrictions, new DynamicMetaObject[] { target });
            }
            return this.MakeDeferred(target.Restrictions.Merge(BindingRestrictions.Combine(args)), args.AddFirst<DynamicMetaObject>(target));
        }
        public virtual DynamicMetaObject Defer(params DynamicMetaObject[] args)
        {
            if (args == null || args.Length == 0) throw new ArgumentException("args");
            return this.Defer(args[0]);
        }

        // Default object equality/hash behavior is sufficient for most usage in this port.
        public override bool Equals(object obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();
        private DynamicMetaObject MakeDeferred(BindingRestrictions rs, params DynamicMetaObject[] args)
        {
            Expression[] expressions = DynamicMetaObject.GetExpressions(args);
            Type type = DelegateHelpers.MakeDeferredSiteDelegate(args, this.ReturnType);
            return new DynamicMetaObject(DynamicExpression.Make(this.ReturnType, type, this, new TrueReadOnlyCollection<Expression>(expressions)), rs);
        }
    }
}