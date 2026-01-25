namespace System;

internal static class AppContextDefaultValues
{
    internal static readonly string SwitchNoAsyncCurrentCulture;

    internal static readonly string SwitchEnforceJapaneseEraYearRanges;

    internal static readonly string SwitchFormatJapaneseFirstYearAsANumber;

    internal static readonly string SwitchEnforceLegacyJapaneseDateParsing;

    internal static readonly string SwitchThrowExceptionIfDisposedCancellationTokenSource;

    internal static readonly string SwitchPreserveEventListnerObjectIdentity;

    internal static readonly string SwitchUseLegacyPathHandling;

    internal static readonly string SwitchBlockLongPaths;

    internal static readonly string SwitchDoNotAddrOfCspParentWindowHandle;

    internal static readonly string SwitchSetActorAsReferenceWhenCopyingClaimsIdentity;

    internal static readonly string SwitchIgnorePortablePDBsInStackTraces;

    internal static readonly string SwitchUseNewMaxArraySize;

    internal static readonly string SwitchUseConcurrentFormatterTypeCache;

    internal static readonly string SwitchUseLegacyExecutionContextBehaviorUponUndoFailure;

    internal static readonly string SwitchCryptographyUseLegacyFipsThrow;

    internal static readonly string SwitchDoNotMarshalOutByrefSafeArrayOnInvoke;

    internal static readonly string SwitchUseNetCoreTimer;

#pragma warning disable CS0169 // Field is never used
    private static volatile bool s_errorReadingRegistry;
#pragma warning restore CS0169 // Field is never used

    static AppContextDefaultValues()
    {
        SwitchNoAsyncCurrentCulture = "Switch.System.Globalization.NoAsyncCurrentCulture";
        SwitchEnforceJapaneseEraYearRanges = "Switch.System.Globalization.EnforceJapaneseEraYearRanges";
        SwitchFormatJapaneseFirstYearAsANumber = "Switch.System.Globalization.FormatJapaneseFirstYearAsANumber";
        SwitchEnforceLegacyJapaneseDateParsing = "Switch.System.Globalization.EnforceLegacyJapaneseDateParsing";
        SwitchThrowExceptionIfDisposedCancellationTokenSource =
            "Switch.System.Threading.ThrowExceptionIfDisposedCancellationTokenSource";
        SwitchPreserveEventListnerObjectIdentity =
            "Switch.System.Diagnostics.EventSource.PreserveEventListnerObjectIdentity";
        SwitchUseLegacyPathHandling = "Switch.System.IO.UseLegacyPathHandling";
        SwitchBlockLongPaths = "Switch.System.IO.BlockLongPaths";
        SwitchDoNotAddrOfCspParentWindowHandle = "Switch.System.Security.Cryptography.DoNotAddrOfCspParentWindowHandle";
        SwitchSetActorAsReferenceWhenCopyingClaimsIdentity =
            "Switch.System.Security.ClaimsIdentity.SetActorAsReferenceWhenCopyingClaimsIdentity";
        SwitchIgnorePortablePDBsInStackTraces = "Switch.System.Diagnostics.IgnorePortablePDBsInStackTraces";
        SwitchUseNewMaxArraySize = "Switch.System.Runtime.Serialization.UseNewMaxArraySize";
        SwitchUseConcurrentFormatterTypeCache = "Switch.System.Runtime.Serialization.UseConcurrentFormatterTypeCache";
        SwitchUseLegacyExecutionContextBehaviorUponUndoFailure =
            "Switch.System.Threading.UseLegacyExecutionContextBehaviorUponUndoFailure";
        SwitchCryptographyUseLegacyFipsThrow = "Switch.System.Security.Cryptography.UseLegacyFipsThrow";
        SwitchDoNotMarshalOutByrefSafeArrayOnInvoke =
            "Switch.System.Runtime.InteropServices.DoNotMarshalOutByrefSafeArrayOnInvoke";
        SwitchUseNetCoreTimer = "Switch.System.Threading.UseNetCoreTimer";
    }

    public static void PopulateDefaultValues()
    {
        string str;
        string str1;
        int num;
        ParseTargetFrameworkName(out str, out str1, out num);
    }

    private static void ParseTargetFrameworkName(out string identifier, out string profile, out int version)
    {
        identifier = ".NETFramework";
        version = 40000;
        profile = string.Empty;
    }

    private static bool TryParseFrameworkName(string frameworkName, out string identifier, out int version,
        out string profile)
    {
        var empty = string.Empty;
        var str = empty;
        profile = empty;
        identifier = str;
        version = 0;
        if (frameworkName == null || frameworkName.Length == 0)
        {
            return false;
        }

        var strArrays = frameworkName.Split(',');
        version = 0;
        if (strArrays.Length < 2 || strArrays.Length > 3)
        {
            return false;
        }

        identifier = strArrays[0].Trim();
        if (identifier.Length == 0)
        {
            return false;
        }

        var flag = false;
        profile = null;
        for (var i = 1; i < strArrays.Length; i++)
        {
            var strArrays1 = strArrays[i].Split('=');
            if (strArrays1.Length != 2)
            {
                return false;
            }

            var str1 = strArrays1[0].Trim();
            var str2 = strArrays1[1].Trim();
            if (!str1.Equals("Version", StringComparison.OrdinalIgnoreCase))
            {
                if (!str1.Equals("Profile", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (!string.IsNullOrEmpty(str2))
                {
                    profile = str2;
                }
            }
            else
            {
                flag = true;
                if (str2.Length > 0 && (str2[0] == 'v' || str2[0] == 'V'))
                {
                    str2 = str2.Substring(1);
                }

                var version1 = new Version(str2);
                version = version1.Major * 10000;
                if (version1.Minor > 0)
                {
                    version = version + version1.Minor * 100;
                }

                if (version1.Build > 0)
                {
                    version += version1.Build;
                }
            }
        }

        if (!flag)
        {
            return false;
        }

        return true;
    }
}