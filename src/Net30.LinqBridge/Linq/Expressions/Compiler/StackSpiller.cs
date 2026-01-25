#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Compiler;

internal class StackSpiller
{
    private readonly StackGuard _guard = new();
    private readonly Stack _startingStack;
    private readonly TempMaker _tm = new();
    private RewriteAction _lambdaRewrite;

    private StackSpiller(Stack stack)
    {
        _startingStack = stack;
    }

    internal static LambdaExpression AnalyzeLambda(LambdaExpression lambda)
    {
        return lambda.Accept(new StackSpiller(Stack.Empty));
    }

    internal Expression<T> Rewrite<T>(Expression<T> lambda)
    {
        var result = RewriteExpressionFreeTemps(lambda.Body, _startingStack);
        _lambdaRewrite = result.Action;
        if (result.Action == RewriteAction.None)
        {
            return lambda;
        }

        var body = result.Node;
        if (_tm.Temps.Count > 0)
        {
            body = Expression.Block(_tm.Temps, body);
        }

        return new Expression<T>(body, lambda.Name, lambda.TailCall, lambda.Parameters);
    }

    private static T[] Clone<T>(ReadOnlyCollection<T> original, int max)
    {
        var objArray = new T[original.Count];
        for (var index = 0; index < max; ++index)
        {
            objArray[index] = original[index];
        }

        return objArray;
    }

    private void Free(int mark)
    {
        _tm.Free(mark);
    }

    private static Expression MakeBlock(params Expression[] expressions)
    {
        return MakeBlock((IList<Expression>)expressions);
    }

    private static Expression MakeBlock(IList<Expression> expressions)
    {
        return new SpilledExpressionBlock(expressions);
    }

    private ParameterExpression MakeTemp(Type type)
    {
        return _tm.Temp(type);
    }

    private int Mark()
    {
        return _tm.Mark();
    }

    private static void RequireNoRefArgs(MethodBase method)
    {
        if (method != null && method.GetParameters().Any(p => p.ParameterType.IsByRef))
        {
            throw Error.TryNotSupportedForMethodsWithRefArgs(method);
        }
    }

    private static void RequireNotRefInstance(Expression instance)
    {
        if (instance != null && instance.Type.IsValueType && Type.GetTypeCode(instance.Type) == TypeCode.Object)
        {
            throw Error.TryNotSupportedForValueTypeInstances(instance.Type);
        }
    }

    private Result RewriteAssignBinaryExpression(
        Expression expr,
        Stack stack)
    {
        var node = (BinaryExpression)expr;
        switch (node.Left.NodeType)
        {
            case ExpressionType.MemberAccess:
                return RewriteMemberAssignment(node, stack);
            case ExpressionType.Parameter:
                return RewriteVariableAssignment(node, stack);
            case ExpressionType.Extension:
                return RewriteExtensionAssignment(node, stack);
            case ExpressionType.Index:
                return RewriteIndexAssignment(node, stack);
            default:
                throw Error.InvalidLvalue(node.Left.NodeType);
        }
    }

    private Result RewriteBinaryExpression(Expression expr, Stack stack)
    {
        var binaryExpression = (BinaryExpression)expr;
        var childRewriter = new ChildRewriter(this, stack, 3);
        childRewriter.Add(binaryExpression.Left);
        childRewriter.Add(binaryExpression.Right);
        childRewriter.Add(binaryExpression.Conversion);
        if (childRewriter.Action == RewriteAction.SpillStack)
        {
            RequireNoRefArgs(binaryExpression.Method);
        }

        return childRewriter.Finish(childRewriter.Rewrite
            ? BinaryExpression.Create(binaryExpression.NodeType, childRewriter[0], childRewriter[1],
                binaryExpression.Type, binaryExpression.Method, (LambdaExpression)childRewriter[2])
            : expr);
    }

    private Result RewriteBlockExpression(Expression expr, Stack stack)
    {
        var blockExpression = (BlockExpression)expr;
        var expressionCount = blockExpression.ExpressionCount;
        var action = RewriteAction.None;
        var args = (Expression[])null;
        for (var index = 0; index < expressionCount; ++index)
        {
            var result = RewriteExpression(blockExpression.GetExpression(index), stack);
            action |= result.Action;
            if (args == null && result.Action != RewriteAction.None)
            {
                args = Clone(blockExpression.Expressions, index);
            }

            if (args != null)
            {
                args[index] = result.Node;
            }
        }

        if (action != RewriteAction.None)
        {
            expr = blockExpression.Rewrite(null, args);
        }

        return new Result(action, expr);
    }

    private Result RewriteConditionalExpression(
        Expression expr,
        Stack stack)
    {
        var conditionalExpression = (ConditionalExpression)expr;
        var result1 = RewriteExpression(conditionalExpression.Test, stack);
        var result2 = RewriteExpression(conditionalExpression.IfTrue, stack);
        var result3 = RewriteExpression(conditionalExpression.IfFalse, stack);
        var action = result1.Action | result2.Action | result3.Action;
        if (action != RewriteAction.None)
        {
            expr = Expression.Condition(result1.Node, result2.Node, result3.Node, conditionalExpression.Type);
        }

        return new Result(action, expr);
    }

    private Result RewriteDynamicExpression(Expression expr, Stack stack)
    {
        var dynamicExpression = (DynamicExpression)expr;
        var expressions = (IArgumentProvider)dynamicExpression;
        var childRewriter = new ChildRewriter(this, Stack.NonEmpty, expressions.ArgumentCount);
        childRewriter.AddArguments(expressions);
        if (childRewriter.Action == RewriteAction.SpillStack)
        {
            RequireNoRefArgs(dynamicExpression.DelegateType.GetMethod("Invoke"));
        }

        return childRewriter.Finish(childRewriter.Rewrite ? dynamicExpression.Rewrite(childRewriter[0, -1]) : expr);
    }

    private Result RewriteExpression(Expression node, Stack stack)
    {
        if (node == null)
        {
            return new Result(RewriteAction.None, null);
        }

        if (!_guard.TryEnterOnCurrentStack())
        {
            return _guard.RunOnEmptyStack((@this, n, s) => @this.RewriteExpression(n, s), this, node, stack);
        }

        switch (node.NodeType)
        {
            case ExpressionType.Add:
                return RewriteBinaryExpression(node, stack);
            case ExpressionType.AddChecked:
                return RewriteBinaryExpression(node, stack);
            case ExpressionType.And:
                return RewriteBinaryExpression(node, stack);
            case ExpressionType.AndAlso:
                return RewriteLogicalBinaryExpression(node, stack);
            case ExpressionType.ArrayLength:
                return RewriteUnaryExpression(node, stack);
            case ExpressionType.ArrayIndex:
                return RewriteBinaryExpression(node, stack);
            case ExpressionType.Call:
                return RewriteMethodCallExpression(node, stack);
            case ExpressionType.Coalesce:
                return RewriteLogicalBinaryExpression(node, stack);
            case ExpressionType.Conditional:
                return RewriteConditionalExpression(node, stack);
            case ExpressionType.Constant:
            case ExpressionType.Parameter:
            case ExpressionType.Quote:
            case ExpressionType.DebugInfo:
            case ExpressionType.Default:
            case ExpressionType.RuntimeVariables:
                return new Result(RewriteAction.None, node);
            case ExpressionType.Convert:
                return RewriteUnaryExpression(node, stack);
            case ExpressionType.ConvertChecked:
                return RewriteUnaryExpression(node, stack);
            case ExpressionType.Divide:
                return RewriteBinaryExpression(node, stack);
            case ExpressionType.Equal:
                return RewriteBinaryExpression(node, stack);
            case ExpressionType.ExclusiveOr:
                return RewriteBinaryExpression(node, stack);
            case ExpressionType.GreaterThan:
                return RewriteBinaryExpression(node, stack);
            case ExpressionType.GreaterThanOrEqual:
                return RewriteBinaryExpression(node, stack);
            case ExpressionType.Invoke:
                return RewriteInvocationExpression(node, stack);
            case ExpressionType.Lambda:
                return RewriteLambdaExpression(node, stack);
            case ExpressionType.LeftShift:
                return RewriteBinaryExpression(node, stack);
            case ExpressionType.LessThan:
                return RewriteBinaryExpression(node, stack);
            case ExpressionType.LessThanOrEqual:
                return RewriteBinaryExpression(node, stack);
            case ExpressionType.ListInit:
                return RewriteListInitExpression(node, stack);
            case ExpressionType.MemberAccess:
                return RewriteMemberExpression(node, stack);
            case ExpressionType.MemberInit:
                return RewriteMemberInitExpression(node, stack);
            case ExpressionType.Modulo:
                return RewriteBinaryExpression(node, stack);
            case ExpressionType.Multiply:
                return RewriteBinaryExpression(node, stack);
            case ExpressionType.MultiplyChecked:
                return RewriteBinaryExpression(node, stack);
            case ExpressionType.Negate:
                return RewriteUnaryExpression(node, stack);
            case ExpressionType.UnaryPlus:
                return RewriteUnaryExpression(node, stack);
            case ExpressionType.NegateChecked:
                return RewriteUnaryExpression(node, stack);
            case ExpressionType.New:
                return RewriteNewExpression(node, stack);
            case ExpressionType.NewArrayInit:
                return RewriteNewArrayExpression(node, stack);
            case ExpressionType.NewArrayBounds:
                return RewriteNewArrayExpression(node, stack);
            case ExpressionType.Not:
                return RewriteUnaryExpression(node, stack);
            case ExpressionType.NotEqual:
                return RewriteBinaryExpression(node, stack);
            case ExpressionType.Or:
                return RewriteBinaryExpression(node, stack);
            case ExpressionType.OrElse:
                return RewriteLogicalBinaryExpression(node, stack);
            case ExpressionType.Power:
                return RewriteBinaryExpression(node, stack);
            case ExpressionType.RightShift:
                return RewriteBinaryExpression(node, stack);
            case ExpressionType.Subtract:
                return RewriteBinaryExpression(node, stack);
            case ExpressionType.SubtractChecked:
                return RewriteBinaryExpression(node, stack);
            case ExpressionType.TypeAs:
                return RewriteUnaryExpression(node, stack);
            case ExpressionType.TypeIs:
                return RewriteTypeBinaryExpression(node, stack);
            case ExpressionType.Assign:
                return RewriteAssignBinaryExpression(node, stack);
            case ExpressionType.Block:
                return RewriteBlockExpression(node, stack);
            case ExpressionType.Decrement:
                return RewriteUnaryExpression(node, stack);
            case ExpressionType.Dynamic:
                return RewriteDynamicExpression(node, stack);
            case ExpressionType.Extension:
                return RewriteExtensionExpression(node, stack);
            case ExpressionType.Goto:
                return RewriteGotoExpression(node, stack);
            case ExpressionType.Increment:
                return RewriteUnaryExpression(node, stack);
            case ExpressionType.Index:
                return RewriteIndexExpression(node, stack);
            case ExpressionType.Label:
                return RewriteLabelExpression(node, stack);
            case ExpressionType.Loop:
                return RewriteLoopExpression(node, stack);
            case ExpressionType.Switch:
                return RewriteSwitchExpression(node, stack);
            case ExpressionType.Throw:
                return RewriteThrowUnaryExpression(node, stack);
            case ExpressionType.Try:
                return RewriteTryExpression(node, stack);
            case ExpressionType.Unbox:
                return RewriteUnaryExpression(node, stack);
            case ExpressionType.AddAssign:
            case ExpressionType.AndAssign:
            case ExpressionType.DivideAssign:
            case ExpressionType.ExclusiveOrAssign:
            case ExpressionType.LeftShiftAssign:
            case ExpressionType.ModuloAssign:
            case ExpressionType.MultiplyAssign:
            case ExpressionType.OrAssign:
            case ExpressionType.PowerAssign:
            case ExpressionType.RightShiftAssign:
            case ExpressionType.SubtractAssign:
            case ExpressionType.AddAssignChecked:
            case ExpressionType.MultiplyAssignChecked:
            case ExpressionType.SubtractAssignChecked:
            case ExpressionType.PreIncrementAssign:
            case ExpressionType.PreDecrementAssign:
            case ExpressionType.PostIncrementAssign:
            case ExpressionType.PostDecrementAssign:
                return RewriteReducibleExpression(node, stack);
            case ExpressionType.TypeEqual:
                return RewriteTypeBinaryExpression(node, stack);
            case ExpressionType.OnesComplement:
                return RewriteUnaryExpression(node, stack);
            case ExpressionType.IsTrue:
                return RewriteUnaryExpression(node, stack);
            case ExpressionType.IsFalse:
                return RewriteUnaryExpression(node, stack);
            default:
                throw ContractUtils.Unreachable;
        }
    }

    private Result RewriteExpressionFreeTemps(
        Expression expression,
        Stack stack)
    {
        var mark = Mark();
        var result = RewriteExpression(expression, stack);
        Free(mark);
        return result;
    }

    private Result RewriteExtensionAssignment(
        BinaryExpression node,
        Stack stack)
    {
        node = Expression.Assign(node.Left.ReduceExtensions(), node.Right);
        var result = RewriteAssignBinaryExpression(node, stack);
        return new Result(result.Action | RewriteAction.Copy, result.Node);
    }

    private Result RewriteExtensionExpression(Expression expr, Stack stack)
    {
        var result = RewriteExpression(expr.ReduceExtensions(), stack);
        return new Result(result.Action | RewriteAction.Copy, result.Node);
    }

    private Result RewriteGotoExpression(Expression expr, Stack stack)
    {
        var gotoExpression = (GotoExpression)expr;
        var result = RewriteExpressionFreeTemps(gotoExpression.Value, Stack.Empty);
        var action = result.Action;
        if (stack != Stack.Empty)
        {
            action = RewriteAction.SpillStack;
        }

        if (action != RewriteAction.None)
        {
            expr = Expression.MakeGoto(gotoExpression.Kind, gotoExpression.Target, result.Node, gotoExpression.Type);
        }

        return new Result(action, expr);
    }

    private Result RewriteIndexAssignment(
        BinaryExpression node,
        Stack stack)
    {
        var left = (IndexExpression)node.Left;
        var childRewriter = new ChildRewriter(this, stack, 2 + left.Arguments.Count);
        childRewriter.Add(left.Object);
        childRewriter.Add(left.Arguments);
        childRewriter.Add(node.Right);
        if (childRewriter.Action == RewriteAction.SpillStack)
        {
            RequireNotRefInstance(left.Object);
        }

        if (childRewriter.Rewrite)
        {
            node = new AssignBinaryExpression(new IndexExpression(childRewriter[0], left.Indexer, childRewriter[1, -2]),
                childRewriter[-1]);
        }

        return childRewriter.Finish(node);
    }

    private Result RewriteIndexExpression(Expression expr, Stack stack)
    {
        var indexExpression = (IndexExpression)expr;
        var childRewriter = new ChildRewriter(this, stack, indexExpression.Arguments.Count + 1);
        childRewriter.Add(indexExpression.Object);
        childRewriter.Add(indexExpression.Arguments);
        if (childRewriter.Action == RewriteAction.SpillStack)
        {
            RequireNotRefInstance(indexExpression.Object);
        }

        if (childRewriter.Rewrite)
        {
            expr = new IndexExpression(childRewriter[0], indexExpression.Indexer, childRewriter[1, -1]);
        }

        return childRewriter.Finish(expr);
    }

    private Result RewriteInvocationExpression(Expression expr, Stack stack)
    {
        var expr1 = (InvocationExpression)expr;
        var lambdaOperand = expr1.LambdaOperand;
        if (lambdaOperand != null)
        {
            var childRewriter = new ChildRewriter(this, stack, expr1.Arguments.Count);
            childRewriter.Add(expr1.Arguments);
            if (childRewriter.Action == RewriteAction.SpillStack)
            {
                RequireNoRefArgs(Expression.GetInvokeMethod(expr1.Expression));
            }

            var spiller = new StackSpiller(stack);
            var lambda = lambdaOperand.Accept(spiller);
            if (childRewriter.Rewrite || spiller._lambdaRewrite != RewriteAction.None)
            {
                expr1 = new InvocationExpression(lambda, childRewriter[0, -1], expr1.Type);
            }

            var result = childRewriter.Finish(expr1);
            return new Result(result.Action | spiller._lambdaRewrite, result.Node);
        }

        var childRewriter1 = new ChildRewriter(this, stack, expr1.Arguments.Count + 1);
        childRewriter1.Add(expr1.Expression);
        childRewriter1.Add(expr1.Arguments);
        if (childRewriter1.Action == RewriteAction.SpillStack)
        {
            RequireNoRefArgs(Expression.GetInvokeMethod(expr1.Expression));
        }

        return childRewriter1.Finish(childRewriter1.Rewrite
            ? new InvocationExpression(childRewriter1[0], childRewriter1[1, -1], expr1.Type)
            : expr);
    }

    private Result RewriteLabelExpression(Expression expr, Stack stack)
    {
        var labelExpression = (LabelExpression)expr;
        var result = RewriteExpression(labelExpression.DefaultValue, stack);
        if (result.Action != RewriteAction.None)
        {
            expr = Expression.Label(labelExpression.Target, result.Node);
        }

        return new Result(result.Action, expr);
    }

    private static Result RewriteLambdaExpression(
        Expression expr,
        Stack stack)
    {
        var lambda = (LambdaExpression)expr;
        expr = AnalyzeLambda(lambda);
        return new Result(expr == lambda ? RewriteAction.None : RewriteAction.Copy, expr);
    }

    private Result RewriteListInitExpression(Expression expr, Stack stack)
    {
        var listInitExpression = (ListInitExpression)expr;
        var result1 = RewriteExpression(listInitExpression.NewExpression, stack);
        var node = result1.Node;
        var action = result1.Action;
        var initializers = listInitExpression.Initializers;
        var childRewriterArray = new ChildRewriter[initializers.Count];
        for (var index = 0; index < initializers.Count; ++index)
        {
            var elementInit = initializers[index];
            var childRewriter = new ChildRewriter(this, Stack.NonEmpty, elementInit.Arguments.Count);
            childRewriter.Add(elementInit.Arguments);
            action |= childRewriter.Action;
            childRewriterArray[index] = childRewriter;
        }

        switch (action)
        {
            case RewriteAction.None:
                return new Result(action, expr);
            case RewriteAction.Copy:
                var list = new ElementInit[initializers.Count];
                for (var index = 0; index < initializers.Count; ++index)
                {
                    var childRewriter = childRewriterArray[index];
                    list[index] = childRewriter.Action != RewriteAction.None
                        ? Expression.ElementInit(initializers[index].AddMethod, childRewriter[0, -1])
                        : initializers[index];
                }

                expr = Expression.ListInit((NewExpression)node, new TrueReadOnlyCollection<ElementInit>(list));
                goto case RewriteAction.None;
            case RewriteAction.SpillStack:
                RequireNotRefInstance(listInitExpression.NewExpression);
                var parameterExpression = MakeTemp(node.Type);
                var expressionArray = new Expression[initializers.Count + 2];
                expressionArray[0] = Expression.Assign(parameterExpression, node);
                for (var index = 0; index < initializers.Count; ++index)
                {
                    var childRewriter = childRewriterArray[index];
                    var result2 = childRewriter.Finish(Expression.Call(parameterExpression,
                        initializers[index].AddMethod, childRewriter[0, -1]));
                    expressionArray[index + 1] = result2.Node;
                }

                expressionArray[initializers.Count + 1] = parameterExpression;
                expr = MakeBlock(expressionArray);
                goto case RewriteAction.None;
            default:
                throw ContractUtils.Unreachable;
        }
    }

    private Result RewriteLogicalBinaryExpression(
        Expression expr,
        Stack stack)
    {
        var binaryExpression = (BinaryExpression)expr;
        var result1 = RewriteExpression(binaryExpression.Left, stack);
        var result2 = RewriteExpression(binaryExpression.Right, stack);
        var result3 = RewriteExpression(binaryExpression.Conversion, stack);
        var action = result1.Action | result2.Action | result3.Action;
        if (action != RewriteAction.None)
        {
            expr = BinaryExpression.Create(binaryExpression.NodeType, result1.Node, result2.Node, binaryExpression.Type,
                binaryExpression.Method, (LambdaExpression)result3.Node);
        }

        return new Result(action, expr);
    }

    private Result RewriteLoopExpression(Expression expr, Stack stack)
    {
        var loopExpression = (LoopExpression)expr;
        var result = RewriteExpression(loopExpression.Body, Stack.Empty);
        var action = result.Action;
        if (stack != Stack.Empty)
        {
            action = RewriteAction.SpillStack;
        }

        if (action != RewriteAction.None)
        {
            expr = new LoopExpression(result.Node, loopExpression.BreakLabel, loopExpression.ContinueLabel);
        }

        return new Result(action, expr);
    }

    private Result RewriteMemberAssignment(
        BinaryExpression node,
        Stack stack)
    {
        var left = (MemberExpression)node.Left;
        var childRewriter = new ChildRewriter(this, stack, 2);
        childRewriter.Add(left.Expression);
        childRewriter.Add(node.Right);
        if (childRewriter.Action == RewriteAction.SpillStack)
        {
            RequireNotRefInstance(left.Expression);
        }

        return childRewriter.Rewrite
            ? childRewriter.Finish(new AssignBinaryExpression(MemberExpression.Make(childRewriter[0], left.Member),
                childRewriter[1]))
            : new Result(RewriteAction.None, node);
    }

    private Result RewriteMemberExpression(Expression expr, Stack stack)
    {
        var memberExpression = (MemberExpression)expr;
        var result = RewriteExpression(memberExpression.Expression, stack);
        if (result.Action != RewriteAction.None)
        {
            if (result.Action == RewriteAction.SpillStack && memberExpression.Member.MemberType == MemberTypes.Property)
            {
                RequireNotRefInstance(memberExpression.Expression);
            }

            expr = MemberExpression.Make(result.Node, memberExpression.Member);
        }

        return new Result(result.Action, expr);
    }

    private Result RewriteMemberInitExpression(Expression expr, Stack stack)
    {
        var memberInitExpression = (MemberInitExpression)expr;
        var result = RewriteExpression(memberInitExpression.NewExpression, stack);
        var node = result.Node;
        var action = result.Action;
        var bindings = memberInitExpression.Bindings;
        var bindingRewriterArray = new BindingRewriter[bindings.Count];
        for (var index = 0; index < bindings.Count; ++index)
        {
            var bindingRewriter = BindingRewriter.Create(bindings[index], this, Stack.NonEmpty);
            bindingRewriterArray[index] = bindingRewriter;
            action |= bindingRewriter.Action;
        }

        switch (action)
        {
            case RewriteAction.None:
                return new Result(action, expr);
            case RewriteAction.Copy:
                var list = new MemberBinding[bindings.Count];
                for (var index = 0; index < bindings.Count; ++index)
                {
                    list[index] = bindingRewriterArray[index].AsBinding();
                }

                expr = Expression.MemberInit((NewExpression)node, new TrueReadOnlyCollection<MemberBinding>(list));
                goto case RewriteAction.None;
            case RewriteAction.SpillStack:
                RequireNotRefInstance(memberInitExpression.NewExpression);
                var parameterExpression = MakeTemp(node.Type);
                var expressionArray = new Expression[bindings.Count + 2];
                expressionArray[0] = Expression.Assign(parameterExpression, node);
                for (var index = 0; index < bindings.Count; ++index)
                {
                    var expression = bindingRewriterArray[index].AsExpression(parameterExpression);
                    expressionArray[index + 1] = expression;
                }

                expressionArray[bindings.Count + 1] = parameterExpression;
                expr = MakeBlock(expressionArray);
                goto case RewriteAction.None;
            default:
                throw ContractUtils.Unreachable;
        }
    }

    private Result RewriteMethodCallExpression(Expression expr, Stack stack)
    {
        var expressions = (MethodCallExpression)expr;
        var childRewriter = new ChildRewriter(this, stack, expressions.Arguments.Count + 1);
        childRewriter.Add(expressions.Object);
        childRewriter.AddArguments(expressions);
        if (childRewriter.Action == RewriteAction.SpillStack)
        {
            RequireNotRefInstance(expressions.Object);
            RequireNoRefArgs(expressions.Method);
        }

        return childRewriter.Finish(childRewriter.Rewrite
            ? expressions.Rewrite(childRewriter[0], childRewriter[1, -1])
            : expr);
    }

    private Result RewriteNewArrayExpression(Expression expr, Stack stack)
    {
        var newArrayExpression = (NewArrayExpression)expr;
        if (newArrayExpression.NodeType == ExpressionType.NewArrayInit)
        {
            stack = Stack.NonEmpty;
        }

        var childRewriter = new ChildRewriter(this, stack, newArrayExpression.Expressions.Count);
        childRewriter.Add(newArrayExpression.Expressions);
        if (childRewriter.Rewrite)
        {
            var elementType = newArrayExpression.Type.GetElementType();
            expr = newArrayExpression.NodeType != ExpressionType.NewArrayInit
                ? Expression.NewArrayBounds(elementType, childRewriter[0, -1])
                : (Expression)Expression.NewArrayInit(elementType, childRewriter[0, -1]);
        }

        return childRewriter.Finish(expr);
    }

    private Result RewriteNewExpression(Expression expr, Stack stack)
    {
        var expressions = (NewExpression)expr;
        var childRewriter = new ChildRewriter(this, stack, expressions.Arguments.Count);
        childRewriter.AddArguments(expressions);
        if (childRewriter.Action == RewriteAction.SpillStack)
        {
            RequireNoRefArgs(expressions.Constructor);
        }

        return childRewriter.Finish(childRewriter.Rewrite
            ? new NewExpression(expressions.Constructor, childRewriter[0, -1], expressions.Members)
            : expr);
    }

    private Result RewriteReducibleExpression(Expression expr, Stack stack)
    {
        var result = RewriteExpression(expr.Reduce(), stack);
        return new Result(result.Action | RewriteAction.Copy, result.Node);
    }

    private Result RewriteSwitchExpression(Expression expr, Stack stack)
    {
        var switchExpression = (SwitchExpression)expr;
        var result1 = RewriteExpressionFreeTemps(switchExpression.SwitchValue, stack);
        var action1 = result1.Action;
        var readOnlyCollection1 = switchExpression.Cases;
        var list1 = (SwitchCase[])null;
        for (var index1 = 0; index1 < readOnlyCollection1.Count; ++index1)
        {
            var switchCase = readOnlyCollection1[index1];
            var list2 = (Expression[])null;
            var readOnlyCollection2 = switchCase.TestValues;
            for (var index2 = 0; index2 < readOnlyCollection2.Count; ++index2)
            {
                var result2 = RewriteExpression(readOnlyCollection2[index2], stack);
                action1 |= result2.Action;
                if (list2 == null && result2.Action != RewriteAction.None)
                {
                    list2 = Clone(readOnlyCollection2, index2);
                }

                if (list2 != null)
                {
                    list2[index2] = result2.Node;
                }
            }

            var result3 = RewriteExpression(switchCase.Body, stack);
            action1 |= result3.Action;
            if (result3.Action != RewriteAction.None || list2 != null)
            {
                if (list2 != null)
                {
                    readOnlyCollection2 = new ReadOnlyCollection<Expression>(list2);
                }

                switchCase = new SwitchCase(result3.Node, readOnlyCollection2);
                if (list1 == null)
                {
                    list1 = Clone(readOnlyCollection1, index1);
                }
            }

            if (list1 != null)
            {
                list1[index1] = switchCase;
            }
        }

        var result4 = RewriteExpression(switchExpression.DefaultBody, stack);
        var action2 = action1 | result4.Action;
        if (action2 != RewriteAction.None)
        {
            if (list1 != null)
            {
                readOnlyCollection1 = new ReadOnlyCollection<SwitchCase>(list1);
            }

            expr = new SwitchExpression(switchExpression.Type, result1.Node, result4.Node, switchExpression.Comparison,
                readOnlyCollection1);
        }

        return new Result(action2, expr);
    }

    private Result RewriteThrowUnaryExpression(Expression expr, Stack stack)
    {
        var unaryExpression = (UnaryExpression)expr;
        var result = RewriteExpressionFreeTemps(unaryExpression.Operand, Stack.Empty);
        var action = result.Action;
        if (stack != Stack.Empty)
        {
            action = RewriteAction.SpillStack;
        }

        if (action != RewriteAction.None)
        {
            expr = Expression.Throw(result.Node, unaryExpression.Type);
        }

        return new Result(action, expr);
    }

    private Result RewriteTryExpression(Expression expr, Stack stack)
    {
        var tryExpression = (TryExpression)expr;
        var result1 = RewriteExpression(tryExpression.Body, Stack.Empty);
        var readOnlyCollection = tryExpression.Handlers;
        var list = (CatchBlock[])null;
        var action1 = result1.Action;
        if (readOnlyCollection != null)
        {
            for (var index = 0; index < readOnlyCollection.Count; ++index)
            {
                var action2 = result1.Action;
                var catchBlock = readOnlyCollection[index];
                var filter = catchBlock.Filter;
                if (catchBlock.Filter != null)
                {
                    var result2 = RewriteExpression(catchBlock.Filter, Stack.Empty);
                    action1 |= result2.Action;
                    action2 |= result2.Action;
                    filter = result2.Node;
                }

                var result3 = RewriteExpression(catchBlock.Body, Stack.Empty);
                action1 |= result3.Action;
                if ((action2 | result3.Action) != RewriteAction.None)
                {
                    catchBlock = Expression.MakeCatchBlock(catchBlock.Test, catchBlock.Variable, result3.Node, filter);
                    if (list == null)
                    {
                        list = Clone(readOnlyCollection, index);
                    }
                }

                if (list != null)
                {
                    list[index] = catchBlock;
                }
            }
        }

        var result4 = RewriteExpression(tryExpression.Fault, Stack.Empty);
        var rewriteAction = action1 | result4.Action;
        var result5 = RewriteExpression(tryExpression.Finally, Stack.Empty);
        var action3 = rewriteAction | result5.Action;
        if (stack != Stack.Empty)
        {
            action3 = RewriteAction.SpillStack;
        }

        if (action3 != RewriteAction.None)
        {
            if (list != null)
            {
                readOnlyCollection = new ReadOnlyCollection<CatchBlock>(list);
            }

            expr = new TryExpression(tryExpression.Type, result1.Node, result5.Node, result4.Node, readOnlyCollection);
        }

        return new Result(action3, expr);
    }

    private Result RewriteTypeBinaryExpression(Expression expr, Stack stack)
    {
        var binaryExpression = (TypeBinaryExpression)expr;
        var result = RewriteExpression(binaryExpression.Expression, stack);
        if (result.Action != RewriteAction.None)
        {
            expr = binaryExpression.NodeType != ExpressionType.TypeIs
                ? Expression.TypeEqual(result.Node, binaryExpression.TypeOperand)
                : (Expression)Expression.TypeIs(result.Node, binaryExpression.TypeOperand);
        }

        return new Result(result.Action, expr);
    }

    private Result RewriteUnaryExpression(Expression expr, Stack stack)
    {
        var unaryExpression = (UnaryExpression)expr;
        var result = RewriteExpression(unaryExpression.Operand, stack);
        if (result.Action == RewriteAction.SpillStack)
        {
            RequireNoRefArgs(unaryExpression.Method);
        }

        if (result.Action != RewriteAction.None)
        {
            expr = new UnaryExpression(unaryExpression.NodeType, result.Node, unaryExpression.Type,
                unaryExpression.Method);
        }

        return new Result(result.Action, expr);
    }

    private Result RewriteVariableAssignment(
        BinaryExpression node,
        Stack stack)
    {
        var result = RewriteExpression(node.Right, stack);
        if (result.Action != RewriteAction.None)
        {
            node = Expression.Assign(node.Left, result.Node);
        }

        return new Result(result.Action, node);
    }

    private ParameterExpression ToTemp(Expression expression, out Expression save)
    {
        var left = MakeTemp(expression.Type);
        save = Expression.Assign(left, expression);
        return left;
    }

    [Conditional("DEBUG")]
    private static void VerifyRewrite(Result result, Expression node)
    {
    }

    [Conditional("DEBUG")]
    private void VerifyTemps()
    {
    }

    private abstract class BindingRewriter
    {
        protected readonly MemberBinding _binding;
        protected readonly StackSpiller _spiller;
        protected RewriteAction _action;

        internal BindingRewriter(MemberBinding binding, StackSpiller spiller)
        {
            _binding = binding;
            _spiller = spiller;
        }

        internal RewriteAction Action => _action;

        internal abstract MemberBinding AsBinding();

        internal abstract Expression AsExpression(Expression target);

        internal static BindingRewriter Create(
            MemberBinding binding,
            StackSpiller spiller,
            Stack stack)
        {
            switch (binding.BindingType)
            {
                case MemberBindingType.Assignment:
                    return new MemberAssignmentRewriter((MemberAssignment)binding, spiller, stack);
                case MemberBindingType.MemberBinding:
                    return new MemberMemberBindingRewriter((MemberMemberBinding)binding, spiller, stack);
                case MemberBindingType.ListBinding:
                    return new ListBindingRewriter((MemberListBinding)binding, spiller, stack);
                default:
                    throw Error.UnhandledBinding();
            }
        }
    }

    private class MemberMemberBindingRewriter : BindingRewriter
    {
        private readonly BindingRewriter[] _bindingRewriters;
        private readonly ReadOnlyCollection<MemberBinding> _bindings;

        internal MemberMemberBindingRewriter(
            MemberMemberBinding binding,
            StackSpiller spiller,
            Stack stack)
            : base(binding, spiller)
        {
            _bindings = binding.Bindings;
            _bindingRewriters = new BindingRewriter[_bindings.Count];
            for (var index = 0; index < _bindings.Count; ++index)
            {
                var bindingRewriter = Create(_bindings[index], spiller, stack);
                _action |= bindingRewriter.Action;
                _bindingRewriters[index] = bindingRewriter;
            }
        }

        internal override MemberBinding AsBinding()
        {
            switch (_action)
            {
                case RewriteAction.None:
                    return _binding;
                case RewriteAction.Copy:
                    var list = new MemberBinding[_bindings.Count];
                    for (var index = 0; index < _bindings.Count; ++index)
                    {
                        list[index] = _bindingRewriters[index].AsBinding();
                    }

                    return Expression.MemberBind(_binding.Member, new TrueReadOnlyCollection<MemberBinding>(list));
                default:
                    throw ContractUtils.Unreachable;
            }
        }

        internal override Expression AsExpression(Expression target)
        {
            if (target.Type.IsValueType && _binding.Member as PropertyInfo != null)
            {
                throw Error.CannotAutoInitializeValueTypeMemberThroughProperty(_binding.Member);
            }

            RequireNotRefInstance(target);
            var right = Expression.MakeMemberAccess(target, _binding.Member);
            var parameterExpression = _spiller.MakeTemp(right.Type);
            var expressionArray = new Expression[_bindings.Count + 2];
            expressionArray[0] = Expression.Assign(parameterExpression, right);
            for (var index = 0; index < _bindings.Count; ++index)
            {
                var bindingRewriter = _bindingRewriters[index];
                expressionArray[index + 1] = bindingRewriter.AsExpression(parameterExpression);
            }

            if (parameterExpression.Type.IsValueType)
            {
                expressionArray[_bindings.Count + 1] = Expression.Block(typeof(void),
                    Expression.Assign(Expression.MakeMemberAccess(target, _binding.Member), parameterExpression));
            }
            else
            {
                expressionArray[_bindings.Count + 1] = Expression.Empty();
            }

            return MakeBlock(expressionArray);
        }
    }

    private class ListBindingRewriter : BindingRewriter
    {
        private readonly ChildRewriter[] _childRewriters;
        private readonly ReadOnlyCollection<ElementInit> _inits;

        internal ListBindingRewriter(
            MemberListBinding binding,
            StackSpiller spiller,
            Stack stack)
            : base(binding, spiller)
        {
            _inits = binding.Initializers;
            _childRewriters = new ChildRewriter[_inits.Count];
            for (var index = 0; index < _inits.Count; ++index)
            {
                var init = _inits[index];
                var childRewriter = new ChildRewriter(spiller, stack, init.Arguments.Count);
                childRewriter.Add(init.Arguments);
                _action |= childRewriter.Action;
                _childRewriters[index] = childRewriter;
            }
        }

        internal override MemberBinding AsBinding()
        {
            switch (_action)
            {
                case RewriteAction.None:
                    return _binding;
                case RewriteAction.Copy:
                    var list = new ElementInit[_inits.Count];
                    for (var index = 0; index < _inits.Count; ++index)
                    {
                        var childRewriter = _childRewriters[index];
                        list[index] = childRewriter.Action != RewriteAction.None
                            ? Expression.ElementInit(_inits[index].AddMethod, childRewriter[0, -1])
                            : _inits[index];
                    }

                    return Expression.ListBind(_binding.Member, new TrueReadOnlyCollection<ElementInit>(list));
                default:
                    throw ContractUtils.Unreachable;
            }
        }

        internal override Expression AsExpression(Expression target)
        {
            if (target.Type.IsValueType && _binding.Member as PropertyInfo != null)
            {
                throw Error.CannotAutoInitializeValueTypeElementThroughProperty(_binding.Member);
            }

            RequireNotRefInstance(target);
            var right = Expression.MakeMemberAccess(target, _binding.Member);
            var parameterExpression = _spiller.MakeTemp(right.Type);
            var expressionArray = new Expression[_inits.Count + 2];
            expressionArray[0] = Expression.Assign(parameterExpression, right);
            for (var index = 0; index < _inits.Count; ++index)
            {
                var childRewriter = _childRewriters[index];
                var result = childRewriter.Finish(Expression.Call(parameterExpression, _inits[index].AddMethod,
                    childRewriter[0, -1]));
                expressionArray[index + 1] = result.Node;
            }

            if (parameterExpression.Type.IsValueType)
            {
                expressionArray[_inits.Count + 1] = Expression.Block(typeof(void),
                    Expression.Assign(Expression.MakeMemberAccess(target, _binding.Member), parameterExpression));
            }
            else
            {
                expressionArray[_inits.Count + 1] = Expression.Empty();
            }

            return MakeBlock(expressionArray);
        }
    }

    private class MemberAssignmentRewriter : BindingRewriter
    {
        private readonly Expression _rhs;

        internal MemberAssignmentRewriter(
            MemberAssignment binding,
            StackSpiller spiller,
            Stack stack)
            : base(binding, spiller)
        {
            var result = spiller.RewriteExpression(binding.Expression, stack);
            _action = result.Action;
            _rhs = result.Node;
        }

        internal override MemberBinding AsBinding()
        {
            switch (_action)
            {
                case RewriteAction.None:
                    return _binding;
                case RewriteAction.Copy:
                    return Expression.Bind(_binding.Member, _rhs);
                default:
                    throw ContractUtils.Unreachable;
            }
        }

        internal override Expression AsExpression(Expression target)
        {
            RequireNotRefInstance(target);
            var left = Expression.MakeMemberAccess(target, _binding.Member);
            var parameterExpression = _spiller.MakeTemp(left.Type);
            return MakeBlock(Expression.Assign(parameterExpression, _rhs), Expression.Assign(left, parameterExpression),
                Expression.Empty());
        }
    }

    private enum Stack
    {
        Empty,
        NonEmpty
    }

    [Flags]
    private enum RewriteAction
    {
        None = 0,
        Copy = 1,
        SpillStack = 3
    }

    private struct Result
    {
        internal readonly RewriteAction Action;
        internal readonly Expression Node;

        internal Result(RewriteAction action, Expression node)
        {
            Action = action;
            Node = node;
        }
    }

    private class TempMaker
    {
        private List<ParameterExpression> _freeTemps;
        private int _temp;
        private Stack<ParameterExpression> _usedTemps;

        internal List<ParameterExpression> Temps { get; } = new();

        internal void Free(int mark)
        {
            if (_usedTemps == null)
            {
                return;
            }

            while (mark < _usedTemps.Count)
            {
                FreeTemp(_usedTemps.Pop());
            }
        }

        internal int Mark()
        {
            return _usedTemps == null ? 0 : _usedTemps.Count;
        }

        internal ParameterExpression Temp(Type type)
        {
            if (_freeTemps != null)
            {
                for (var index = _freeTemps.Count - 1; index >= 0; --index)
                {
                    var freeTemp = _freeTemps[index];
                    if (freeTemp.Type == type)
                    {
                        _freeTemps.RemoveAt(index);
                        return UseTemp(freeTemp);
                    }
                }
            }

            var temp = Expression.Variable(type, "$temp$" + _temp++);
            Temps.Add(temp);
            return UseTemp(temp);
        }

        [Conditional("DEBUG")]
        internal void VerifyTemps()
        {
        }

        private void FreeTemp(ParameterExpression temp)
        {
            if (_freeTemps == null)
            {
                _freeTemps = new List<ParameterExpression>();
            }

            _freeTemps.Add(temp);
        }

        private ParameterExpression UseTemp(ParameterExpression temp)
        {
            if (_usedTemps == null)
            {
                _usedTemps = new Stack<ParameterExpression>();
            }

            _usedTemps.Push(temp);
            return temp;
        }
    }

    private class ChildRewriter
    {
        private readonly Expression[] _expressions;
        private readonly StackSpiller _self;
        private List<Expression> _comma;
        private bool _done;
        private int _expressionsCount;
        private Stack _stack;

        internal ChildRewriter(StackSpiller self, Stack stack, int count)
        {
            _self = self;
            _stack = stack;
            _expressions = new Expression[count];
        }

        internal bool Rewrite => Action != 0;

        internal RewriteAction Action { get; private set; }

        internal Expression this[int index]
        {
            get
            {
                EnsureDone();
                if (index < 0)
                {
                    index += _expressions.Length;
                }

                return _expressions[index];
            }
        }

        internal Expression[] this[int first, int last]
        {
            get
            {
                EnsureDone();
                if (last < 0)
                {
                    last += _expressions.Length;
                }

                var length = last - first + 1;
                ContractUtils.RequiresArrayRange(_expressions, first, length, nameof(first), nameof(last));
                if (length == _expressions.Length)
                {
                    return _expressions;
                }

                var destinationArray = new Expression[length];
                Array.Copy(_expressions, first, destinationArray, 0, length);
                return destinationArray;
            }
        }

        internal void Add(Expression node)
        {
            if (node == null)
            {
                _expressions[_expressionsCount++] = null;
            }
            else
            {
                var result = _self.RewriteExpression(node, _stack);
                Action |= result.Action;
                _stack = Stack.NonEmpty;
                _expressions[_expressionsCount++] = result.Node;
            }
        }

        internal void Add(IList<Expression> expressions)
        {
            var index = 0;
            for (var count = expressions.Count; index < count; ++index)
            {
                Add(expressions[index]);
            }
        }

        internal void AddArguments(IArgumentProvider expressions)
        {
            var index = 0;
            for (var argumentCount = expressions.ArgumentCount; index < argumentCount; ++index)
            {
                Add(expressions.GetArgument(index));
            }
        }

        internal Result Finish(Expression expr)
        {
            EnsureDone();
            if (Action == RewriteAction.SpillStack)
            {
                _comma.Add(expr);
                expr = MakeBlock(_comma);
            }

            return new Result(Action, expr);
        }

        private void EnsureDone()
        {
            if (_done)
            {
                return;
            }

            _done = true;
            if (Action != RewriteAction.SpillStack)
            {
                return;
            }

            var expressions = _expressions;
            var length = expressions.Length;
            var expressionList = new List<Expression>(length + 1);
            for (var index = 0; index < length; ++index)
            {
                if (expressions[index] != null)
                {
                    Expression save;
                    expressions[index] = _self.ToTemp(expressions[index], out save);
                    expressionList.Add(save);
                }
            }

            expressionList.Capacity = expressionList.Count + 1;
            _comma = expressionList;
        }
    }
}