// Type: System.Dynamic.DispCallable
using System.Globalization;
using System.Linq.Expressions;

#nullable disable
namespace System.Linq.Expressions;

internal sealed class DispCallable : IDynamicMetaObjectProvider
{
  private readonly IDispatchComObject _dispatch;
  private readonly string _memberName;
  private readonly int _dispId;

  internal DispCallable(IDispatchComObject dispatch, string memberName, int dispId)
  {
    this._dispatch = dispatch;
    this._memberName = memberName;
    this._dispId = dispId;
  }

  public override string ToString()
  {
    return string.Format((IFormatProvider) CultureInfo.CurrentCulture, "<bound dispmethod {0}>", new object[1]
    {
      (object) this._memberName
    });
  }

  public IDispatchComObject DispatchComObject
  {
    get => this._dispatch;
  }

  public IDispatch DispatchObject
  {
    get => this._dispatch.DispatchObject;
  }

  public string MemberName => this._memberName;

  public int DispId
  {
    get => this._dispId;
  }

  public DynamicMetaObject GetMetaObject(Expression parameter)
  {
    return (DynamicMetaObject) new DispCallableMetaObject(parameter, this);
  }

  public override bool Equals(object obj)
  {
    return obj is DispCallable dispCallable && dispCallable._dispatch == this._dispatch && dispCallable._dispId == this._dispId;
  }

  public override int GetHashCode() => this._dispatch.GetHashCode() ^ this._dispId;
}
