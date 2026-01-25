using System;

#nullable disable
namespace System.Linq.Expressions
{
    // Minimal GetIndexBinder implementation for .NET 3.5 compatibility.
    //
    // Mirrors the small surface used by the port:
    // - stores CallInfo
    // - defines abstract FallbackGetIndex
    // - provides Defer helpers that call the fallback
    public abstract class GetIndexBinder: CallSiteBinder
    {
        protected GetIndexBinder(CallInfo callInfo)
        {
            this.CallInfo = callInfo ?? throw new ArgumentNullException("callInfo");
        }

        public CallInfo CallInfo { get; private set; }

        // Implementers must provide this to produce the binding result.
        public abstract DynamicMetaObject FallbackGetIndex(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject errorSuggestion);

        // Defer helpers used by the COM shim and other binders.
        public virtual DynamicMetaObject Defer(DynamicMetaObject target, DynamicMetaObject[] indexes)
        {
            if (target == null) throw new ArgumentNullException("target");
            return this.FallbackGetIndex(target, indexes ?? DynamicMetaObject.EmptyMetaObjects, null);
        }

        public virtual DynamicMetaObject Defer(params DynamicMetaObject[] args)
        {
            if (args == null || args.Length == 0) throw new ArgumentException("args");
            var target = args[0];
            DynamicMetaObject[] indexes = args.Length == 1 ? DynamicMetaObject.EmptyMetaObjects : new DynamicMetaObject[args.Length - 1];
            if (args.Length > 1) Array.Copy(args, 1, indexes, 0, indexes.Length);
            return this.Defer(target, indexes);
        }

        public override bool Equals(object obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();
    }
}