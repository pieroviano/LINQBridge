#nullable disable
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace System.Dynamic;

/// <summary>
///     Represents a set of binding restrictions on the <see cref="T:System.Dynamic.DynamicMetaObject" /> under which
///     the dynamic binding is valid.
/// </summary>
[DebuggerTypeProxy(typeof(BindingRestrictionsProxy))]
[DebuggerDisplay("{DebugView}")]
public abstract class BindingRestrictions
{
    private const int TypeRestrictionHash = 268435456 /*0x10000000*/;
    private const int InstanceRestrictionHash = 536870912 /*0x20000000*/;
    private const int CustomRestrictionHash = 1073741824 /*0x40000000*/;

    /// <summary>Represents an empty set of binding restrictions. This field is read only.</summary>
    public static readonly BindingRestrictions Empty = new CustomRestriction(Expression.Constant(true));

    private BindingRestrictions()
    {
    }

    private string DebugView => ToExpression().ToString();

    /// <summary>
    ///     Combines binding restrictions from the list of <see cref="T:System.Dynamic.DynamicMetaObject" /> instances
    ///     into one set of restrictions.
    /// </summary>
    /// <returns>The new set of binding restrictions.</returns>
    /// <param name="contributingObjects">
    ///     The list of <see cref="T:System.Dynamic.DynamicMetaObject" /> instances from which to
    ///     combine restrictions.
    /// </param>
    public static BindingRestrictions Combine(IList<DynamicMetaObject> contributingObjects)
    {
        var bindingRestrictions = Empty;
        if (contributingObjects != null)
        {
            foreach (var contributingObject in contributingObjects)
            {
                if (contributingObject != null)
                {
                    bindingRestrictions = bindingRestrictions.Merge(contributingObject.Restrictions);
                }
            }
        }

        return bindingRestrictions;
    }

    /// <summary>Creates the binding restriction that checks the expression for arbitrary immutable properties.</summary>
    /// <returns>The new binding restrictions.</returns>
    /// <param name="expression">The expression representing the restrictions.</param>
    public static BindingRestrictions GetExpressionRestriction(Expression expression)
    {
        ContractUtils.RequiresNotNull(expression, nameof(expression));
        ContractUtils.Requires(expression.Type == typeof(bool), nameof(expression));
        return new CustomRestriction(expression);
    }

    /// <summary>Creates the binding restriction that checks the expression for object instance identity.</summary>
    /// <returns>The new binding restrictions.</returns>
    /// <param name="expression">The expression to test.</param>
    /// <param name="instance">The exact object instance to test.</param>
    public static BindingRestrictions GetInstanceRestriction(Expression expression, object instance)
    {
        ContractUtils.RequiresNotNull(expression, nameof(expression));
        return new InstanceRestriction(expression, instance);
    }

    /// <summary>Creates the binding restriction that check the expression for runtime type identity.</summary>
    /// <returns>The new binding restrictions.</returns>
    /// <param name="expression">The expression to test.</param>
    /// <param name="type">The exact type to test.</param>
    public static BindingRestrictions GetTypeRestriction(Expression expression, Type type)
    {
        ContractUtils.RequiresNotNull(expression, nameof(expression));
        ContractUtils.RequiresNotNull(type, nameof(type));
        return new TypeRestriction(expression, type);
    }

    /// <summary>Merges the set of binding restrictions with the current binding restrictions.</summary>
    /// <returns>The new set of binding restrictions.</returns>
    /// <param name="restrictions">The set of restrictions with which to merge the current binding restrictions.</param>
    public BindingRestrictions Merge(BindingRestrictions restrictions)
    {
        ContractUtils.RequiresNotNull(restrictions, nameof(restrictions));
        if (this == Empty)
        {
            return restrictions;
        }

        return restrictions == Empty ? this : new MergedRestriction(this, restrictions);
    }

    /// <summary>Creates the <see cref="T:System.Linq.Expressions.Expression" /> representing the binding restrictions.</summary>
    /// <returns>The expression tree representing the restrictions.</returns>
    public Expression ToExpression()
    {
        if (this == Empty)
        {
            return Expression.Constant(true);
        }

        var testBuilder = new TestBuilder();
        var bindingRestrictionsStack = new Stack<BindingRestrictions>();
        bindingRestrictionsStack.Push(this);
        do
        {
            var restrictions = bindingRestrictionsStack.Pop();
            if (restrictions is MergedRestriction mergedRestriction)
            {
                bindingRestrictionsStack.Push(mergedRestriction.Right);
                bindingRestrictionsStack.Push(mergedRestriction.Left);
            }
            else
            {
                testBuilder.Append(restrictions);
            }
        } while (bindingRestrictionsStack.Count > 0);

        return testBuilder.ToExpression();
    }

    internal abstract Expression GetExpression();

    internal static BindingRestrictions GetTypeRestriction(DynamicMetaObject obj)
    {
        return obj.Value == null && obj.HasValue
            ? GetInstanceRestriction(obj.Expression, null)
            : GetTypeRestriction(obj.Expression, obj.LimitType);
    }

    private sealed class TestBuilder
    {
        private readonly Stack<AndNode> _tests = new();
        private readonly Set<BindingRestrictions> _unique = new();

        internal void Append(BindingRestrictions restrictions)
        {
            if (_unique.Contains(restrictions))
            {
                return;
            }

            _unique.Add(restrictions);
            Push(restrictions.GetExpression(), 0);
        }

        internal Expression ToExpression()
        {
            var right = _tests.Pop().Node;
            while (_tests.Count > 0)
            {
                right = Expression.AndAlso(_tests.Pop().Node, right);
            }

            return right;
        }

        private void Push(Expression node, int depth)
        {
            for (; _tests.Count > 0 && _tests.Peek().Depth == depth; ++depth)
            {
                node = Expression.AndAlso(_tests.Pop().Node, node);
            }

            _tests.Push(new AndNode
            {
                Node = node,
                Depth = depth
            });
        }

        private struct AndNode
        {
            internal int Depth;
            internal Expression Node;
        }
    }

    private sealed class MergedRestriction : BindingRestrictions
    {
        internal readonly BindingRestrictions Left;
        internal readonly BindingRestrictions Right;

        internal MergedRestriction(BindingRestrictions left, BindingRestrictions right)
        {
            Left = left;
            Right = right;
        }

        internal override Expression GetExpression()
        {
            throw ContractUtils.Unreachable;
        }
    }

    private sealed class CustomRestriction : BindingRestrictions
    {
        private readonly Expression _expression;

        internal CustomRestriction(Expression expression)
        {
            _expression = expression;
        }

        public override bool Equals(object obj)
        {
            return obj is CustomRestriction customRestriction && customRestriction._expression == _expression;
        }

        public override int GetHashCode()
        {
            return 1073741824 /*0x40000000*/ ^ _expression.GetHashCode();
        }

        internal override Expression GetExpression()
        {
            return _expression;
        }
    }

    private sealed class TypeRestriction : BindingRestrictions
    {
        private readonly Expression _expression;
        private readonly Type _type;

        internal TypeRestriction(Expression parameter, Type type)
        {
            _expression = parameter;
            _type = type;
        }

        public override bool Equals(object obj)
        {
            return obj is TypeRestriction typeRestriction && TypeUtils.AreEquivalent(typeRestriction._type, _type) &&
                   typeRestriction._expression == _expression;
        }

        public override int GetHashCode()
        {
            return 268435456 /*0x10000000*/ ^ _expression.GetHashCode() ^ _type.GetHashCode();
        }

        internal override Expression GetExpression()
        {
            return Expression.TypeEqual(_expression, _type);
        }
    }

    private sealed class InstanceRestriction : BindingRestrictions
    {
        private readonly Expression _expression;
        private readonly object _instance;

        internal InstanceRestriction(Expression parameter, object instance)
        {
            _expression = parameter;
            _instance = instance;
        }

        public override bool Equals(object obj)
        {
            return obj is InstanceRestriction instanceRestriction && instanceRestriction._instance == _instance &&
                   instanceRestriction._expression == _expression;
        }

        public override int GetHashCode()
        {
            return 536870912 /*0x20000000*/ ^ RuntimeHelpers.GetHashCode(_instance) ^ _expression.GetHashCode();
        }

        internal override Expression GetExpression()
        {
            if (_instance == null)
            {
                return Expression.Equal(Expression.Convert(_expression, typeof(object)), Expression.Constant(null));
            }

            var parameterExpression = Expression.Parameter(typeof(object), null);
            return Expression.Block(new ParameterExpression[1]
                {
                    parameterExpression
                },
                Expression.Assign(parameterExpression,
                    Expression.Property(Expression.Constant(new WeakReference(_instance)),
                        typeof(WeakReference).GetProperty("Target"))),
                Expression.AndAlso(Expression.NotEqual(parameterExpression, Expression.Constant(null)),
                    Expression.Equal(Expression.Convert(_expression, typeof(object)), parameterExpression)));
        }
    }

    private sealed class BindingRestrictionsProxy
    {
        private readonly BindingRestrictions _node;

        public BindingRestrictionsProxy(BindingRestrictions node)
        {
            _node = node;
        }

        public bool IsEmpty => _node == Empty;

        public Expression Test => _node.ToExpression();

        public BindingRestrictions[] Restrictions
        {
            get
            {
                var bindingRestrictionsList = new List<BindingRestrictions>();
                var bindingRestrictionsStack = new Stack<BindingRestrictions>();
                bindingRestrictionsStack.Push(_node);
                do
                {
                    var bindingRestrictions = bindingRestrictionsStack.Pop();
                    if (bindingRestrictions is MergedRestriction mergedRestriction)
                    {
                        bindingRestrictionsStack.Push(mergedRestriction.Right);
                        bindingRestrictionsStack.Push(mergedRestriction.Left);
                    }
                    else
                    {
                        bindingRestrictionsList.Add(bindingRestrictions);
                    }
                } while (bindingRestrictionsStack.Count > 0);

                return bindingRestrictionsList.ToArray();
            }
        }

        public override string ToString()
        {
            return _node.DebugView;
        }
    }
}