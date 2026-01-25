using System;

#nullable disable
namespace System.Linq.Expressions
{
    // Minimal LabelTarget shim for .NET 3.5 compatibility.
    // Matches the small surface used by the expression APIs in this repo:
    // - Type: the type that can be returned to this label
    // - Name: optional display name for debugging
    public sealed class LabelTarget
    {
        public LabelTarget(Type type) : this(type, null) { }

        public LabelTarget(Type type, string name)
        {
            if (type == null) throw new ArgumentNullException("type");
            this.Type = type;
            this.Name = name;
        }

        // The type expected by a return to this label (use typeof(void) for no value)
        public Type Type { get; }

        // Optional name (for debugging / expression printing)
        public string Name { get; }

        public override string ToString()
        {
            return string.Format("LabelTarget(Name={0}, Type={1})", this.Name ?? "<null>", this.Type);
        }

        // Keep reference equality semantics but provide stable hash based on runtime identity.
        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
        }
    }
}