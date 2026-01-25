using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Linq.Expressions;
using System.Linq.Expressions.Compiler;
using System.Reflection;
using System.Reflection.Emit;

namespace System.Runtime.CompilerServices;

internal sealed class SymbolDocumentGenerator : DebugInfoGenerator
{
    private Dictionary<SymbolDocumentInfo, ISymbolDocumentWriter> _symbolWriters;

    public override void MarkSequencePoint(LambdaExpression method, int ilOffset, DebugInfoExpression sequencePoint)
    {
        throw Error.PdbGeneratorNeedsExpressionCompiler();
    }

    internal override void MarkSequencePoint(LambdaExpression method, MethodBase methodBase, ILGenerator ilg,
        DebugInfoExpression sequencePoint)
    {
        var methodBuilder = methodBase as MethodBuilder;
        if (methodBuilder != null)
        {
            ilg.MarkSequencePoint(GetSymbolWriter(methodBuilder, sequencePoint.Document), sequencePoint.StartLine,
                sequencePoint.StartColumn, sequencePoint.EndLine, sequencePoint.EndColumn);
        }
    }

    internal override void SetLocalName(LocalBuilder localBuilder, string name)
    {
        localBuilder.SetLocalSymInfo(name);
    }

    private ISymbolDocumentWriter GetSymbolWriter(MethodBuilder method, SymbolDocumentInfo document)
    {
        ISymbolDocumentWriter symbolDocumentWriter;
        if (_symbolWriters == null)
        {
            _symbolWriters = new Dictionary<SymbolDocumentInfo, ISymbolDocumentWriter>();
        }

        if (!_symbolWriters.TryGetValue(document, out symbolDocumentWriter))
        {
            symbolDocumentWriter = ((ModuleBuilder)method.Module).DefineDocument(document.FileName, document.Language,
                document.LanguageVendor, SymbolGuids.DocumentType_Text);
            _symbolWriters.Add(document, symbolDocumentWriter);
        }

        return symbolDocumentWriter;
    }
}