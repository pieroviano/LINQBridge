#nullable disable
using System.Collections;
using System.Collections.Generic;

namespace System.Linq;

/// <summary>Represents the result of a sorting operation.</summary>
/// <typeparam name="T">
///     The type of the content of the data source.This type parameter is covariant. That is, you can use
///     either the type you specified or any type that is more derived. For more information about covariance and
///     contravariance, see Covariance and Contravariance in Generics.
/// </typeparam>
public interface IOrderedQueryable<T> :
    IQueryable<T>,
    IEnumerable<T>,
    IEnumerable,
    IQueryable,
    IOrderedQueryable
{
}