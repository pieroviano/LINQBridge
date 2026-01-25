#nullable disable
namespace System.Linq.Expressions;

internal sealed class ClearDebugInfoExpression : DebugInfoExpression
{
    internal ClearDebugInfoExpression(SymbolDocumentInfo document)
        : base(document)
    {
    }

    public override bool IsClear => true;

    public override int StartLine => 16707566 /*0xFEEFEE*/;

    public override int StartColumn => 0;

    public override int EndLine => 16707566 /*0xFEEFEE*/;

    public override int EndColumn => 0;
}