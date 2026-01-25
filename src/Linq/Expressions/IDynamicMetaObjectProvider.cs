using System.Linq.Expressions;

#nullable disable
namespace System.Linq.Expressions
{
    // Minimal IDynamicMetaObjectProvider for .NET 3.5 compatibility.
    // Types in this project implement this to supply a DynamicMetaObject for the DLR-style binders.
    public interface IDynamicMetaObjectProvider
    {
        DynamicMetaObject GetMetaObject(Expression parameter);
    }
}