// Type: System.Dynamic.Assert
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#nullable disable
namespace System.Linq.Expressions;

internal static class Assert
{
  internal static Exception Unreachable
  {
    get
    {
      Debug.Assert(false, nameof (Unreachable));
      return (Exception) new InvalidOperationException("Code supposed to be unreachable");
    }
  }

  [Conditional("DEBUG")]
  internal static void NotNull(object var) => Debug.Assert(var != null);

  [Conditional("DEBUG")]
  internal static void NotNull(object var1, object var2)
  {
    Debug.Assert(var1 != null && var2 != null);
  }

  [Conditional("DEBUG")]
  internal static void NotNull(object var1, object var2, object var3)
  {
    Debug.Assert(var1 != null && var2 != null && var3 != null);
  }

  [Conditional("DEBUG")]
  internal static void NotNull(object var1, object var2, object var3, object var4)
  {
    Debug.Assert(var1 != null && var2 != null && var3 != null && var4 != null);
  }

  [Conditional("DEBUG")]
  internal static void NotEmpty(string str) => Debug.Assert(!string.IsNullOrEmpty(str));

  [Conditional("DEBUG")]
  internal static void NotEmpty<T>(ICollection<T> array)
  {
    Debug.Assert(array != null && array.Count > 0);
  }

  [Conditional("DEBUG")]
  internal static void NotNullItems<T>(IEnumerable<T> items) where T : class
  {
    Debug.Assert(items != null);
    foreach (T obj in items)
      Debug.Assert((object) obj != null);
  }

  [Conditional("DEBUG")]
  internal static void IsTrue(Func<bool> predicate)
  {
    ContractUtils.RequiresNotNull((object) predicate, nameof (predicate));
    Debug.Assert(predicate());
  }
}
