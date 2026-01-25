using System.Diagnostics;

namespace System.Collections.Generic;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
internal readonly struct Marker
{
    public int Count { get; }

    private string DebuggerDisplay
    {
        get { return string.Format("{0}: {1}, {2}: {3}", "Index", Index, "Count", Count); }
    }

    public int Index { get; }

    public Marker(int count, int index)
    {
        Count = count;
        Index = index;
    }
}