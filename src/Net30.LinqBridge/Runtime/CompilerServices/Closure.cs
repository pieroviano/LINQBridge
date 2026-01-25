using System.ComponentModel;
using System.Diagnostics;

namespace System.Runtime.CompilerServices;

/// <summary>Represents the runtime state of a dynamically generated method.</summary>
[DebuggerStepThrough]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class Closure
{
    /// <summary>
    ///     Represents the non-trivial constants and locally executable expressions that are referenced by a dynamically
    ///     generated method.
    /// </summary>
    public readonly object[] Constants;

    /// <summary>Represents the hoisted local variables from the parent context.</summary>
    public readonly object[] Locals;

    /// <summary>Creates an object to hold state of a dynamically generated method.</summary>
    /// <param name="constants">The constant values that are used by the method.</param>
    /// <param name="locals">The hoisted local variables from the parent context.</param>
    public Closure(object[] constants, object[] locals)
    {
        Constants = constants;
        Locals = locals;
    }
}