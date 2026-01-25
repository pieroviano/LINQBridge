#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace System.Dynamic;

/// <summary>Describes arguments in the dynamic binding process.</summary>
public sealed class CallInfo
{
    /// <summary>Creates a new PositionalArgumentInfo.</summary>
    /// <param name="argCount">The number of arguments.</param>
    /// <param name="argNames">The argument names.</param>
    public CallInfo(int argCount, params string[] argNames)
        : this(argCount, (IEnumerable<string>)argNames)
    {
    }

    /// <summary>Creates a new CallInfo that represents arguments in the dynamic binding process.</summary>
    /// <param name="argCount">The number of arguments.</param>
    /// <param name="argNames">The argument names.</param>
    public CallInfo(int argCount, IEnumerable<string> argNames)
    {
        ContractUtils.RequiresNotNull(argNames, nameof(argNames));
        var array = argNames.ToReadOnly();
        if (argCount < array.Count)
        {
            throw Error.ArgCntMustBeGreaterThanNameCnt();
        }

        ContractUtils.RequiresNotNullItems(array, nameof(argNames));
        ArgumentCount = argCount;
        ArgumentNames = array;
    }

    /// <summary>The number of arguments.</summary>
    /// <returns>The number of arguments.</returns>
    public int ArgumentCount { get; }

    /// <summary>The argument names.</summary>
    /// <returns>The read-only collection of argument names.</returns>
    public ReadOnlyCollection<string> ArgumentNames { get; }

    /// <summary>Determines whether the specified CallInfo instance is considered equal to the current.</summary>
    /// <returns>true if the specified instance is equal to the current one otherwise, false.</returns>
    /// <param name="obj">The instance of <see cref="T:System.Dynamic.CallInfo" /> to compare with the current instance.</param>
    public override bool Equals(object obj)
    {
        var callInfo = obj as CallInfo;
        return ArgumentCount == callInfo.ArgumentCount && ArgumentNames.ListEquals(callInfo.ArgumentNames);
    }

    /// <summary>Serves as a hash function for the current <see cref="T:System.Dynamic.CallInfo" />.</summary>
    /// <returns>A hash code for the current <see cref="T:System.Dynamic.CallInfo" />.</returns>
    public override int GetHashCode()
    {
        return ArgumentCount ^ ArgumentNames.ListHashCode();
    }
}