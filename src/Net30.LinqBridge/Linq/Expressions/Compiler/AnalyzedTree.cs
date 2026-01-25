#nullable disable
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Compiler;

internal sealed class AnalyzedTree
{
    internal readonly Dictionary<LambdaExpression, BoundConstants> Constants = new();
    internal readonly Dictionary<object, CompilerScope> Scopes = new();

    internal DebugInfoGenerator DebugInfoGenerator { get; set; }
}