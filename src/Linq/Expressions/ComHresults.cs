// Type: System.Dynamic.ComHresults
#nullable disable
namespace System.Linq.Expressions;

internal static class ComHresults
{
  internal const int S_OK = 0;
  internal const int CONNECT_E_NOCONNECTION = -2147220992 /*0x80040200*/;
  internal const int DISP_E_UNKNOWNINTERFACE = -2147352575 /*0x80020001*/;
  internal const int DISP_E_MEMBERNOTFOUND = -2147352573 /*0x80020003*/;
  internal const int DISP_E_PARAMNOTFOUND = -2147352572 /*0x80020004*/;
  internal const int DISP_E_TYPEMISMATCH = -2147352571 /*0x80020005*/;
  internal const int DISP_E_UNKNOWNNAME = -2147352570 /*0x80020006*/;
  internal const int DISP_E_NONAMEDARGS = -2147352569 /*0x80020007*/;
  internal const int DISP_E_BADVARTYPE = -2147352568 /*0x80020008*/;
  internal const int DISP_E_EXCEPTION = -2147352567 /*0x80020009*/;
  internal const int DISP_E_OVERFLOW = -2147352566 /*0x8002000A*/;
  internal const int DISP_E_BADINDEX = -2147352565 /*0x8002000B*/;
  internal const int DISP_E_UNKNOWNLCID = -2147352564 /*0x8002000C*/;
  internal const int DISP_E_ARRAYISLOCKED = -2147352563 /*0x8002000D*/;
  internal const int DISP_E_BADPARAMCOUNT = -2147352562 /*0x8002000E*/;
  internal const int DISP_E_PARAMNOTOPTIONAL = -2147352561 /*0x8002000F*/;
  internal const int E_NOINTERFACE = -2147467262 /*0x80004002*/;
  internal const int E_FAIL = -2147467259 /*0x80004005*/;
  internal const int TYPE_E_LIBNOTREGISTERED = -2147319779;

  internal static bool IsSuccess(int hresult) => hresult >= 0;
}
