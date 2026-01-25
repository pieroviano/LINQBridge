#nullable disable
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions;

internal sealed class DebugViewWriter : ExpressionVisitor
{
    private const int Tab = 4;
    private const int MaxColumn = 120;
    private readonly TextWriter _out;
    private readonly Stack<int> _stack = new();
    private int _column;
    private Flow _flow;
    private Dictionary<LabelTarget, int> _labelIds;
    private Dictionary<LambdaExpression, int> _lambdaIds;
    private Queue<LambdaExpression> _lambdas;
    private Dictionary<ParameterExpression, int> _paramIds;

    private DebugViewWriter(TextWriter file)
    {
        _out = file;
    }

    private int Base => _stack.Count <= 0 ? 0 : _stack.Peek();

    private int Delta { get; set; }

    private int Depth => Base + Delta;

    public override CatchBlock VisitCatchBlock(CatchBlock node)
    {
        Out(Flow.NewLine, "} .Catch (" + node.Test);
        if (node.Variable != null)
        {
            Out(Flow.Space, "");
            VisitParameter(node.Variable);
        }

        if (node.Filter != null)
        {
            Out(") .If (", Flow.Break);
            Visit(node.Filter);
        }

        Out(") {", Flow.NewLine);
        Indent();
        Visit(node.Body);
        Dedent();
        return node;
    }

    public override SwitchCase VisitSwitchCase(SwitchCase node)
    {
        foreach (var testValue in node.TestValues)
        {
            Out(".Case (");
            Visit(testValue);
            Out("):", Flow.NewLine);
        }

        Indent();
        Indent();
        Visit(node.Body);
        Dedent();
        Dedent();
        NewLine();
        return node;
    }

    protected internal override Expression VisitBinary(BinaryExpression node)
    {
        if (node.NodeType == ExpressionType.ArrayIndex)
        {
            ParenthesizedVisit(node, node.Left);
            Out("[");
            Visit(node.Right);
            Out("]");
        }
        else
        {
            var flag1 = NeedsParentheses(node, node.Left);
            var flag2 = NeedsParentheses(node, node.Right);
            var flag3 = false;
            var before = Flow.Space;
            string s;
            switch (node.NodeType)
            {
                case ExpressionType.Add:
                    s = "+";
                    break;
                case ExpressionType.AddChecked:
                    s = "+";
                    flag3 = true;
                    break;
                case ExpressionType.And:
                    s = "&";
                    break;
                case ExpressionType.AndAlso:
                    s = "&&";
                    before = Flow.Space | Flow.Break;
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
                    flag3 = true;
                    break;
                case ExpressionType.NotEqual:
                    s = "!=";
                    break;
                case ExpressionType.Or:
                    s = "|";
                    break;
                case ExpressionType.OrElse:
                    s = "||";
                    before = Flow.Space | Flow.Break;
                    break;
                case ExpressionType.Power:
                    s = "**";
                    break;
                case ExpressionType.RightShift:
                    s = ">>";
                    break;
                case ExpressionType.Subtract:
                    s = "-";
                    break;
                case ExpressionType.SubtractChecked:
                    s = "-";
                    flag3 = true;
                    break;
                case ExpressionType.Assign:
                    s = "=";
                    break;
                case ExpressionType.AddAssign:
                    s = "+=";
                    break;
                case ExpressionType.AndAssign:
                    s = "&=";
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
                    s = "|=";
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
                    flag3 = true;
                    break;
                case ExpressionType.MultiplyAssignChecked:
                    s = "*=";
                    flag3 = true;
                    break;
                case ExpressionType.SubtractAssignChecked:
                    s = "-=";
                    flag3 = true;
                    break;
                default:
                    throw new InvalidOperationException();
            }

            if (flag1)
            {
                Out("(", Flow.None);
            }

            Visit(node.Left);
            if (flag1)
            {
                Out(Flow.None, ")", Flow.Break);
            }

            if (flag3)
            {
                s = string.Format(CultureInfo.CurrentCulture, "#{0}", s);
            }

            Out(before, s, Flow.Space | Flow.Break);
            if (flag2)
            {
                Out("(", Flow.None);
            }

            Visit(node.Right);
            if (flag2)
            {
                Out(Flow.None, ")", Flow.Break);
            }
        }

        return node;
    }

    protected internal override Expression VisitBlock(BlockExpression node)
    {
        Out(".Block");
        if (node.Type != node.GetExpression(node.ExpressionCount - 1).Type)
        {
            Out(string.Format(CultureInfo.CurrentCulture, "<{0}>", node.Type.ToString()));
        }

        VisitDeclarations(node.Variables);
        Out(" ");
        VisitExpressions('{', ';', node.Expressions);
        return node;
    }

    protected internal override Expression VisitConditional(ConditionalExpression node)
    {
        if (IsSimpleExpression(node.Test))
        {
            Out(".If (");
            Visit(node.Test);
            Out(") {", Flow.NewLine);
        }
        else
        {
            Out(".If (", Flow.NewLine);
            Indent();
            Visit(node.Test);
            Dedent();
            Out(Flow.NewLine, ") {", Flow.NewLine);
        }

        Indent();
        Visit(node.IfTrue);
        Dedent();
        Out(Flow.NewLine, "} .Else {", Flow.NewLine);
        Indent();
        Visit(node.IfFalse);
        Dedent();
        Out(Flow.NewLine, "}");
        return node;
    }

    protected internal override Expression VisitConstant(ConstantExpression node)
    {
        var obj = node.Value;
        switch (obj)
        {
            case null:
                Out("null");
                break;
            case string _ when node.Type == typeof(string):
                Out(string.Format(CultureInfo.CurrentCulture, "\"{0}\"", obj));
                break;
            case char _ when node.Type == typeof(char):
                Out(string.Format(CultureInfo.CurrentCulture, "'{0}'", obj));
                break;
            case int _ when node.Type == typeof(int):
            case bool _ when node.Type == typeof(bool):
                Out(obj.ToString());
                break;
            default:
                var constantValueSuffix = GetConstantValueSuffix(node.Type);
                if (constantValueSuffix != null)
                {
                    Out(obj.ToString());
                    Out(constantValueSuffix);
                    break;
                }

                Out(string.Format(CultureInfo.CurrentCulture, ".Constant<{0}>({1})", node.Type.ToString(), obj));
                break;
        }

        return node;
    }

    protected internal override Expression VisitDebugInfo(DebugInfoExpression node)
    {
        Out(string.Format(CultureInfo.CurrentCulture, ".DebugInfo({0}: {1}, {2} - {3}, {4})", node.Document.FileName,
            node.StartLine, node.StartColumn, node.EndLine, node.EndColumn));
        return node;
    }

    protected internal override Expression VisitDefault(DefaultExpression node)
    {
        Out($".Default({node.Type})");
        return node;
    }

    protected internal override Expression VisitDynamic(DynamicExpression node)
    {
        Out(".Dynamic", Flow.Space);
        Out(FormatBinder(node.Binder));
        VisitExpressions('(', node.Arguments);
        return node;
    }

    protected internal override Expression VisitExtension(Expression node)
    {
        Out(string.Format(CultureInfo.CurrentCulture, ".Extension<{0}>", node.GetType().ToString()));
        if (node.CanReduce)
        {
            Out(Flow.Space, "{", Flow.NewLine);
            Indent();
            Visit(node.Reduce());
            Dedent();
            Out(Flow.NewLine, "}");
        }

        return node;
    }

    protected internal override Expression VisitGoto(GotoExpression node)
    {
        Out("." + node.Kind, Flow.Space);
        Out(GetLabelTargetName(node.Target), Flow.Space);
        Out("{", Flow.Space);
        Visit(node.Value);
        Out(Flow.Space, "}");
        return node;
    }

    protected internal override Expression VisitIndex(IndexExpression node)
    {
        if (node.Indexer != null)
        {
            OutMember(node, node.Object, node.Indexer);
        }
        else
        {
            ParenthesizedVisit(node, node.Object);
        }

        VisitExpressions('[', node.Arguments);
        return node;
    }

    protected internal override Expression VisitInvocation(InvocationExpression node)
    {
        Out(".Invoke ");
        ParenthesizedVisit(node, node.Expression);
        VisitExpressions('(', node.Arguments);
        return node;
    }

    protected internal override Expression VisitLabel(LabelExpression node)
    {
        Out(".Label", Flow.NewLine);
        Indent();
        Visit(node.DefaultValue);
        Dedent();
        NewLine();
        DumpLabel(node.Target);
        return node;
    }

    protected internal override Expression VisitLambda<T>(Expression<T> node)
    {
        Out(string.Format(CultureInfo.CurrentCulture, "{0} {1}<{2}>", ".Lambda", GetLambdaName(node),
            node.Type.ToString()));
        if (_lambdas == null)
        {
            _lambdas = new Queue<LambdaExpression>();
        }

        if (!_lambdas.Contains(node))
        {
            _lambdas.Enqueue(node);
        }

        return node;
    }

    protected internal override Expression VisitListInit(ListInitExpression node)
    {
        Visit(node.NewExpression);
        VisitExpressions('{', ',', node.Initializers, e => VisitElementInit(e));
        return node;
    }

    protected internal override Expression VisitLoop(LoopExpression node)
    {
        Out(".Loop", Flow.Space);
        if (node.ContinueLabel != null)
        {
            DumpLabel(node.ContinueLabel);
        }

        Out(" {", Flow.NewLine);
        Indent();
        Visit(node.Body);
        Dedent();
        Out(Flow.NewLine, "}");
        if (node.BreakLabel != null)
        {
            Out("", Flow.NewLine);
            DumpLabel(node.BreakLabel);
        }

        return node;
    }

    protected internal override Expression VisitMember(MemberExpression node)
    {
        OutMember(node, node.Expression, node.Member);
        return node;
    }

    protected internal override Expression VisitMemberInit(MemberInitExpression node)
    {
        Visit(node.NewExpression);
        VisitExpressions('{', ',', node.Bindings, e => VisitMemberBinding(e));
        return node;
    }

    protected internal override Expression VisitMethodCall(MethodCallExpression node)
    {
        Out(".Call ");
        if (node.Object != null)
        {
            ParenthesizedVisit(node, node.Object);
        }
        else if (node.Method.DeclaringType != null)
        {
            Out(node.Method.DeclaringType.ToString());
        }
        else
        {
            Out("<UnknownType>");
        }

        Out(".");
        Out(node.Method.Name);
        VisitExpressions('(', node.Arguments);
        return node;
    }

    protected internal override Expression VisitNew(NewExpression node)
    {
        Out(".New " + node.Type);
        VisitExpressions('(', node.Arguments);
        return node;
    }

    protected internal override Expression VisitNewArray(NewArrayExpression node)
    {
        if (node.NodeType == ExpressionType.NewArrayBounds)
        {
            Out(".NewArray " + node.Type.GetElementType());
            VisitExpressions('[', node.Expressions);
        }
        else
        {
            Out(".NewArray " + node.Type, Flow.Space);
            VisitExpressions('{', node.Expressions);
        }

        return node;
    }

    protected internal override Expression VisitParameter(ParameterExpression node)
    {
        Out("$");
        if (string.IsNullOrEmpty(node.Name))
        {
            Out("var" + GetParamId(node));
        }
        else
        {
            Out(GetDisplayName(node.Name));
        }

        return node;
    }

    protected internal override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
    {
        Out(".RuntimeVariables");
        VisitExpressions('(', node.Variables);
        return node;
    }

    protected internal override Expression VisitSwitch(SwitchExpression node)
    {
        Out(".Switch ");
        Out("(");
        Visit(node.SwitchValue);
        Out(") {", Flow.NewLine);
        Visit(node.Cases, this.VisitSwitchCase);
        if (node.DefaultBody != null)
        {
            Out(".Default:", Flow.NewLine);
            Indent();
            Indent();
            Visit(node.DefaultBody);
            Dedent();
            Dedent();
            NewLine();
        }

        Out("}");
        return node;
    }

    protected internal override Expression VisitTry(TryExpression node)
    {
        Out(".Try {", Flow.NewLine);
        Indent();
        Visit(node.Body);
        Dedent();
        Visit(node.Handlers, this.VisitCatchBlock);
        if (node.Finally != null)
        {
            Out(Flow.NewLine, "} .Finally {", Flow.NewLine);
            Indent();
            Visit(node.Finally);
            Dedent();
        }
        else if (node.Fault != null)
        {
            Out(Flow.NewLine, "} .Fault {", Flow.NewLine);
            Indent();
            Visit(node.Fault);
            Dedent();
        }

        Out(Flow.NewLine, "}");
        return node;
    }

    protected internal override Expression VisitTypeBinary(TypeBinaryExpression node)
    {
        ParenthesizedVisit(node, node.Expression);
        switch (node.NodeType)
        {
            case ExpressionType.TypeIs:
                Out(Flow.Space, ".Is", Flow.Space);
                break;
            case ExpressionType.TypeEqual:
                Out(Flow.Space, ".TypeEqual", Flow.Space);
                break;
        }

        Out(node.TypeOperand.ToString());
        return node;
    }

    protected internal override Expression VisitUnary(UnaryExpression node)
    {
        switch (node.NodeType)
        {
            case ExpressionType.Convert:
                Out($"({node.Type})");
                break;
            case ExpressionType.ConvertChecked:
                Out($"#({node.Type})");
                break;
            case ExpressionType.Negate:
                Out("-");
                break;
            case ExpressionType.UnaryPlus:
                Out("+");
                break;
            case ExpressionType.NegateChecked:
                Out("#-");
                break;
            case ExpressionType.Not:
                Out(node.Type == typeof(bool) ? "!" : "~");
                break;
            case ExpressionType.Quote:
                Out("'");
                break;
            case ExpressionType.Decrement:
                Out(".Decrement");
                break;
            case ExpressionType.Increment:
                Out(".Increment");
                break;
            case ExpressionType.Throw:
                if (node.Operand == null)
                {
                    Out(".Rethrow");
                    break;
                }

                Out(".Throw", Flow.Space);
                break;
            case ExpressionType.Unbox:
                Out(".Unbox");
                break;
            case ExpressionType.PreIncrementAssign:
                Out("++");
                break;
            case ExpressionType.PreDecrementAssign:
                Out("--");
                break;
            case ExpressionType.OnesComplement:
                Out("~");
                break;
            case ExpressionType.IsTrue:
                Out(".IsTrue");
                break;
            case ExpressionType.IsFalse:
                Out(".IsFalse");
                break;
        }

        ParenthesizedVisit(node, node.Operand);
        switch (node.NodeType)
        {
            case ExpressionType.ArrayLength:
                Out(".Length");
                break;
            case ExpressionType.TypeAs:
                Out(Flow.Space, ".As", Flow.Space | Flow.Break);
                Out(node.Type.ToString());
                break;
            case ExpressionType.PostIncrementAssign:
                Out("++");
                break;
            case ExpressionType.PostDecrementAssign:
                Out("--");
                break;
        }

        return node;
    }

    protected override ElementInit VisitElementInit(ElementInit node)
    {
        if (node.Arguments.Count == 1)
        {
            Visit(node.Arguments[0]);
        }
        else
        {
            VisitExpressions('{', node.Arguments);
        }

        return node;
    }

    protected override MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
    {
        Out(assignment.Member.Name);
        Out(Flow.Space, "=", Flow.Space);
        Visit(assignment.Expression);
        return assignment;
    }

    protected override MemberListBinding VisitMemberListBinding(MemberListBinding binding)
    {
        Out(binding.Member.Name);
        Out(Flow.Space, "=", Flow.Space);
        VisitExpressions('{', ',', binding.Initializers, e => VisitElementInit(e));
        return binding;
    }

    protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding)
    {
        Out(binding.Member.Name);
        Out(Flow.Space, "=", Flow.Space);
        VisitExpressions('{', ',', binding.Bindings, e => VisitMemberBinding(e));
        return binding;
    }

    internal static void WriteTo(Expression node, TextWriter writer)
    {
        new DebugViewWriter(writer).WriteTo(node);
    }

    private Flow CheckBreak(Flow flow)
    {
        if ((flow & Flow.Break) != Flow.None)
        {
            if (_column > 120 + Depth)
            {
                flow = Flow.NewLine;
            }
            else
            {
                flow &= ~Flow.Break;
            }
        }

        return flow;
    }

    private static bool ContainsWhiteSpace(string name)
    {
        foreach (var c in name)
        {
            if (char.IsWhiteSpace(c))
            {
                return true;
            }
        }

        return false;
    }

    private void Dedent()
    {
        Delta -= 4;
    }

    private void DumpLabel(LabelTarget target)
    {
        Out(string.Format(CultureInfo.CurrentCulture, ".LabelTarget {0}:", GetLabelTargetName(target)));
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
                return "UnaryOperation " + unaryOperationBinder.Operation;
            case BinaryOperationBinder binaryOperationBinder:
                return "BinaryOperation " + binaryOperationBinder.Operation;
            default:
                return binder.ToString();
        }
    }

    private static string GetConstantValueSuffix(Type type)
    {
        if (type == typeof(uint))
        {
            return "U";
        }

        if (type == typeof(long))
        {
            return "L";
        }

        if (type == typeof(ulong))
        {
            return "UL";
        }

        if (type == typeof(double))
        {
            return "D";
        }

        if (type == typeof(float))
        {
            return "F";
        }

        return type == typeof(decimal) ? "M" : null;
    }

    private static string GetDisplayName(string name)
    {
        return ContainsWhiteSpace(name) ? QuoteName(name) : name;
    }

    private Flow GetFlow(Flow flow)
    {
        var val1 = CheckBreak(_flow);
        flow = CheckBreak(flow);
        return (Flow)Math.Max((int)val1, (int)flow);
    }

    private static int GetId<T>(T e, ref Dictionary<T, int> ids)
    {
        if (ids == null)
        {
            ids = new Dictionary<T, int>();
            ids.Add(e, 1);
            return 1;
        }

        int id;
        if (!ids.TryGetValue(e, out id))
        {
            id = ids.Count + 1;
            ids.Add(e, id);
        }

        return id;
    }

    private int GetLabelTargetId(LabelTarget target)
    {
        return GetId(target, ref _labelIds);
    }

    private string GetLabelTargetName(LabelTarget target)
    {
        if (!string.IsNullOrEmpty(target.Name))
        {
            return GetDisplayName(target.Name);
        }

        return string.Format(CultureInfo.CurrentCulture, "#Label{0}", GetLabelTargetId(target));
    }

    private int GetLambdaId(LambdaExpression le)
    {
        return GetId(le, ref _lambdaIds);
    }

    private string GetLambdaName(LambdaExpression lambda)
    {
        return string.IsNullOrEmpty(lambda.Name) ? "#Lambda" + GetLambdaId(lambda) : GetDisplayName(lambda.Name);
    }

    private static int GetOperatorPrecedence(Expression node)
    {
        switch (node.NodeType)
        {
            case ExpressionType.Add:
            case ExpressionType.AddChecked:
            case ExpressionType.Subtract:
            case ExpressionType.SubtractChecked:
                return 10;
            case ExpressionType.And:
                return 6;
            case ExpressionType.AndAlso:
                return 3;
            case ExpressionType.Coalesce:
            case ExpressionType.Assign:
            case ExpressionType.AddAssign:
            case ExpressionType.AndAssign:
            case ExpressionType.DivideAssign:
            case ExpressionType.ExclusiveOrAssign:
            case ExpressionType.LeftShiftAssign:
            case ExpressionType.ModuloAssign:
            case ExpressionType.MultiplyAssign:
            case ExpressionType.OrAssign:
            case ExpressionType.PowerAssign:
            case ExpressionType.RightShiftAssign:
            case ExpressionType.SubtractAssign:
            case ExpressionType.AddAssignChecked:
            case ExpressionType.MultiplyAssignChecked:
            case ExpressionType.SubtractAssignChecked:
                return 1;
            case ExpressionType.Constant:
            case ExpressionType.Parameter:
                return 15;
            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked:
            case ExpressionType.Negate:
            case ExpressionType.UnaryPlus:
            case ExpressionType.NegateChecked:
            case ExpressionType.Not:
            case ExpressionType.Decrement:
            case ExpressionType.Increment:
            case ExpressionType.Throw:
            case ExpressionType.Unbox:
            case ExpressionType.PreIncrementAssign:
            case ExpressionType.PreDecrementAssign:
            case ExpressionType.OnesComplement:
            case ExpressionType.IsTrue:
            case ExpressionType.IsFalse:
                return 12;
            case ExpressionType.Divide:
            case ExpressionType.Modulo:
            case ExpressionType.Multiply:
            case ExpressionType.MultiplyChecked:
                return 11;
            case ExpressionType.Equal:
            case ExpressionType.NotEqual:
                return 7;
            case ExpressionType.ExclusiveOr:
                return 5;
            case ExpressionType.GreaterThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.LessThan:
            case ExpressionType.LessThanOrEqual:
            case ExpressionType.TypeAs:
            case ExpressionType.TypeIs:
            case ExpressionType.TypeEqual:
                return 8;
            case ExpressionType.LeftShift:
            case ExpressionType.RightShift:
                return 9;
            case ExpressionType.Or:
                return 4;
            case ExpressionType.OrElse:
                return 2;
            case ExpressionType.Power:
                return 13;
            default:
                return 14;
        }
    }

    private int GetParamId(ParameterExpression p)
    {
        return GetId(p, ref _paramIds);
    }

    private void Indent()
    {
        Delta += 4;
    }

    private static bool IsSimpleExpression(Expression node)
    {
        return node is BinaryExpression binaryExpression && !(binaryExpression.Left is BinaryExpression) &&
               !(binaryExpression.Right is BinaryExpression);
    }

    private static bool NeedsParentheses(Expression parent, Expression child)
    {
        if (child == null)
        {
            return false;
        }

        switch (parent.NodeType)
        {
            case ExpressionType.Decrement:
            case ExpressionType.Increment:
            case ExpressionType.Unbox:
            case ExpressionType.IsTrue:
            case ExpressionType.IsFalse:
                return true;
            default:
                var operatorPrecedence1 = GetOperatorPrecedence(child);
                var operatorPrecedence2 = GetOperatorPrecedence(parent);
                if (operatorPrecedence1 == operatorPrecedence2)
                {
                    switch (parent.NodeType)
                    {
                        case ExpressionType.Add:
                        case ExpressionType.AddChecked:
                        case ExpressionType.Multiply:
                        case ExpressionType.MultiplyChecked:
                            return false;
                        case ExpressionType.And:
                        case ExpressionType.AndAlso:
                        case ExpressionType.ExclusiveOr:
                        case ExpressionType.Or:
                        case ExpressionType.OrElse:
                            return false;
                        case ExpressionType.Divide:
                        case ExpressionType.Modulo:
                        case ExpressionType.Subtract:
                        case ExpressionType.SubtractChecked:
                            var binaryExpression = parent as BinaryExpression;
                            return child == binaryExpression.Right;
                        default:
                            return true;
                    }
                }

                return (child != null && child.NodeType == ExpressionType.Constant &&
                        (parent.NodeType == ExpressionType.Negate ||
                         parent.NodeType == ExpressionType.NegateChecked)) || operatorPrecedence1 < operatorPrecedence2;
        }
    }

    private void NewLine()
    {
        _flow = Flow.NewLine;
    }

    private void Out(string s)
    {
        Out(Flow.None, s, Flow.None);
    }

    private void Out(Flow before, string s)
    {
        Out(before, s, Flow.None);
    }

    private void Out(string s, Flow after)
    {
        Out(Flow.None, s, after);
    }

    private void Out(Flow before, string s, Flow after)
    {
        switch (GetFlow(before))
        {
            case Flow.Space:
                Write(" ");
                break;
            case Flow.NewLine:
                WriteLine();
                Write(new string(' ', Depth));
                break;
        }

        Write(s);
        _flow = after;
    }

    private void OutMember(Expression node, Expression instance, MemberInfo member)
    {
        if (instance != null)
        {
            ParenthesizedVisit(node, instance);
            Out("." + member.Name);
        }
        else
        {
            Out($"{member.DeclaringType}.{member.Name}");
        }
    }

    private void ParenthesizedVisit(Expression parent, Expression nodeToVisit)
    {
        if (NeedsParentheses(parent, nodeToVisit))
        {
            Out("(");
            Visit(nodeToVisit);
            Out(")");
        }
        else
        {
            Visit(nodeToVisit);
        }
    }

    private static string QuoteName(string name)
    {
        return string.Format(CultureInfo.CurrentCulture, "'{0}'", name);
    }

    private void VisitDeclarations(IList<ParameterExpression> expressions)
    {
        VisitExpressions('(', ',', expressions, variable =>
        {
            Out(variable.Type.ToString());
            if (variable.IsByRef)
            {
                Out("&");
            }

            Out(" ");
            VisitParameter(variable);
        });
    }

    private void VisitExpressions<T>(char open, IList<T> expressions) where T : Expression
    {
        VisitExpressions(open, ',', expressions);
    }

    private void VisitExpressions<T>(char open, char separator, IList<T> expressions) where T : Expression
    {
        VisitExpressions(open, separator, expressions, e => Visit(e));
    }

    private void VisitExpressions<T>(
        char open,
        char separator,
        IList<T> expressions,
        Action<T> visit)
    {
        Out(open.ToString());
        if (expressions != null)
        {
            Indent();
            var flag = true;
            foreach (var expression in expressions)
            {
                if (flag)
                {
                    if (open == '{' || expressions.Count > 1)
                    {
                        NewLine();
                    }

                    flag = false;
                }
                else
                {
                    Out(separator.ToString(), Flow.NewLine);
                }

                visit(expression);
            }

            Dedent();
        }

        char ch;
        switch (open)
        {
            case '(':
                ch = ')';
                break;
            case '<':
                ch = '>';
                break;
            case '[':
                ch = ']';
                break;
            case '{':
                ch = '}';
                break;
            default:
                throw ContractUtils.Unreachable;
        }

        if (open == '{')
        {
            NewLine();
        }

        Out(ch.ToString(), Flow.Break);
    }

    private void Write(string s)
    {
        _out.Write(s);
        _column += s.Length;
    }

    private void WriteLambda(LambdaExpression lambda)
    {
        Out(string.Format(CultureInfo.CurrentCulture, ".Lambda {0}<{1}>", GetLambdaName(lambda),
            lambda.Type.ToString()));
        VisitDeclarations(lambda.Parameters);
        Out(Flow.Space, "{", Flow.NewLine);
        Indent();
        Visit(lambda.Body);
        Dedent();
        Out(Flow.NewLine, "}");
    }

    private void WriteLine()
    {
        _out.WriteLine();
        _column = 0;
    }

    private void WriteTo(Expression node)
    {
        if (node is LambdaExpression lambda)
        {
            WriteLambda(lambda);
        }
        else
        {
            Visit(node);
        }

        while (_lambdas != null && _lambdas.Count > 0)
        {
            WriteLine();
            WriteLine();
            WriteLambda(_lambdas.Dequeue());
        }
    }

    [Flags]
    private enum Flow
    {
        None = 0,
        Space = 1,
        NewLine = 2,
        Break = 32768 // 0x00008000
    }
}