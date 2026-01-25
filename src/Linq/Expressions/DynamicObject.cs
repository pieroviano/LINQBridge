using System;
using System.Collections.Generic;
using System.Linq.Expressions;

#nullable disable
namespace System.Linq.Expressions
{
    // Minimal DynamicObject for .NET 3.5 compatibility.
    //
    // Provides:
    // - IDynamicMetaObjectProvider implementation (GetMetaObject)
    // - common Try* virtual hooks (returning false by default)
    // - GetDynamicMemberNames (empty by default)
    //
    // Note: This is a lightweight shim to enable the COM/dynamic code in this
    // repository to compile and run. It does not implement the full DLR
    // integration semantics of the .NET Framework's DynamicObject.
    public class DynamicObject : IDynamicMetaObjectProvider
    {
        public DynamicObject()
        {
        }

        // Return a simple DynamicMetaObject that carries this instance as the runtime value.
        // Many binders in this codebase will call back into the DynamicObject instance
        // by obtaining the Value from the meta-object.
        public virtual DynamicMetaObject GetMetaObject(Expression parameter)
        {
            if (parameter == null) throw new ArgumentNullException("parameter");
            // Restrict to this runtime type to help callers; keep restrictions minimal.
            var restrictions = BindingRestrictions.GetTypeRestriction(parameter, this.GetType());
            return new DynamicMetaObject(parameter, restrictions, this);
        }

        // Default: no dynamic member names.
        public virtual IEnumerable<string> GetDynamicMemberNames()
        {
            return (IEnumerable<string>)new string[0];
        }

        // The Try* methods mirror the surface of System.Dynamic.DynamicObject.
        // Default implementations return false (operation not handled).

        public virtual bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (binder == null) throw new ArgumentNullException("binder");
            result = null;
            return false;
        }

        public virtual bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (binder == null) throw new ArgumentNullException("binder");
            return false;
        }

        public virtual bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            if (binder == null) throw new ArgumentNullException("binder");
            result = null;
            return false;
        }

        public virtual bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            if (binder == null) throw new ArgumentNullException("binder");
            result = null;
            return false;
        }

        public virtual bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            if (binder == null) throw new ArgumentNullException("binder");
            result = null;
            return false;
        }

        public virtual bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            if (binder == null) throw new ArgumentNullException("binder");
            return false;
        }

        public virtual bool TryBinaryOperation(BinaryOperationBinder binder, object arg, out object result)
        {
            if (binder == null) throw new ArgumentNullException("binder");
            result = null;
            return false;
        }

        public virtual bool TryUnaryOperation(UnaryOperationBinder binder, out object result)
        {
            if (binder == null) throw new ArgumentNullException("binder");
            result = null;
            return false;
        }

        public virtual bool TryConvert(ConvertBinder binder, out object result)
        {
            if (binder == null) throw new ArgumentNullException("binder");
            result = null;
            return false;
        }
    }
}