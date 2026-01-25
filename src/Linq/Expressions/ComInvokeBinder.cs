// Type: System.Dynamic.ComInvokeBinder
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.InteropServices;

#nullable disable
namespace System.Linq.Expressions;

internal sealed class ComInvokeBinder
{
  private readonly ComMethodDesc _methodDesc;
  private readonly Expression _method;
  private readonly Expression _dispatch;
  private readonly CallInfo _callInfo;
  private readonly DynamicMetaObject[] _args;
  private readonly bool[] _isByRef;
  private readonly Expression _instance;
  private BindingRestrictions _restrictions;
  private VarEnumSelector _varEnumSelector;
  private string[] _keywordArgNames;
  private int _totalExplicitArgs;
  private ParameterExpression _dispatchObject;
  private ParameterExpression _dispatchPointer;
  private ParameterExpression _dispId;
  private ParameterExpression _dispParams;
  private ParameterExpression _paramVariants;
  private ParameterExpression _invokeResult;
  private ParameterExpression _returnValue;
  private ParameterExpression _dispIdsOfKeywordArgsPinned;
  private ParameterExpression _propertyPutDispId;

  internal ComInvokeBinder(
    CallInfo callInfo,
    DynamicMetaObject[] args,
    bool[] isByRef,
    BindingRestrictions restrictions,
    Expression method,
    Expression dispatch,
    ComMethodDesc methodDesc)
  {
    this._method = method;
    this._dispatch = dispatch;
    this._methodDesc = methodDesc;
    this._callInfo = callInfo;
    this._args = args;
    this._isByRef = isByRef;
    this._restrictions = restrictions;
    this._instance = dispatch;
  }

  private ParameterExpression DispatchObjectVariable
  {
    get
    {
      return ComInvokeBinder.EnsureVariable(ref this._dispatchObject, typeof (IDispatch), "dispatchObject");
    }
  }

  private ParameterExpression DispatchPointerVariable
  {
    get
    {
      return ComInvokeBinder.EnsureVariable(ref this._dispatchPointer, typeof (IntPtr), "dispatchPointer");
    }
  }

  private ParameterExpression DispIdVariable
  {
    get => ComInvokeBinder.EnsureVariable(ref this._dispId, typeof (int), "dispId");
  }

  private ParameterExpression DispParamsVariable
  {
    get => ComInvokeBinder.EnsureVariable(ref this._dispParams, typeof (System.Runtime.InteropServices.ComTypes.DISPPARAMS), "dispParams");
  }

  private ParameterExpression InvokeResultVariable
  {
    get => ComInvokeBinder.EnsureVariable(ref this._invokeResult, typeof (Variant), "invokeResult");
  }

  private ParameterExpression ReturnValueVariable
  {
    get => ComInvokeBinder.EnsureVariable(ref this._returnValue, typeof (object), "returnValue");
  }

  private ParameterExpression DispIdsOfKeywordArgsPinnedVariable
  {
    get
    {
      return ComInvokeBinder.EnsureVariable(ref this._dispIdsOfKeywordArgsPinned, typeof (GCHandle), "dispIdsOfKeywordArgsPinned");
    }
  }

  private ParameterExpression PropertyPutDispIdVariable
  {
    get
    {
      return ComInvokeBinder.EnsureVariable(ref this._propertyPutDispId, typeof (int), "propertyPutDispId");
    }
  }

  private ParameterExpression ParamVariantsVariable
  {
    get
    {
      if (this._paramVariants == null)
        this._paramVariants = Expression.Variable(VariantArray.GetStructType(this._args.Length), "paramVariants");
      return this._paramVariants;
    }
  }

  private static ParameterExpression EnsureVariable(
    ref ParameterExpression var,
    Type type,
    string name)
  {
    return var != null ? var : (var = Expression.Variable(type, name));
  }

  private static Type MarshalType(DynamicMetaObject mo, bool isByRef)
  {
    Type type = mo.Value != null || !mo.HasValue || mo.LimitType.IsValueType ? mo.LimitType : (Type) null;
    if (isByRef)
    {
      if (type == (Type) null)
        type = mo.Expression.Type;
      type = type.MakeByRefType();
    }
    return type;
  }

  internal DynamicMetaObject Invoke()
  {
    this._keywordArgNames = this._callInfo.ArgumentNames.ToArray<string>();
    this._totalExplicitArgs = this._args.Length;
    Type[] explicitArgTypes = new Type[this._args.Length];
    for (int index = 0; index < this._args.Length; ++index)
    {
      DynamicMetaObject mo = this._args[index];
      this._restrictions = this._restrictions.Merge(ComBinderHelpers.GetTypeRestrictionForDynamicMetaObject(mo));
      explicitArgTypes[index] = ComInvokeBinder.MarshalType(mo, this._isByRef[index]);
    }
    this._varEnumSelector = new VarEnumSelector(explicitArgTypes);
    return new DynamicMetaObject(this.CreateScope(this.MakeIDispatchInvokeTarget()), BindingRestrictions.Combine((IList<DynamicMetaObject>) this._args).Merge(this._restrictions));
  }

  private static void AddNotNull(List<ParameterExpression> list, ParameterExpression var)
  {
    if (var == null)
      return;
    list.Add(var);
  }

  private Expression CreateScope(Expression expression)
  {
    List<ParameterExpression> parameterExpressionList = new List<ParameterExpression>();
    ComInvokeBinder.AddNotNull(parameterExpressionList, this._dispatchObject);
    ComInvokeBinder.AddNotNull(parameterExpressionList, this._dispatchPointer);
    ComInvokeBinder.AddNotNull(parameterExpressionList, this._dispId);
    ComInvokeBinder.AddNotNull(parameterExpressionList, this._dispParams);
    ComInvokeBinder.AddNotNull(parameterExpressionList, this._paramVariants);
    ComInvokeBinder.AddNotNull(parameterExpressionList, this._invokeResult);
    ComInvokeBinder.AddNotNull(parameterExpressionList, this._returnValue);
    ComInvokeBinder.AddNotNull(parameterExpressionList, this._dispIdsOfKeywordArgsPinned);
    ComInvokeBinder.AddNotNull(parameterExpressionList, this._propertyPutDispId);
    if (parameterExpressionList.Count <= 0)
      return expression;
    return (Expression) Expression.Block((IEnumerable<ParameterExpression>) parameterExpressionList, expression);
  }

  private Expression GenerateTryBlock()
  {
    ParameterExpression parameterExpression1 = Expression.Variable(typeof (ExcepInfo), "excepInfo");
    ParameterExpression parameterExpression2 = Expression.Variable(typeof (uint), "argErr");
    ParameterExpression left = Expression.Variable(typeof (int), "hresult");
    List<Expression> expressionList = new List<Expression>();
    if (this._keywordArgNames.Length != 0)
    {
      string[] strArray = ((IList<string>) this._keywordArgNames).AddFirst<string>(this._methodDesc.Name);
      expressionList.Add((Expression) Expression.Assign((Expression) Expression.Field((Expression) this.DispParamsVariable, typeof (System.Runtime.InteropServices.ComTypes.DISPPARAMS).GetField("rgdispidNamedArgs")), (Expression) Expression.Call(typeof (UnsafeMethods).GetMethod("GetIdsOfNamedParameters"), (Expression) this.DispatchObjectVariable, (Expression) Expression.Constant((object) strArray), (Expression) this.DispIdVariable, (Expression) this.DispIdsOfKeywordArgsPinnedVariable)));
    }
    Expression[] expressionArray1 = this.MakeArgumentExpressions();
    int num1 = this._varEnumSelector.VariantBuilders.Length - 1;
    int num2 = this._varEnumSelector.VariantBuilders.Length - this._keywordArgNames.Length;
    int index1 = 0;
    while (index1 < this._varEnumSelector.VariantBuilders.Length)
    {
      int field = index1 < num2 ? num1 : index1 - num2;
      Expression expression = this._varEnumSelector.VariantBuilders[index1].InitializeArgumentVariant(VariantArray.GetStructField(this.ParamVariantsVariable, field), expressionArray1[index1 + 1]);
      if (expression != null)
        expressionList.Add(expression);
      ++index1;
      --num1;
    }
    System.Runtime.InteropServices.ComTypes.INVOKEKIND invokekind = !this._methodDesc.IsPropertyPut ? System.Runtime.InteropServices.ComTypes.INVOKEKIND.INVOKE_FUNC | System.Runtime.InteropServices.ComTypes.INVOKEKIND.INVOKE_PROPERTYGET : (!this._methodDesc.IsPropertyPutRef ? System.Runtime.InteropServices.ComTypes.INVOKEKIND.INVOKE_PROPERTYPUT : System.Runtime.InteropServices.ComTypes.INVOKEKIND.INVOKE_PROPERTYPUTREF);
    MethodCallExpression right1 = Expression.Call(typeof (UnsafeMethods).GetMethod("IDispatchInvoke"), (Expression) this.DispatchPointerVariable, (Expression) this.DispIdVariable, (Expression) Expression.Constant((object) invokekind), (Expression) this.DispParamsVariable, (Expression) this.InvokeResultVariable, (Expression) parameterExpression1, (Expression) parameterExpression2);
    Expression expression1 = (Expression) Expression.Assign((Expression) left, (Expression) right1);
    expressionList.Add(expression1);
    Expression expression2 = (Expression) Expression.Call(typeof (ComRuntimeHelpers).GetMethod("CheckThrowException"), (Expression) left, (Expression) parameterExpression1, (Expression) parameterExpression2, (Expression) Expression.Constant((object) this._methodDesc.Name, typeof (string)));
    expressionList.Add(expression2);
    Expression right2 = (Expression) Expression.Call((Expression) this.InvokeResultVariable, typeof (Variant).GetMethod("ToObject"));
    VariantBuilder[] variantBuilders = this._varEnumSelector.VariantBuilders;
    Expression[] expressionArray2 = this.MakeArgumentExpressions();
    expressionList.Add((Expression) Expression.Assign((Expression) this.ReturnValueVariable, right2));
    int index2 = 0;
    for (int length = variantBuilders.Length; index2 < length; ++index2)
    {
      Expression expression3 = variantBuilders[index2].UpdateFromReturn(expressionArray2[index2 + 1]);
      if (expression3 != null)
        expressionList.Add(expression3);
    }
    expressionList.Add((Expression) Expression.Empty());
    return (Expression) Expression.Block((IEnumerable<ParameterExpression>) new ParameterExpression[3]
    {
      parameterExpression1,
      parameterExpression2,
      left
    }, (IEnumerable<Expression>) expressionList);
  }

  private Expression GenerateFinallyBlock()
  {
    List<Expression> expressionList = new List<Expression>();
    expressionList.Add((Expression) Expression.Call(typeof (UnsafeMethods).GetMethod("IUnknownRelease"), (Expression) this.DispatchPointerVariable));
    int index = 0;
    for (int length = this._varEnumSelector.VariantBuilders.Length; index < length; ++index)
    {
      Expression expression = this._varEnumSelector.VariantBuilders[index].Clear();
      if (expression != null)
        expressionList.Add(expression);
    }
    expressionList.Add((Expression) Expression.Call((Expression) this.InvokeResultVariable, typeof (Variant).GetMethod("Clear")));
    if (this._dispIdsOfKeywordArgsPinned != null)
      expressionList.Add((Expression) Expression.Call((Expression) this.DispIdsOfKeywordArgsPinnedVariable, typeof (GCHandle).GetMethod("Free")));
    expressionList.Add((Expression) Expression.Empty());
    return (Expression) Expression.Block((IEnumerable<Expression>) expressionList);
  }

  private Expression MakeIDispatchInvokeTarget()
  {
    List<Expression> expressionList = new List<Expression>();
    expressionList.Add((Expression) Expression.Assign((Expression) this.DispIdVariable, (Expression) Expression.Property(this._method, typeof (ComMethodDesc).GetProperty("DispId"))));
    if (this._totalExplicitArgs != 0)
      expressionList.Add((Expression) Expression.Assign((Expression) Expression.Field((Expression) this.DispParamsVariable, typeof (System.Runtime.InteropServices.ComTypes.DISPPARAMS).GetField("rgvarg")), (Expression) Expression.Call(typeof (UnsafeMethods).GetMethod("ConvertVariantByrefToPtr"), (Expression) VariantArray.GetStructField(this.ParamVariantsVariable, 0))));
    expressionList.Add((Expression) Expression.Assign((Expression) Expression.Field((Expression) this.DispParamsVariable, typeof (System.Runtime.InteropServices.ComTypes.DISPPARAMS).GetField("cArgs")), (Expression) Expression.Constant((object) this._totalExplicitArgs)));
    if (this._methodDesc.IsPropertyPut)
    {
      expressionList.Add((Expression) Expression.Assign((Expression) Expression.Field((Expression) this.DispParamsVariable, typeof (System.Runtime.InteropServices.ComTypes.DISPPARAMS).GetField("cNamedArgs")), (Expression) Expression.Constant((object) 1)));
      expressionList.Add((Expression) Expression.Assign((Expression) this.PropertyPutDispIdVariable, (Expression) Expression.Constant((object) -3)));
      expressionList.Add((Expression) Expression.Assign((Expression) Expression.Field((Expression) this.DispParamsVariable, typeof (System.Runtime.InteropServices.ComTypes.DISPPARAMS).GetField("rgdispidNamedArgs")), (Expression) Expression.Call(typeof (UnsafeMethods).GetMethod("ConvertInt32ByrefToPtr"), (Expression) this.PropertyPutDispIdVariable)));
    }
    else
      expressionList.Add((Expression) Expression.Assign((Expression) Expression.Field((Expression) this.DispParamsVariable, typeof (System.Runtime.InteropServices.ComTypes.DISPPARAMS).GetField("cNamedArgs")), (Expression) Expression.Constant((object) this._keywordArgNames.Length)));
    expressionList.Add((Expression) Expression.Assign((Expression) this.DispatchObjectVariable, this._dispatch));
    expressionList.Add((Expression) Expression.Assign((Expression) this.DispatchPointerVariable, (Expression) Expression.Call(typeof (Marshal).GetMethod("GetIDispatchForObject"), (Expression) this.DispatchObjectVariable)));
    Expression tryBlock = this.GenerateTryBlock();
    Expression finallyBlock = this.GenerateFinallyBlock();
    expressionList.Add((Expression) Expression.TryFinally(tryBlock, finallyBlock));
    expressionList.Add((Expression) this.ReturnValueVariable);
    List<ParameterExpression> variables = new List<ParameterExpression>();
    foreach (VariantBuilder variantBuilder in this._varEnumSelector.VariantBuilders)
    {
      if (variantBuilder.TempVariable != null)
        variables.Add(variantBuilder.TempVariable);
    }
    return (Expression) Expression.Block((IEnumerable<ParameterExpression>) variables, (IEnumerable<Expression>) expressionList);
  }

  private Expression[] MakeArgumentExpressions()
  {
    int num = 0;
    Expression[] expressionArray;
    if (this._instance != null)
    {
      expressionArray = new Expression[this._args.Length + 1];
      expressionArray[num++] = this._instance;
    }
    else
      expressionArray = new Expression[this._args.Length];
    for (int index = 0; index < this._args.Length; ++index)
      expressionArray[num++] = this._args[index].Expression;
    return expressionArray;
  }
}
