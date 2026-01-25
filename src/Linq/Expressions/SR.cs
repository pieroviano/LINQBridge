// Type: System.Dynamic.SR
using System.Globalization;
using System.Resources;
using System.Threading;

#nullable disable
namespace System.Linq.Expressions;

internal sealed class SR
{
  internal const string InvalidArgumentValue = "InvalidArgumentValue";
  internal const string ComObjectExpected = "ComObjectExpected";
  internal const string CannotCall = "CannotCall";
  internal const string COMObjectDoesNotSupportEvents = "COMObjectDoesNotSupportEvents";
  internal const string COMObjectDoesNotSupportSourceInterface = "COMObjectDoesNotSupportSourceInterface";
  internal const string SetComObjectDataFailed = "SetComObjectDataFailed";
  internal const string MethodShouldNotBeCalled = "MethodShouldNotBeCalled";
  internal const string UnexpectedVarEnum = "UnexpectedVarEnum";
  internal const string DispBadParamCount = "DispBadParamCount";
  internal const string DispMemberNotFound = "DispMemberNotFound";
  internal const string DispNoNamedArgs = "DispNoNamedArgs";
  internal const string DispOverflow = "DispOverflow";
  internal const string DispTypeMismatch = "DispTypeMismatch";
  internal const string DispParamNotOptional = "DispParamNotOptional";
  internal const string CannotRetrieveTypeInformation = "CannotRetrieveTypeInformation";
  internal const string GetIDsOfNamesInvalid = "GetIDsOfNamesInvalid";
  internal const string UnsupportedEnumType = "UnsupportedEnumType";
  internal const string UnsupportedHandlerType = "UnsupportedHandlerType";
  internal const string CouldNotGetDispId = "CouldNotGetDispId";
  internal const string AmbiguousConversion = "AmbiguousConversion";
  internal const string VariantGetAccessorNYI = "VariantGetAccessorNYI";
  private static SR loader;
  private ResourceManager resources;

  internal SR() => this.resources = new ResourceManager("System.Dynamic", this.GetType().Assembly);

  private static SR GetLoader()
  {
    if (SR.loader == null)
    {
      SR sr = new SR();
      Interlocked.CompareExchange<SR>(ref SR.loader, sr, (SR) null);
    }
    return SR.loader;
  }

  private static CultureInfo Culture => (CultureInfo) null;

  public static ResourceManager Resources => SR.GetLoader().resources;

  public static string GetString(string name, params object[] args)
  {
    SR loader = SR.GetLoader();
    if (loader == null)
      return (string) null;
    string format = loader.resources.GetString(name, SR.Culture);
    if (args == null || args.Length == 0)
      return format;
    for (int index = 0; index < args.Length; ++index)
    {
      if (args[index] is string str && str.Length > 1024 /*0x0400*/)
        args[index] = (object) (str.Substring(0, 1021) + "...");
    }
    return string.Format((IFormatProvider) CultureInfo.CurrentCulture, format, args);
  }

  public static string GetString(string name)
  {
    return SR.GetLoader()?.resources.GetString(name, SR.Culture);
  }

  public static string GetString(string name, out bool usedFallback)
  {
    usedFallback = false;
    return SR.GetString(name);
  }

  public static object GetObject(string name)
  {
    return SR.GetLoader()?.resources.GetObject(name, SR.Culture);
  }
}
