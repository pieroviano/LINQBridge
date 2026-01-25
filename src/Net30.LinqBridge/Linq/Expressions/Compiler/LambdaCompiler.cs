#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic.Utils;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Linq.Expressions.Compiler;

internal sealed class LambdaCompiler
{
    private static int _Counter;
    private readonly BoundConstants _boundConstants;
    private readonly KeyedQueue<Type, LocalBuilder> _freeLocals = new();
    private readonly StackGuard _guard = new();
    private readonly bool _hasClosureArgument;
    private readonly Dictionary<LabelTarget, LabelInfo> _labelInfo = new();
    private readonly LambdaExpression _lambda;
    private readonly MethodInfo _method;
    private readonly AnalyzedTree _tree;
    private readonly TypeBuilder _typeBuilder;
    private LabelScopeInfo _labelBlock = new(null, LabelScopeKind.Lambda);
    private CompilerScope _scope;
    private bool _sequencePointCleared;

    private LambdaCompiler(AnalyzedTree tree, LambdaExpression lambda)
    {
        var parameterTypes = GetParameterTypes(lambda).AddFirst(typeof(Closure));
        var dynamicMethod = new DynamicMethod(lambda.Name ?? "lambda_method", lambda.ReturnType, parameterTypes, true);
        _tree = tree;
        _lambda = lambda;
        _method = dynamicMethod;
        IL = dynamicMethod.GetILGenerator();
        _hasClosureArgument = true;
        _scope = tree.Scopes[lambda];
        _boundConstants = tree.Constants[lambda];
        InitializeMethod();
    }

    private LambdaCompiler(AnalyzedTree tree, LambdaExpression lambda, MethodBuilder method)
    {
        _hasClosureArgument = tree.Scopes[lambda].NeedsClosure;
        var list = GetParameterTypes(lambda);
        if (_hasClosureArgument)
        {
            list = list.AddFirst(typeof(Closure));
        }

        method.SetReturnType(lambda.ReturnType);
        method.SetParameters(list);
        var strArray = lambda.Parameters.Map((Func<ParameterExpression, string>)(p => p.Name));
        var num = _hasClosureArgument ? 2 : 1;
        for (var index = 0; index < strArray.Length; ++index)
        {
            method.DefineParameter(index + num, ParameterAttributes.None, strArray[index]);
        }

        _tree = tree;
        _lambda = lambda;
        _typeBuilder = (TypeBuilder)method.DeclaringType;
        _method = method;
        IL = method.GetILGenerator();
        _scope = tree.Scopes[lambda];
        _boundConstants = tree.Constants[lambda];
        InitializeMethod();
    }

    private LambdaCompiler(LambdaCompiler parent, LambdaExpression lambda)
    {
        _tree = parent._tree;
        _lambda = lambda;
        _method = parent._method;
        IL = parent.IL;
        _hasClosureArgument = parent._hasClosureArgument;
        _typeBuilder = parent._typeBuilder;
        _scope = _tree.Scopes[lambda];
        _boundConstants = parent._boundConstants;
    }

    private bool EmitDebugSymbols => _tree.DebugInfoGenerator != null;

    internal ILGenerator IL { get; }

    internal ReadOnlyCollection<ParameterExpression> Parameters => _lambda.Parameters;

    internal bool CanEmitBoundConstants => _method is DynamicMethod;

    public override string ToString()
    {
        return _method.ToString();
    }

    internal static Delegate Compile(LambdaExpression lambda, DebugInfoGenerator debugInfoGenerator)
    {
        var tree = AnalyzeLambda(ref lambda);
        tree.DebugInfoGenerator = debugInfoGenerator;
        var lambdaCompiler = new LambdaCompiler(tree, lambda);
        lambdaCompiler.EmitLambdaBody();
        return lambdaCompiler.CreateDelegate();
    }

    internal static void Compile(
        LambdaExpression lambda,
        MethodBuilder method,
        DebugInfoGenerator debugInfoGenerator)
    {
        var tree = AnalyzeLambda(ref lambda);
        tree.DebugInfoGenerator = debugInfoGenerator;
        new LambdaCompiler(tree, lambda, method).EmitLambdaBody();
    }

    internal void EmitClosureArgument()
    {
        IL.EmitLoadArg(0);
    }

    internal void EmitConstantArray<T>(T[] array)
    {
        if (_method is DynamicMethod)
        {
            EmitConstant(array, typeof(T[]));
        }
        else if (_typeBuilder != null)
        {
            var staticField = CreateStaticField("ConstantArray", typeof(T[]));
            var label = IL.DefineLabel();
            IL.Emit(OpCodes.Ldsfld, staticField);
            IL.Emit(OpCodes.Ldnull);
            IL.Emit(OpCodes.Bne_Un, label);
            IL.EmitArray(array);
            IL.Emit(OpCodes.Stsfld, staticField);
            IL.MarkLabel(label);
            IL.Emit(OpCodes.Ldsfld, staticField);
        }
        else
        {
            IL.EmitArray(array);
        }
    }

    internal void EmitExpression(Expression node)
    {
        EmitExpression(node, CompilationFlags.EmitExpressionStart | CompilationFlags.EmitAsNoTail);
    }

    internal void EmitLambdaArgument(int index)
    {
        IL.EmitLoadArg(GetLambdaArgument(index));
    }

    internal void FreeLocal(LocalBuilder local)
    {
        if (local == null)
        {
            return;
        }

        _freeLocals.Enqueue(local.LocalType, local);
    }

    internal int GetLambdaArgument(int index)
    {
        return index + (_hasClosureArgument ? 1 : 0) + (_method.IsStatic ? 0 : 1);
    }

    internal LocalBuilder GetLocal(Type type)
    {
        LocalBuilder localBuilder;
        return _freeLocals.TryDequeue(type, out localBuilder) ? localBuilder : IL.DeclareLocal(type);
    }

    internal LocalBuilder GetNamedLocal(Type type, ParameterExpression variable)
    {
        var localBuilder = IL.DeclareLocal(type);
        if (EmitDebugSymbols && variable.Name != null)
        {
            _tree.DebugInfoGenerator.SetLocalName(localBuilder, variable.Name);
        }

        return localBuilder;
    }

    internal static void ValidateLift(
        IList<ParameterExpression> variables,
        IList<Expression> arguments)
    {
        if (variables.Count != arguments.Count)
        {
            throw Error.IncorrectNumberOfIndexes();
        }

        var index = 0;
        for (var count = variables.Count; index < count; ++index)
        {
            if (!TypeUtils.AreReferenceAssignable(variables[index].Type, arguments[index].Type.GetNonNullableType()))
            {
                throw Error.ArgumentTypesMustMatch();
            }
        }
    }

    private void AddressOf(BinaryExpression node, Type type)
    {
        if (TypeUtils.AreEquivalent(type, node.Type))
        {
            EmitExpression(node.Left);
            EmitExpression(node.Right);
            var type1 = node.Right.Type;
            if (type1.IsNullableType())
            {
                var local = GetLocal(type1);
                IL.Emit(OpCodes.Stloc, local);
                IL.Emit(OpCodes.Ldloca, local);
                IL.EmitGetValue(type1);
                FreeLocal(local);
            }

            var nonNullableType = type1.GetNonNullableType();
            if (nonNullableType != typeof(int))
            {
                IL.EmitConvertToType(nonNullableType, typeof(int), true);
            }

            IL.Emit(OpCodes.Ldelema, node.Type);
        }
        else
        {
            EmitExpressionAddress(node, type);
        }
    }

    private void AddressOf(ParameterExpression node, Type type)
    {
        if (TypeUtils.AreEquivalent(type, node.Type))
        {
            if (node.IsByRef)
            {
                _scope.EmitGet(node);
            }
            else
            {
                _scope.EmitAddressOf(node);
            }
        }
        else
        {
            EmitExpressionAddress(node, type);
        }
    }

    private void AddressOf(MemberExpression node, Type type)
    {
        if (TypeUtils.AreEquivalent(type, node.Type))
        {
            var objectType = (Type)null;
            if (node.Expression != null)
            {
                EmitInstance(node.Expression, objectType = node.Expression.Type);
            }

            EmitMemberAddress(node.Member, objectType);
        }
        else
        {
            EmitExpressionAddress(node, type);
        }
    }

    private void AddressOf(MethodCallExpression node, Type type)
    {
        if (!node.Method.IsStatic && node.Object.Type.IsArray && node.Method ==
            node.Object.Type.GetMethod("Get", BindingFlags.Instance | BindingFlags.Public))
        {
            var method = node.Object.Type.GetMethod("Address", BindingFlags.Instance | BindingFlags.Public);
            EmitMethodCall(node.Object, method, node);
        }
        else
        {
            EmitExpressionAddress(node, type);
        }
    }

    private void AddressOf(IndexExpression node, Type type)
    {
        if (!TypeUtils.AreEquivalent(type, node.Type) || node.Indexer != null)
        {
            EmitExpressionAddress(node, type);
        }
        else if (node.Arguments.Count == 1)
        {
            EmitExpression(node.Object);
            EmitExpression(node.Arguments[0]);
            IL.Emit(OpCodes.Ldelema, node.Type);
        }
        else
        {
            var method = node.Object.Type.GetMethod("Address", BindingFlags.Instance | BindingFlags.Public);
            EmitMethodCall(node.Object, method, node);
        }
    }

    private void AddressOf(UnaryExpression node, Type type)
    {
        EmitExpression(node.Operand);
        IL.Emit(OpCodes.Unbox, type);
    }

    private WriteBack AddressOfWriteBack(MemberExpression node)
    {
        if (node.Member.MemberType != MemberTypes.Property || !((PropertyInfo)node.Member).CanWrite)
        {
            return null;
        }

        var instanceLocal = (LocalBuilder)null;
        var instanceType = (Type)null;
        if (node.Expression != null)
        {
            EmitInstance(node.Expression, instanceType = node.Expression.Type);
            IL.Emit(OpCodes.Dup);
            IL.Emit(OpCodes.Stloc, instanceLocal = GetLocal(instanceType));
        }

        var pi = (PropertyInfo)node.Member;
        EmitCall(instanceType, pi.GetGetMethod(true));
        var valueLocal = GetLocal(node.Type);
        IL.Emit(OpCodes.Stloc, valueLocal);
        IL.Emit(OpCodes.Ldloca, valueLocal);
        return () =>
        {
            if (instanceLocal != null)
            {
                IL.Emit(OpCodes.Ldloc, instanceLocal);
                FreeLocal(instanceLocal);
            }

            IL.Emit(OpCodes.Ldloc, valueLocal);
            FreeLocal(valueLocal);
            EmitCall(instanceType, pi.GetSetMethod(true));
        };
    }

    private WriteBack AddressOfWriteBack(IndexExpression node)
    {
        if (node.Indexer == null || !node.Indexer.CanWrite)
        {
            return null;
        }

        var instanceLocal = (LocalBuilder)null;
        var instanceType = (Type)null;
        if (node.Object != null)
        {
            EmitInstance(node.Object, instanceType = node.Object.Type);
            IL.Emit(OpCodes.Dup);
            IL.Emit(OpCodes.Stloc, instanceLocal = GetLocal(instanceType));
        }

        var args = new List<LocalBuilder>();
        foreach (var node1 in node.Arguments)
        {
            EmitExpression(node1);
            var local = GetLocal(node1.Type);
            IL.Emit(OpCodes.Dup);
            IL.Emit(OpCodes.Stloc, local);
            args.Add(local);
        }

        EmitGetIndexCall(node, instanceType);
        var valueLocal = GetLocal(node.Type);
        IL.Emit(OpCodes.Stloc, valueLocal);
        IL.Emit(OpCodes.Ldloca, valueLocal);
        return () =>
        {
            if (instanceLocal != null)
            {
                IL.Emit(OpCodes.Ldloc, instanceLocal);
                FreeLocal(instanceLocal);
            }

            foreach (var local in args)
            {
                IL.Emit(OpCodes.Ldloc, local);
                FreeLocal(local);
            }

            IL.Emit(OpCodes.Ldloc, valueLocal);
            FreeLocal(valueLocal);
            EmitSetIndexCall(node, instanceType);
        };
    }

    private void AddReturnLabel(LambdaExpression lambda)
    {
        var node = lambda.Body;
        label_1:
        switch (node.NodeType)
        {
            case ExpressionType.Block:
                var blockExpression = (BlockExpression)node;
                for (var index = blockExpression.ExpressionCount - 1; index >= 0; --index)
                {
                    node = blockExpression.GetExpression(index);
                    if (Significant(node))
                    {
                        break;
                    }
                }

                goto label_1;
            case ExpressionType.Label:
                var target = ((LabelExpression)node).Target;
                _labelInfo.Add(target,
                    new LabelInfo(IL, target, TypeUtils.AreReferenceAssignable(lambda.ReturnType, target.Type)));
                break;
        }
    }

    private static void AddToBuckets(
        List<List<SwitchLabel>> buckets,
        SwitchLabel key)
    {
        if (buckets.Count > 0)
        {
            var bucket = buckets[buckets.Count - 1];
            if (FitsInBucket(bucket, key.Key, 1))
            {
                bucket.Add(key);
                MergeBuckets(buckets);
                return;
            }
        }

        buckets.Add(new List<SwitchLabel>
        {
            key
        });
    }

    private static AnalyzedTree AnalyzeLambda(ref LambdaExpression lambda)
    {
        lambda = StackSpiller.AnalyzeLambda(lambda);
        return VariableBinder.Bind(lambda);
    }

    private static bool CanOptimizeSwitchType(Type valueType)
    {
        switch (Type.GetTypeCode(valueType))
        {
            case TypeCode.Char:
            case TypeCode.SByte:
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64:
                return true;
            default:
                return false;
        }
    }

    private void CheckRethrow()
    {
        for (var labelScopeInfo = _labelBlock; labelScopeInfo != null; labelScopeInfo = labelScopeInfo.Parent)
        {
            if (labelScopeInfo.Kind == LabelScopeKind.Catch)
            {
                return;
            }

            if (labelScopeInfo.Kind == LabelScopeKind.Finally)
            {
                break;
            }
        }

        throw Error.RethrowRequiresCatch();
    }

    private void CheckTry()
    {
        for (var labelScopeInfo = _labelBlock; labelScopeInfo != null; labelScopeInfo = labelScopeInfo.Parent)
        {
            if (labelScopeInfo.Kind == LabelScopeKind.Filter)
            {
                throw Error.TryNotAllowedInFilter();
            }
        }
    }

    private static decimal ConvertSwitchValue(object value)
    {
        return value is char ch ? ch : Convert.ToDecimal(value, CultureInfo.InvariantCulture);
    }

    private Delegate CreateDelegate()
    {
        return _method.CreateDelegate(_lambda.Type, new Closure(_boundConstants.ToArray(), null));
    }

    private MemberExpression CreateLazyInitializedField<T>(string name)
    {
        return _method is DynamicMethod
            ? Expression.Field(Expression.Constant(new StrongBox<T>(default)), "Value")
            : Expression.Field(null, CreateStaticField(name, typeof(T)));
    }

    private FieldBuilder CreateStaticField(string name, Type type)
    {
        return _typeBuilder.DefineField(
            $"<ExpressionCompilerImplementationDetails>{{{Interlocked.Increment(ref _Counter).ToString()}}}{name}",
            type, FieldAttributes.Private | FieldAttributes.Static);
    }

    private void DefineBlockLabels(Expression node)
    {
        if (!(node is BlockExpression blockExpression) || blockExpression is SpilledExpressionBlock)
        {
            return;
        }

        var index = 0;
        for (var expressionCount = blockExpression.ExpressionCount; index < expressionCount; ++index)
        {
            if (blockExpression.GetExpression(index) is LabelExpression expression)
            {
                DefineLabel(expression.Target);
            }
        }
    }

    private LabelInfo DefineLabel(LabelTarget node)
    {
        if (node == null)
        {
            return new LabelInfo(IL, null, false);
        }

        var labelInfo = EnsureLabel(node);
        labelInfo.Define(_labelBlock);
        return labelInfo;
    }

    private void DefineSwitchCaseLabel(SwitchCase @case, out Label label, out bool isGoto)
    {
        if (@case.Body is GotoExpression body && body.Value == null)
        {
            var labelInfo = ReferenceLabel(body.Target);
            if (labelInfo.CanBranch)
            {
                label = labelInfo.Label;
                isGoto = true;
                return;
            }
        }

        label = IL.DefineLabel();
        isGoto = false;
    }

    private void Emit(BlockExpression node, CompilationFlags flags)
    {
        EnterScope(node);
        var compilationFlags = flags & CompilationFlags.EmitAsTypeMask;
        var expressionCount = node.ExpressionCount;
        var flags1 = flags & CompilationFlags.EmitAsTailCallMask;
        for (var index = 0; index < expressionCount - 1; ++index)
        {
            var expression1 = node.GetExpression(index);
            var expression2 = node.GetExpression(index + 1);
            if (!EmitDebugSymbols || !(expression1 is DebugInfoExpression debugInfoExpression) ||
                !debugInfoExpression.IsClear || !(expression2 is DebugInfoExpression))
            {
                var newValue = flags1 == CompilationFlags.EmitAsNoTail ? CompilationFlags.EmitAsNoTail :
                    !(expression2 is GotoExpression gotoExpression) ||
                    (gotoExpression.Value != null && Significant(gotoExpression.Value)) ||
                    !ReferenceLabel(gotoExpression.Target).CanReturn ? CompilationFlags.EmitAsMiddle :
                    CompilationFlags.EmitAsTail;
                flags = UpdateEmitAsTailCallFlag(flags, newValue);
                EmitExpressionAsVoid(expression1, flags);
            }
        }

        if (compilationFlags == CompilationFlags.EmitAsVoidType || node.Type == typeof(void))
        {
            EmitExpressionAsVoid(node.GetExpression(expressionCount - 1), flags1);
        }
        else
        {
            EmitExpressionAsType(node.GetExpression(expressionCount - 1), node.Type, flags1);
        }

        ExitScope(node);
    }

    private void EmitAddress(Expression node, Type type)
    {
        EmitAddress(node, type, CompilationFlags.EmitExpressionStart);
    }

    private void EmitAddress(Expression node, Type type, CompilationFlags flags)
    {
        var flag = (flags & CompilationFlags.EmitExpressionStartMask) == CompilationFlags.EmitExpressionStart;
        var flags1 = flag ? EmitExpressionStart(node) : CompilationFlags.EmitNoExpressionStart;
        switch (node.NodeType)
        {
            case ExpressionType.ArrayIndex:
                AddressOf((BinaryExpression)node, type);
                break;
            case ExpressionType.Call:
                AddressOf((MethodCallExpression)node, type);
                break;
            case ExpressionType.MemberAccess:
                AddressOf((MemberExpression)node, type);
                break;
            case ExpressionType.Parameter:
                AddressOf((ParameterExpression)node, type);
                break;
            case ExpressionType.Index:
                AddressOf((IndexExpression)node, type);
                break;
            case ExpressionType.Unbox:
                AddressOf((UnaryExpression)node, type);
                break;
            default:
                EmitExpressionAddress(node, type);
                break;
        }

        if (!flag)
        {
            return;
        }

        EmitExpressionEnd(flags1);
    }

    private WriteBack EmitAddressWriteBack(Expression node, Type type)
    {
        var flags = EmitExpressionStart(node);
        var writeBack = (WriteBack)null;
        if (TypeUtils.AreEquivalent(type, node.Type))
        {
            switch (node.NodeType)
            {
                case ExpressionType.MemberAccess:
                    writeBack = AddressOfWriteBack((MemberExpression)node);
                    break;
                case ExpressionType.Index:
                    writeBack = AddressOfWriteBack((IndexExpression)node);
                    break;
            }
        }

        if (writeBack == null)
        {
            EmitAddress(node, type, CompilationFlags.EmitNoExpressionStart | CompilationFlags.EmitAsNoTail);
        }

        EmitExpressionEnd(flags);
        return writeBack;
    }

    private void EmitAndAlsoBinaryExpression(Expression expr, CompilationFlags flags)
    {
        var b = (BinaryExpression)expr;
        if (b.Method != null && !b.IsLiftedLogical)
        {
            EmitMethodAndAlso(b, flags);
        }
        else if (b.Left.Type == typeof(bool?))
        {
            EmitLiftedAndAlso(b);
        }
        else if (b.IsLiftedLogical)
        {
            EmitExpression(b.ReduceUserdefinedLifted());
        }
        else
        {
            EmitUnliftedAndAlso(b);
        }
    }

    private List<WriteBack> EmitArguments(MethodBase method, IArgumentProvider args)
    {
        return EmitArguments(method, args, 0);
    }

    private List<WriteBack> EmitArguments(
        MethodBase method,
        IArgumentProvider args,
        int skipParameters)
    {
        var parametersCached = method.GetParameters();
        var writeBackList = new List<WriteBack>();
        var index = skipParameters;
        for (var length = parametersCached.Length; index < length; ++index)
        {
            var parameterInfo = parametersCached[index];
            var node = args.GetArgument(index - skipParameters);
            var parameterType = parameterInfo.ParameterType;
            if (parameterType.IsByRef)
            {
                var elementType = parameterType.GetElementType();
                var writeBack = EmitAddressWriteBack(node, elementType);
                if (writeBack != null)
                {
                    writeBackList.Add(writeBack);
                }
            }
            else
            {
                EmitExpression(node);
            }
        }

        return writeBackList;
    }

    private void EmitAssign(BinaryExpression node, CompilationFlags emitAs)
    {
        switch (node.Left.NodeType)
        {
            case ExpressionType.MemberAccess:
                EmitMemberAssignment(node, emitAs);
                break;
            case ExpressionType.Parameter:
                EmitVariableAssignment(node, emitAs);
                break;
            case ExpressionType.Index:
                EmitIndexAssignment(node, emitAs);
                break;
            default:
                throw Error.InvalidLvalue(node.Left.NodeType);
        }
    }

    private void EmitAssignBinaryExpression(Expression expr)
    {
        EmitAssign((BinaryExpression)expr, CompilationFlags.EmitAsDefaultType);
    }

    private void EmitBinaryExpression(Expression expr)
    {
        EmitBinaryExpression(expr, CompilationFlags.EmitAsNoTail);
    }

    private void EmitBinaryExpression(Expression expr, CompilationFlags flags)
    {
        var b = (BinaryExpression)expr;
        if (b.Method != null)
        {
            EmitBinaryMethod(b, flags);
        }
        else
        {
            if ((b.NodeType == ExpressionType.Equal || b.NodeType == ExpressionType.NotEqual) &&
                (b.Type == typeof(bool) || b.Type == typeof(bool?)))
            {
                if (ConstantCheck.IsNull(b.Left) && !ConstantCheck.IsNull(b.Right) && b.Right.Type.IsNullableType())
                {
                    EmitNullEquality(b.NodeType, b.Right, b.IsLiftedToNull);
                    return;
                }

                if (ConstantCheck.IsNull(b.Right) && !ConstantCheck.IsNull(b.Left) && b.Left.Type.IsNullableType())
                {
                    EmitNullEquality(b.NodeType, b.Left, b.IsLiftedToNull);
                    return;
                }

                EmitExpression(GetEqualityOperand(b.Left));
                EmitExpression(GetEqualityOperand(b.Right));
            }
            else
            {
                EmitExpression(b.Left);
                EmitExpression(b.Right);
            }

            EmitBinaryOperator(b.NodeType, b.Left.Type, b.Right.Type, b.Type, b.IsLiftedToNull);
        }
    }

    private void EmitBinaryMethod(BinaryExpression b, CompilationFlags flags)
    {
        if (b.IsLifted)
        {
            var parameterExpression1 = Expression.Variable(b.Left.Type.GetNonNullableType(), null);
            var parameterExpression2 = Expression.Variable(b.Right.Type.GetNonNullableType(), null);
            var mc = Expression.Call(null, b.Method, parameterExpression1, parameterExpression2);
            Type resultType;
            if (b.IsLiftedToNull)
            {
                resultType = TypeUtils.GetNullableType(mc.Type);
            }
            else
            {
                switch (b.NodeType)
                {
                    case ExpressionType.Equal:
                    case ExpressionType.GreaterThan:
                    case ExpressionType.GreaterThanOrEqual:
                    case ExpressionType.LessThan:
                    case ExpressionType.LessThanOrEqual:
                    case ExpressionType.NotEqual:
                        if (mc.Type != typeof(bool))
                        {
                            throw Error.ArgumentMustBeBoolean();
                        }

                        resultType = typeof(bool);
                        break;
                    default:
                        resultType = TypeUtils.GetNullableType(mc.Type);
                        break;
                }
            }

            var parameterExpressionArray = new ParameterExpression[2]
            {
                parameterExpression1,
                parameterExpression2
            };
            var expressionArray = new Expression[2]
            {
                b.Left,
                b.Right
            };
            ValidateLift(parameterExpressionArray, expressionArray);
            EmitLift(b.NodeType, resultType, mc, parameterExpressionArray, expressionArray);
        }
        else
        {
            EmitMethodCallExpression(Expression.Call(null, b.Method, b.Left, b.Right), flags);
        }
    }

    private void EmitBinaryOperator(
        ExpressionType op,
        Type leftType,
        Type rightType,
        Type resultType,
        bool liftedToNull)
    {
        var flag1 = leftType.IsNullableType();
        var flag2 = rightType.IsNullableType();
        if (op != ExpressionType.ArrayIndex)
        {
            if (op == ExpressionType.Coalesce)
            {
                throw Error.UnexpectedCoalesceOperator();
            }

            if (flag1 | flag2)
            {
                EmitLiftedBinaryOp(op, leftType, rightType, resultType, liftedToNull);
            }
            else
            {
                EmitUnliftedBinaryOp(op, leftType, rightType);
                EmitConvertArithmeticResult(op, resultType);
            }
        }
        else
        {
            if (rightType != typeof(int))
            {
                throw ContractUtils.Unreachable;
            }

            IL.EmitLoadElement(leftType.GetElementType());
        }
    }

    private void EmitBinding(MemberBinding binding, Type objectType)
    {
        switch (binding.BindingType)
        {
            case MemberBindingType.Assignment:
                EmitMemberAssignment((MemberAssignment)binding, objectType);
                break;
            case MemberBindingType.MemberBinding:
                EmitMemberMemberBinding((MemberMemberBinding)binding);
                break;
            case MemberBindingType.ListBinding:
                EmitMemberListBinding((MemberListBinding)binding);
                break;
            default:
                throw Error.UnknownBindingType();
        }
    }

    private void EmitBlockExpression(Expression expr, CompilationFlags flags)
    {
        Emit((BlockExpression)expr, UpdateEmitAsTypeFlag(flags, CompilationFlags.EmitAsDefaultType));
    }

    private void EmitBranchAnd(bool branch, BinaryExpression node, Label label)
    {
        var label1 = IL.DefineLabel();
        EmitExpressionAndBranch(!branch, node.Left, label1);
        EmitExpressionAndBranch(branch, node.Right, label);
        IL.MarkLabel(label1);
    }

    private void EmitBranchBlock(bool branch, BlockExpression node, Label label)
    {
        EnterScope(node);
        var expressionCount = node.ExpressionCount;
        for (var index = 0; index < expressionCount - 1; ++index)
        {
            EmitExpressionAsVoid(node.GetExpression(index));
        }

        EmitExpressionAndBranch(branch, node.GetExpression(expressionCount - 1), label);
        ExitScope(node);
    }

    private void EmitBranchComparison(bool branch, BinaryExpression node, Label label)
    {
        var flag = branch == (node.NodeType == ExpressionType.Equal);
        if (node.Method != null)
        {
            EmitBinaryMethod(node, CompilationFlags.EmitAsNoTail);
            EmitBranchOp(branch, label);
        }
        else if (ConstantCheck.IsNull(node.Left))
        {
            if (node.Right.Type.IsNullableType())
            {
                EmitAddress(node.Right, node.Right.Type);
                IL.EmitHasValue(node.Right.Type);
            }
            else
            {
                EmitExpression(GetEqualityOperand(node.Right));
            }

            EmitBranchOp(!flag, label);
        }
        else if (ConstantCheck.IsNull(node.Right))
        {
            if (node.Left.Type.IsNullableType())
            {
                EmitAddress(node.Left, node.Left.Type);
                IL.EmitHasValue(node.Left.Type);
            }
            else
            {
                EmitExpression(GetEqualityOperand(node.Left));
            }

            EmitBranchOp(!flag, label);
        }
        else if (node.Left.Type.IsNullableType() || node.Right.Type.IsNullableType())
        {
            EmitBinaryExpression(node);
            EmitBranchOp(branch, label);
        }
        else
        {
            EmitExpression(GetEqualityOperand(node.Left));
            EmitExpression(GetEqualityOperand(node.Right));
            if (flag)
            {
                IL.Emit(OpCodes.Beq, label);
            }
            else
            {
                IL.Emit(OpCodes.Ceq);
                IL.Emit(OpCodes.Brfalse, label);
            }
        }
    }

    private void EmitBranchLogical(bool branch, BinaryExpression node, Label label)
    {
        if (node.Method != null || node.IsLifted)
        {
            EmitExpression(node);
            EmitBranchOp(branch, label);
        }
        else
        {
            var flag = node.NodeType == ExpressionType.AndAlso;
            if (branch == flag)
            {
                EmitBranchAnd(branch, node, label);
            }
            else
            {
                EmitBranchOr(branch, node, label);
            }
        }
    }

    private void EmitBranchNot(bool branch, UnaryExpression node, Label label)
    {
        if (node.Method != null)
        {
            EmitExpression(node, CompilationFlags.EmitNoExpressionStart | CompilationFlags.EmitAsNoTail);
            EmitBranchOp(branch, label);
        }
        else
        {
            EmitExpressionAndBranch(!branch, node.Operand, label);
        }
    }

    private void EmitBranchOp(bool branch, Label label)
    {
        IL.Emit(branch ? OpCodes.Brtrue : OpCodes.Brfalse, label);
    }

    private void EmitBranchOr(bool branch, BinaryExpression node, Label label)
    {
        EmitExpressionAndBranch(branch, node.Left, label);
        EmitExpressionAndBranch(branch, node.Right, label);
    }

    private void EmitCall(Type objectType, MethodInfo method)
    {
        if (method.CallingConvention == CallingConventions.VarArgs)
        {
            throw Error.UnexpectedVarArgsCall(method);
        }

        var opcode = UseVirtual(method) ? OpCodes.Callvirt : OpCodes.Call;
        if (opcode == OpCodes.Callvirt && objectType.IsValueType)
        {
            IL.Emit(OpCodes.Constrained, objectType);
        }

        IL.Emit(opcode, method);
    }

    private void EmitCatchStart(CatchBlock cb)
    {
        if (cb.Filter == null)
        {
            EmitSaveExceptionOrPop(cb);
        }
        else
        {
            var label1 = IL.DefineLabel();
            var label2 = IL.DefineLabel();
            IL.Emit(OpCodes.Isinst, cb.Test);
            IL.Emit(OpCodes.Dup);
            IL.Emit(OpCodes.Brtrue, label2);
            IL.Emit(OpCodes.Pop);
            IL.Emit(OpCodes.Ldc_I4_0);
            IL.Emit(OpCodes.Br, label1);
            IL.MarkLabel(label2);
            EmitSaveExceptionOrPop(cb);
            PushLabelBlock(LabelScopeKind.Filter);
            EmitExpression(cb.Filter);
            PopLabelBlock(LabelScopeKind.Filter);
            IL.MarkLabel(label1);
            IL.BeginCatchBlock(null);
            IL.Emit(OpCodes.Pop);
        }
    }

    private void EmitClosureCreation(LambdaCompiler inner)
    {
        var needsClosure = inner._scope.NeedsClosure;
        var flag = inner._boundConstants.Count > 0;
        if (!needsClosure && !flag)
        {
            IL.EmitNull();
        }
        else
        {
            if (flag)
            {
                _boundConstants.EmitConstant(this, inner._boundConstants.ToArray(), typeof(object[]));
            }
            else
            {
                IL.EmitNull();
            }

            if (needsClosure)
            {
                _scope.EmitGet(_scope.NearestHoistedLocals.SelfVariable);
            }
            else
            {
                IL.EmitNull();
            }

            IL.EmitNew(typeof(Closure).GetConstructor(new Type[2]
            {
                typeof(object[]),
                typeof(object[])
            }));
        }
    }

    private void EmitCoalesceBinaryExpression(Expression expr)
    {
        var b = (BinaryExpression)expr;
        if (b.Left.Type.IsNullableType())
        {
            EmitNullableCoalesce(b);
        }
        else
        {
            if (b.Left.Type.IsValueType)
            {
                throw Error.CoalesceUsedOnNonNullType();
            }

            if (b.Conversion != null)
            {
                EmitLambdaReferenceCoalesce(b);
            }
            else
            {
                EmitReferenceCoalesceWithoutConversion(b);
            }
        }
    }

    private void EmitConditionalExpression(Expression expr, CompilationFlags flags)
    {
        var conditionalExpression = (ConditionalExpression)expr;
        var label1 = IL.DefineLabel();
        EmitExpressionAndBranch(false, conditionalExpression.Test, label1);
        EmitExpressionAsType(conditionalExpression.IfTrue, conditionalExpression.Type, flags);
        if (NotEmpty(conditionalExpression.IfFalse))
        {
            var label2 = IL.DefineLabel();
            if ((flags & CompilationFlags.EmitAsTailCallMask) == CompilationFlags.EmitAsTail)
            {
                IL.Emit(OpCodes.Ret);
            }
            else
            {
                IL.Emit(OpCodes.Br, label2);
            }

            IL.MarkLabel(label1);
            EmitExpressionAsType(conditionalExpression.IfFalse, conditionalExpression.Type, flags);
            IL.MarkLabel(label2);
        }
        else
        {
            IL.MarkLabel(label1);
        }
    }

    private void EmitConstant(object value, Type type)
    {
        if (ILGen.CanEmitConstant(value, type))
        {
            IL.EmitConstant(value, type);
        }
        else
        {
            _boundConstants.EmitConstant(this, value, type);
        }
    }

    private void EmitConstantExpression(Expression expr)
    {
        var constantExpression = (ConstantExpression)expr;
        EmitConstant(constantExpression.Value, constantExpression.Type);
    }

    private void EmitConstantOne(Type type)
    {
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
                IL.Emit(OpCodes.Ldc_I4_1);
                break;
            case TypeCode.Int64:
            case TypeCode.UInt64:
                IL.Emit(OpCodes.Ldc_I8, 1L);
                break;
            case TypeCode.Single:
                IL.Emit(OpCodes.Ldc_R4, 1f);
                break;
            case TypeCode.Double:
                IL.Emit(OpCodes.Ldc_R8, 1.0);
                break;
            default:
                throw ContractUtils.Unreachable;
        }
    }

    private void EmitConvert(UnaryExpression node, CompilationFlags flags)
    {
        if (node.Method != null)
        {
            if (node.IsLifted && (!node.Type.IsValueType || !node.Operand.Type.IsValueType))
            {
                var parametersCached = node.Method.GetParameters();
                var parameterType = parametersCached[0].ParameterType;
                if (parameterType.IsByRef)
                {
                    parameterType.GetElementType();
                }

                EmitConvert(
                    Expression.Convert(
                        Expression.Call(node.Method,
                            Expression.Convert(node.Operand, parametersCached[0].ParameterType)), node.Type), flags);
            }
            else
            {
                EmitUnaryMethod(node, flags);
            }
        }
        else if (node.Type == typeof(void))
        {
            EmitExpressionAsVoid(node.Operand, flags);
        }
        else if (TypeUtils.AreEquivalent(node.Operand.Type, node.Type))
        {
            EmitExpression(node.Operand, flags);
        }
        else
        {
            EmitExpression(node.Operand);
            IL.EmitConvertToType(node.Operand.Type, node.Type, node.NodeType == ExpressionType.ConvertChecked);
        }
    }

    private void EmitConvertArithmeticResult(ExpressionType op, Type resultType)
    {
        switch (Type.GetTypeCode(resultType))
        {
            case TypeCode.SByte:
                IL.Emit(IsChecked(op) ? OpCodes.Conv_Ovf_I1 : OpCodes.Conv_I1);
                break;
            case TypeCode.Byte:
                IL.Emit(IsChecked(op) ? OpCodes.Conv_Ovf_U1 : OpCodes.Conv_U1);
                break;
            case TypeCode.Int16:
                IL.Emit(IsChecked(op) ? OpCodes.Conv_Ovf_I2 : OpCodes.Conv_I2);
                break;
            case TypeCode.UInt16:
                IL.Emit(IsChecked(op) ? OpCodes.Conv_Ovf_U2 : OpCodes.Conv_U2);
                break;
        }
    }

    private void EmitConvertUnaryExpression(Expression expr, CompilationFlags flags)
    {
        EmitConvert((UnaryExpression)expr, flags);
    }

    private void EmitDebugInfoExpression(Expression expr)
    {
        if (!EmitDebugSymbols)
        {
            return;
        }

        var sequencePoint = (DebugInfoExpression)expr;
        if (sequencePoint.IsClear && _sequencePointCleared)
        {
            return;
        }

        _tree.DebugInfoGenerator.MarkSequencePoint(_lambda, _method, IL, sequencePoint);
        IL.Emit(OpCodes.Nop);
        _sequencePointCleared = sequencePoint.IsClear;
    }

    private void EmitDefaultExpression(Expression expr)
    {
        var defaultExpression = (DefaultExpression)expr;
        if (!(defaultExpression.Type != typeof(void)))
        {
            return;
        }

        IL.EmitDefault(defaultExpression.Type);
    }

    private void EmitDelegateConstruction(LambdaCompiler inner)
    {
        var type = inner._lambda.Type;
        var method = inner._method as DynamicMethod;
        if (method != null)
        {
            _boundConstants.EmitConstant(this, method, typeof(MethodInfo));
            IL.EmitType(type);
            EmitClosureCreation(inner);
            IL.Emit(OpCodes.Callvirt, typeof(MethodInfo).GetMethod("CreateDelegate", new Type[2]
            {
                typeof(Type),
                typeof(object)
            }));
            IL.Emit(OpCodes.Castclass, type);
        }
        else
        {
            EmitClosureCreation(inner);
            IL.Emit(OpCodes.Ldftn, inner._method);
            IL.Emit(OpCodes.Newobj, (ConstructorInfo)type.GetMember(".ctor")[0]);
        }
    }

    private void EmitDelegateConstruction(LambdaExpression lambda)
    {
        LambdaCompiler inner;
        if (_method is DynamicMethod)
        {
            inner = new LambdaCompiler(_tree, lambda);
        }
        else
        {
            var method = _typeBuilder.DefineMethod(
                string.IsNullOrEmpty(lambda.Name) ? GetUniqueMethodName() : lambda.Name,
                MethodAttributes.Private | MethodAttributes.Static);
            inner = new LambdaCompiler(_tree, lambda, method);
        }

        inner.EmitLambdaBody(_scope, false, CompilationFlags.EmitAsNoTail);
        EmitDelegateConstruction(inner);
    }

    private void EmitDynamicExpression(Expression expr)
    {
        if (!(_method is DynamicMethod))
        {
            throw Error.CannotCompileDynamic();
        }

        var args = (DynamicExpression)expr;
        var callSite = CallSite.Create(args.DelegateType, args.Binder);
        var type = callSite.GetType();
        var method = args.DelegateType.GetMethod("Invoke");
        EmitConstant(callSite, type);
        IL.Emit(OpCodes.Dup);
        var local = GetLocal(typeof(CallSite));
        IL.Emit(OpCodes.Stloc, local);
        IL.Emit(OpCodes.Ldfld, type.GetField("Target"));
        IL.Emit(OpCodes.Ldloc, local);
        FreeLocal(local);
        var writeBacks = EmitArguments(method, args, 1);
        IL.Emit(OpCodes.Callvirt, method);
        EmitWriteBack(writeBacks);
    }

    private void EmitExpression(Expression node, CompilationFlags flags)
    {
        if (!_guard.TryEnterOnCurrentStack())
        {
            _guard.RunOnEmptyStack((@this, n, f) => @this.EmitExpression(n, f), this, node, flags);
        }
        else
        {
            var flag = (flags & CompilationFlags.EmitExpressionStartMask) == CompilationFlags.EmitExpressionStart;
            var flags1 = flag ? EmitExpressionStart(node) : CompilationFlags.EmitNoExpressionStart;
            flags &= CompilationFlags.EmitAsTailCallMask;
            switch (node.NodeType)
            {
                case ExpressionType.Add:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.AddChecked:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.And:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.AndAlso:
                    EmitAndAlsoBinaryExpression(node, flags);
                    break;
                case ExpressionType.ArrayLength:
                    EmitUnaryExpression(node, flags);
                    break;
                case ExpressionType.ArrayIndex:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.Call:
                    EmitMethodCallExpression(node, flags);
                    break;
                case ExpressionType.Coalesce:
                    EmitCoalesceBinaryExpression(node);
                    break;
                case ExpressionType.Conditional:
                    EmitConditionalExpression(node, flags);
                    break;
                case ExpressionType.Constant:
                    EmitConstantExpression(node);
                    break;
                case ExpressionType.Convert:
                    EmitConvertUnaryExpression(node, flags);
                    break;
                case ExpressionType.ConvertChecked:
                    EmitConvertUnaryExpression(node, flags);
                    break;
                case ExpressionType.Divide:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.Equal:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.ExclusiveOr:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.GreaterThan:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.Invoke:
                    EmitInvocationExpression(node, flags);
                    break;
                case ExpressionType.Lambda:
                    EmitLambdaExpression(node);
                    break;
                case ExpressionType.LeftShift:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.LessThan:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.LessThanOrEqual:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.ListInit:
                    EmitListInitExpression(node);
                    break;
                case ExpressionType.MemberAccess:
                    EmitMemberExpression(node);
                    break;
                case ExpressionType.MemberInit:
                    EmitMemberInitExpression(node);
                    break;
                case ExpressionType.Modulo:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.Multiply:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.MultiplyChecked:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.Negate:
                    EmitUnaryExpression(node, flags);
                    break;
                case ExpressionType.UnaryPlus:
                    EmitUnaryExpression(node, flags);
                    break;
                case ExpressionType.NegateChecked:
                    EmitUnaryExpression(node, flags);
                    break;
                case ExpressionType.New:
                    EmitNewExpression(node);
                    break;
                case ExpressionType.NewArrayInit:
                    EmitNewArrayExpression(node);
                    break;
                case ExpressionType.NewArrayBounds:
                    EmitNewArrayExpression(node);
                    break;
                case ExpressionType.Not:
                    EmitUnaryExpression(node, flags);
                    break;
                case ExpressionType.NotEqual:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.Or:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.OrElse:
                    EmitOrElseBinaryExpression(node, flags);
                    break;
                case ExpressionType.Parameter:
                    EmitParameterExpression(node);
                    break;
                case ExpressionType.Power:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.Quote:
                    EmitQuoteUnaryExpression(node);
                    break;
                case ExpressionType.RightShift:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.Subtract:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.SubtractChecked:
                    EmitBinaryExpression(node, flags);
                    break;
                case ExpressionType.TypeAs:
                    EmitUnaryExpression(node, flags);
                    break;
                case ExpressionType.TypeIs:
                    EmitTypeBinaryExpression(node);
                    break;
                case ExpressionType.Assign:
                    EmitAssignBinaryExpression(node);
                    break;
                case ExpressionType.Block:
                    EmitBlockExpression(node, flags);
                    break;
                case ExpressionType.DebugInfo:
                    EmitDebugInfoExpression(node);
                    break;
                case ExpressionType.Decrement:
                    EmitUnaryExpression(node, flags);
                    break;
                case ExpressionType.Dynamic:
                    EmitDynamicExpression(node);
                    break;
                case ExpressionType.Default:
                    EmitDefaultExpression(node);
                    break;
                case ExpressionType.Extension:
                    EmitExtensionExpression(node);
                    break;
                case ExpressionType.Goto:
                    EmitGotoExpression(node, flags);
                    break;
                case ExpressionType.Increment:
                    EmitUnaryExpression(node, flags);
                    break;
                case ExpressionType.Index:
                    EmitIndexExpression(node);
                    break;
                case ExpressionType.Label:
                    EmitLabelExpression(node, flags);
                    break;
                case ExpressionType.RuntimeVariables:
                    EmitRuntimeVariablesExpression(node);
                    break;
                case ExpressionType.Loop:
                    EmitLoopExpression(node);
                    break;
                case ExpressionType.Switch:
                    EmitSwitchExpression(node, flags);
                    break;
                case ExpressionType.Throw:
                    EmitThrowUnaryExpression(node);
                    break;
                case ExpressionType.Try:
                    EmitTryExpression(node);
                    break;
                case ExpressionType.Unbox:
                    EmitUnboxUnaryExpression(node);
                    break;
                case ExpressionType.TypeEqual:
                    EmitTypeBinaryExpression(node);
                    break;
                case ExpressionType.OnesComplement:
                    EmitUnaryExpression(node, flags);
                    break;
                case ExpressionType.IsTrue:
                    EmitUnaryExpression(node, flags);
                    break;
                case ExpressionType.IsFalse:
                    EmitUnaryExpression(node, flags);
                    break;
                default:
                    throw ContractUtils.Unreachable;
            }

            if (!flag)
            {
                return;
            }

            EmitExpressionEnd(flags1);
        }
    }

    private void EmitExpressionAddress(Expression node, Type type)
    {
        EmitExpression(node, CompilationFlags.EmitNoExpressionStart | CompilationFlags.EmitAsNoTail);
        var local = GetLocal(type);
        IL.Emit(OpCodes.Stloc, local);
        IL.Emit(OpCodes.Ldloca, local);
    }

    private void EmitExpressionAndBranch(bool branchValue, Expression node, Label label)
    {
        var flags = EmitExpressionStart(node);
        try
        {
            if (node.Type == typeof(bool))
            {
                switch (node.NodeType)
                {
                    case ExpressionType.AndAlso:
                    case ExpressionType.OrElse:
                        EmitBranchLogical(branchValue, (BinaryExpression)node, label);
                        return;
                    case ExpressionType.Equal:
                    case ExpressionType.NotEqual:
                        EmitBranchComparison(branchValue, (BinaryExpression)node, label);
                        return;
                    case ExpressionType.Not:
                        EmitBranchNot(branchValue, (UnaryExpression)node, label);
                        return;
                    case ExpressionType.Block:
                        EmitBranchBlock(branchValue, (BlockExpression)node, label);
                        return;
                }
            }

            EmitExpression(node, CompilationFlags.EmitNoExpressionStart | CompilationFlags.EmitAsNoTail);
            EmitBranchOp(branchValue, label);
        }
        finally
        {
            EmitExpressionEnd(flags);
        }
    }

    private void EmitExpressionAsType(
        Expression node,
        Type type,
        CompilationFlags flags)
    {
        if (type == typeof(void))
        {
            EmitExpressionAsVoid(node, flags);
        }
        else if (!TypeUtils.AreEquivalent(node.Type, type))
        {
            EmitExpression(node);
            IL.Emit(OpCodes.Castclass, type);
        }
        else
        {
            EmitExpression(node, UpdateEmitExpressionStartFlag(flags, CompilationFlags.EmitExpressionStart));
        }
    }

    private void EmitExpressionAsVoid(Expression node)
    {
        EmitExpressionAsVoid(node, CompilationFlags.EmitAsNoTail);
    }

    private void EmitExpressionAsVoid(Expression node, CompilationFlags flags)
    {
        var flags1 = EmitExpressionStart(node);
        switch (node.NodeType)
        {
            case ExpressionType.Constant:
            case ExpressionType.Parameter:
            case ExpressionType.Default:
                EmitExpressionEnd(flags1);
                break;
            case ExpressionType.Assign:
                EmitAssign((BinaryExpression)node, CompilationFlags.EmitAsVoidType);
                goto case ExpressionType.Constant;
            case ExpressionType.Block:
                Emit((BlockExpression)node, UpdateEmitAsTypeFlag(flags, CompilationFlags.EmitAsVoidType));
                goto case ExpressionType.Constant;
            case ExpressionType.Goto:
                EmitGotoExpression(node, UpdateEmitAsTypeFlag(flags, CompilationFlags.EmitAsVoidType));
                goto case ExpressionType.Constant;
            case ExpressionType.Throw:
                EmitThrow((UnaryExpression)node, CompilationFlags.EmitAsVoidType);
                goto case ExpressionType.Constant;
            default:
                if (node.Type == typeof(void))
                {
                    EmitExpression(node, UpdateEmitExpressionStartFlag(flags, CompilationFlags.EmitNoExpressionStart));
                    goto case ExpressionType.Constant;
                }

                EmitExpression(node, CompilationFlags.EmitNoExpressionStart | CompilationFlags.EmitAsNoTail);
                IL.Emit(OpCodes.Pop);
                goto case ExpressionType.Constant;
        }
    }

    private void EmitExpressionEnd(CompilationFlags flags)
    {
        if ((flags & CompilationFlags.EmitExpressionStartMask) != CompilationFlags.EmitExpressionStart)
        {
            return;
        }

        PopLabelBlock(_labelBlock.Kind);
    }

    private CompilationFlags EmitExpressionStart(Expression node)
    {
        return TryPushLabelBlock(node) ? CompilationFlags.EmitExpressionStart : CompilationFlags.EmitNoExpressionStart;
    }

    private static void EmitExtensionExpression(Expression expr)
    {
        throw Error.ExtensionNotReduced();
    }

    private void EmitGetIndexCall(IndexExpression node, Type objectType)
    {
        if (node.Indexer != null)
        {
            var getMethod = node.Indexer.GetGetMethod(true);
            EmitCall(objectType, getMethod);
        }
        else if (node.Arguments.Count != 1)
        {
            IL.Emit(OpCodes.Call, node.Object.Type.GetMethod("Get", BindingFlags.Instance | BindingFlags.Public));
        }
        else
        {
            IL.EmitLoadElement(node.Type);
        }
    }

    private void EmitGotoExpression(Expression expr, CompilationFlags flags)
    {
        var node = (GotoExpression)expr;
        var labelInfo = ReferenceLabel(node.Target);
        if ((flags & CompilationFlags.EmitAsTailCallMask) != CompilationFlags.EmitAsNoTail)
        {
            var newValue = labelInfo.CanReturn ? CompilationFlags.EmitAsTail : CompilationFlags.EmitAsNoTail;
            flags = UpdateEmitAsTailCallFlag(flags, newValue);
        }

        if (node.Value != null)
        {
            if (node.Target.Type == typeof(void))
            {
                EmitExpressionAsVoid(node.Value, flags);
            }
            else
            {
                flags = UpdateEmitExpressionStartFlag(flags, CompilationFlags.EmitExpressionStart);
                EmitExpression(node.Value, flags);
            }
        }

        labelInfo.EmitJump();
        EmitUnreachable(node, flags);
    }

    private void EmitIndexAssignment(BinaryExpression node, CompilationFlags flags)
    {
        var left = (IndexExpression)node.Left;
        var compilationFlags = flags & CompilationFlags.EmitAsTypeMask;
        var objectType = (Type)null;
        if (left.Object != null)
        {
            EmitInstance(left.Object, objectType = left.Object.Type);
        }

        foreach (var node1 in left.Arguments)
        {
            EmitExpression(node1);
        }

        EmitExpression(node.Right);
        var local = (LocalBuilder)null;
        if (compilationFlags != CompilationFlags.EmitAsVoidType)
        {
            IL.Emit(OpCodes.Dup);
            IL.Emit(OpCodes.Stloc, local = GetLocal(node.Type));
        }

        EmitSetIndexCall(left, objectType);
        if (compilationFlags == CompilationFlags.EmitAsVoidType)
        {
            return;
        }

        IL.Emit(OpCodes.Ldloc, local);
        FreeLocal(local);
    }

    private void EmitIndexExpression(Expression expr)
    {
        var node1 = (IndexExpression)expr;
        var objectType = (Type)null;
        if (node1.Object != null)
        {
            EmitInstance(node1.Object, objectType = node1.Object.Type);
        }

        foreach (var node2 in node1.Arguments)
        {
            EmitExpression(node2);
        }

        EmitGetIndexCall(node1, objectType);
    }

    private void EmitInlinedInvoke(InvocationExpression invoke, CompilationFlags flags)
    {
        var lambdaOperand = invoke.LambdaOperand;
        var writeBacks = EmitArguments(lambdaOperand.Type.GetMethod("Invoke"), invoke);
        var lambdaCompiler = new LambdaCompiler(this, lambdaOperand);
        if (writeBacks.Count != 0)
        {
            flags = UpdateEmitAsTailCallFlag(flags, CompilationFlags.EmitAsNoTail);
        }

        lambdaCompiler.EmitLambdaBody(_scope, true, flags);
        EmitWriteBack(writeBacks);
    }

    private void EmitInstance(Expression instance, Type type)
    {
        if (instance == null)
        {
            return;
        }

        if (type.IsValueType)
        {
            EmitAddress(instance, type);
        }
        else
        {
            EmitExpression(instance);
        }
    }

    private void EmitInvocationExpression(Expression expr, CompilationFlags flags)
    {
        var invoke = (InvocationExpression)expr;
        if (invoke.LambdaOperand != null)
        {
            EmitInlinedInvoke(invoke, flags);
        }
        else
        {
            expr = invoke.Expression;
            if (typeof(LambdaExpression).IsAssignableFrom(expr.Type))
            {
                expr = Expression.Call(expr, expr.Type.GetMethod("Compile", new Type[0]));
            }

            expr = Expression.Call(expr, expr.Type.GetMethod("Invoke"), invoke.Arguments);
            EmitExpression(expr);
        }
    }

    private void EmitLabelExpression(Expression expr, CompilationFlags flags)
    {
        var labelExpression = (LabelExpression)expr;
        var info = (LabelInfo)null;
        if (_labelBlock.Kind == LabelScopeKind.Block)
        {
            _labelBlock.TryGetLabelInfo(labelExpression.Target, out info);
            if (info == null && _labelBlock.Parent.Kind == LabelScopeKind.Switch)
            {
                _labelBlock.Parent.TryGetLabelInfo(labelExpression.Target, out info);
            }
        }

        if (info == null)
        {
            info = DefineLabel(labelExpression.Target);
        }

        if (labelExpression.DefaultValue != null)
        {
            if (labelExpression.Target.Type == typeof(void))
            {
                EmitExpressionAsVoid(labelExpression.DefaultValue, flags);
            }
            else
            {
                flags = UpdateEmitExpressionStartFlag(flags, CompilationFlags.EmitExpressionStart);
                EmitExpression(labelExpression.DefaultValue, flags);
            }
        }

        info.Mark();
    }

    private void EmitLambdaBody()
    {
        EmitLambdaBody(null, false, _lambda.TailCall ? CompilationFlags.EmitAsTail : CompilationFlags.EmitAsNoTail);
    }

    private void EmitLambdaBody(
        CompilerScope parent,
        bool inlined,
        CompilationFlags flags)
    {
        _scope.Enter(this, parent);
        if (inlined)
        {
            for (var index = _lambda.Parameters.Count - 1; index >= 0; --index)
            {
                _scope.EmitSet(_lambda.Parameters[index]);
            }
        }

        flags = UpdateEmitExpressionStartFlag(flags, CompilationFlags.EmitExpressionStart);
        if (_lambda.ReturnType == typeof(void))
        {
            EmitExpressionAsVoid(_lambda.Body, flags);
        }
        else
        {
            EmitExpression(_lambda.Body, flags);
        }

        if (!inlined)
        {
            IL.Emit(OpCodes.Ret);
        }

        _scope.Exit();
        foreach (var labelInfo in _labelInfo.Values)
        {
            labelInfo.ValidateFinish();
        }
    }

    private void EmitLambdaExpression(Expression expr)
    {
        EmitDelegateConstruction((LambdaExpression)expr);
    }

    private void EmitLambdaReferenceCoalesce(BinaryExpression b)
    {
        var local = GetLocal(b.Left.Type);
        var label1 = IL.DefineLabel();
        var label2 = IL.DefineLabel();
        EmitExpression(b.Left);
        IL.Emit(OpCodes.Dup);
        IL.Emit(OpCodes.Stloc, local);
        IL.Emit(OpCodes.Ldnull);
        IL.Emit(OpCodes.Ceq);
        IL.Emit(OpCodes.Brfalse, label2);
        EmitExpression(b.Right);
        IL.Emit(OpCodes.Br, label1);
        IL.MarkLabel(label2);
        EmitLambdaExpression(b.Conversion);
        IL.Emit(OpCodes.Ldloc, local);
        FreeLocal(local);
        IL.Emit(OpCodes.Callvirt, b.Conversion.Type.GetMethod("Invoke"));
        IL.MarkLabel(label1);
    }

    private void EmitLift(
        ExpressionType nodeType,
        Type resultType,
        MethodCallExpression mc,
        ParameterExpression[] paramList,
        Expression[] argList)
    {
        switch (nodeType)
        {
            case ExpressionType.Equal:
            case ExpressionType.NotEqual:
                if (!TypeUtils.AreEquivalent(resultType, TypeUtils.GetNullableType(mc.Type)))
                {
                    var label1 = IL.DefineLabel();
                    var label2 = IL.DefineLabel();
                    var label3 = IL.DefineLabel();
                    var local1 = IL.DeclareLocal(typeof(bool));
                    var local2 = IL.DeclareLocal(typeof(bool));
                    IL.Emit(OpCodes.Ldc_I4_0);
                    IL.Emit(OpCodes.Stloc, local1);
                    IL.Emit(OpCodes.Ldc_I4_1);
                    IL.Emit(OpCodes.Stloc, local2);
                    var index = 0;
                    for (var length = paramList.Length; index < length; ++index)
                    {
                        var variable = paramList[index];
                        var node = argList[index];
                        _scope.AddLocal(this, variable);
                        if (node.Type.IsNullableType())
                        {
                            EmitAddress(node, node.Type);
                            IL.Emit(OpCodes.Dup);
                            IL.EmitHasValue(node.Type);
                            IL.Emit(OpCodes.Ldc_I4_0);
                            IL.Emit(OpCodes.Ceq);
                            IL.Emit(OpCodes.Dup);
                            IL.Emit(OpCodes.Ldloc, local1);
                            IL.Emit(OpCodes.Or);
                            IL.Emit(OpCodes.Stloc, local1);
                            IL.Emit(OpCodes.Ldloc, local2);
                            IL.Emit(OpCodes.And);
                            IL.Emit(OpCodes.Stloc, local2);
                            IL.EmitGetValueOrDefault(node.Type);
                        }
                        else
                        {
                            EmitExpression(node);
                            if (!node.Type.IsValueType)
                            {
                                IL.Emit(OpCodes.Dup);
                                IL.Emit(OpCodes.Ldnull);
                                IL.Emit(OpCodes.Ceq);
                                IL.Emit(OpCodes.Dup);
                                IL.Emit(OpCodes.Ldloc, local1);
                                IL.Emit(OpCodes.Or);
                                IL.Emit(OpCodes.Stloc, local1);
                                IL.Emit(OpCodes.Ldloc, local2);
                                IL.Emit(OpCodes.And);
                                IL.Emit(OpCodes.Stloc, local2);
                            }
                            else
                            {
                                IL.Emit(OpCodes.Ldc_I4_0);
                                IL.Emit(OpCodes.Stloc, local2);
                            }
                        }

                        _scope.EmitSet(variable);
                    }

                    IL.Emit(OpCodes.Ldloc, local2);
                    IL.Emit(OpCodes.Brtrue, label2);
                    IL.Emit(OpCodes.Ldloc, local1);
                    IL.Emit(OpCodes.Brtrue, label3);
                    EmitMethodCallExpression(mc);
                    if (resultType.IsNullableType() && !TypeUtils.AreEquivalent(resultType, mc.Type))
                    {
                        var constructor = resultType.GetConstructor(new Type[1]
                        {
                            mc.Type
                        });
                        IL.Emit(OpCodes.Newobj, constructor);
                    }

                    IL.Emit(OpCodes.Br_S, label1);
                    IL.MarkLabel(label2);
                    IL.EmitBoolean(nodeType == ExpressionType.Equal);
                    IL.Emit(OpCodes.Br_S, label1);
                    IL.MarkLabel(label3);
                    IL.EmitBoolean(nodeType == ExpressionType.NotEqual);
                    IL.MarkLabel(label1);
                    return;
                }

                break;
        }

        var label4 = IL.DefineLabel();
        var label5 = IL.DefineLabel();
        var local3 = IL.DeclareLocal(typeof(bool));
        var index1 = 0;
        for (var length = paramList.Length; index1 < length; ++index1)
        {
            var variable = paramList[index1];
            var node = argList[index1];
            if (node.Type.IsNullableType())
            {
                _scope.AddLocal(this, variable);
                EmitAddress(node, node.Type);
                IL.Emit(OpCodes.Dup);
                IL.EmitHasValue(node.Type);
                IL.Emit(OpCodes.Ldc_I4_0);
                IL.Emit(OpCodes.Ceq);
                IL.Emit(OpCodes.Stloc, local3);
                IL.EmitGetValueOrDefault(node.Type);
                _scope.EmitSet(variable);
            }
            else
            {
                _scope.AddLocal(this, variable);
                EmitExpression(node);
                if (!node.Type.IsValueType)
                {
                    IL.Emit(OpCodes.Dup);
                    IL.Emit(OpCodes.Ldnull);
                    IL.Emit(OpCodes.Ceq);
                    IL.Emit(OpCodes.Stloc, local3);
                }

                _scope.EmitSet(variable);
            }

            IL.Emit(OpCodes.Ldloc, local3);
            IL.Emit(OpCodes.Brtrue, label5);
        }

        EmitMethodCallExpression(mc);
        if (resultType.IsNullableType() && !TypeUtils.AreEquivalent(resultType, mc.Type))
        {
            var constructor = resultType.GetConstructor(new Type[1]
            {
                mc.Type
            });
            IL.Emit(OpCodes.Newobj, constructor);
        }

        IL.Emit(OpCodes.Br_S, label4);
        IL.MarkLabel(label5);
        if (TypeUtils.AreEquivalent(resultType, TypeUtils.GetNullableType(mc.Type)))
        {
            if (resultType.IsValueType)
            {
                var local4 = GetLocal(resultType);
                IL.Emit(OpCodes.Ldloca, local4);
                IL.Emit(OpCodes.Initobj, resultType);
                IL.Emit(OpCodes.Ldloc, local4);
                FreeLocal(local4);
            }
            else
            {
                IL.Emit(OpCodes.Ldnull);
            }
        }
        else
        {
            switch (nodeType)
            {
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                    IL.Emit(OpCodes.Ldc_I4_0);
                    break;
                default:
                    throw Error.UnknownLiftType(nodeType);
            }
        }

        IL.MarkLabel(label4);
    }

    private void EmitLiftedAndAlso(BinaryExpression b)
    {
        var type = typeof(bool?);
        var label1 = IL.DefineLabel();
        var label2 = IL.DefineLabel();
        var label3 = IL.DefineLabel();
        var label4 = IL.DefineLabel();
        var label5 = IL.DefineLabel();
        var local1 = GetLocal(type);
        var local2 = GetLocal(type);
        EmitExpression(b.Left);
        IL.Emit(OpCodes.Stloc, local1);
        IL.Emit(OpCodes.Ldloca, local1);
        IL.EmitHasValue(type);
        IL.Emit(OpCodes.Brfalse, label1);
        IL.Emit(OpCodes.Ldloca, local1);
        IL.EmitGetValueOrDefault(type);
        IL.Emit(OpCodes.Ldc_I4_0);
        IL.Emit(OpCodes.Ceq);
        IL.Emit(OpCodes.Brtrue, label2);
        IL.MarkLabel(label1);
        EmitExpression(b.Right);
        IL.Emit(OpCodes.Stloc, local2);
        IL.Emit(OpCodes.Ldloca, local2);
        IL.EmitHasValue(type);
        IL.Emit(OpCodes.Brfalse_S, label3);
        IL.Emit(OpCodes.Ldloca, local2);
        IL.EmitGetValueOrDefault(type);
        IL.Emit(OpCodes.Ldc_I4_0);
        IL.Emit(OpCodes.Ceq);
        IL.Emit(OpCodes.Brtrue_S, label2);
        IL.Emit(OpCodes.Ldloca, local1);
        IL.EmitHasValue(type);
        IL.Emit(OpCodes.Brfalse, label3);
        IL.Emit(OpCodes.Ldc_I4_1);
        IL.Emit(OpCodes.Br_S, label4);
        IL.MarkLabel(label2);
        IL.Emit(OpCodes.Ldc_I4_0);
        IL.Emit(OpCodes.Br_S, label4);
        IL.MarkLabel(label4);
        var constructor = type.GetConstructor(new Type[1]
        {
            typeof(bool)
        });
        IL.Emit(OpCodes.Newobj, constructor);
        IL.Emit(OpCodes.Stloc, local1);
        IL.Emit(OpCodes.Br, label5);
        IL.MarkLabel(label3);
        IL.Emit(OpCodes.Ldloca, local1);
        IL.Emit(OpCodes.Initobj, type);
        IL.MarkLabel(label5);
        IL.Emit(OpCodes.Ldloc, local1);
        FreeLocal(local1);
        FreeLocal(local2);
    }

    private void EmitLiftedBinaryArithmetic(
        ExpressionType op,
        Type leftType,
        Type rightType,
        Type resultType)
    {
        var flag1 = leftType.IsNullableType();
        var flag2 = rightType.IsNullableType();
        var label1 = IL.DefineLabel();
        var label2 = IL.DefineLabel();
        var local1 = GetLocal(leftType);
        var local2 = GetLocal(rightType);
        var local3 = GetLocal(resultType);
        IL.Emit(OpCodes.Stloc, local2);
        IL.Emit(OpCodes.Stloc, local1);
        if (flag1)
        {
            IL.Emit(OpCodes.Ldloca, local1);
            IL.EmitHasValue(leftType);
            IL.Emit(OpCodes.Brfalse_S, label1);
        }

        if (flag2)
        {
            IL.Emit(OpCodes.Ldloca, local2);
            IL.EmitHasValue(rightType);
            IL.Emit(OpCodes.Brfalse_S, label1);
        }

        if (flag1)
        {
            IL.Emit(OpCodes.Ldloca, local1);
            IL.EmitGetValueOrDefault(leftType);
        }
        else
        {
            IL.Emit(OpCodes.Ldloc, local1);
        }

        if (flag2)
        {
            IL.Emit(OpCodes.Ldloca, local2);
            IL.EmitGetValueOrDefault(rightType);
        }
        else
        {
            IL.Emit(OpCodes.Ldloc, local2);
        }

        FreeLocal(local1);
        FreeLocal(local2);
        EmitBinaryOperator(op, leftType.GetNonNullableType(), rightType.GetNonNullableType(),
            resultType.GetNonNullableType(), false);
        var constructor = resultType.GetConstructor(new Type[1]
        {
            resultType.GetNonNullableType()
        });
        IL.Emit(OpCodes.Newobj, constructor);
        IL.Emit(OpCodes.Stloc, local3);
        IL.Emit(OpCodes.Br_S, label2);
        IL.MarkLabel(label1);
        IL.Emit(OpCodes.Ldloca, local3);
        IL.Emit(OpCodes.Initobj, resultType);
        IL.MarkLabel(label2);
        IL.Emit(OpCodes.Ldloc, local3);
        FreeLocal(local3);
    }

    private void EmitLiftedBinaryOp(
        ExpressionType op,
        Type leftType,
        Type rightType,
        Type resultType,
        bool liftedToNull)
    {
        switch (op)
        {
            case ExpressionType.Add:
            case ExpressionType.AddChecked:
            case ExpressionType.Divide:
            case ExpressionType.ExclusiveOr:
            case ExpressionType.LeftShift:
            case ExpressionType.Modulo:
            case ExpressionType.Multiply:
            case ExpressionType.MultiplyChecked:
            case ExpressionType.RightShift:
            case ExpressionType.Subtract:
            case ExpressionType.SubtractChecked:
                EmitLiftedBinaryArithmetic(op, leftType, rightType, resultType);
                break;
            case ExpressionType.And:
                if (leftType == typeof(bool?))
                {
                    EmitLiftedBooleanAnd();
                    break;
                }

                EmitLiftedBinaryArithmetic(op, leftType, rightType, resultType);
                break;
            case ExpressionType.Equal:
            case ExpressionType.GreaterThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.LessThan:
            case ExpressionType.LessThanOrEqual:
            case ExpressionType.NotEqual:
                EmitLiftedRelational(op, leftType, rightType, resultType, liftedToNull);
                break;
            case ExpressionType.Or:
                if (leftType == typeof(bool?))
                {
                    EmitLiftedBooleanOr();
                    break;
                }

                EmitLiftedBinaryArithmetic(op, leftType, rightType, resultType);
                break;
            default:
                throw ContractUtils.Unreachable;
        }
    }

    private void EmitLiftedBooleanAnd()
    {
        var type = typeof(bool?);
        var label1 = IL.DefineLabel();
        var label2 = IL.DefineLabel();
        var label3 = IL.DefineLabel();
        var label4 = IL.DefineLabel();
        var label5 = IL.DefineLabel();
        var local1 = GetLocal(type);
        var local2 = GetLocal(type);
        IL.Emit(OpCodes.Stloc, local2);
        IL.Emit(OpCodes.Stloc, local1);
        IL.Emit(OpCodes.Ldloca, local1);
        IL.EmitHasValue(type);
        IL.Emit(OpCodes.Brfalse, label1);
        IL.Emit(OpCodes.Ldloca, local1);
        IL.EmitGetValueOrDefault(type);
        IL.Emit(OpCodes.Ldc_I4_0);
        IL.Emit(OpCodes.Ceq);
        IL.Emit(OpCodes.Brtrue, label2);
        IL.MarkLabel(label1);
        IL.Emit(OpCodes.Ldloca, local2);
        IL.EmitHasValue(type);
        IL.Emit(OpCodes.Brfalse_S, label3);
        IL.Emit(OpCodes.Ldloca, local2);
        FreeLocal(local2);
        IL.EmitGetValueOrDefault(type);
        IL.Emit(OpCodes.Ldc_I4_0);
        IL.Emit(OpCodes.Ceq);
        IL.Emit(OpCodes.Brtrue_S, label2);
        IL.Emit(OpCodes.Ldloca, local1);
        IL.EmitHasValue(type);
        IL.Emit(OpCodes.Brfalse, label3);
        IL.Emit(OpCodes.Ldc_I4_1);
        IL.Emit(OpCodes.Br_S, label4);
        IL.MarkLabel(label2);
        IL.Emit(OpCodes.Ldc_I4_0);
        IL.Emit(OpCodes.Br_S, label4);
        IL.MarkLabel(label4);
        var constructor = type.GetConstructor(new Type[1]
        {
            typeof(bool)
        });
        IL.Emit(OpCodes.Newobj, constructor);
        IL.Emit(OpCodes.Stloc, local1);
        IL.Emit(OpCodes.Br, label5);
        IL.MarkLabel(label3);
        IL.Emit(OpCodes.Ldloca, local1);
        IL.Emit(OpCodes.Initobj, type);
        IL.MarkLabel(label5);
        IL.Emit(OpCodes.Ldloc, local1);
        FreeLocal(local1);
    }

    private void EmitLiftedBooleanOr()
    {
        var type = typeof(bool?);
        var label1 = IL.DefineLabel();
        var label2 = IL.DefineLabel();
        var label3 = IL.DefineLabel();
        var label4 = IL.DefineLabel();
        var label5 = IL.DefineLabel();
        var local1 = GetLocal(type);
        var local2 = GetLocal(type);
        IL.Emit(OpCodes.Stloc, local2);
        IL.Emit(OpCodes.Stloc, local1);
        IL.Emit(OpCodes.Ldloca, local1);
        IL.EmitHasValue(type);
        IL.Emit(OpCodes.Brfalse, label1);
        IL.Emit(OpCodes.Ldloca, local1);
        IL.EmitGetValueOrDefault(type);
        IL.Emit(OpCodes.Ldc_I4_0);
        IL.Emit(OpCodes.Ceq);
        IL.Emit(OpCodes.Brfalse, label2);
        IL.MarkLabel(label1);
        IL.Emit(OpCodes.Ldloca, local2);
        IL.EmitHasValue(type);
        IL.Emit(OpCodes.Brfalse_S, label3);
        IL.Emit(OpCodes.Ldloca, local2);
        FreeLocal(local2);
        IL.EmitGetValueOrDefault(type);
        IL.Emit(OpCodes.Ldc_I4_0);
        IL.Emit(OpCodes.Ceq);
        IL.Emit(OpCodes.Brfalse_S, label2);
        IL.Emit(OpCodes.Ldloca, local1);
        IL.EmitHasValue(type);
        IL.Emit(OpCodes.Brfalse, label3);
        IL.Emit(OpCodes.Ldc_I4_0);
        IL.Emit(OpCodes.Br_S, label4);
        IL.MarkLabel(label2);
        IL.Emit(OpCodes.Ldc_I4_1);
        IL.Emit(OpCodes.Br_S, label4);
        IL.MarkLabel(label4);
        var constructor = type.GetConstructor(new Type[1]
        {
            typeof(bool)
        });
        IL.Emit(OpCodes.Newobj, constructor);
        IL.Emit(OpCodes.Stloc, local1);
        IL.Emit(OpCodes.Br, label5);
        IL.MarkLabel(label3);
        IL.Emit(OpCodes.Ldloca, local1);
        IL.Emit(OpCodes.Initobj, type);
        IL.MarkLabel(label5);
        IL.Emit(OpCodes.Ldloc, local1);
        FreeLocal(local1);
    }

    private void EmitLiftedOrElse(BinaryExpression b)
    {
        var type = typeof(bool?);
        var label1 = IL.DefineLabel();
        var label2 = IL.DefineLabel();
        var label3 = IL.DefineLabel();
        var label4 = IL.DefineLabel();
        var label5 = IL.DefineLabel();
        var local1 = GetLocal(type);
        var local2 = GetLocal(type);
        EmitExpression(b.Left);
        IL.Emit(OpCodes.Stloc, local1);
        IL.Emit(OpCodes.Ldloca, local1);
        IL.EmitHasValue(type);
        IL.Emit(OpCodes.Brfalse, label1);
        IL.Emit(OpCodes.Ldloca, local1);
        IL.EmitGetValueOrDefault(type);
        IL.Emit(OpCodes.Ldc_I4_0);
        IL.Emit(OpCodes.Ceq);
        IL.Emit(OpCodes.Brfalse, label2);
        IL.MarkLabel(label1);
        EmitExpression(b.Right);
        IL.Emit(OpCodes.Stloc, local2);
        IL.Emit(OpCodes.Ldloca, local2);
        IL.EmitHasValue(type);
        IL.Emit(OpCodes.Brfalse_S, label3);
        IL.Emit(OpCodes.Ldloca, local2);
        IL.EmitGetValueOrDefault(type);
        IL.Emit(OpCodes.Ldc_I4_0);
        IL.Emit(OpCodes.Ceq);
        IL.Emit(OpCodes.Brfalse_S, label2);
        IL.Emit(OpCodes.Ldloca, local1);
        IL.EmitHasValue(type);
        IL.Emit(OpCodes.Brfalse, label3);
        IL.Emit(OpCodes.Ldc_I4_0);
        IL.Emit(OpCodes.Br_S, label4);
        IL.MarkLabel(label2);
        IL.Emit(OpCodes.Ldc_I4_1);
        IL.Emit(OpCodes.Br_S, label4);
        IL.MarkLabel(label4);
        var constructor = type.GetConstructor(new Type[1]
        {
            typeof(bool)
        });
        IL.Emit(OpCodes.Newobj, constructor);
        IL.Emit(OpCodes.Stloc, local1);
        IL.Emit(OpCodes.Br, label5);
        IL.MarkLabel(label3);
        IL.Emit(OpCodes.Ldloca, local1);
        IL.Emit(OpCodes.Initobj, type);
        IL.MarkLabel(label5);
        IL.Emit(OpCodes.Ldloc, local1);
        FreeLocal(local1);
        FreeLocal(local2);
    }

    private void EmitLiftedRelational(
        ExpressionType op,
        Type leftType,
        Type rightType,
        Type resultType,
        bool liftedToNull)
    {
        var label1 = IL.DefineLabel();
        var local1 = GetLocal(leftType);
        var local2 = GetLocal(rightType);
        IL.Emit(OpCodes.Stloc, local2);
        IL.Emit(OpCodes.Stloc, local1);
        switch (op)
        {
            case ExpressionType.Equal:
                IL.Emit(OpCodes.Ldloca, local1);
                IL.EmitHasValue(leftType);
                IL.Emit(OpCodes.Ldc_I4_0);
                IL.Emit(OpCodes.Ceq);
                IL.Emit(OpCodes.Ldloca, local2);
                IL.EmitHasValue(rightType);
                IL.Emit(OpCodes.Ldc_I4_0);
                IL.Emit(OpCodes.Ceq);
                IL.Emit(OpCodes.And);
                IL.Emit(OpCodes.Dup);
                IL.Emit(OpCodes.Brtrue_S, label1);
                IL.Emit(OpCodes.Pop);
                IL.Emit(OpCodes.Ldloca, local1);
                IL.EmitHasValue(leftType);
                IL.Emit(OpCodes.Ldloca, local2);
                IL.EmitHasValue(rightType);
                IL.Emit(OpCodes.And);
                IL.Emit(OpCodes.Dup);
                IL.Emit(OpCodes.Brfalse_S, label1);
                IL.Emit(OpCodes.Pop);
                break;
            case ExpressionType.NotEqual:
                IL.Emit(OpCodes.Ldloca, local1);
                IL.EmitHasValue(leftType);
                IL.Emit(OpCodes.Ldloca, local2);
                IL.EmitHasValue(rightType);
                IL.Emit(OpCodes.Or);
                IL.Emit(OpCodes.Dup);
                IL.Emit(OpCodes.Brfalse_S, label1);
                IL.Emit(OpCodes.Pop);
                IL.Emit(OpCodes.Ldloca, local1);
                IL.EmitHasValue(leftType);
                IL.Emit(OpCodes.Ldc_I4_0);
                IL.Emit(OpCodes.Ceq);
                IL.Emit(OpCodes.Ldloca, local2);
                IL.EmitHasValue(rightType);
                IL.Emit(OpCodes.Ldc_I4_0);
                IL.Emit(OpCodes.Ceq);
                IL.Emit(OpCodes.Or);
                IL.Emit(OpCodes.Dup);
                IL.Emit(OpCodes.Brtrue_S, label1);
                IL.Emit(OpCodes.Pop);
                break;
            default:
                IL.Emit(OpCodes.Ldloca, local1);
                IL.EmitHasValue(leftType);
                IL.Emit(OpCodes.Ldloca, local2);
                IL.EmitHasValue(rightType);
                IL.Emit(OpCodes.And);
                IL.Emit(OpCodes.Dup);
                IL.Emit(OpCodes.Brfalse_S, label1);
                IL.Emit(OpCodes.Pop);
                break;
        }

        IL.Emit(OpCodes.Ldloca, local1);
        IL.EmitGetValueOrDefault(leftType);
        IL.Emit(OpCodes.Ldloca, local2);
        IL.EmitGetValueOrDefault(rightType);
        FreeLocal(local1);
        FreeLocal(local2);
        EmitBinaryOperator(op, leftType.GetNonNullableType(), rightType.GetNonNullableType(),
            resultType.GetNonNullableType(), false);
        if (!liftedToNull)
        {
            IL.MarkLabel(label1);
        }

        if (!TypeUtils.AreEquivalent(resultType, resultType.GetNonNullableType()))
        {
            IL.EmitConvertToType(resultType.GetNonNullableType(), resultType, true);
        }

        if (!liftedToNull)
        {
            return;
        }

        var label2 = IL.DefineLabel();
        IL.Emit(OpCodes.Br, label2);
        IL.MarkLabel(label1);
        IL.Emit(OpCodes.Pop);
        IL.Emit(OpCodes.Ldnull);
        IL.Emit(OpCodes.Unbox_Any, resultType);
        IL.MarkLabel(label2);
    }

    private void EmitListInit(ListInitExpression init)
    {
        EmitExpression(init.NewExpression);
        var local = (LocalBuilder)null;
        if (init.NewExpression.Type.IsValueType)
        {
            local = IL.DeclareLocal(init.NewExpression.Type);
            IL.Emit(OpCodes.Stloc, local);
            IL.Emit(OpCodes.Ldloca, local);
        }

        EmitListInit(init.Initializers, local == null, init.NewExpression.Type);
        if (local == null)
        {
            return;
        }

        IL.Emit(OpCodes.Ldloc, local);
    }

    private void EmitListInit(
        ReadOnlyCollection<ElementInit> initializers,
        bool keepOnStack,
        Type objectType)
    {
        var count = initializers.Count;
        if (count == 0)
        {
            if (keepOnStack)
            {
                return;
            }

            IL.Emit(OpCodes.Pop);
        }
        else
        {
            for (var index = 0; index < count; ++index)
            {
                if (keepOnStack || index < count - 1)
                {
                    IL.Emit(OpCodes.Dup);
                }

                EmitMethodCall(initializers[index].AddMethod, initializers[index], objectType);
                if (initializers[index].AddMethod.ReturnType != typeof(void))
                {
                    IL.Emit(OpCodes.Pop);
                }
            }
        }
    }

    private void EmitListInitExpression(Expression expr)
    {
        EmitListInit((ListInitExpression)expr);
    }

    private void EmitLoopExpression(Expression expr)
    {
        var loopExpression = (LoopExpression)expr;
        PushLabelBlock(LabelScopeKind.Statement);
        var labelInfo1 = DefineLabel(loopExpression.BreakLabel);
        var labelInfo2 = DefineLabel(loopExpression.ContinueLabel);
        labelInfo2.MarkWithEmptyStack();
        EmitExpressionAsVoid(loopExpression.Body);
        IL.Emit(OpCodes.Br, labelInfo2.Label);
        PopLabelBlock(LabelScopeKind.Statement);
        labelInfo1.MarkWithEmptyStack();
    }

    private void EmitMemberAddress(MemberInfo member, Type objectType)
    {
        if (member.MemberType == MemberTypes.Field)
        {
            var fi = (FieldInfo)member;
            if (!fi.IsLiteral && !fi.IsInitOnly)
            {
                IL.EmitFieldAddress(fi);
                return;
            }
        }

        EmitMemberGet(member, objectType);
        var local = GetLocal(GetMemberType(member));
        IL.Emit(OpCodes.Stloc, local);
        IL.Emit(OpCodes.Ldloca, local);
    }

    private void EmitMemberAssignment(BinaryExpression node, CompilationFlags flags)
    {
        var left = (MemberExpression)node.Left;
        var member = left.Member;
        var objectType = (Type)null;
        if (left.Expression != null)
        {
            EmitInstance(left.Expression, objectType = left.Expression.Type);
        }

        EmitExpression(node.Right);
        var local = (LocalBuilder)null;
        var compilationFlags = flags & CompilationFlags.EmitAsTypeMask;
        if (compilationFlags != CompilationFlags.EmitAsVoidType)
        {
            IL.Emit(OpCodes.Dup);
            IL.Emit(OpCodes.Stloc, local = GetLocal(node.Type));
        }

        switch (member.MemberType)
        {
            case MemberTypes.Field:
                IL.EmitFieldSet((FieldInfo)member);
                break;
            case MemberTypes.Property:
                EmitCall(objectType, ((PropertyInfo)member).GetSetMethod(true));
                break;
            default:
                throw Error.InvalidMemberType(member.MemberType);
        }

        if (compilationFlags == CompilationFlags.EmitAsVoidType)
        {
            return;
        }

        IL.Emit(OpCodes.Ldloc, local);
        FreeLocal(local);
    }

    private void EmitMemberAssignment(MemberAssignment binding, Type objectType)
    {
        EmitExpression(binding.Expression);
        var member1 = binding.Member as FieldInfo;
        if (member1 != null)
        {
            IL.Emit(OpCodes.Stfld, member1);
        }
        else
        {
            var member2 = binding.Member as PropertyInfo;
            if (!(member2 != null))
            {
                throw Error.UnhandledBinding();
            }

            EmitCall(objectType, member2.GetSetMethod(true));
        }
    }

    private void EmitMemberExpression(Expression expr)
    {
        var memberExpression = (MemberExpression)expr;
        var objectType = (Type)null;
        if (memberExpression.Expression != null)
        {
            EmitInstance(memberExpression.Expression, objectType = memberExpression.Expression.Type);
        }

        EmitMemberGet(memberExpression.Member, objectType);
    }

    private void EmitMemberGet(MemberInfo member, Type objectType)
    {
        switch (member.MemberType)
        {
            case MemberTypes.Field:
                var fi = (FieldInfo)member;
                if (fi.IsLiteral)
                {
                    EmitConstant(fi.GetRawConstantValue(), fi.FieldType);
                    break;
                }

                IL.EmitFieldGet(fi);
                break;
            case MemberTypes.Property:
                EmitCall(objectType, ((PropertyInfo)member).GetGetMethod(true));
                break;
            default:
                throw ContractUtils.Unreachable;
        }
    }

    private void EmitMemberInit(MemberInitExpression init)
    {
        EmitExpression(init.NewExpression);
        var local = (LocalBuilder)null;
        if (init.NewExpression.Type.IsValueType && init.Bindings.Count > 0)
        {
            local = IL.DeclareLocal(init.NewExpression.Type);
            IL.Emit(OpCodes.Stloc, local);
            IL.Emit(OpCodes.Ldloca, local);
        }

        EmitMemberInit(init.Bindings, local == null, init.NewExpression.Type);
        if (local == null)
        {
            return;
        }

        IL.Emit(OpCodes.Ldloc, local);
    }

    private void EmitMemberInit(
        ReadOnlyCollection<MemberBinding> bindings,
        bool keepOnStack,
        Type objectType)
    {
        var count = bindings.Count;
        if (count == 0)
        {
            if (keepOnStack)
            {
                return;
            }

            IL.Emit(OpCodes.Pop);
        }
        else
        {
            for (var index = 0; index < count; ++index)
            {
                if (keepOnStack || index < count - 1)
                {
                    IL.Emit(OpCodes.Dup);
                }

                EmitBinding(bindings[index], objectType);
            }
        }
    }

    private void EmitMemberInitExpression(Expression expr)
    {
        EmitMemberInit((MemberInitExpression)expr);
    }

    private void EmitMemberListBinding(MemberListBinding binding)
    {
        var memberType = GetMemberType(binding.Member);
        if (binding.Member as PropertyInfo != null && memberType.IsValueType)
        {
            throw Error.CannotAutoInitializeValueTypeElementThroughProperty(binding.Member);
        }

        if (memberType.IsValueType)
        {
            EmitMemberAddress(binding.Member, binding.Member.DeclaringType);
        }
        else
        {
            EmitMemberGet(binding.Member, binding.Member.DeclaringType);
        }

        EmitListInit(binding.Initializers, false, memberType);
    }

    private void EmitMemberMemberBinding(MemberMemberBinding binding)
    {
        var memberType = GetMemberType(binding.Member);
        if (binding.Member as PropertyInfo != null && memberType.IsValueType)
        {
            throw Error.CannotAutoInitializeValueTypeMemberThroughProperty(binding.Member);
        }

        if (memberType.IsValueType)
        {
            EmitMemberAddress(binding.Member, binding.Member.DeclaringType);
        }
        else
        {
            EmitMemberGet(binding.Member, binding.Member.DeclaringType);
        }

        EmitMemberInit(binding.Bindings, false, memberType);
    }

    private void EmitMethodAndAlso(BinaryExpression b, CompilationFlags flags)
    {
        var label = IL.DefineLabel();
        EmitExpression(b.Left);
        IL.Emit(OpCodes.Dup);
        var booleanOperator = TypeUtils.GetBooleanOperator(b.Method.DeclaringType, "op_False");
        IL.Emit(OpCodes.Call, booleanOperator);
        IL.Emit(OpCodes.Brtrue, label);
        var local1 = GetLocal(b.Left.Type);
        IL.Emit(OpCodes.Stloc, local1);
        EmitExpression(b.Right);
        var local2 = GetLocal(b.Right.Type);
        IL.Emit(OpCodes.Stloc, local2);
        IL.Emit(OpCodes.Ldloc, local1);
        IL.Emit(OpCodes.Ldloc, local2);
        if ((flags & CompilationFlags.EmitAsTailCallMask) == CompilationFlags.EmitAsTail)
        {
            IL.Emit(OpCodes.Tailcall);
        }

        IL.Emit(OpCodes.Call, b.Method);
        FreeLocal(local1);
        FreeLocal(local2);
        IL.MarkLabel(label);
    }

    private void EmitMethodCall(Expression obj, MethodInfo method, IArgumentProvider methodCallExpr)
    {
        EmitMethodCall(obj, method, methodCallExpr, CompilationFlags.EmitAsNoTail);
    }

    private void EmitMethodCall(
        Expression obj,
        MethodInfo method,
        IArgumentProvider methodCallExpr,
        CompilationFlags flags)
    {
        var objectType = (Type)null;
        if (!method.IsStatic)
        {
            EmitInstance(obj, objectType = obj.Type);
        }

        if (obj != null && obj.Type.IsValueType)
        {
            EmitMethodCall(method, methodCallExpr, objectType);
        }
        else
        {
            EmitMethodCall(method, methodCallExpr, objectType, flags);
        }
    }

    private void EmitMethodCall(MethodInfo mi, IArgumentProvider args, Type objectType)
    {
        EmitMethodCall(mi, args, objectType, CompilationFlags.EmitAsNoTail);
    }

    private void EmitMethodCall(
        MethodInfo mi,
        IArgumentProvider args,
        Type objectType,
        CompilationFlags flags)
    {
        var writeBacks = EmitArguments(mi, args);
        var opcode = UseVirtual(mi) ? OpCodes.Callvirt : OpCodes.Call;
        if (opcode == OpCodes.Callvirt && objectType.IsValueType)
        {
            IL.Emit(OpCodes.Constrained, objectType);
        }

        if ((flags & CompilationFlags.EmitAsTailCallMask) == CompilationFlags.EmitAsTail &&
            !MethodHasByRefParameter(mi))
        {
            IL.Emit(OpCodes.Tailcall);
        }

        if (mi.CallingConvention == CallingConventions.VarArgs)
        {
            IL.EmitCall(opcode, mi, args.Map(a => a.Type));
        }
        else
        {
            IL.Emit(opcode, mi);
        }

        EmitWriteBack(writeBacks);
    }

    private void EmitMethodCallExpression(Expression expr, CompilationFlags flags)
    {
        var methodCallExpr = (MethodCallExpression)expr;
        EmitMethodCall(methodCallExpr.Object, methodCallExpr.Method, methodCallExpr, flags);
    }

    private void EmitMethodCallExpression(Expression expr)
    {
        EmitMethodCallExpression(expr, CompilationFlags.EmitAsNoTail);
    }

    private void EmitMethodOrElse(BinaryExpression b, CompilationFlags flags)
    {
        var label = IL.DefineLabel();
        EmitExpression(b.Left);
        IL.Emit(OpCodes.Dup);
        var booleanOperator = TypeUtils.GetBooleanOperator(b.Method.DeclaringType, "op_True");
        IL.Emit(OpCodes.Call, booleanOperator);
        IL.Emit(OpCodes.Brtrue, label);
        var local1 = GetLocal(b.Left.Type);
        IL.Emit(OpCodes.Stloc, local1);
        EmitExpression(b.Right);
        var local2 = GetLocal(b.Right.Type);
        IL.Emit(OpCodes.Stloc, local2);
        IL.Emit(OpCodes.Ldloc, local1);
        IL.Emit(OpCodes.Ldloc, local2);
        if ((flags & CompilationFlags.EmitAsTailCallMask) == CompilationFlags.EmitAsTail)
        {
            IL.Emit(OpCodes.Tailcall);
        }

        IL.Emit(OpCodes.Call, b.Method);
        FreeLocal(local1);
        FreeLocal(local2);
        IL.MarkLabel(label);
    }

    private void EmitNewArrayExpression(Expression expr)
    {
        var node = (NewArrayExpression)expr;
        if (node.NodeType == ExpressionType.NewArrayInit)
        {
            IL.EmitArray(node.Type.GetElementType(), node.Expressions.Count,
                index => EmitExpression(node.Expressions[index]));
        }
        else
        {
            var expressions = node.Expressions;
            for (var index = 0; index < expressions.Count; ++index)
            {
                var node1 = expressions[index];
                EmitExpression(node1);
                IL.EmitConvertToType(node1.Type, typeof(int), true);
            }

            IL.EmitArray(node.Type);
        }
    }

    private void EmitNewExpression(Expression expr)
    {
        var args = (NewExpression)expr;
        if (args.Constructor != null)
        {
            var writeBacks = EmitArguments(args.Constructor, args);
            IL.Emit(OpCodes.Newobj, args.Constructor);
            EmitWriteBack(writeBacks);
        }
        else
        {
            var local = GetLocal(args.Type);
            IL.Emit(OpCodes.Ldloca, local);
            IL.Emit(OpCodes.Initobj, args.Type);
            IL.Emit(OpCodes.Ldloc, local);
            FreeLocal(local);
        }
    }

    private void EmitNullableCoalesce(BinaryExpression b)
    {
        var local = GetLocal(b.Left.Type);
        var label1 = IL.DefineLabel();
        var label2 = IL.DefineLabel();
        EmitExpression(b.Left);
        IL.Emit(OpCodes.Stloc, local);
        IL.Emit(OpCodes.Ldloca, local);
        IL.EmitHasValue(b.Left.Type);
        IL.Emit(OpCodes.Brfalse, label1);
        var nonNullableType = b.Left.Type.GetNonNullableType();
        if (b.Conversion != null)
        {
            var parameter = b.Conversion.Parameters[0];
            EmitLambdaExpression(b.Conversion);
            if (!parameter.Type.IsAssignableFrom(b.Left.Type))
            {
                IL.Emit(OpCodes.Ldloca, local);
                IL.EmitGetValueOrDefault(b.Left.Type);
            }
            else
            {
                IL.Emit(OpCodes.Ldloc, local);
            }

            IL.Emit(OpCodes.Callvirt, b.Conversion.Type.GetMethod("Invoke"));
        }
        else if (!TypeUtils.AreEquivalent(b.Type, nonNullableType))
        {
            IL.Emit(OpCodes.Ldloca, local);
            IL.EmitGetValueOrDefault(b.Left.Type);
            IL.EmitConvertToType(nonNullableType, b.Type, true);
        }
        else
        {
            IL.Emit(OpCodes.Ldloca, local);
            IL.EmitGetValueOrDefault(b.Left.Type);
        }

        FreeLocal(local);
        IL.Emit(OpCodes.Br, label2);
        IL.MarkLabel(label1);
        EmitExpression(b.Right);
        if (!TypeUtils.AreEquivalent(b.Right.Type, b.Type))
        {
            IL.EmitConvertToType(b.Right.Type, b.Type, true);
        }

        IL.MarkLabel(label2);
    }

    private void EmitNullEquality(ExpressionType op, Expression e, bool isLiftedToNull)
    {
        if (isLiftedToNull)
        {
            EmitExpressionAsVoid(e);
            IL.EmitDefault(typeof(bool?));
        }
        else
        {
            EmitAddress(e, e.Type);
            IL.EmitHasValue(e.Type);
            if (op != ExpressionType.Equal)
            {
                return;
            }

            IL.Emit(OpCodes.Ldc_I4_0);
            IL.Emit(OpCodes.Ceq);
        }
    }

    private void EmitOrElseBinaryExpression(Expression expr, CompilationFlags flags)
    {
        var b = (BinaryExpression)expr;
        if (b.Method != null && !b.IsLiftedLogical)
        {
            EmitMethodOrElse(b, flags);
        }
        else if (b.Left.Type == typeof(bool?))
        {
            EmitLiftedOrElse(b);
        }
        else if (b.IsLiftedLogical)
        {
            EmitExpression(b.ReduceUserdefinedLifted());
        }
        else
        {
            EmitUnliftedOrElse(b);
        }
    }

    private void EmitParameterExpression(Expression expr)
    {
        var variable = (ParameterExpression)expr;
        _scope.EmitGet(variable);
        if (!variable.IsByRef)
        {
            return;
        }

        IL.EmitLoadValueIndirect(variable.Type);
    }

    private void EmitQuote(UnaryExpression quote)
    {
        EmitConstant(quote.Operand, quote.Type);
        if (_scope.NearestHoistedLocals == null)
        {
            return;
        }

        EmitConstant(_scope.NearestHoistedLocals, typeof(object));
        _scope.EmitGet(_scope.NearestHoistedLocals.SelfVariable);
        IL.Emit(OpCodes.Call, typeof(RuntimeOps).GetMethod("Quote"));
        if (!(quote.Type != typeof(Expression)))
        {
            return;
        }

        IL.Emit(OpCodes.Castclass, quote.Type);
    }

    private void EmitQuoteUnaryExpression(Expression expr)
    {
        EmitQuote((UnaryExpression)expr);
    }

    private void EmitReferenceCoalesceWithoutConversion(BinaryExpression b)
    {
        var label1 = IL.DefineLabel();
        var label2 = IL.DefineLabel();
        EmitExpression(b.Left);
        IL.Emit(OpCodes.Dup);
        IL.Emit(OpCodes.Ldnull);
        IL.Emit(OpCodes.Ceq);
        IL.Emit(OpCodes.Brfalse, label2);
        IL.Emit(OpCodes.Pop);
        EmitExpression(b.Right);
        if (!TypeUtils.AreEquivalent(b.Right.Type, b.Type))
        {
            if (b.Right.Type.IsValueType)
            {
                IL.Emit(OpCodes.Box, b.Right.Type);
            }

            IL.Emit(OpCodes.Castclass, b.Type);
        }

        IL.Emit(OpCodes.Br_S, label1);
        IL.MarkLabel(label2);
        if (!TypeUtils.AreEquivalent(b.Left.Type, b.Type))
        {
            IL.Emit(OpCodes.Castclass, b.Type);
        }

        IL.MarkLabel(label1);
    }

    private void EmitRuntimeVariablesExpression(Expression expr)
    {
        _scope.EmitVariableAccess(this, ((RuntimeVariablesExpression)expr).Variables);
    }

    private void EmitSaveExceptionOrPop(CatchBlock cb)
    {
        if (cb.Variable != null)
        {
            _scope.EmitSet(cb.Variable);
        }
        else
        {
            IL.Emit(OpCodes.Pop);
        }
    }

    private void EmitSetIndexCall(IndexExpression node, Type objectType)
    {
        if (node.Indexer != null)
        {
            var setMethod = node.Indexer.GetSetMethod(true);
            EmitCall(objectType, setMethod);
        }
        else if (node.Arguments.Count != 1)
        {
            IL.Emit(OpCodes.Call, node.Object.Type.GetMethod("Set", BindingFlags.Instance | BindingFlags.Public));
        }
        else
        {
            IL.EmitStoreElement(node.Type);
        }
    }

    private void EmitSwitchBucket(
        SwitchInfo info,
        List<SwitchLabel> bucket)
    {
        if (bucket.Count == 1)
        {
            IL.Emit(OpCodes.Ldloc, info.Value);
            IL.EmitConstant(bucket[0].Constant);
            IL.Emit(OpCodes.Beq, bucket[0].Label);
        }
        else
        {
            var nullable = new Label?();
            if (info.Is64BitSwitch)
            {
                nullable = IL.DefineLabel();
                IL.Emit(OpCodes.Ldloc, info.Value);
                IL.EmitConstant(bucket.Last().Constant);
                IL.Emit(info.IsUnsigned ? OpCodes.Bgt_Un : OpCodes.Bgt, nullable.Value);
                IL.Emit(OpCodes.Ldloc, info.Value);
                IL.EmitConstant(bucket[0].Constant);
                IL.Emit(info.IsUnsigned ? OpCodes.Blt_Un : OpCodes.Blt, nullable.Value);
            }

            IL.Emit(OpCodes.Ldloc, info.Value);
            var key = bucket[0].Key;
            if (key != 0M)
            {
                IL.EmitConstant(bucket[0].Constant);
                IL.Emit(OpCodes.Sub);
            }

            if (info.Is64BitSwitch)
            {
                IL.Emit(OpCodes.Conv_I4);
            }

            var labels = new Label[(int)(bucket[bucket.Count - 1].Key - bucket[0].Key + 1M)];
            var num = 0;
            foreach (var switchLabel in bucket)
            {
                while (key++ != switchLabel.Key)
                {
                    labels[num++] = info.Default;
                }

                labels[num++] = switchLabel.Label;
            }

            IL.Emit(OpCodes.Switch, labels);
            if (!info.Is64BitSwitch)
            {
                return;
            }

            IL.MarkLabel(nullable.Value);
        }
    }

    private void EmitSwitchBuckets(
        SwitchInfo info,
        List<List<SwitchLabel>> buckets,
        int first,
        int last)
    {
        if (first == last)
        {
            EmitSwitchBucket(info, buckets[first]);
        }
        else
        {
            var first1 = (int)((first + (long)last + 1L) / 2L);
            if (first == first1 - 1)
            {
                EmitSwitchBucket(info, buckets[first]);
            }
            else
            {
                var label = IL.DefineLabel();
                IL.Emit(OpCodes.Ldloc, info.Value);
                IL.EmitConstant(buckets[first1 - 1].Last().Constant);
                IL.Emit(info.IsUnsigned ? OpCodes.Bgt_Un : OpCodes.Bgt, label);
                EmitSwitchBuckets(info, buckets, first, first1 - 1);
                IL.MarkLabel(label);
            }

            EmitSwitchBuckets(info, buckets, first1, last);
        }
    }

    private void EmitSwitchCases(
        SwitchExpression node,
        Label[] labels,
        bool[] isGoto,
        Label @default,
        Label end,
        CompilationFlags flags)
    {
        IL.Emit(OpCodes.Br, @default);
        var index = 0;
        for (var count = node.Cases.Count; index < count; ++index)
        {
            if (!isGoto[index])
            {
                IL.MarkLabel(labels[index]);
                EmitExpressionAsType(node.Cases[index].Body, node.Type, flags);
                if (node.DefaultBody != null || index < count - 1)
                {
                    if ((flags & CompilationFlags.EmitAsTailCallMask) == CompilationFlags.EmitAsTail)
                    {
                        IL.Emit(OpCodes.Ret);
                    }
                    else
                    {
                        IL.Emit(OpCodes.Br, end);
                    }
                }
            }
        }

        if (node.DefaultBody != null)
        {
            IL.MarkLabel(@default);
            EmitExpressionAsType(node.DefaultBody, node.Type, flags);
        }

        IL.MarkLabel(end);
    }

    private void EmitSwitchExpression(Expression expr, CompilationFlags flags)
    {
        var node = (SwitchExpression)expr;
        if (TryEmitSwitchInstruction(node, flags) || TryEmitHashtableSwitch(node, flags))
        {
            return;
        }

        var parameterExpression1 = Expression.Parameter(node.SwitchValue.Type, "switchValue");
        var parameterExpression2 = Expression.Parameter(GetTestValueType(node), "testValue");
        _scope.AddLocal(this, parameterExpression1);
        _scope.AddLocal(this, parameterExpression2);
        EmitExpression(node.SwitchValue);
        _scope.EmitSet(parameterExpression1);
        var labels = new Label[node.Cases.Count];
        var isGoto = new bool[node.Cases.Count];
        var index = 0;
        for (var count = node.Cases.Count; index < count; ++index)
        {
            DefineSwitchCaseLabel(node.Cases[index], out labels[index], out isGoto[index]);
            foreach (var testValue in node.Cases[index].TestValues)
            {
                EmitExpression(testValue);
                _scope.EmitSet(parameterExpression2);
                EmitExpressionAndBranch(true,
                    Expression.Equal(parameterExpression1, parameterExpression2, false, node.Comparison),
                    labels[index]);
            }
        }

        var end = IL.DefineLabel();
        var @default = node.DefaultBody == null ? end : IL.DefineLabel();
        EmitSwitchCases(node, labels, isGoto, @default, end, flags);
    }

    private void EmitThrow(UnaryExpression expr, CompilationFlags flags)
    {
        if (expr.Operand == null)
        {
            CheckRethrow();
            IL.Emit(OpCodes.Rethrow);
        }
        else
        {
            EmitExpression(expr.Operand);
            IL.Emit(OpCodes.Throw);
        }

        EmitUnreachable(expr, flags);
    }

    private void EmitThrowUnaryExpression(Expression expr)
    {
        EmitThrow((UnaryExpression)expr, CompilationFlags.EmitAsDefaultType);
    }

    private void EmitTryExpression(Expression expr)
    {
        var tryExpression = (TryExpression)expr;
        CheckTry();
        PushLabelBlock(LabelScopeKind.Try);
        IL.BeginExceptionBlock();
        EmitExpression(tryExpression.Body);
        var type = expr.Type;
        var local = (LocalBuilder)null;
        if (type != typeof(void))
        {
            local = GetLocal(type);
            IL.Emit(OpCodes.Stloc, local);
        }

        foreach (var handler in tryExpression.Handlers)
        {
            PushLabelBlock(LabelScopeKind.Catch);
            if (handler.Filter == null)
            {
                IL.BeginCatchBlock(handler.Test);
            }
            else
            {
                IL.BeginExceptFilterBlock();
            }

            EnterScope(handler);
            EmitCatchStart(handler);
            EmitExpression(handler.Body);
            if (type != typeof(void))
            {
                IL.Emit(OpCodes.Stloc, local);
            }

            ExitScope(handler);
            PopLabelBlock(LabelScopeKind.Catch);
        }

        if (tryExpression.Finally != null || tryExpression.Fault != null)
        {
            PushLabelBlock(LabelScopeKind.Finally);
            if (tryExpression.Finally != null)
            {
                IL.BeginFinallyBlock();
            }
            else
            {
                IL.BeginFaultBlock();
            }

            EmitExpressionAsVoid(tryExpression.Finally ?? tryExpression.Fault);
            IL.EndExceptionBlock();
            PopLabelBlock(LabelScopeKind.Finally);
        }
        else
        {
            IL.EndExceptionBlock();
        }

        if (type != typeof(void))
        {
            IL.Emit(OpCodes.Ldloc, local);
            FreeLocal(local);
        }

        PopLabelBlock(LabelScopeKind.Try);
    }

    private void EmitTypeBinaryExpression(Expression expr)
    {
        var typeIs = (TypeBinaryExpression)expr;
        if (typeIs.NodeType == ExpressionType.TypeEqual)
        {
            EmitExpression(typeIs.ReduceTypeEqual());
        }
        else
        {
            var type = typeIs.Expression.Type;
            var analyzeTypeIsResult = ConstantCheck.AnalyzeTypeIs(typeIs);
            switch (analyzeTypeIsResult)
            {
                case AnalyzeTypeIsResult.KnownFalse:
                case AnalyzeTypeIsResult.KnownTrue:
                    EmitExpressionAsVoid(typeIs.Expression);
                    IL.EmitBoolean(analyzeTypeIsResult == AnalyzeTypeIsResult.KnownTrue);
                    break;
                case AnalyzeTypeIsResult.KnownAssignable:
                    if (type.IsNullableType())
                    {
                        EmitAddress(typeIs.Expression, type);
                        IL.EmitHasValue(type);
                        break;
                    }

                    EmitExpression(typeIs.Expression);
                    IL.Emit(OpCodes.Ldnull);
                    IL.Emit(OpCodes.Ceq);
                    IL.Emit(OpCodes.Ldc_I4_0);
                    IL.Emit(OpCodes.Ceq);
                    break;
                default:
                    EmitExpression(typeIs.Expression);
                    if (type.IsValueType)
                    {
                        IL.Emit(OpCodes.Box, type);
                    }

                    IL.Emit(OpCodes.Isinst, typeIs.TypeOperand);
                    IL.Emit(OpCodes.Ldnull);
                    IL.Emit(OpCodes.Cgt_Un);
                    break;
            }
        }
    }

    private void EmitUnary(UnaryExpression node, CompilationFlags flags)
    {
        if (node.Method != null)
        {
            EmitUnaryMethod(node, flags);
        }
        else if (node.NodeType == ExpressionType.NegateChecked && TypeUtils.IsInteger(node.Operand.Type))
        {
            EmitExpression(node.Operand);
            var local = GetLocal(node.Operand.Type);
            IL.Emit(OpCodes.Stloc, local);
            IL.EmitInt(0);
            IL.EmitConvertToType(typeof(int), node.Operand.Type, false);
            IL.Emit(OpCodes.Ldloc, local);
            FreeLocal(local);
            EmitBinaryOperator(ExpressionType.SubtractChecked, node.Operand.Type, node.Operand.Type, node.Type, false);
        }
        else
        {
            EmitExpression(node.Operand);
            EmitUnaryOperator(node.NodeType, node.Operand.Type, node.Type);
        }
    }

    private void EmitUnaryExpression(Expression expr, CompilationFlags flags)
    {
        EmitUnary((UnaryExpression)expr, flags);
    }

    private void EmitUnaryMethod(UnaryExpression node, CompilationFlags flags)
    {
        if (node.IsLifted)
        {
            var parameterExpression = Expression.Variable(node.Operand.Type.GetNonNullableType(), null);
            var mc = Expression.Call(node.Method, parameterExpression);
            var nullableType = TypeUtils.GetNullableType(mc.Type);
            EmitLift(node.NodeType, nullableType, mc, new ParameterExpression[1]
            {
                parameterExpression
            }, new Expression[1] { node.Operand });
            IL.EmitConvertToType(nullableType, node.Type, false);
        }
        else
        {
            EmitMethodCallExpression(Expression.Call(node.Method, node.Operand), flags);
        }
    }

    private void EmitUnaryOperator(ExpressionType op, Type operandType, Type resultType)
    {
        var flag = operandType.IsNullableType();
        if (op == ExpressionType.ArrayLength)
        {
            IL.Emit(OpCodes.Ldlen);
        }
        else if (flag)
        {
            switch (op)
            {
                case ExpressionType.Negate:
                case ExpressionType.UnaryPlus:
                case ExpressionType.NegateChecked:
                case ExpressionType.Decrement:
                case ExpressionType.Increment:
                case ExpressionType.OnesComplement:
                case ExpressionType.IsTrue:
                case ExpressionType.IsFalse:
                    var label1 = IL.DefineLabel();
                    var label2 = IL.DefineLabel();
                    var local1 = GetLocal(operandType);
                    IL.Emit(OpCodes.Stloc, local1);
                    IL.Emit(OpCodes.Ldloca, local1);
                    IL.EmitHasValue(operandType);
                    IL.Emit(OpCodes.Brfalse_S, label1);
                    IL.Emit(OpCodes.Ldloca, local1);
                    IL.EmitGetValueOrDefault(operandType);
                    var nonNullableType1 = resultType.GetNonNullableType();
                    EmitUnaryOperator(op, nonNullableType1, nonNullableType1);
                    var constructor1 = resultType.GetConstructor(new Type[1]
                    {
                        nonNullableType1
                    });
                    IL.Emit(OpCodes.Newobj, constructor1);
                    IL.Emit(OpCodes.Stloc, local1);
                    IL.Emit(OpCodes.Br_S, label2);
                    IL.MarkLabel(label1);
                    IL.Emit(OpCodes.Ldloca, local1);
                    IL.Emit(OpCodes.Initobj, resultType);
                    IL.MarkLabel(label2);
                    IL.Emit(OpCodes.Ldloc, local1);
                    FreeLocal(local1);
                    break;
                case ExpressionType.Not:
                    if (!(operandType != typeof(bool?)))
                    {
                        var label3 = IL.DefineLabel();
                        var local2 = GetLocal(operandType);
                        IL.Emit(OpCodes.Stloc, local2);
                        IL.Emit(OpCodes.Ldloca, local2);
                        IL.EmitHasValue(operandType);
                        IL.Emit(OpCodes.Brfalse_S, label3);
                        IL.Emit(OpCodes.Ldloca, local2);
                        IL.EmitGetValueOrDefault(operandType);
                        var nonNullableType2 = operandType.GetNonNullableType();
                        EmitUnaryOperator(op, nonNullableType2, typeof(bool));
                        var constructor2 = resultType.GetConstructor(new Type[1]
                        {
                            typeof(bool)
                        });
                        IL.Emit(OpCodes.Newobj, constructor2);
                        IL.Emit(OpCodes.Stloc, local2);
                        IL.MarkLabel(label3);
                        IL.Emit(OpCodes.Ldloc, local2);
                        FreeLocal(local2);
                        break;
                    }

                    goto case ExpressionType.Negate;
                case ExpressionType.TypeAs:
                    IL.Emit(OpCodes.Box, operandType);
                    IL.Emit(OpCodes.Isinst, resultType);
                    if (!resultType.IsNullableType())
                    {
                        break;
                    }

                    IL.Emit(OpCodes.Unbox_Any, resultType);
                    break;
                default:
                    throw Error.UnhandledUnary(op);
            }
        }
        else
        {
            switch (op)
            {
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                    IL.Emit(OpCodes.Neg);
                    break;
                case ExpressionType.UnaryPlus:
                    IL.Emit(OpCodes.Nop);
                    break;
                case ExpressionType.Not:
                    if (operandType == typeof(bool))
                    {
                        IL.Emit(OpCodes.Ldc_I4_0);
                        IL.Emit(OpCodes.Ceq);
                        break;
                    }

                    IL.Emit(OpCodes.Not);
                    break;
                case ExpressionType.TypeAs:
                    if (operandType.IsValueType)
                    {
                        IL.Emit(OpCodes.Box, operandType);
                    }

                    IL.Emit(OpCodes.Isinst, resultType);
                    if (!resultType.IsNullableType())
                    {
                        return;
                    }

                    IL.Emit(OpCodes.Unbox_Any, resultType);
                    return;
                case ExpressionType.Decrement:
                    EmitConstantOne(resultType);
                    IL.Emit(OpCodes.Sub);
                    break;
                case ExpressionType.Increment:
                    EmitConstantOne(resultType);
                    IL.Emit(OpCodes.Add);
                    break;
                case ExpressionType.OnesComplement:
                    IL.Emit(OpCodes.Not);
                    break;
                case ExpressionType.IsTrue:
                    IL.Emit(OpCodes.Ldc_I4_1);
                    IL.Emit(OpCodes.Ceq);
                    return;
                case ExpressionType.IsFalse:
                    IL.Emit(OpCodes.Ldc_I4_0);
                    IL.Emit(OpCodes.Ceq);
                    return;
                default:
                    throw Error.UnhandledUnary(op);
            }

            EmitConvertArithmeticResult(op, resultType);
        }
    }

    private void EmitUnboxUnaryExpression(Expression expr)
    {
        var unaryExpression = (UnaryExpression)expr;
        EmitExpression(unaryExpression.Operand);
        IL.Emit(OpCodes.Unbox_Any, unaryExpression.Type);
    }

    private void EmitUnliftedAndAlso(BinaryExpression b)
    {
        var label1 = IL.DefineLabel();
        var label2 = IL.DefineLabel();
        EmitExpressionAndBranch(false, b.Left, label1);
        EmitExpression(b.Right);
        IL.Emit(OpCodes.Br, label2);
        IL.MarkLabel(label1);
        IL.Emit(OpCodes.Ldc_I4_0);
        IL.MarkLabel(label2);
    }

    private void EmitUnliftedBinaryOp(ExpressionType op, Type leftType, Type rightType)
    {
        if (op == ExpressionType.Equal || op == ExpressionType.NotEqual)
        {
            EmitUnliftedEquality(op, leftType);
        }
        else
        {
            if (!leftType.IsPrimitive)
            {
                throw Error.OperatorNotImplementedForType(op, leftType);
            }

            switch (op)
            {
                case ExpressionType.Add:
                    IL.Emit(OpCodes.Add);
                    break;
                case ExpressionType.AddChecked:
                    if (TypeUtils.IsFloatingPoint(leftType))
                    {
                        IL.Emit(OpCodes.Add);
                        break;
                    }

                    if (TypeUtils.IsUnsigned(leftType))
                    {
                        IL.Emit(OpCodes.Add_Ovf_Un);
                        break;
                    }

                    IL.Emit(OpCodes.Add_Ovf);
                    break;
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    IL.Emit(OpCodes.And);
                    break;
                case ExpressionType.Divide:
                    if (TypeUtils.IsUnsigned(leftType))
                    {
                        IL.Emit(OpCodes.Div_Un);
                        break;
                    }

                    IL.Emit(OpCodes.Div);
                    break;
                case ExpressionType.ExclusiveOr:
                    IL.Emit(OpCodes.Xor);
                    break;
                case ExpressionType.GreaterThan:
                    if (TypeUtils.IsUnsigned(leftType))
                    {
                        IL.Emit(OpCodes.Cgt_Un);
                        break;
                    }

                    IL.Emit(OpCodes.Cgt);
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    var label1 = IL.DefineLabel();
                    var label2 = IL.DefineLabel();
                    if (TypeUtils.IsUnsigned(leftType))
                    {
                        IL.Emit(OpCodes.Bge_Un_S, label1);
                    }
                    else
                    {
                        IL.Emit(OpCodes.Bge_S, label1);
                    }

                    IL.Emit(OpCodes.Ldc_I4_0);
                    IL.Emit(OpCodes.Br_S, label2);
                    IL.MarkLabel(label1);
                    IL.Emit(OpCodes.Ldc_I4_1);
                    IL.MarkLabel(label2);
                    break;
                case ExpressionType.LeftShift:
                    if (rightType != typeof(int))
                    {
                        throw ContractUtils.Unreachable;
                    }

                    IL.Emit(OpCodes.Shl);
                    break;
                case ExpressionType.LessThan:
                    if (TypeUtils.IsUnsigned(leftType))
                    {
                        IL.Emit(OpCodes.Clt_Un);
                        break;
                    }

                    IL.Emit(OpCodes.Clt);
                    break;
                case ExpressionType.LessThanOrEqual:
                    var label3 = IL.DefineLabel();
                    var label4 = IL.DefineLabel();
                    if (TypeUtils.IsUnsigned(leftType))
                    {
                        IL.Emit(OpCodes.Ble_Un_S, label3);
                    }
                    else
                    {
                        IL.Emit(OpCodes.Ble_S, label3);
                    }

                    IL.Emit(OpCodes.Ldc_I4_0);
                    IL.Emit(OpCodes.Br_S, label4);
                    IL.MarkLabel(label3);
                    IL.Emit(OpCodes.Ldc_I4_1);
                    IL.MarkLabel(label4);
                    break;
                case ExpressionType.Modulo:
                    if (TypeUtils.IsUnsigned(leftType))
                    {
                        IL.Emit(OpCodes.Rem_Un);
                        break;
                    }

                    IL.Emit(OpCodes.Rem);
                    break;
                case ExpressionType.Multiply:
                    IL.Emit(OpCodes.Mul);
                    break;
                case ExpressionType.MultiplyChecked:
                    if (TypeUtils.IsFloatingPoint(leftType))
                    {
                        IL.Emit(OpCodes.Mul);
                        break;
                    }

                    if (TypeUtils.IsUnsigned(leftType))
                    {
                        IL.Emit(OpCodes.Mul_Ovf_Un);
                        break;
                    }

                    IL.Emit(OpCodes.Mul_Ovf);
                    break;
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    IL.Emit(OpCodes.Or);
                    break;
                case ExpressionType.RightShift:
                    if (rightType != typeof(int))
                    {
                        throw ContractUtils.Unreachable;
                    }

                    if (TypeUtils.IsUnsigned(leftType))
                    {
                        IL.Emit(OpCodes.Shr_Un);
                        break;
                    }

                    IL.Emit(OpCodes.Shr);
                    break;
                case ExpressionType.Subtract:
                    IL.Emit(OpCodes.Sub);
                    break;
                case ExpressionType.SubtractChecked:
                    if (TypeUtils.IsFloatingPoint(leftType))
                    {
                        IL.Emit(OpCodes.Sub);
                        break;
                    }

                    if (TypeUtils.IsUnsigned(leftType))
                    {
                        IL.Emit(OpCodes.Sub_Ovf_Un);
                        break;
                    }

                    IL.Emit(OpCodes.Sub_Ovf);
                    break;
                default:
                    throw Error.UnhandledBinary(op);
            }
        }
    }

    private void EmitUnliftedEquality(ExpressionType op, Type type)
    {
        if (!type.IsPrimitive && type.IsValueType && !type.IsEnum)
        {
            throw Error.OperatorNotImplementedForType(op, type);
        }

        IL.Emit(OpCodes.Ceq);
        if (op != ExpressionType.NotEqual)
        {
            return;
        }

        IL.Emit(OpCodes.Ldc_I4_0);
        IL.Emit(OpCodes.Ceq);
    }

    private void EmitUnliftedOrElse(BinaryExpression b)
    {
        var label1 = IL.DefineLabel();
        var label2 = IL.DefineLabel();
        EmitExpressionAndBranch(false, b.Left, label1);
        IL.Emit(OpCodes.Ldc_I4_1);
        IL.Emit(OpCodes.Br, label2);
        IL.MarkLabel(label1);
        EmitExpression(b.Right);
        IL.MarkLabel(label2);
    }

    private void EmitUnreachable(Expression node, CompilationFlags flags)
    {
        if (!(node.Type != typeof(void)) || (flags & CompilationFlags.EmitAsVoidType) != 0)
        {
            return;
        }

        IL.EmitDefault(node.Type);
    }

    private void EmitVariableAssignment(BinaryExpression node, CompilationFlags flags)
    {
        var left = (ParameterExpression)node.Left;
        var compilationFlags = flags & CompilationFlags.EmitAsTypeMask;
        EmitExpression(node.Right);
        if (compilationFlags != CompilationFlags.EmitAsVoidType)
        {
            IL.Emit(OpCodes.Dup);
        }

        if (left.IsByRef)
        {
            var local = GetLocal(left.Type);
            IL.Emit(OpCodes.Stloc, local);
            _scope.EmitGet(left);
            IL.Emit(OpCodes.Ldloc, local);
            FreeLocal(local);
            IL.EmitStoreValueIndirect(left.Type);
        }
        else
        {
            _scope.EmitSet(left);
        }
    }

    private static void EmitWriteBack(IList<WriteBack> writeBacks)
    {
        foreach (var writeBack in writeBacks)
        {
            writeBack();
        }
    }

    private LabelInfo EnsureLabel(LabelTarget node)
    {
        LabelInfo labelInfo;
        if (!_labelInfo.TryGetValue(node, out labelInfo))
        {
            _labelInfo.Add(node, labelInfo = new LabelInfo(IL, node, false));
        }

        return labelInfo;
    }

    private void EnterScope(object node)
    {
        if (!HasVariables(node) || (_scope.MergedScopes != null && _scope.MergedScopes.Contains(node)))
        {
            return;
        }

        CompilerScope compilerScope;
        if (!_tree.Scopes.TryGetValue(node, out compilerScope))
        {
            compilerScope = new CompilerScope(node, false)
            {
                NeedsClosure = _scope.NeedsClosure
            };
        }

        _scope = compilerScope.Enter(this, _scope);
    }

    private void ExitScope(object node)
    {
        if (_scope.Node != node)
        {
            return;
        }

        _scope = _scope.Exit();
    }

    private static bool FitsInBucket(
        List<SwitchLabel> buckets,
        decimal key,
        int count)
    {
        var num = key - buckets[0].Key + 1M;
        return !(num > 2147483647M) && (buckets.Count + count) * 2 > num;
    }

    private static Expression GetEqualityOperand(Expression expression)
    {
        if (expression.NodeType == ExpressionType.Convert)
        {
            var unaryExpression = (UnaryExpression)expression;
            if (TypeUtils.AreReferenceAssignable(unaryExpression.Type, unaryExpression.Operand.Type))
            {
                return unaryExpression.Operand;
            }
        }

        return expression;
    }

    private static Type GetMemberType(MemberInfo member)
    {
        var fieldInfo = member as FieldInfo;
        if (fieldInfo != null)
        {
            return fieldInfo.FieldType;
        }

        var propertyInfo = member as PropertyInfo;
        return propertyInfo != null ? propertyInfo.PropertyType : throw Error.MemberNotFieldOrProperty(member);
    }

    private static Type[] GetParameterTypes(LambdaExpression lambda)
    {
        return lambda.Parameters.Map(
            (Func<ParameterExpression, Type>)(p => !p.IsByRef ? p.Type : p.Type.MakeByRefType()));
    }

    private static Type GetTestValueType(SwitchExpression node)
    {
        if (node.Comparison == null)
        {
            return node.Cases[0].TestValues[0].Type;
        }

        var type = node.Comparison.GetParameters()[1].ParameterType.GetNonRefType();
        if (node.IsLifted)
        {
            type = TypeUtils.GetNullableType(type);
        }

        return type;
    }

    private static string GetUniqueMethodName()
    {
        return
            $"<ExpressionCompilerImplementationDetails>{{{Interlocked.Increment(ref _Counter).ToString()}}}lambda_method";
    }

    private static bool HasVariables(object node)
    {
        return node is BlockExpression blockExpression
            ? blockExpression.Variables.Count > 0
            : ((CatchBlock)node).Variable != null;
    }

    private void InitializeMethod()
    {
        AddReturnLabel(_lambda);
        _boundConstants.EmitCacheConstants(this);
    }

    private static bool IsChecked(ExpressionType op)
    {
        switch (op)
        {
            case ExpressionType.AddChecked:
            case ExpressionType.ConvertChecked:
            case ExpressionType.MultiplyChecked:
            case ExpressionType.NegateChecked:
            case ExpressionType.SubtractChecked:
            case ExpressionType.AddAssignChecked:
            case ExpressionType.MultiplyAssignChecked:
            case ExpressionType.SubtractAssignChecked:
                return true;
            default:
                return false;
        }
    }

    private static void MergeBuckets(List<List<SwitchLabel>> buckets)
    {
        while (buckets.Count > 1)
        {
            var bucket1 = buckets[buckets.Count - 2];
            var bucket2 = buckets[buckets.Count - 1];
            if (!FitsInBucket(bucket1, bucket2[bucket2.Count - 1].Key, bucket2.Count))
            {
                break;
            }

            bucket1.AddRange(bucket2);
            buckets.RemoveAt(buckets.Count - 1);
        }
    }

    private static bool MethodHasByRefParameter(MethodInfo mi)
    {
        foreach (var pi in mi.GetParameters())
        {
            if (pi.IsByRefParameter())
            {
                return true;
            }
        }

        return false;
    }

    private static bool NotEmpty(Expression node)
    {
        return !(node is DefaultExpression defaultExpression) || defaultExpression.Type != typeof(void);
    }

    private void PopLabelBlock(LabelScopeKind kind)
    {
        _labelBlock = _labelBlock.Parent;
    }

    private void PushLabelBlock(LabelScopeKind type)
    {
        _labelBlock = new LabelScopeInfo(_labelBlock, type);
    }

    private LabelInfo ReferenceLabel(LabelTarget node)
    {
        var labelInfo = EnsureLabel(node);
        labelInfo.Reference(_labelBlock);
        return labelInfo;
    }

    private static bool Significant(Expression node)
    {
        if (node is BlockExpression blockExpression)
        {
            for (var index = 0; index < blockExpression.ExpressionCount; ++index)
            {
                if (Significant(blockExpression.GetExpression(index)))
                {
                    return true;
                }
            }

            return false;
        }

        return NotEmpty(node) && !(node is DebugInfoExpression);
    }

    private bool TryEmitHashtableSwitch(SwitchExpression node, CompilationFlags flags)
    {
        if (node.Comparison != typeof(string).GetMethod("op_Equality",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null, new Type[2]
                {
                    typeof(string),
                    typeof(string)
                }, null))
        {
            return false;
        }

        var capacity = 0;
        foreach (var switchCase in node.Cases)
        {
            foreach (var testValue in switchCase.TestValues)
            {
                if (!(testValue is ConstantExpression))
                {
                    return false;
                }

                ++capacity;
            }
        }

        if (capacity < 7)
        {
            return false;
        }

        var initializers = new List<ElementInit>(capacity);
        var cases = new List<SwitchCase>(node.Cases.Count);
        var num = -1;
        var method = typeof(Dictionary<string, int>).GetMethod("Add", new Type[2]
        {
            typeof(string),
            typeof(int)
        });
        var index = 0;
        for (var count = node.Cases.Count; index < count; ++index)
        {
            foreach (ConstantExpression testValue in node.Cases[index].TestValues)
            {
                if (testValue.Value != null)
                {
                    initializers.Add(Expression.ElementInit(method, testValue, Expression.Constant(index)));
                }
                else
                {
                    num = index;
                }
            }

            cases.Add(Expression.SwitchCase(node.Cases[index].Body, Expression.Constant(index)));
        }

        var initializedField = CreateLazyInitializedField<Dictionary<string, int>>("dictionarySwitch");
        var instance = (Expression)Expression.Condition(
            Expression.Equal(initializedField, Expression.Constant(null, initializedField.Type)), Expression.Assign(
                initializedField, Expression.ListInit(Expression.New(typeof(Dictionary<string, int>).GetConstructor(
                    new Type[1]
                    {
                        typeof(int)
                    }), Expression.Constant(initializers.Count)), initializers)), initializedField);
        var left = Expression.Variable(typeof(string), "switchValue");
        var parameterExpression = Expression.Variable(typeof(int), "switchIndex");
        EmitExpression(Expression.Block(new ParameterExpression[2]
            {
                parameterExpression,
                left
            }, Expression.Assign(left, node.SwitchValue),
            Expression.IfThenElse(Expression.Equal(left, Expression.Constant(null, typeof(string))),
                Expression.Assign(parameterExpression, Expression.Constant(num)),
                Expression.IfThenElse(Expression.Call(instance, "TryGetValue", null, left, parameterExpression),
                    Expression.Empty(), Expression.Assign(parameterExpression, Expression.Constant(-1)))),
            Expression.Switch(node.Type, parameterExpression, node.DefaultBody, null, cases)), flags);
        return true;
    }

    private bool TryEmitSwitchInstruction(
        SwitchExpression node,
        CompilationFlags flags)
    {
        if (node.Comparison != null)
        {
            return false;
        }

        var type = node.SwitchValue.Type;
        if (!CanOptimizeSwitchType(type) || !TypeUtils.AreEquivalent(type, node.Cases[0].TestValues[0].Type) ||
            !node.Cases.All(c => c.TestValues.All(t => t is ConstantExpression)))
        {
            return false;
        }

        var labels = new Label[node.Cases.Count];
        var isGoto = new bool[node.Cases.Count];
        var set = new Set<decimal>();
        var switchLabelList = new List<SwitchLabel>();
        for (var index = 0; index < node.Cases.Count; ++index)
        {
            DefineSwitchCaseLabel(node.Cases[index], out labels[index], out isGoto[index]);
            foreach (ConstantExpression testValue in node.Cases[index].TestValues)
            {
                var key = ConvertSwitchValue(testValue.Value);
                if (!set.Contains(key))
                {
                    switchLabelList.Add(new SwitchLabel(key, testValue.Value, labels[index]));
                    set.Add(key);
                }
            }
        }

        switchLabelList.Sort((x, y) => Math.Sign(x.Key - y.Key));
        var buckets = new List<List<SwitchLabel>>();
        foreach (var key in switchLabelList)
        {
            AddToBuckets(buckets, key);
        }

        var local = GetLocal(node.SwitchValue.Type);
        EmitExpression(node.SwitchValue);
        IL.Emit(OpCodes.Stloc, local);
        var end = IL.DefineLabel();
        var @default = node.DefaultBody == null ? end : IL.DefineLabel();
        EmitSwitchBuckets(new SwitchInfo(node, local, @default), buckets, 0, buckets.Count - 1);
        EmitSwitchCases(node, labels, isGoto, @default, end, flags);
        FreeLocal(local);
        return true;
    }

    private bool TryPushLabelBlock(Expression node)
    {
        switch (node.NodeType)
        {
            case ExpressionType.Conditional:
            case ExpressionType.Goto:
            case ExpressionType.Loop:
                PushLabelBlock(LabelScopeKind.Statement);
                return true;
            case ExpressionType.Convert:
                if (!(node.Type != typeof(void)))
                {
                    PushLabelBlock(LabelScopeKind.Statement);
                    return true;
                }

                break;
            case ExpressionType.Block:
                if (!(node is SpilledExpressionBlock))
                {
                    PushLabelBlock(LabelScopeKind.Block);
                    if (_labelBlock.Parent.Kind != LabelScopeKind.Switch)
                    {
                        DefineBlockLabels(node);
                    }

                    return true;
                }

                break;
            case ExpressionType.Label:
                if (_labelBlock.Kind == LabelScopeKind.Block)
                {
                    var target = ((LabelExpression)node).Target;
                    if (_labelBlock.ContainsTarget(target) || (_labelBlock.Parent.Kind == LabelScopeKind.Switch &&
                                                               _labelBlock.Parent.ContainsTarget(target)))
                    {
                        return false;
                    }
                }

                PushLabelBlock(LabelScopeKind.Statement);
                return true;
            case ExpressionType.Switch:
                PushLabelBlock(LabelScopeKind.Switch);
                var switchExpression = (SwitchExpression)node;
                foreach (var switchCase in switchExpression.Cases)
                {
                    DefineBlockLabels(switchCase.Body);
                }

                DefineBlockLabels(switchExpression.DefaultBody);
                return true;
        }

        if (_labelBlock.Kind == LabelScopeKind.Expression)
        {
            return false;
        }

        PushLabelBlock(LabelScopeKind.Expression);
        return true;
    }

    private static CompilationFlags UpdateEmitAsTailCallFlag(
        CompilationFlags flags,
        CompilationFlags newValue)
    {
        var compilationFlags = flags & CompilationFlags.EmitAsTailCallMask;
        return (flags ^ compilationFlags) | newValue;
    }

    private static CompilationFlags UpdateEmitAsTypeFlag(
        CompilationFlags flags,
        CompilationFlags newValue)
    {
        var compilationFlags = flags & CompilationFlags.EmitAsTypeMask;
        return (flags ^ compilationFlags) | newValue;
    }

    private static CompilationFlags UpdateEmitExpressionStartFlag(
        CompilationFlags flags,
        CompilationFlags newValue)
    {
        var compilationFlags = flags & CompilationFlags.EmitExpressionStartMask;
        return (flags ^ compilationFlags) | newValue;
    }

    private static bool UseVirtual(MethodInfo mi)
    {
        return !mi.IsStatic && !mi.DeclaringType.IsValueType;
    }

    private delegate void WriteBack();

    [Flags]
    internal enum CompilationFlags
    {
        EmitExpressionStart = 1,
        EmitNoExpressionStart = 2,
        EmitAsDefaultType = 16, // 0x00000010
        EmitAsVoidType = 32, // 0x00000020
        EmitAsTail = 256, // 0x00000100
        EmitAsMiddle = 512, // 0x00000200
        EmitAsNoTail = 1024, // 0x00000400
        EmitExpressionStartMask = 15, // 0x0000000F
        EmitAsTypeMask = 240, // 0x000000F0
        EmitAsTailCallMask = 3840 // 0x00000F00
    }

    private sealed class SwitchLabel
    {
        internal readonly object Constant;
        internal readonly decimal Key;
        internal readonly Label Label;

        internal SwitchLabel(decimal key, object constant, Label label)
        {
            Key = key;
            Constant = constant;
            Label = label;
        }
    }

    private sealed class SwitchInfo
    {
        internal readonly Label Default;
        internal readonly bool Is64BitSwitch;
        internal readonly bool IsUnsigned;
        internal readonly SwitchExpression Node;
        internal readonly Type Type;
        internal readonly LocalBuilder Value;

        internal SwitchInfo(SwitchExpression node, LocalBuilder value, Label @default)
        {
            Node = node;
            Value = value;
            Default = @default;
            Type = Node.SwitchValue.Type;
            IsUnsigned = TypeUtils.IsUnsigned(Type);
            var typeCode = Type.GetTypeCode(Type);
            Is64BitSwitch = typeCode == TypeCode.UInt64 || typeCode == TypeCode.Int64;
        }
    }
}