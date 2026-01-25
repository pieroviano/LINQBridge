using System;

#nullable disable
namespace Microsoft.CSharp.RuntimeBinder
{
    // Minimal interface used by CSharpGetMemberBinder to indicate
    // whether the binder should invoke on get (i.e. result-indexed).
    public interface IInvokeOnGetBinder
    {
        bool InvokeOnGet { get; }
    }
}