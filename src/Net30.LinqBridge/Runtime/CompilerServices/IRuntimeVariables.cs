namespace System.Runtime.CompilerServices;

/// <summary>Represents the values of run-time variables.</summary>
public interface IRuntimeVariables
{
    /// <summary>Gets a count of the run-time variables.</summary>
    /// <returns>The number of run-time variables.</returns>
    int Count { get; }

    /// <summary>Gets the value of the run-time variable at the specified index.</summary>
    /// <returns>The value of the run-time variable.</returns>
    /// <param name="index">The zero-based index of the run-time variable whose value is to be returned.</param>
    object this[int index] { get; set; }
}