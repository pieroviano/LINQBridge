namespace System;

internal static class LocalAppContextSwitches
{
    internal const string DontReliablyClonePrivateKeyStr =
        "Switch.System.Security.Cryptography.X509Certificates.RSACertificateExtensions.DontReliablyClonePrivateKey";

    internal const string UseLegacyPublicKeyBehaviorStr =
        "Switch.System.Security.Cryptography.X509Certificates.ECDsaCertificateExtensions.UseLegacyPublicKeyBehavior";

    internal const string AesCryptoServiceProviderDontCorrectlyResetDecryptorStr =
        "Switch.System.Security.Cryptography.AesCryptoServiceProvider.DontCorrectlyResetDecryptor";

    internal const string SymmetricCngAlwaysUseNCryptStr =
        "Switch.System.Security.Cryptography.SymmetricCng.AlwaysUseNCrypt";

    private static int _dontReliablyClonePrivateKeyName;

    private static int _useLegacyPublicKeyBehavior;

    private static int _aesCryptoServiceProviderDontCorrectlyResetDecryptorName;

    private static int _symmetricCngAlwaysUseNCryptName;

    internal static readonly string SwitchCryptographyUseLegacyFipsThrow;

    private static int _useLegacyFipsThrow;

    static LocalAppContextSwitches()
    {
        SwitchCryptographyUseLegacyFipsThrow = "Switch.System.Security.Cryptography.UseLegacyFipsThrow";
    }

    public static bool AesCryptoServiceProviderDontCorrectlyResetDecryptor => LocalAppContext.GetCachedSwitchValue(
        "Switch.System.Security.Cryptography.AesCryptoServiceProvider.DontCorrectlyResetDecryptor",
        ref _aesCryptoServiceProviderDontCorrectlyResetDecryptorName);

    public static bool DontReliablyClonePrivateKey => LocalAppContext.GetCachedSwitchValue(
        "Switch.System.Security.Cryptography.X509Certificates.RSACertificateExtensions.DontReliablyClonePrivateKey",
        ref _dontReliablyClonePrivateKeyName);

    public static bool SymmetricCngAlwaysUseNCrypt => LocalAppContext.GetCachedSwitchValue(
        "Switch.System.Security.Cryptography.SymmetricCng.AlwaysUseNCrypt", ref _symmetricCngAlwaysUseNCryptName);

    public static bool UseLegacyFipsThrow =>
        LocalAppContext.GetCachedSwitchValue(SwitchCryptographyUseLegacyFipsThrow, ref _useLegacyFipsThrow);

    public static bool UseLegacyPublicKeyBehavior => LocalAppContext.GetCachedSwitchValue(
        "Switch.System.Security.Cryptography.X509Certificates.ECDsaCertificateExtensions.UseLegacyPublicKeyBehavior",
        ref _useLegacyPublicKeyBehavior);
}