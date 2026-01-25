// Type: System.Dynamic.ExcepInfo
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;

#nullable disable
namespace System.Linq.Expressions;

internal struct ExcepInfo
{
  private short wCode;
  private short wReserved;
  private IntPtr bstrSource;
  private IntPtr bstrDescription;
  private IntPtr bstrHelpFile;
  private int dwHelpContext;
  private IntPtr pvReserved;
  private IntPtr pfnDeferredFillIn;
  private int scode;

  [SecurityCritical]
  private static string ConvertAndFreeBstr(ref IntPtr bstr)
  {
    if (bstr == IntPtr.Zero)
      return (string) null;
    string stringBstr = Marshal.PtrToStringBSTR(bstr);
    Marshal.FreeBSTR(bstr);
    bstr = IntPtr.Zero;
    return stringBstr;
  }

  internal void Dummy()
  {
    this.wCode = (short) 0;
    this.wReserved = (short) 0;
    ++this.wReserved;
    this.bstrSource = IntPtr.Zero;
    this.bstrDescription = IntPtr.Zero;
    this.bstrHelpFile = IntPtr.Zero;
    this.dwHelpContext = 0;
    this.pfnDeferredFillIn = IntPtr.Zero;
    this.pvReserved = IntPtr.Zero;
    this.scode = 0;
    throw Error.MethodShouldNotBeCalled();
  }

  [SecurityCritical]
  internal Exception GetException()
  {
    int errorCode = this.scode != 0 ? this.scode : (int) this.wCode;
    Exception exception = Marshal.GetExceptionForHR(errorCode);
    string message = ExcepInfo.ConvertAndFreeBstr(ref this.bstrDescription);
    if (message != null)
    {
      if (exception is COMException)
      {
        exception = (Exception) new COMException(message, errorCode);
      }
      else
      {
        ConstructorInfo constructor = exception.GetType().GetConstructor(new Type[1]
        {
          typeof (string)
        });
        if (constructor != (ConstructorInfo) null)
          exception = (Exception) constructor.Invoke(new object[1]
          {
            (object) message
          });
      }
    }
    exception.Source = ExcepInfo.ConvertAndFreeBstr(ref this.bstrSource);
    string str = ExcepInfo.ConvertAndFreeBstr(ref this.bstrHelpFile);
    if (str != null && this.dwHelpContext != 0)
      str = $"{str}#{this.dwHelpContext.ToString()}";
    exception.HelpLink = str;
    return exception;
  }
}
