using System;

#nullable disable
namespace System.Linq.Expressions
{
    // Minimal ConvertBinder emulation for .NET 3.5
    //
    // Surface implemented:
    // - constructor (Type targetType, bool isExplicit)
    // - Type property
    // - IsExplicit property
    // - abstract FallbackConvert
    // - simple Defer helpers that call the fallback
    public abstract class ConvertBinder
    {
        protected ConvertBinder(Type type, bool isExplicit)
        {
            if (type == null) throw new ArgumentNullException("type");
            this.Type = type;
            this.IsExplicit = isExplicit;
        }

        // Target conversion type
        public Type Type { get; private set; }

        // Whether the conversion is explicit
        public bool IsExplicit { get; private set; }

        // Implementers must provide fallback conversion behavior.
        public abstract DynamicMetaObject FallbackConvert(DynamicMetaObject target, DynamicMetaObject errorSuggestion);

        // Defer helpers: simple implementations used by the COM shim and binders.
        public virtual DynamicMetaObject Defer(DynamicMetaObject target)
        {
            if (target == null) throw new ArgumentNullException("target");
            return this.FallbackConvert(target, null);
        }

        public virtual DynamicMetaObject Defer(params DynamicMetaObject[] args)
        {
            if (args == null || args.Length == 0) throw new ArgumentException("args");
            return this.Defer(args[0]);
        }

        public override bool Equals(object obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();
    }
}