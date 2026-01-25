#nullable disable
namespace System.Linq.Expressions;

internal sealed class SpanDebugInfoExpression : DebugInfoExpression
{
    internal SpanDebugInfoExpression(
        SymbolDocumentInfo document,
        int startLine,
        int startColumn,
        int endLine,
        int endColumn)
        : base(document)
    {
        StartLine = startLine;
        StartColumn = startColumn;
        EndLine = endLine;
        EndColumn = endColumn;
    }

    public override int StartLine { get; }

    public override int StartColumn { get; }

    public override int EndLine { get; }

    public override int EndColumn { get; }

    public override bool IsClear => false;

    protected internal override Expression Accept(ExpressionVisitor visitor)
    {
        return visitor.VisitDebugInfo(this);
    }
}