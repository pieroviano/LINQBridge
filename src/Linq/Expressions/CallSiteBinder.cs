using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace System.Linq.Expressions
{
    // Minimal CallSiteBinder shim used by the small DLR surface implemented in this repo.
    //
    // The concrete binders in this codebase (e.g. SplatInvokeBinder) override
    // Bind to produce an Expression tree for the dynamic operation. The real
    // BCL CallSiteBinder has many more members; this minimal form provides the
    // single API surface used by the project's code.
    public abstract class CallSiteBinder
    {
        protected CallSiteBinder()
        {
        }

        /// <summary>
        /// Produce the expression tree implementing the dynamic operation.
        /// args: the original runtime arguments (used by some binders for shape)
        /// parameters: parameters to use for the dynamic site's generated delegate
        /// returnLabel: label used to return a value from the generated block
        /// </summary>
        public abstract Expression Bind(object[] args, ReadOnlyCollection<ParameterExpression> parameters, LabelTarget returnLabel);

    }
}