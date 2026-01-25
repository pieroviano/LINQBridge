using System;

#nullable disable
namespace System.Linq.Expressions
{
    // Minimal InvokeBinder emulation for .NET 3.5
    //
    // Supplies:
    // - CallInfo property (used by various call sites in the codebase)
    // - abstract FallbackInvoke for binder implementations
    // - Defer helpers used by the COM shim (simple implementation that calls FallbackInvoke)
    public abstract class InvokeBinder: CallSiteBinder
    {
        protected InvokeBinder(CallInfo callInfo)
        {
            this.CallInfo = callInfo ?? throw new ArgumentNullException("callInfo");
        }

        public CallInfo CallInfo { get; private set; }

        // Binder implementations must provide this to produce binding result.
        public abstract DynamicMetaObject FallbackInvoke(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion);

        // Defer helpers: minimal behavior used by the COM-related code in this project.
        // Accepts an array where the first element is the target and remaining elements are the call args.
        public virtual DynamicMetaObject Defer(params DynamicMetaObject[] allArgs)
        {
            if (allArgs == null || allArgs.Length == 0) throw new ArgumentException("allArgs");
            var target = allArgs[0];
            if (allArgs.Length == 1)
            {
                return this.FallbackInvoke(target, DynamicMetaObject.EmptyMetaObjects, null);
            }
            var args = new DynamicMetaObject[allArgs.Length - 1];
            Array.Copy(allArgs, 1, args, 0, args.Length);
            return this.FallbackInvoke(target, args, null);
        }

        // Overload accepting explicit target + args.
        public virtual DynamicMetaObject Defer(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            if (target == null) throw new ArgumentNullException("target");
            return this.FallbackInvoke(target, args ?? DynamicMetaObject.EmptyMetaObjects, null);
        }

    }
}