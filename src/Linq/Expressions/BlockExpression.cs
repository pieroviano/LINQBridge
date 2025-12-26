using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Linq.Expressions;

[__DynamicallyInvokable]
public class BlockExpression : Expression
{
    internal virtual int ExpressionCount => throw ContractUtils.Unreachable;

    [__DynamicallyInvokable]
    public ReadOnlyCollection<Expression> Expressions
    {
        [__DynamicallyInvokable]
        get => GetOrMakeExpressions();
    }

    internal static ReadOnlyCollection<Expression> ReturnReadOnlyExpressions(BlockExpression provider, ref object collection)
    {
        var expression = collection as Expression;
        if (expression != null)
        {
            Net20Interlocked.CompareExchange(ref collection, new ReadOnlyCollection<Expression>(new BlockExpressionList(provider, expression)), expression);
        }
        return (ReadOnlyCollection<Expression>)collection;
    }


    [__DynamicallyInvokable]
    public sealed override ExpressionType NodeType
    {
        [__DynamicallyInvokable]
        get => ExpressionType.Block;
    }

    [__DynamicallyInvokable]
    public Expression Result
    {
        [__DynamicallyInvokable]
        get => GetExpression(ExpressionCount - 1);
    }

    [__DynamicallyInvokable]
    public override Type Type
    {
        [__DynamicallyInvokable]
        get => GetExpression(ExpressionCount - 1).Type;
    }

    internal virtual int VariableCount => 0;

    [__DynamicallyInvokable]
    public ReadOnlyCollection<ParameterExpression> Variables
    {
        [__DynamicallyInvokable]
        get => GetOrMakeVariables();
    }

    internal virtual Expression GetExpression(int index)
    {
        throw ContractUtils.Unreachable;
    }

    internal virtual ReadOnlyCollection<Expression> GetOrMakeExpressions()
    {
        throw ContractUtils.Unreachable;
    }

    internal virtual ReadOnlyCollection<ParameterExpression> GetOrMakeVariables()
    {
        return EmptyReadOnlyCollection<ParameterExpression>.Instance;
    }

    internal virtual ParameterExpression GetVariable(int index)
    {
        throw ContractUtils.Unreachable;
    }

    internal virtual BlockExpression Rewrite(ReadOnlyCollection<ParameterExpression> variables, Expression[] args)
    {
        throw ContractUtils.Unreachable;
    }

    [__DynamicallyInvokable]
    public BlockExpression Update(IEnumerable<ParameterExpression> variables, IEnumerable<Expression> expressions)
    {
        if (variables == Variables && expressions == Expressions)
        {
            return this;
        }
        return Block(Type, variables, expressions);
    }

    public BlockExpression(ExpressionType nodeType, Type type) : base(nodeType, type)
    {
    }

    public BlockExpression() : base()
    {
    }
}