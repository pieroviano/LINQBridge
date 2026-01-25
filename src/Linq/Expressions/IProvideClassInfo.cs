// Type: System.Dynamic.IProvideClassInfo
using System.Runtime.InteropServices;

#nullable disable
namespace System.Linq.Expressions;

[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("B196B283-BAB4-101A-B69C-00AA00341D07")]
[ComImport]
internal interface IProvideClassInfo
{
  void GetClassInfo(out IntPtr info);
}
