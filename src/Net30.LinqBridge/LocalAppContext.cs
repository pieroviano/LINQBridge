using System.Collections.Generic;
using System.Reflection;

namespace System;

internal static class LocalAppContext
{
    private static TryGetSwitchDelegate TryGetSwitchFromCentralAppContext;

    private static readonly bool s_canForwardCalls;

    private static readonly Dictionary<string, bool> s_switchMap;

    private static readonly object s_syncLock;

    static LocalAppContext()
    {
        s_switchMap = new Dictionary<string, bool>();
        s_syncLock = new object();
        s_canForwardCalls = SetupDelegate();
        AppContextDefaultValues.PopulateDefaultValues();
        DisableCaching = IsSwitchEnabled("TestSwitch.LocalAppContext.DisableCaching");
    }

    private static bool DisableCaching { get; }

    public static bool IsSwitchEnabled(string switchName)
    {
        var flag = false;
        if (s_canForwardCalls && TryGetSwitchFromCentralAppContext(switchName, out flag))
        {
            return flag;
        }

        return IsSwitchEnabledLocal(switchName);
    }

    internal static void DefineSwitchDefault(string switchName, bool initialValue)
    {
        s_switchMap[switchName] = initialValue;
    }

    internal static bool GetCachedSwitchValue(string switchName, ref int switchValue)
    {
        if (switchValue < 0)
        {
            return false;
        }

        if (switchValue > 0)
        {
            return true;
        }

        return GetCachedSwitchValueInternal(switchName, ref switchValue);
    }

    private static bool GetCachedSwitchValueInternal(string switchName, ref int switchValue)
    {
        if (DisableCaching)
        {
            return IsSwitchEnabled(switchName);
        }

        var flag = IsSwitchEnabled(switchName);
        switchValue = flag ? 1 : -1;
        return flag;
    }

    private static bool IsSwitchEnabledLocal(string switchName)
    {
        bool flag;
        bool flag1;
        lock (s_switchMap)
        {
            flag1 = s_switchMap.TryGetValue(switchName, out flag);
        }

        if (flag1)
        {
            return flag;
        }

        return false;
    }

    private static bool SetupDelegate()
    {
        var type = typeof(object).Assembly.GetType("System.AppContext");
        if (type == null)
        {
            return false;
        }

        var method = type.GetMethod("TryGetSwitch", BindingFlags.Static | BindingFlags.Public, null,
            new[] { typeof(string), typeof(bool).MakeByRefType() }, null);
        if (method == null)
        {
            return false;
        }

        TryGetSwitchFromCentralAppContext =
            (TryGetSwitchDelegate)Delegate.CreateDelegate(typeof(TryGetSwitchDelegate), method);
        return true;
    }

    private delegate bool TryGetSwitchDelegate(string switchName, out bool value);
}