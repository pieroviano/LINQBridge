﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Linq.Expressions
{
    internal class BlockN : BlockExpression
    {
        private IList<Expression> _expressions;

        internal override int ExpressionCount
        {
            get
            {
                return this._expressions.Count;
            }
        }

        internal BlockN(IList<Expression> expressions)
        {
            this._expressions = expressions;
        }

        internal override Expression GetExpression(int index)
        {
            return this._expressions[index];
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeExpressions()
        {
            return Expression.ReturnReadOnly<Expression>(ref this._expressions);
        }

        internal override BlockExpression Rewrite(ReadOnlyCollection<ParameterExpression> variables, Expression[] args)
        {
            return new BlockN(args);
        }
    }
}