// Type: System.Dynamic.SplatCallSite

#nullable disable
namespace System.Linq.Expressions;

internal sealed class SplatCallSite
{
    internal readonly object _callable;
    internal CallSite<Func<CallSite, object, object[], object>> _site;

    internal SplatCallSite(object callable)
    {
        _callable = callable;
    }

    internal object Invoke(object[] args)
    {
        var callable = _callable as Delegate;
        if ((object)callable != null)
        {
            return callable.DynamicInvoke(args);
        }

        if (_site == null)
        {
            _site = CallSite<Func<CallSite, object, object[], object>>.Create(
                SplatInvokeBinder.Instance);
        }

        return _site.Target(_site, _callable, args);
    }
}