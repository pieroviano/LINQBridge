using System;
using System.Runtime.CompilerServices;

#nullable disable
namespace System.Linq.Expressions
{
    // Minimal SetMemberBinder implementation for .NET 3.5 compatibility.
    //
    // Provides:
    // - Name, IgnoreCase
    // - abstract FallbackSetMember for binders to implement
    // - Defer helpers used by the COM shim (simple implementation that calls FallbackSetMember)
    public abstract class SetMemberBinder
    {
        protected SetMemberBinder(string name, bool ignoreCase)
        {
            if (name == null) throw new ArgumentNullException("name");
            this.Name = name;
            this.IgnoreCase = ignoreCase;
        }

        public virtual Type ReturnType
        {
            [__DynamicallyInvokable]
            get
            {
                return typeof(object);
            }
        }

        public string Name { get; private set; }
        public bool IgnoreCase { get; private set; }

        // Binder implementations must provide this to produce binding result.
        public abstract DynamicMetaObject FallbackSetMember(DynamicMetaObject target, DynamicMetaObject value, DynamicMetaObject errorSuggestion);

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

        private DynamicMetaObject MakeDeferred(BindingRestrictions rs, params DynamicMetaObject[] args)
        {
            Expression[] expressions = DynamicMetaObject.GetExpressions(args);
            Type type = DelegateHelpers.MakeDeferredSiteDelegate(args, this.ReturnType);
            return new DynamicMetaObject(DynamicExpression.Make(this.ReturnType, type, this, new TrueReadOnlyCollection<Expression>(expressions)), rs);
        }

        // Defer helpers: minimal behavior used by the COM-related code in this project.
        // They simply delegate to the FallbackSetMember with no error suggestion.
        public virtual DynamicMetaObject Defer(DynamicMetaObject target, DynamicMetaObject value)
        {
            if (target == null) throw new ArgumentNullException("target");
            if (value == null) throw new ArgumentNullException("value");
            return this.FallbackSetMember(target, value, null);
        }

        public virtual DynamicMetaObject Defer(params DynamicMetaObject[] args)
        {
            if (args == null || args.Length < 2) throw new ArgumentException("args");
            return this.Defer(args[0], args[1]);
        }

        // Default object equality/hash behavior is sufficient for most usage in this port.
        public override bool Equals(object obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();
    }

    public static class ArrayExtension
    {
        internal static T[] RemoveLast<T>(this T[] array)
        {
            T[] tArray = new T[(int)array.Length - 1];
            Array.Copy(array, 0, tArray, 0, (int)tArray.Length);
            return tArray;
        }
    }
}