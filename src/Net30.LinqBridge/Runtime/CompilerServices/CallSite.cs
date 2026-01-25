using System.Dynamic.Utils;
using System.Linq;
using System.Reflection;

namespace System.Runtime.CompilerServices;

/// <summary>A dynamic call site base class. This type is used as a parameter type to the dynamic site targets.</summary>
public class CallSite
{
    private static volatile CacheDict<Type, Func<CallSiteBinder, CallSite>> _SiteCtors;

    internal readonly CallSiteBinder _binder;

    internal bool _match;

    internal CallSite(CallSiteBinder binder)
    {
        _binder = binder;
    }

    /// <summary>Class responsible for binding dynamic operations on the dynamic site.</summary>
    /// <returns>
    ///     The <see cref="T:System.Runtime.CompilerServices.CallSiteBinder" /> object responsible for binding dynamic
    ///     operations.
    /// </returns>
    public CallSiteBinder Binder => _binder;

    /// <summary>Creates a call site with the given delegate type and binder.</summary>
    /// <returns>The new call site.</returns>
    /// <param name="delegateType">The call site delegate type.</param>
    /// <param name="binder">The call site binder.</param>
    public static CallSite Create(Type delegateType, CallSiteBinder binder)
    {
        Func<CallSiteBinder, CallSite> func;
        ContractUtils.RequiresNotNull(delegateType, "delegateType");
        ContractUtils.RequiresNotNull(binder, "binder");
        if (!delegateType.IsSubclassOf(typeof(MulticastDelegate)))
        {
            throw Error.TypeMustBeDerivedFromSystemDelegate();
        }

        var cacheDict = _SiteCtors;
        if (cacheDict == null)
        {
            var cacheDict1 = new CacheDict<Type, Func<CallSiteBinder, CallSite>>(100);
            cacheDict = cacheDict1;
            _SiteCtors = cacheDict1;
        }

        MethodInfo method = null;
        if (!cacheDict.TryGetValue(delegateType, out func))
        {
            method = typeof(CallSite<>).MakeGenericType(delegateType).GetMethod("Create");
            if (delegateType.CanCache())
            {
                func = (Func<CallSiteBinder, CallSite>)Delegate.CreateDelegate(typeof(Func<CallSiteBinder, CallSite>),
                    method);
                cacheDict.Add(delegateType, func);
            }
        }

        if (func != null)
        {
            return func(binder);
        }

        return (CallSite)method.Invoke(null, new object[] { binder });
    }
}