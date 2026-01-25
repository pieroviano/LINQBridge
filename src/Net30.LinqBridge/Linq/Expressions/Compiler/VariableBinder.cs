#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic.Utils;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Compiler;

internal sealed class VariableBinder : ExpressionVisitor
{
    private readonly Stack<BoundConstants> _constants = new();
    private readonly StackGuard _guard = new();
    private readonly Stack<CompilerScope> _scopes = new();
    private readonly AnalyzedTree _tree = new();
    private bool _inQuote;

    private VariableBinder()
    {
    }

    private string CurrentLambdaName
    {
        get
        {
            foreach (var scope in _scopes)
            {
                if (scope.Node is LambdaExpression node)
                {
                    return node.Name;
                }
            }

            throw ContractUtils.Unreachable;
        }
    }

    public override Expression Visit(Expression node)
    {
        return !_guard.TryEnterOnCurrentStack()
            ? _guard.RunOnEmptyStack((@this, e) => @this.Visit(e), this, node)
            : base.Visit(node);
    }

    public override CatchBlock VisitCatchBlock(CatchBlock node)
    {
        if (node.Variable == null)
        {
            Visit(node.Body);
            return node;
        }

        _scopes.Push(_tree.Scopes[node] = new CompilerScope(node, false));
        Visit(node.Body);
        _scopes.Pop();
        return node;
    }

    protected internal override Expression VisitBlock(BlockExpression node)
    {
        if (node.Variables.Count == 0)
        {
            Visit(node.Expressions);
            return node;
        }

        _scopes.Push(_tree.Scopes[node] = new CompilerScope(node, false));
        Visit(MergeScopes(node));
        _scopes.Pop();
        return node;
    }

    protected internal override Expression VisitConstant(ConstantExpression node)
    {
        if (_inQuote)
        {
            return node;
        }

        if (ILGen.CanEmitConstant(node.Value, node.Type))
        {
            return node;
        }

        _constants.Peek().AddReference(node.Value, node.Type);
        return node;
    }

    protected internal override Expression VisitInvocation(InvocationExpression node)
    {
        var lambdaOperand = node.LambdaOperand;
        if (lambdaOperand == null)
        {
            return base.VisitInvocation(node);
        }

        _scopes.Push(_tree.Scopes[lambdaOperand] = new CompilerScope(lambdaOperand, false));
        Visit(MergeScopes(lambdaOperand));
        _scopes.Pop();
        Visit(node.Arguments);
        return node;
    }

    protected internal override Expression VisitLambda<T>(Expression<T> node)
    {
        _scopes.Push(_tree.Scopes[node] = new CompilerScope(node, true));
        _constants.Push(_tree.Constants[node] = new BoundConstants());
        Visit(MergeScopes(node));
        _constants.Pop();
        _scopes.Pop();
        return node;
    }

    protected internal override Expression VisitParameter(ParameterExpression node)
    {
        Reference(node, VariableStorageKind.Local);
        var compilerScope = (CompilerScope)null;
        foreach (var scope in _scopes)
        {
            if (scope.IsMethod || scope.Definitions.ContainsKey(node))
            {
                compilerScope = scope;
                break;
            }
        }

        if (compilerScope.ReferenceCount == null)
        {
            compilerScope.ReferenceCount = new Dictionary<ParameterExpression, int>();
        }

        Helpers.IncrementCount(node, compilerScope.ReferenceCount);
        return node;
    }

    protected internal override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
    {
        foreach (var variable in node.Variables)
        {
            Reference(variable, VariableStorageKind.Hoisted);
        }

        return node;
    }

    protected internal override Expression VisitUnary(UnaryExpression node)
    {
        if (node.NodeType == ExpressionType.Quote)
        {
            var inQuote = _inQuote;
            _inQuote = true;
            Visit(node.Operand);
            _inQuote = inQuote;
        }
        else
        {
            Visit(node.Operand);
        }

        return node;
    }

    internal static AnalyzedTree Bind(LambdaExpression lambda)
    {
        var variableBinder = new VariableBinder();
        variableBinder.Visit(lambda);
        return variableBinder._tree;
    }

    private ReadOnlyCollection<Expression> MergeScopes(Expression node)
    {
        ReadOnlyCollection<Expression> readOnlyCollection;
        if (node is LambdaExpression lambdaExpression)
        {
            readOnlyCollection = new ReadOnlyCollection<Expression>(new Expression[1]
            {
                lambdaExpression.Body
            });
        }
        else
        {
            readOnlyCollection = ((BlockExpression)node).Expressions;
        }

        var compilerScope = _scopes.Peek();
        BlockExpression blockExpression;
        for (;
             readOnlyCollection.Count == 1 && readOnlyCollection[0].NodeType == ExpressionType.Block;
             readOnlyCollection = blockExpression.Expressions)
        {
            blockExpression = (BlockExpression)readOnlyCollection[0];
            if (blockExpression.Variables.Count > 0)
            {
                foreach (var variable in blockExpression.Variables)
                {
                    if (compilerScope.Definitions.ContainsKey(variable))
                    {
                        return readOnlyCollection;
                    }
                }

                if (compilerScope.MergedScopes == null)
                {
                    compilerScope.MergedScopes = new Set<object>(ReferenceEqualityComparer<object>.Instance);
                }

                compilerScope.MergedScopes.Add(blockExpression);
                foreach (var variable in blockExpression.Variables)
                {
                    compilerScope.Definitions.Add(variable, VariableStorageKind.Local);
                }
            }

            node = blockExpression;
        }

        return readOnlyCollection;
    }

    private void Reference(ParameterExpression node, VariableStorageKind storage)
    {
        var compilerScope = (CompilerScope)null;
        foreach (var scope in _scopes)
        {
            if (scope.Definitions.ContainsKey(node))
            {
                compilerScope = scope;
                break;
            }

            scope.NeedsClosure = true;
            if (scope.IsMethod)
            {
                storage = VariableStorageKind.Hoisted;
            }
        }

        if (compilerScope == null)
        {
            throw Error.UndefinedVariable(node.Name, node.Type, CurrentLambdaName);
        }

        if (storage != VariableStorageKind.Hoisted)
        {
            return;
        }

        if (node.IsByRef)
        {
            throw Error.CannotCloseOverByRef(node.Name, CurrentLambdaName);
        }

        compilerScope.Definitions[node] = VariableStorageKind.Hoisted;
    }
}