#nullable disable
using System.Linq.Expressions.Compiler;

namespace System.Linq.Expressions;

internal sealed class SymbolDocumentWithGuids : SymbolDocumentInfo
{
    internal SymbolDocumentWithGuids(string fileName, ref Guid language)
        : base(fileName)
    {
        Language = language;
        DocumentType = SymbolGuids.DocumentType_Text;
    }

    internal SymbolDocumentWithGuids(string fileName, ref Guid language, ref Guid vendor)
        : base(fileName)
    {
        Language = language;
        LanguageVendor = vendor;
        DocumentType = SymbolGuids.DocumentType_Text;
    }

    internal SymbolDocumentWithGuids(
        string fileName,
        ref Guid language,
        ref Guid vendor,
        ref Guid documentType)
        : base(fileName)
    {
        Language = language;
        LanguageVendor = vendor;
        DocumentType = documentType;
    }

    public override Guid Language { get; }

    public override Guid LanguageVendor { get; }

    public override Guid DocumentType { get; }
}