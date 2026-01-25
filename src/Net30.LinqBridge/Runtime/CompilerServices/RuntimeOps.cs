using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq.Expressions;
using System.Linq.Expressions.Compiler;

namespace System.Runtime.CompilerServices;

/// <summary>Contains helper methods called from dynamically generated methods.</summary>
[DebuggerStepThrough]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class RuntimeOps
{
    /// <summary>Creates an interface that can be used to modify closed over variables at runtime.</summary>
    /// <returns>An interface to access variables.</returns>
    /// <param name="data">The closure array.</param>
    /// <param name="indexes">An array of indicies into the closure array where variables are found.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("do not use this method", true)]
    public static IRuntimeVariables CreateRuntimeVariables(object[] data, long[] indexes)
    {
        return new RuntimeVariableList(data, indexes);
    }

    /// <summary>Creates an interface that can be used to modify closed over variables at runtime.</summary>
    /// <returns>An interface to access variables.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("do not use this method", true)]
    public static IRuntimeVariables CreateRuntimeVariables()
    {
        return new EmptyRuntimeVariables();
    }

    /// <summary>Checks the version of the Expando object.</summary>
    /// <returns>Returns true if the version is equal; otherwise, false.</returns>
    /// <param name="expando">The Expando object.</param>
    /// <param name="version">The version to check.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("do not use this method", true)]
    public static bool ExpandoCheckVersion(ExpandoObject expando, object version)
    {
        return expando.Class == version;
    }

    /// <summary>Promotes an Expando object from one class to a new class.</summary>
    /// <param name="expando">The Expando object.</param>
    /// <param name="oldClass">The old class of the Expando object.</param>
    /// <param name="newClass">The new class of the Expando object.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("do not use this method", true)]
    public static void ExpandoPromoteClass(ExpandoObject expando, object oldClass, object newClass)
    {
        expando.PromoteClass(oldClass, newClass);
    }

    /// <summary>Deletes the value of an item in an expando object.</summary>
    /// <returns>true if the item was successfully removed; otherwise, false.</returns>
    /// <param name="expando">The expando object.</param>
    /// <param name="indexClass">The class of the expando object.</param>
    /// <param name="index">The index of the member.</param>
    /// <param name="name">The name of the member.</param>
    /// <param name="ignoreCase">true if the name should be matched ignoring case; false otherwise.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("do not use this method", true)]
    public static bool ExpandoTryDeleteValue(ExpandoObject expando, object indexClass, int index, string name,
        bool ignoreCase)
    {
        return expando.TryDeleteValue(indexClass, index, name, ignoreCase, ExpandoObject.Uninitialized);
    }

    /// <summary>Gets the value of an item in an expando object.</summary>
    /// <returns>True if the member exists in the expando object, otherwise false.</returns>
    /// <param name="expando">The expando object.</param>
    /// <param name="indexClass">The class of the expando object.</param>
    /// <param name="index">The index of the member.</param>
    /// <param name="name">The name of the member.</param>
    /// <param name="ignoreCase">true if the name should be matched ignoring case; false otherwise.</param>
    /// <param name="value">The out parameter containing the value of the member.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("do not use this method", true)]
    public static bool ExpandoTryGetValue(ExpandoObject expando, object indexClass, int index, string name,
        bool ignoreCase, out object value)
    {
        return expando.TryGetValue(indexClass, index, name, ignoreCase, out value);
    }

    /// <summary>Sets the value of an item in an expando object.</summary>
    /// <returns>Returns the index for the set member.</returns>
    /// <param name="expando">The expando object.</param>
    /// <param name="indexClass">The class of the expando object.</param>
    /// <param name="index">The index of the member.</param>
    /// <param name="value">The value of the member.</param>
    /// <param name="name">The name of the member.</param>
    /// <param name="ignoreCase">true if the name should be matched ignoring case; false otherwise.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("do not use this method", true)]
    public static object ExpandoTrySetValue(ExpandoObject expando, object indexClass, int index, object value,
        string name, bool ignoreCase)
    {
        expando.TrySetValue(indexClass, index, value, name, ignoreCase, false);
        return value;
    }

    /// <summary>Combines two runtime variable lists and returns a new list.</summary>
    /// <returns>The merged runtime variables.</returns>
    /// <param name="first">The first list.</param>
    /// <param name="second">The second list.</param>
    /// <param name="indexes">The index array indicating which list to get variables from.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("do not use this method", true)]
    public static IRuntimeVariables MergeRuntimeVariables(IRuntimeVariables first, IRuntimeVariables second,
        int[] indexes)
    {
        return new MergedRuntimeVariables(first, second, indexes);
    }

    /// <summary>Quotes the provided expression tree.</summary>
    /// <returns>The quoted expression.</returns>
    /// <param name="expression">The expression to quote.</param>
    /// <param name="hoistedLocals">The hoisted local state provided by the compiler.</param>
    /// <param name="locals">The actual hoisted local values.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("do not use this method", true)]
    public static Expression Quote(Expression expression, object hoistedLocals, object[] locals)
    {
        return new ExpressionQuoter((HoistedLocals)hoistedLocals, locals).Visit(expression);
    }

    private sealed class EmptyRuntimeVariables : IRuntimeVariables
    {
        int IRuntimeVariables.Count => 0;

        object IRuntimeVariables.this[int index]
        {
            get => throw new ArgumentOutOfRangeException("index");
            set => throw new ArgumentOutOfRangeException("index");
        }
    }

    private sealed class ExpressionQuoter : ExpressionVisitor
    {
        private readonly object[] _locals;
        private readonly HoistedLocals _scope;

        private readonly Stack<Set<ParameterExpression>> _shadowedVars = new();

        internal ExpressionQuoter(HoistedLocals scope, object[] locals)
        {
            _scope = scope;
            _locals = locals;
        }

        public override CatchBlock VisitCatchBlock(CatchBlock node)
        {
            if (node.Variable != null)
            {
                _shadowedVars.Push(new Set<ParameterExpression>(new[] { node.Variable }));
            }

            var expression = Visit(node.Body);
            var expression1 = Visit(node.Filter);
            if (node.Variable != null)
            {
                _shadowedVars.Pop();
            }

            if (expression == node.Body && expression1 == node.Filter)
            {
                return node;
            }

            return Expression.MakeCatchBlock(node.Test, node.Variable, expression, expression1);
        }

        protected internal override Expression VisitBlock(BlockExpression node)
        {
            if (node.Variables.Count > 0)
            {
                _shadowedVars.Push(new Set<ParameterExpression>(node.Variables));
            }

            ReadOnlyCollection<Expression> expressions = Visit(node.Expressions);
            if (node.Variables.Count > 0)
            {
                _shadowedVars.Pop();
            }

            if (expressions == node.Expressions)
            {
                return node;
            }

            return Expression.Block(node.Variables, expressions);
        }

        protected internal override Expression VisitLambda<T>(Expression<T> node)
        {
            _shadowedVars.Push(new Set<ParameterExpression>(node.Parameters));
            var expression = Visit(node.Body);
            _shadowedVars.Pop();
            if (expression == node.Body)
            {
                return node;
            }

            return Expression.Lambda<T>(expression, node.Name, node.TailCall, node.Parameters);
        }

        protected internal override Expression VisitParameter(ParameterExpression node)
        {
            var box = GetBox(node);
            if (box == null)
            {
                return node;
            }

            return Expression.Field(Expression.Constant(box), "Value");
        }

        protected internal override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
        {
            var count = node.Variables.Count;
            var strongBoxes = new List<IStrongBox>();
            var parameterExpressions = new List<ParameterExpression>();
            var numArray = new int[count];
            for (var i = 0; i < count; i++)
            {
                var box = GetBox(node.Variables[i]);
                if (box != null)
                {
                    numArray[i] = -1 - strongBoxes.Count;
                    strongBoxes.Add(box);
                }
                else
                {
                    numArray[i] = parameterExpressions.Count;
                    parameterExpressions.Add(node.Variables[i]);
                }
            }

            if (strongBoxes.Count == 0)
            {
                return node;
            }

            var constantExpression =
                Expression.Constant(new RuntimeVariables(strongBoxes.ToArray()), typeof(IRuntimeVariables));
            if (parameterExpressions.Count == 0)
            {
                return constantExpression;
            }

            return Expression.Call(typeof(RuntimeOps).GetMethod("MergeRuntimeVariables"),
                Expression.RuntimeVariables(
                    new TrueReadOnlyCollection<ParameterExpression>(parameterExpressions.ToArray())),
                constantExpression, Expression.Constant(numArray));
        }

        private IStrongBox GetBox(ParameterExpression variable)
        {
            IStrongBox strongBox;
            int num;
            var enumerator = _shadowedVars.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    if (!enumerator.Current.Contains(variable))
                    {
                        continue;
                    }

                    strongBox = null;
                    return strongBox;
                }

                var parent = _scope;
                var objArray = _locals;
                while (!parent.Indexes.TryGetValue(variable, out num))
                {
                    parent = parent.Parent;
                    if (parent == null)
                    {
                        throw ContractUtils.Unreachable;
                    }

                    objArray = HoistedLocals.GetParent(objArray);
                }

                return (IStrongBox)objArray[num];
            }
            finally
            {
                enumerator.Dispose();
            }

            return strongBox;
        }
    }

    private sealed class MergedRuntimeVariables : IRuntimeVariables
    {
        private readonly IRuntimeVariables _first;

        private readonly int[] _indexes;

        private readonly IRuntimeVariables _second;

        internal MergedRuntimeVariables(IRuntimeVariables first, IRuntimeVariables second, int[] indexes)
        {
            _first = first;
            _second = second;
            _indexes = indexes;
        }

        public int Count => _indexes.Length;

        public object this[int index]
        {
            get
            {
                index = _indexes[index];
                if (index >= 0)
                {
                    return _first[index];
                }

                return _second[-1 - index];
            }
            set
            {
                index = _indexes[index];
                if (index >= 0)
                {
                    _first[index] = value;
                    return;
                }

                _second[-1 - index] = value;
            }
        }
    }

    private sealed class RuntimeVariableList : IRuntimeVariables
    {
        private readonly object[] _data;

        private readonly long[] _indexes;

        internal RuntimeVariableList(object[] data, long[] indexes)
        {
            _data = data;
            _indexes = indexes;
        }

        public int Count => _indexes.Length;

        public object this[int index]
        {
            get => GetStrongBox(index).Value;
            set => GetStrongBox(index).Value = value;
        }

        private IStrongBox GetStrongBox(int index)
        {
            var num = _indexes[index];
            var parent = _data;
            for (var i = (int)(num >> 32); i > 0; i--)
            {
                parent = HoistedLocals.GetParent(parent);
            }

            return (IStrongBox)parent[(int)num];
        }
    }

    private sealed class RuntimeVariables : IRuntimeVariables
    {
        private readonly IStrongBox[] _boxes;

        internal RuntimeVariables(IStrongBox[] boxes)
        {
            _boxes = boxes;
        }

        int IRuntimeVariables.Count => _boxes.Length;

        object IRuntimeVariables.this[int index]
        {
            get => _boxes[index].Value;
            set => _boxes[index].Value = value;
        }
    }
}