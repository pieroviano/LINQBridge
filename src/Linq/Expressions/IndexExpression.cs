using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;

namespace System.Linq.Expressions;

[__DynamicallyInvokable]
[DebuggerTypeProxy(typeof(IndexExpressionProxy))]
public sealed class IndexExpression : Expression, IArgumentProvider
{
    private readonly Expression _instance;

    private readonly PropertyInfo _indexer;

    private IList<Expression> _arguments;

    [__DynamicallyInvokable]
    public ReadOnlyCollection<Expression> Arguments
    {
        [__DynamicallyInvokable]
        get => ReturnReadOnly<Expression>(ref _arguments);
    }

    [__DynamicallyInvokable]
    public PropertyInfo Indexer
    {
        [__DynamicallyInvokable]
        get => _indexer;
    }

    [__DynamicallyInvokable]
    public sealed override ExpressionType NodeType
    {
        [__DynamicallyInvokable]
        get => ExpressionType.Index;
    }

    [__DynamicallyInvokable]
    public Expression Object
    {
        [__DynamicallyInvokable]
        get => _instance;
    }

    [__DynamicallyInvokable]
    int IArgumentProvider.ArgumentCount
    {
        [__DynamicallyInvokable]
        get => _arguments.Count;
    }

    [__DynamicallyInvokable]
    public sealed override Type Type
    {
        [__DynamicallyInvokable]
        get
        {
            if (_indexer != null)
            {
                return _indexer.PropertyType;
            }
            return _instance.Type.GetElementType();
        }
    }

    public bool CanReduce { get; set; }
    public string DebugView { get; set; }

    [__DynamicallyInvokable]
    Expression IArgumentProvider.GetArgument(int index)
    {
        return _arguments[index];
    }

    public IndexExpression(ExpressionType nodeType, Type type) : base(nodeType, type)
    {

    }
}