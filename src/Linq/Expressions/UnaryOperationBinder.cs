using System;
using System.Linq.Expressions;

#nullable disable
namespace System.Linq.Expressions
{
    // Minimal UnaryOperationBinder shim for .NET 3.5 compatibility.
    //
    // Surface:
    // - Operation: the ExpressionType for the unary operation (Negate, Not, etc.)
    // - abstract FallbackUnaryOperation to be implemented by concrete binders
    // - Defer helpers that call the fallback (used by other binders and the COM shim)
    public abstract class UnaryOperationBinder
    {
        protected UnaryOperationBinder(ExpressionType operation)
        {
            this.Operation = operation;
        }

        public ExpressionType Operation { get; private set; }

        // Implementers must provide fallback behaviour for unary operations.
        public abstract DynamicMetaObject FallbackUnaryOperation(
            DynamicMetaObject target,
            DynamicMetaObject errorSuggestion);

        // Simple Defer helper used in other parts of the port.
        public virtual DynamicMetaObject Defer(DynamicMetaObject target)
        {
            if (target == null) throw new ArgumentNullException("target");
            return this.FallbackUnaryOperation(target, null);
        }

        public virtual DynamicMetaObject Defer(params DynamicMetaObject[] args)
        {
            if (args == null || args.Length == 0) throw new ArgumentException("args");
            return this.Defer(args[0]);
        }

        public override bool Equals(object obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();
    }
}