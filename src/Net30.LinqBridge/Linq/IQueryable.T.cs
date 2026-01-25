#nullable disable
using System.Collections;
using System.Collections.Generic;

namespace System.Linq;

/// <summary>
///     Provides functionality to evaluate queries against a specific data source wherein the type of the data is
///     known.
/// </summary>
/// <typeparam name="T">
///     The type of the data in the data source.This type parameter is covariant. That is, you can use
///     either the type you specified or any type that is more derived. For more information about covariance and
///     contravariance, see Covariance and Contravariance in Generics.
/// </typeparam>
public interface IQueryable<T> : IEnumerable<T>, IEnumerable, IQueryable
{
}