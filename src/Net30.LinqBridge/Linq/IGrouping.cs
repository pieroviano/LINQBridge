#nullable disable
using System.Collections;
using System.Collections.Generic;

namespace System.Linq;

/// <summary>Represents a collection of objects that have a common key.</summary>
/// <typeparam name="TKey">
///     The type of the key of the <see cref="T:System.Linq.IGrouping`2" />.This type parameter is
///     covariant. That is, you can use either the type you specified or any type that is more derived. For more
///     information about covariance and contravariance, see Covariance and Contravariance in Generics.
/// </typeparam>
/// <typeparam name="TElement">The type of the values in the <see cref="T:System.Linq.IGrouping`2" />.</typeparam>
/// <filterpriority>2</filterpriority>
public interface IGrouping<TKey, TElement> : IEnumerable<TElement>, IEnumerable
{
    /// <summary>Gets the key of the <see cref="T:System.Linq.IGrouping`2" />.</summary>
    /// <returns>The key of the <see cref="T:System.Linq.IGrouping`2" />.</returns>
    TKey Key { get; }
}