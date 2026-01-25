using System.Linq.Expressions;

#nullable disable
namespace System.Linq.Expressions
{
    // Minimal fallback meta-object that other COM-related metaobjects derive from.
    internal class ComFallbackMetaObject : DynamicMetaObject
    {
        internal ComFallbackMetaObject(Expression expression, BindingRestrictions restrictions, object value)
            : base(expression, restrictions, value)
        {
        }

        // Derived types (e.g. IDispatchMetaObject) override this to return an "unwrapped" meta-object.
        protected virtual ComUnwrappedMetaObject UnwrapSelf()
        {
            // Default minimal implementation: return null (derived classes override).
            return null;
        }

        public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
        {
            return null;
        }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            return null;
        }

        public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
        {
            return null;
        }

        public override DynamicMetaObject BindSetIndex(SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value)
        {
            return null;
        }

        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
        {
            return null;
        }
    }

}