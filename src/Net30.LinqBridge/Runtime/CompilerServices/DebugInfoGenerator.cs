using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace System.Runtime.CompilerServices;

/// <summary>Generates debug information for lambda expressions in an expression tree.</summary>
public abstract class DebugInfoGenerator
{
    /// <summary>Initializes a new instance of the <see cref="T:System.Runtime.CompilerServices.DebugInfoGenerator" /> class.</summary>
    protected DebugInfoGenerator()
    {
    }

    /// <summary>Creates a program database (PDB) symbol generator.</summary>
    /// <returns>A PDB symbol generator.</returns>
    public static DebugInfoGenerator CreatePdbGenerator()
    {
        return new SymbolDocumentGenerator();
    }

    /// <summary>Marks a sequence point in Microsoft intermediate language (MSIL) code.</summary>
    /// <param name="method">The lambda expression that is generated.</param>
    /// <param name="ilOffset">The offset within MSIL code at which to mark the sequence point.</param>
    /// <param name="sequencePoint">Debug information that corresponds to the sequence point.</param>
    public abstract void MarkSequencePoint(LambdaExpression method, int ilOffset, DebugInfoExpression sequencePoint);

    internal virtual void MarkSequencePoint(LambdaExpression method, MethodBase methodBase, ILGenerator ilg,
        DebugInfoExpression sequencePoint)
    {
        MarkSequencePoint(method, PrivateReader.GetLength(ilg), sequencePoint);
    }

    internal virtual void SetLocalName(LocalBuilder localBuilder, string name)
    {
    }
}