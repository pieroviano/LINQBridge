// Type: System.Dynamic.ComEventSinksContainer
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;

#nullable disable
namespace System.Linq.Expressions;

internal class ComEventSinksContainer : List<ComEventSink>, IDisposable
{
  private static readonly object _ComObjectEventSinksKey = new object();

  private ComEventSinksContainer()
  {
  }

  [SecurityCritical]
  public static ComEventSinksContainer FromRuntimeCallableWrapper(object rcw, bool createIfNotFound)
  {
    object comObjectData1 = Marshal.GetComObjectData(rcw, ComEventSinksContainer._ComObjectEventSinksKey);
    if (comObjectData1 != null || !createIfNotFound)
      return (ComEventSinksContainer) comObjectData1;
    lock (ComEventSinksContainer._ComObjectEventSinksKey)
    {
      object comObjectData2 = Marshal.GetComObjectData(rcw, ComEventSinksContainer._ComObjectEventSinksKey);
      if (comObjectData2 != null)
        return (ComEventSinksContainer) comObjectData2;
      ComEventSinksContainer data = new ComEventSinksContainer();
      if (!Marshal.SetComObjectData(rcw, ComEventSinksContainer._ComObjectEventSinksKey, (object) data))
        throw Error.SetComObjectDataFailed();
      return data;
    }
  }

  [SecuritySafeCritical]
  public void Dispose()
  {
    this.DisposeAll();
    GC.SuppressFinalize((object) this);
  }

  [SecurityCritical]
  private void DisposeAll()
  {
    foreach (ComEventSink comEventSink in (List<ComEventSink>) this)
      comEventSink.Dispose();
  }

  [SecuritySafeCritical]
  ~ComEventSinksContainer() => this.DisposeAll();
}
