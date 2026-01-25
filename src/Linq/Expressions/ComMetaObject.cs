using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions
{
    internal class ComMetaObject : DynamicMetaObject
    {
        internal ComMetaObject(Expression expression, BindingRestrictions restrictions, object arg) : base(expression, restrictions, arg)
        {
        }

        public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
        {
            ContractUtils.RequiresNotNull(binder, "binder");
            return binder.Defer(this.WrapSelf(), indexes);
        }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            ContractUtils.RequiresNotNull(binder, "binder");
            DynamicMetaObject dynamicMetaObject = binder.Defer(this.WrapSelf(), new DynamicMetaObject[0]);
            return dynamicMetaObject;
        }

        public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
        {
            ContractUtils.RequiresNotNull(binder, "binder");
            DynamicMetaObject dynamicMetaObject = binder.Defer(args.AddFirst<DynamicMetaObject>(this.WrapSelf()));
            return dynamicMetaObject;
        }

        public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
        {
            ContractUtils.RequiresNotNull(binder, "binder");
            DynamicMetaObject dynamicMetaObject = binder.Defer(args.AddFirst<DynamicMetaObject>(this.WrapSelf()));
            return dynamicMetaObject;
        }

        public override DynamicMetaObject BindSetIndex(SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value)
        {
            ContractUtils.RequiresNotNull(binder, "binder");
            DynamicMetaObject dynamicMetaObject = binder.Defer(this.WrapSelf(), indexes.AddLast<DynamicMetaObject>(value));
            return dynamicMetaObject;
        }

        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
        {
            ContractUtils.RequiresNotNull(binder, "binder");
            return binder.Defer(this.WrapSelf(), new DynamicMetaObject[] { value });
        }

        private DynamicMetaObject WrapSelf()
        {
            DynamicMetaObject dynamicMetaObject = new DynamicMetaObject(ComObject.RcwToComObject(base.Expression), BindingRestrictions.GetExpressionRestriction(Expression.Call(typeof(ComObject).GetMethod("IsComObject", BindingFlags.Static | BindingFlags.NonPublic), Helpers.Convert(base.Expression, typeof(object)))));
            return dynamicMetaObject;
        }
    }
}