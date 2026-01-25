namespace System.Dynamic;

/// <summary>
///     Represents information about a dynamic get member operation that indicates if the get member should invoke
///     properties when they perform the get operation.
/// </summary>
public interface IInvokeOnGetBinder
{
    /// <summary>
    ///     Gets the value indicating if this get member operation should invoke properties when they perform the get
    ///     operation. The default value when this interface is not present is true.
    /// </summary>
    /// <returns>
    ///     True if this get member operation should invoke properties when they perform the get operation; otherwise
    ///     false.
    /// </returns>
    bool InvokeOnGet { get; }
}