using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

#nullable disable
namespace System.Linq.Expressions
{
    // Minimal BindingRestrictions emulation for .NET 3.5
    public class BindingRestrictions
    {
        private readonly IList<Expression> _conditions;

        private BindingRestrictions(IList<Expression> conditions)
        {
            _conditions = conditions ?? new Expression[0];
        }

        // Empty restrictions
        public static BindingRestrictions Empty { get; } = new BindingRestrictions(new Expression[0]);

        /// <summary>Creates the <see cref="T:System.Linq.Expressions.Expression" /> representing the binding restrictions.</summary>
        /// <returns>The expression tree representing the restrictions.</returns>
        [__DynamicallyInvokable]
        public Expression ToExpression()
        {
            if (this == BindingRestrictions.Empty)
            {
                return Expression.Constant(true);
            }
            BindingRestrictions.TestBuilder testBuilder = new BindingRestrictions.TestBuilder();
            Stack<BindingRestrictions> bindingRestrictions = new Stack<BindingRestrictions>();
            bindingRestrictions.Push(this);
            do
            {
                BindingRestrictions bindingRestriction = bindingRestrictions.Pop();
                BindingRestrictions.MergedRestriction mergedRestriction = bindingRestriction as BindingRestrictions.MergedRestriction;
                if (mergedRestriction == null)
                {
                    testBuilder.Append(bindingRestriction);
                }
                else
                {
                    bindingRestrictions.Push(mergedRestriction.Right);
                    bindingRestrictions.Push(mergedRestriction.Left);
                }
            }
            while (bindingRestrictions.Count > 0);
            return testBuilder.ToExpression();
        }

        internal virtual Expression GetExpression()
        {
            return Expression.Empty();
        }

        private BindingRestrictions()
        {
        }

        private sealed class MergedRestriction : BindingRestrictions
        {
            internal readonly BindingRestrictions Left;

            internal readonly BindingRestrictions Right;

            internal MergedRestriction(BindingRestrictions left, BindingRestrictions right): base()
            {
                this.Left = left;
                this.Right = right;
            }

            internal override Expression GetExpression()
            {
                throw ContractUtils.Unreachable;
            }
        }
        private sealed class TestBuilder
        {
            private readonly Set<BindingRestrictions> _unique = new Set<BindingRestrictions>();

            private readonly Stack<BindingRestrictions.TestBuilder.AndNode> _tests = new Stack<BindingRestrictions.TestBuilder.AndNode>();

            public TestBuilder()
            {
            }

            internal void Append(BindingRestrictions restrictions)
            {
                if (this._unique.Contains(restrictions))
                {
                    return;
                }
                this._unique.Add(restrictions);
                this.Push(restrictions.GetExpression(), 0);
            }

            private void Push(Expression node, int depth)
            {
                while (this._tests.Count > 0 && this._tests.Peek().Depth == depth)
                {
                    node = Expression.AndAlso(this._tests.Pop().Node, node);
                    depth++;
                }
                Stack<BindingRestrictions.TestBuilder.AndNode> andNodes = this._tests;
                BindingRestrictions.TestBuilder.AndNode andNode = new BindingRestrictions.TestBuilder.AndNode()
                {
                    Node = node,
                    Depth = depth
                };
                andNodes.Push(andNode);
            }

            internal Expression ToExpression()
            {
                Expression node = this._tests.Pop().Node;
                while (this._tests.Count > 0)
                {
                    node = Expression.AndAlso(this._tests.Pop().Node, node);
                }
                return node;
            }

            private struct AndNode
            {
                internal int Depth;

                internal Expression Node;
            }
        }

        /// <summary>Combines binding restrictions from the list of <see cref="T:DynamicMetaObject" /> instances into one set of restrictions.</summary>
        /// <returns>The new set of binding restrictions.</returns>
        /// <param name="contributingObjects">The list of <see cref="T:DynamicMetaObject" /> instances from which to combine restrictions.</param>
        public static BindingRestrictions Combine(IList<DynamicMetaObject> contributingObjects)
        {
            BindingRestrictions empty = BindingRestrictions.Empty;
            if (contributingObjects != null)
            {
                foreach (DynamicMetaObject contributingObject in contributingObjects)
                {
                    if (contributingObject == null)
                    {
                        continue;
                    }
                    empty = empty.Merge(contributingObject.Restrictions);
                }
            }
            return empty;
        }

        // Merge two restrictions into a new one containing all conditions
        public BindingRestrictions Merge(BindingRestrictions other)
        {
            if (other == null || other._conditions.Count == 0) return this;
            if (_conditions.Count == 0) return other;

            var combined = new Expression[_conditions.Count + other._conditions.Count];
            _conditions.CopyTo(combined, 0);
            other._conditions.CopyTo(combined, _conditions.Count);
            return new BindingRestrictions(combined);
        }

        // Create a restriction based on a runtime type check: "expr.GetType() == type"
        public static BindingRestrictions GetTypeRestriction(Expression expression, Type type)
        {
            if (expression == null) throw new ArgumentNullException("expression");
            if (type == null) throw new ArgumentNullException("type");

            // Use Convert to object so GetType can be called uniformly for value/reference types.
            MethodInfo getTypeMethod = typeof(object).GetMethod("GetType");
            Expression exprAsObject = Expression.Convert(expression, typeof(object));
            Expression callGetType = Expression.Call(exprAsObject, getTypeMethod);
            Expression typeConst = Expression.Constant(type, typeof(Type));
            Expression condition = Expression.Equal(callGetType, typeConst);

            return new BindingRestrictions(new[] { condition });
        }

        // Create a restriction from an arbitrary boolean expression
        public static BindingRestrictions GetExpressionRestriction(Expression condition)
        {
            if (condition == null) throw new ArgumentNullException("condition");
            return new BindingRestrictions(new[] { condition });
        }

        // Instance equality / identity restriction (expr == instanceValue)
        public static BindingRestrictions GetInstanceRestriction(Expression expression, object instanceValue)
        {
            if (expression == null) throw new ArgumentNullException("expression");
            // Compare as object to avoid issues with value types / null constants.
            Expression left = Expression.Convert(expression, typeof(object));
            Expression right = Expression.Constant(instanceValue, typeof(object));
            Expression cond = Expression.Equal(left, right);
            return new BindingRestrictions(new[] { cond });
        }

        // Expose underlying conditions (read-only) for diagnostics/consumers
        public IList<Expression> Conditions => _conditions;

        public override string ToString()
        {
            return $"BindingRestrictions(Count={_conditions.Count})";
        }
    }
}