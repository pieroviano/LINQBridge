using System;
using System.Linq.Expressions;

#nullable disable
namespace System.Linq.Expressions
{
    // Minimal BinaryOperationBinder shim for .NET 3.5 compatibility.
    //
    // Surface:
    // - Operation: the ExpressionType for the binary operation (Add, SubtractAssign, etc.)
    // - abstract FallbackBinaryOperation to be implemented by concrete binders
    // - Defer helpers that call the fallback (used by COM shim and binder implementations)
    public abstract class BinaryOperationBinder:CallSiteBinder
    {
        protected BinaryOperationBinder(ExpressionType operation)
        {
            this.Operation = operation;
        }

        public ExpressionType Operation { get; private set; }

        // Implementers must provide fallback behaviour for binary operations.
        public abstract DynamicMetaObject FallbackBinaryOperation(
            DynamicMetaObject target,
            DynamicMetaObject arg,
            DynamicMetaObject errorSuggestion);

        // Simple Defer helpers used in other parts of the port.
        public virtual DynamicMetaObject Defer(DynamicMetaObject target, DynamicMetaObject arg)
        {
            if (target == null) throw new ArgumentNullException("target");
            if (arg == null) throw new ArgumentNullException("arg");
            return this.FallbackBinaryOperation(target, arg, null);
        }

        public virtual DynamicMetaObject Defer(params DynamicMetaObject[] args)
        {
            if (args == null || args.Length < 2) throw new ArgumentException("args");
            return this.Defer(args[0], args[1]);
        }

        public override bool Equals(object obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();
    }
}