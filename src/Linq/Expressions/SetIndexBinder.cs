using System;

#nullable disable
namespace System.Linq.Expressions
{
    // Minimal SetIndexBinder implementation for .NET 3.5 compatibility.
    //
    // Mirrors the surface used by the port:
    // - stores CallInfo
    // - defines abstract FallbackSetIndex
    // - provides Defer helpers that call the fallback
    public abstract class SetIndexBinder
    {
        protected SetIndexBinder(CallInfo callInfo)
        {
            this.CallInfo = callInfo ?? throw new ArgumentNullException("callInfo");
        }

        public CallInfo CallInfo { get; private set; }

        // Implementers must provide this to produce the binding result.
        public abstract DynamicMetaObject FallbackSetIndex(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject value, DynamicMetaObject errorSuggestion);

        // Defer helpers used by the COM shim and other binders.
        public virtual DynamicMetaObject Defer(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject value)
        {
            if (target == null) throw new ArgumentNullException("target");
            if (value == null) throw new ArgumentNullException("value");
            return this.FallbackSetIndex(target, indexes ?? DynamicMetaObject.EmptyMetaObjects, value, null);
        }

        // Accepts args: [target, index0, index1, ..., value]
        public virtual DynamicMetaObject Defer(params DynamicMetaObject[] args)
        {
            if (args == null || args.Length < 2) throw new ArgumentException("args");
            var target = args[0];
            var value = args[args.Length - 1];
            int indexCount = args.Length - 2;
            DynamicMetaObject[] indexes = indexCount == 0 ? DynamicMetaObject.EmptyMetaObjects : new DynamicMetaObject[indexCount];
            if (indexCount > 0) Array.Copy(args, 1, indexes, 0, indexCount);
            return this.Defer(target, indexes, value);
        }

        public override bool Equals(object obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();
    }
}