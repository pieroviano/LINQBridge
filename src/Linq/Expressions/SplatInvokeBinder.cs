// Type: System.Dynamic.SplatInvokeBinder
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

#nullable disable
namespace System.Linq.Expressions;

internal sealed class SplatInvokeBinder : CallSiteBinder
{
  internal static readonly SplatInvokeBinder Instance = new SplatInvokeBinder();

  public override Expression Bind(
    object[] args,
    ReadOnlyCollection<ParameterExpression> parameters,
    LabelTarget returnLabel)
  {
    int length = ((object[]) args[1]).Length;
    ParameterExpression parameter = parameters[1];
    ReadOnlyCollectionBuilder<Expression> arguments = new ReadOnlyCollectionBuilder<Expression>(length + 1);
    Type[] typeArray = new Type[length + 3];
    arguments.Add((Expression) parameters[0]);
    typeArray[0] = typeof (CallSite);
    typeArray[1] = typeof (object);
    for (int index = 0; index < length; ++index)
    {
      arguments.Add((Expression) Expression.ArrayAccess((Expression) parameter, (Expression) Expression.Constant((object) index)));
      typeArray[index + 2] = typeof (object).MakeByRefType();
    }
    typeArray[typeArray.Length - 1] = typeof (object);
    return (Expression) Expression.IfThen((Expression) Expression.Equal(
        (Expression) Expression.ArrayLength((Expression) parameter), 
        (Expression) Expression.Constant((object) length)), 
        (Expression) Expression.Return(returnLabel, 
            (Expression) Expression.MakeDynamic(
                Expression.GetDelegateType(typeArray), 
                (CallSiteBinder) new ComInvokeAction(new CallInfo(length, new string[0])), 
                (IEnumerable<Expression>) arguments)));
  }
}
