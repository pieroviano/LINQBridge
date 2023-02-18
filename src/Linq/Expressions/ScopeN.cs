﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Linq.Expressions
{
    internal class ScopeN : ScopeExpression
    {
        private IList<Expression> _body;

        internal override int ExpressionCount
        {
            get
            {
                return this._body.Count;
            }
        }

        internal ScopeN(IList<ParameterExpression> variables, IList<Expression> body) : base(variables)
        {
            this._body = body;
        }

        internal override Expression GetExpression(int index)
        {
            return this._body[index];
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeExpressions()
        {
            return Expression.ReturnReadOnly<Expression>(ref this._body);
        }

        internal override BlockExpression Rewrite(ReadOnlyCollection<ParameterExpression> variables, Expression[] args)
        {
            return new ScopeN(base.ReuseOrValidateVariables(variables), args);
        }
    }
}