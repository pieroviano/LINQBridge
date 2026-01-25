#nullable disable
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Linq.Expressions;

internal sealed class ExpressionStringBuilder : ExpressionVisitor
{
    private readonly StringBuilder _out;
    private Dictionary<object, int> _ids;

    private ExpressionStringBuilder()
    {
        _out = new StringBuilder();
    }

    public override string ToString()
    {
        return _out.ToString();
    }

    public override CatchBlock VisitCatchBlock(CatchBlock node)
    {
        Out("catch (" + node.Test.Name);
        if (node.Variable != null)
        {
            Out(node.Variable.Name ?? "");
        }

        Out(") { ... }");
        return node;
    }

    public override SwitchCase VisitSwitchCase(SwitchCase node)
    {
        Out("case ");
        VisitExpressions('(', node.TestValues, ')');
        Out(": ...");
        return node;
    }

    protected internal override Expression VisitBinary(BinaryExpression node)
    {
        if (node.NodeType == ExpressionType.ArrayIndex)
        {
            Visit(node.Left);
            Out("[");
            Visit(node.Right);
            Out("]");
        }
        else
        {
            string s;
            switch (node.NodeType)
            {
                case ExpressionType.Add:
                    s = "+";
                    break;
                case ExpressionType.AddChecked:
                    s = "+";
                    break;
                case ExpressionType.And:
                    s = node.Type == typeof(bool) || node.Type == typeof(bool?) ? "And" : "&";
                    break;
                case ExpressionType.AndAlso:
                    s = "AndAlso";
                    break;
                case ExpressionType.Coalesce:
                    s = "??";
                    break;
                case ExpressionType.Divide:
                    s = "/";
                    break;
                case ExpressionType.Equal:
                    s = "==";
                    break;
                case ExpressionType.ExclusiveOr:
                    s = "^";
                    break;
                case ExpressionType.GreaterThan:
                    s = ">";
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    s = ">=";
                    break;
                case ExpressionType.LeftShift:
                    s = "<<";
                    break;
                case ExpressionType.LessThan:
                    s = "<";
                    break;
                case ExpressionType.LessThanOrEqual:
                    s = "<=";
                    break;
                case ExpressionType.Modulo:
                    s = "%";
                    break;
                case ExpressionType.Multiply:
                    s = "*";
                    break;
                case ExpressionType.MultiplyChecked:
                    s = "*";
                    break;
                case ExpressionType.NotEqual:
                    s = "!=";
                    break;
                case ExpressionType.Or:
                    s = node.Type == typeof(bool) || node.Type == typeof(bool?) ? "Or" : "|";
                    break;
                case ExpressionType.OrElse:
                    s = "OrElse";
                    break;
                case ExpressionType.Power:
                    s = "^";
                    break;
                case ExpressionType.RightShift:
                    s = ">>";
                    break;
                case ExpressionType.Subtract:
                    s = "-";
                    break;
                case ExpressionType.SubtractChecked:
                    s = "-";
                    break;
                case ExpressionType.Assign:
                    s = "=";
                    break;
                case ExpressionType.AddAssign:
                    s = "+=";
                    break;
                case ExpressionType.AndAssign:
                    s = node.Type == typeof(bool) || node.Type == typeof(bool?) ? "&&=" : "&=";
                    break;
                case ExpressionType.DivideAssign:
                    s = "/=";
                    break;
                case ExpressionType.ExclusiveOrAssign:
                    s = "^=";
                    break;
                case ExpressionType.LeftShiftAssign:
                    s = "<<=";
                    break;
                case ExpressionType.ModuloAssign:
                    s = "%=";
                    break;
                case ExpressionType.MultiplyAssign:
                    s = "*=";
                    break;
                case ExpressionType.OrAssign:
                    s = node.Type == typeof(bool) || node.Type == typeof(bool?) ? "||=" : "|=";
                    break;
                case ExpressionType.PowerAssign:
                    s = "**=";
                    break;
                case ExpressionType.RightShiftAssign:
                    s = ">>=";
                    break;
                case ExpressionType.SubtractAssign:
                    s = "-=";
                    break;
                case ExpressionType.AddAssignChecked:
                    s = "+=";
                    break;
                case ExpressionType.MultiplyAssignChecked:
                    s = "*=";
                    break;
                case ExpressionType.SubtractAssignChecked:
                    s = "-=";
                    break;
                default:
                    throw new InvalidOperationException();
            }

            Out("(");
            Visit(node.Left);
            Out(' ');
            Out(s);
            Out(' ');
            Visit(node.Right);
            Out(")");
        }

        return node;
    }

    protected internal override Expression VisitBlock(BlockExpression node)
    {
        Out("{");
        foreach (var variable in node.Variables)
        {
            Out("var ");
            Visit(variable);
            Out(";");
        }

        Out(" ... }");
        return node;
    }

    protected internal override Expression VisitConditional(ConditionalExpression node)
    {
        Out("IIF(");
        Visit(node.Test);
        Out(", ");
        Visit(node.IfTrue);
        Out(", ");
        Visit(node.IfFalse);
        Out(")");
        return node;
    }

    protected internal override Expression VisitConstant(ConstantExpression node)
    {
        if (node.Value != null)
        {
            var s = node.Value.ToString();
            if (node.Value is string)
            {
                Out("\"");
                Out(s);
                Out("\"");
            }
            else if (s == node.Value.GetType().ToString())
            {
                Out("value(");
                Out(s);
                Out(")");
            }
            else
            {
                Out(s);
            }
        }
        else
        {
            Out("null");
        }

        return node;
    }

    protected internal override Expression VisitDebugInfo(DebugInfoExpression node)
    {
        Out(string.Format(CultureInfo.CurrentCulture, "<DebugInfo({0}: {1}, {2}, {3}, {4})>", node.Document.FileName,
            node.StartLine, node.StartColumn, node.EndLine, node.EndColumn));
        return node;
    }

    protected internal override Expression VisitDefault(DefaultExpression node)
    {
        Out("default(");
        Out(node.Type.Name);
        Out(")");
        return node;
    }

    protected internal override Expression VisitDynamic(DynamicExpression node)
    {
        Out(FormatBinder(node.Binder));
        VisitExpressions('(', node.Arguments, ')');
        return node;
    }

    protected internal override Expression VisitExtension(Expression node)
    {
        var bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.ExactBinding;
        if (node.GetType().GetMethod("ToString", bindingAttr, null, Type.EmptyTypes, null).DeclaringType !=
            typeof(Expression))
        {
            Out(node.ToString());
            return node;
        }

        Out("[");
        if (node.NodeType == ExpressionType.Extension)
        {
            Out(node.GetType().FullName);
        }
        else
        {
            Out(node.NodeType.ToString());
        }

        Out("]");
        return node;
    }

    protected internal override Expression VisitGoto(GotoExpression node)
    {
        Out(node.Kind.ToString().ToLower(CultureInfo.CurrentCulture));
        DumpLabel(node.Target);
        if (node.Value != null)
        {
            Out(" (");
            Visit(node.Value);
            Out(") ");
        }

        return node;
    }

    protected internal override Expression VisitIndex(IndexExpression node)
    {
        if (node.Object != null)
        {
            Visit(node.Object);
        }
        else
        {
            Out(node.Indexer.DeclaringType.Name);
        }

        if (node.Indexer != null)
        {
            Out(".");
            Out(node.Indexer.Name);
        }

        VisitExpressions('[', node.Arguments, ']');
        return node;
    }

    protected internal override Expression VisitInvocation(InvocationExpression node)
    {
        Out("Invoke(");
        Visit(node.Expression);
        var s = ", ";
        var index = 0;
        for (var count = node.Arguments.Count; index < count; ++index)
        {
            Out(s);
            Visit(node.Arguments[index]);
        }

        Out(")");
        return node;
    }

    protected internal override Expression VisitLabel(LabelExpression node)
    {
        Out("{ ... } ");
        DumpLabel(node.Target);
        Out(":");
        return node;
    }

    protected internal override Expression VisitLambda<T>(Expression<T> node)
    {
        if (node.Parameters.Count == 1)
        {
            Visit(node.Parameters[0]);
        }
        else
        {
            VisitExpressions('(', node.Parameters, ')');
        }

        Out(" => ");
        Visit(node.Body);
        return node;
    }

    protected internal override Expression VisitListInit(ListInitExpression node)
    {
        Visit(node.NewExpression);
        Out(" {");
        var index = 0;
        for (var count = node.Initializers.Count; index < count; ++index)
        {
            if (index > 0)
            {
                Out(", ");
            }

            Out(node.Initializers[index].ToString());
        }

        Out("}");
        return node;
    }

    protected internal override Expression VisitLoop(LoopExpression node)
    {
        Out("loop { ... }");
        return node;
    }

    protected internal override Expression VisitMember(MemberExpression node)
    {
        OutMember(node.Expression, node.Member);
        return node;
    }

    protected internal override Expression VisitMemberInit(MemberInitExpression node)
    {
        if (node.NewExpression.Arguments.Count == 0 && node.NewExpression.Type.Name.Contains("<"))
        {
            Out("new");
        }
        else
        {
            Visit(node.NewExpression);
        }

        Out(" {");
        var index = 0;
        for (var count = node.Bindings.Count; index < count; ++index)
        {
            var binding = node.Bindings[index];
            if (index > 0)
            {
                Out(", ");
            }

            VisitMemberBinding(binding);
        }

        Out("}");
        return node;
    }

    protected internal override Expression VisitMethodCall(MethodCallExpression node)
    {
        var num = 0;
        var node1 = node.Object;
        if (Attribute.GetCustomAttribute(node.Method, typeof(ExtensionAttribute)) != null)
        {
            num = 1;
            node1 = node.Arguments[0];
        }

        if (node1 != null)
        {
            Visit(node1);
            Out(".");
        }

        Out(node.Method.Name);
        Out("(");
        var index = num;
        for (var count = node.Arguments.Count; index < count; ++index)
        {
            if (index > num)
            {
                Out(", ");
            }

            Visit(node.Arguments[index]);
        }

        Out(")");
        return node;
    }

    protected internal override Expression VisitNew(NewExpression node)
    {
        Out("new " + node.Type.Name);
        Out("(");
        var members = node.Members;
        for (var index = 0; index < node.Arguments.Count; ++index)
        {
            if (index > 0)
            {
                Out(", ");
            }

            if (members != null)
            {
                Out(members[index].Name);
                Out(" = ");
            }

            Visit(node.Arguments[index]);
        }

        Out(")");
        return node;
    }

    protected internal override Expression VisitNewArray(NewArrayExpression node)
    {
        switch (node.NodeType)
        {
            case ExpressionType.NewArrayInit:
                Out("new [] ");
                VisitExpressions('{', node.Expressions, '}');
                break;
            case ExpressionType.NewArrayBounds:
                Out("new " + node.Type);
                VisitExpressions('(', node.Expressions, ')');
                break;
        }

        return node;
    }

    protected internal override Expression VisitParameter(ParameterExpression node)
    {
        if (node.IsByRef)
        {
            Out("ref ");
        }

        var name = node.Name;
        if (string.IsNullOrEmpty(name))
        {
            Out("Param_" + GetParamId(node));
        }
        else
        {
            Out(name);
        }

        return node;
    }

    protected internal override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
    {
        VisitExpressions('(', node.Variables, ')');
        return node;
    }

    protected internal override Expression VisitSwitch(SwitchExpression node)
    {
        Out("switch ");
        Out("(");
        Visit(node.SwitchValue);
        Out(") { ... }");
        return node;
    }

    protected internal override Expression VisitTry(TryExpression node)
    {
        Out("try { ... }");
        return node;
    }

    protected internal override Expression VisitTypeBinary(TypeBinaryExpression node)
    {
        Out("(");
        Visit(node.Expression);
        switch (node.NodeType)
        {
            case ExpressionType.TypeIs:
                Out(" Is ");
                break;
            case ExpressionType.TypeEqual:
                Out(" TypeEqual ");
                break;
        }

        Out(node.TypeOperand.Name);
        Out(")");
        return node;
    }

    protected internal override Expression VisitUnary(UnaryExpression node)
    {
        switch (node.NodeType)
        {
            case ExpressionType.Negate:
            case ExpressionType.NegateChecked:
                Out("-");
                goto case ExpressionType.Quote;
            case ExpressionType.UnaryPlus:
                Out("+");
                goto case ExpressionType.Quote;
            case ExpressionType.Not:
                Out("Not(");
                goto case ExpressionType.Quote;
            case ExpressionType.Quote:
                Visit(node.Operand);
                switch (node.NodeType)
                {
                    case ExpressionType.Negate:
                    case ExpressionType.UnaryPlus:
                    case ExpressionType.NegateChecked:
                    case ExpressionType.Quote:
                    case ExpressionType.PreIncrementAssign:
                    case ExpressionType.PreDecrementAssign:
                        return node;
                    case ExpressionType.TypeAs:
                        Out(" As ");
                        Out(node.Type.Name);
                        Out(")");
                        goto case ExpressionType.Negate;
                    case ExpressionType.PostIncrementAssign:
                        Out("++");
                        goto case ExpressionType.Negate;
                    case ExpressionType.PostDecrementAssign:
                        Out("--");
                        goto case ExpressionType.Negate;
                    default:
                        Out(")");
                        goto case ExpressionType.Negate;
                }
            case ExpressionType.TypeAs:
                Out("(");
                goto case ExpressionType.Quote;
            case ExpressionType.Decrement:
                Out("Decrement(");
                goto case ExpressionType.Quote;
            case ExpressionType.Increment:
                Out("Increment(");
                goto case ExpressionType.Quote;
            case ExpressionType.Throw:
                Out("throw(");
                goto case ExpressionType.Quote;
            case ExpressionType.PreIncrementAssign:
                Out("++");
                goto case ExpressionType.Quote;
            case ExpressionType.PreDecrementAssign:
                Out("--");
                goto case ExpressionType.Quote;
            case ExpressionType.OnesComplement:
                Out("~(");
                goto case ExpressionType.Quote;
            default:
                Out(node.NodeType.ToString());
                Out("(");
                goto case ExpressionType.Quote;
        }
    }

    protected override ElementInit VisitElementInit(ElementInit initializer)
    {
        Out(initializer.AddMethod.ToString());
        var seperator = ", ";
        VisitExpressions('(', initializer.Arguments, ')', seperator);
        return initializer;
    }

    protected override MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
    {
        Out(assignment.Member.Name);
        Out(" = ");
        Visit(assignment.Expression);
        return assignment;
    }

    protected override MemberListBinding VisitMemberListBinding(MemberListBinding binding)
    {
        Out(binding.Member.Name);
        Out(" = {");
        var index = 0;
        for (var count = binding.Initializers.Count; index < count; ++index)
        {
            if (index > 0)
            {
                Out(", ");
            }

            VisitElementInit(binding.Initializers[index]);
        }

        Out("}");
        return binding;
    }

    protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding)
    {
        Out(binding.Member.Name);
        Out(" = {");
        var index = 0;
        for (var count = binding.Bindings.Count; index < count; ++index)
        {
            if (index > 0)
            {
                Out(", ");
            }

            VisitMemberBinding(binding.Bindings[index]);
        }

        Out("}");
        return binding;
    }

    internal static string CatchBlockToString(CatchBlock node)
    {
        var expressionStringBuilder = new ExpressionStringBuilder();
        expressionStringBuilder.VisitCatchBlock(node);
        return expressionStringBuilder.ToString();
    }

    internal static string ElementInitBindingToString(ElementInit node)
    {
        var expressionStringBuilder = new ExpressionStringBuilder();
        expressionStringBuilder.VisitElementInit(node);
        return expressionStringBuilder.ToString();
    }

    internal static string ExpressionToString(Expression node)
    {
        var expressionStringBuilder = new ExpressionStringBuilder();
        expressionStringBuilder.Visit(node);
        return expressionStringBuilder.ToString();
    }

    internal static string MemberBindingToString(MemberBinding node)
    {
        var expressionStringBuilder = new ExpressionStringBuilder();
        expressionStringBuilder.VisitMemberBinding(node);
        return expressionStringBuilder.ToString();
    }

    internal static string SwitchCaseToString(SwitchCase node)
    {
        var expressionStringBuilder = new ExpressionStringBuilder();
        expressionStringBuilder.VisitSwitchCase(node);
        return expressionStringBuilder.ToString();
    }

    private void AddLabel(LabelTarget label)
    {
        if (_ids == null)
        {
            _ids = new Dictionary<object, int>();
            _ids.Add(label, 0);
        }
        else
        {
            if (_ids.ContainsKey(label))
            {
                return;
            }

            _ids.Add(label, _ids.Count);
        }
    }

    private void AddParam(ParameterExpression p)
    {
        if (_ids == null)
        {
            _ids = new Dictionary<object, int>();
            _ids.Add(_ids, 0);
        }
        else
        {
            if (_ids.ContainsKey(p))
            {
                return;
            }

            _ids.Add(p, _ids.Count);
        }
    }

    private void DumpLabel(LabelTarget target)
    {
        if (!string.IsNullOrEmpty(target.Name))
        {
            Out(target.Name);
        }
        else
        {
            Out("UnamedLabel_" + GetLabelId(target));
        }
    }

    private static string FormatBinder(CallSiteBinder binder)
    {
        switch (binder)
        {
            case ConvertBinder convertBinder:
                return "Convert " + convertBinder.Type;
            case GetMemberBinder getMemberBinder:
                return "GetMember " + getMemberBinder.Name;
            case SetMemberBinder setMemberBinder:
                return "SetMember " + setMemberBinder.Name;
            case DeleteMemberBinder deleteMemberBinder:
                return "DeleteMember " + deleteMemberBinder.Name;
            case GetIndexBinder _:
                return "GetIndex";
            case SetIndexBinder _:
                return "SetIndex";
            case DeleteIndexBinder _:
                return "DeleteIndex";
            case InvokeMemberBinder invokeMemberBinder:
                return "Call " + invokeMemberBinder.Name;
            case InvokeBinder _:
                return "Invoke";
            case CreateInstanceBinder _:
                return "Create";
            case UnaryOperationBinder unaryOperationBinder:
                return unaryOperationBinder.Operation.ToString();
            case BinaryOperationBinder binaryOperationBinder:
                return binaryOperationBinder.Operation.ToString();
            default:
                return "CallSiteBinder";
        }
    }

    private int GetLabelId(LabelTarget label)
    {
        if (_ids == null)
        {
            _ids = new Dictionary<object, int>();
            AddLabel(label);
            return 0;
        }

        int count;
        if (!_ids.TryGetValue(label, out count))
        {
            count = _ids.Count;
            AddLabel(label);
        }

        return count;
    }

    private int GetParamId(ParameterExpression p)
    {
        if (_ids == null)
        {
            _ids = new Dictionary<object, int>();
            AddParam(p);
            return 0;
        }

        int count;
        if (!_ids.TryGetValue(p, out count))
        {
            count = _ids.Count;
            AddParam(p);
        }

        return count;
    }

    private void Out(string s)
    {
        _out.Append(s);
    }

    private void Out(char c)
    {
        _out.Append(c);
    }

    private void OutMember(Expression instance, MemberInfo member)
    {
        if (instance != null)
        {
            Visit(instance);
            Out("." + member.Name);
        }
        else
        {
            Out($"{member.DeclaringType.Name}.{member.Name}");
        }
    }

    private void VisitExpressions<T>(char open, IList<T> expressions, char close) where T : Expression
    {
        VisitExpressions(open, expressions, close, ", ");
    }

    private void VisitExpressions<T>(char open, IList<T> expressions, char close, string seperator) where T : Expression
    {
        Out(open);
        if (expressions != null)
        {
            var flag = true;
            foreach (var expression in expressions)
            {
                if (flag)
                {
                    flag = false;
                }
                else
                {
                    Out(seperator);
                }

                Visit(expression);
            }
        }

        Out(close);
    }
}