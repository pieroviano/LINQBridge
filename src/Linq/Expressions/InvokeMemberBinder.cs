using System;

#nullable disable
namespace System.Linq.Expressions
{
    // Minimal InvokeMemberBinder emulation for .NET 3.5
    //
    // Provides:
    // - Name, IgnoreCase properties
    // - constructor taking name, ignoreCase and CallInfo (passes CallInfo to base)
    // - abstract FallbackInvokeMember to be implemented by concrete binders
    // - inherits Defer behavior from InvokeBinder
    public abstract class InvokeMemberBinder : InvokeBinder
    {
        protected InvokeMemberBinder(string name, bool ignoreCase, CallInfo callInfo)
            : base(callInfo)
        {
            if (name == null) throw new ArgumentNullException("name");
            this.Name = name;
            this.IgnoreCase = ignoreCase;
        }

        public string Name { get; private set; }
        public bool IgnoreCase { get; private set; }

        // Binder implementations must provide this to handle invoke-member binding.
        public abstract DynamicMetaObject FallbackInvokeMember(DynamicMetaObject target, DynamicMetaObject[] args,
            DynamicMetaObject errorSuggestion);

        // Default equality/hash are fine for this minimal port.
        public override bool Equals(object obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();
    }
}