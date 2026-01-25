#nullable disable
namespace System.Linq.Parallel;

internal abstract class SortHelper<TInputOutput>
{
    internal abstract TInputOutput[] Sort();
}