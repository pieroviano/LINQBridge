using System;
using System.Collections.Generic;

#nullable disable
namespace System.Linq.Expressions
{
    // Minimal CallInfo implementation compatible with the codebase.
    // Provides: ArgumentCount and ArgumentNames used by binders.
    public sealed class CallInfo
    {
        public CallInfo(int argumentCount, IEnumerable<string> argumentNames)
        {
            if (argumentCount < 0) throw new ArgumentOutOfRangeException("argumentCount");
            var list = argumentNames == null ? new List<string>() : new List<string>(argumentNames);
            // store read-only view to match expected immutability
            this.ArgumentNames = list.AsReadOnly();
            this.ArgumentCount = argumentCount;
        }

        // convenience ctor used in some places (e.g. new CallInfo(0, new string[0]))
        public CallInfo(int argumentCount, params string[] argumentNames)
            : this(argumentCount, (IEnumerable<string>)argumentNames)
        {
        }

        // Number of arguments supplied (including unnamed ones).
        public int ArgumentCount { get; }

        // Names for named arguments (may be empty).
        public IList<string> ArgumentNames { get; }

        public override bool Equals(object obj)
        {
            var other = obj as CallInfo;
            if (other == null) return false;
            if (this.ArgumentCount != other.ArgumentCount) return false;
            if (this.ArgumentNames.Count != other.ArgumentNames.Count) return false;
            for (int i = 0; i < this.ArgumentNames.Count; i++)
            {
                if (!object.Equals(this.ArgumentNames[i], other.ArgumentNames[i])) return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            int hc = ArgumentCount;
            foreach (var n in ArgumentNames)
            {
                hc = (hc * 31) + (n == null ? 0 : n.GetHashCode());
            }
            return hc;
        }

        public override string ToString()
        {
            return $"CallInfo(Count={ArgumentCount}, Names=[{string.Join(", ", ArgumentNames.ToArray())}])";
        }
    }
}