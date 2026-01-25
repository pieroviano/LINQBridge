#nullable disable
using System.Collections.Generic;

namespace System.Linq.Expressions.Compiler;

internal sealed class LabelScopeInfo
{
    internal readonly LabelScopeKind Kind;
    internal readonly LabelScopeInfo Parent;
    private Dictionary<LabelTarget, LabelInfo> Labels;

    internal LabelScopeInfo(LabelScopeInfo parent, LabelScopeKind kind)
    {
        Parent = parent;
        Kind = kind;
    }

    internal bool CanJumpInto
    {
        get
        {
            switch (Kind)
            {
                case LabelScopeKind.Statement:
                case LabelScopeKind.Block:
                case LabelScopeKind.Switch:
                case LabelScopeKind.Lambda:
                    return true;
                default:
                    return false;
            }
        }
    }

    internal void AddLabelInfo(LabelTarget target, LabelInfo info)
    {
        if (Labels == null)
        {
            Labels = new Dictionary<LabelTarget, LabelInfo>();
        }

        Labels.Add(target, info);
    }

    internal bool ContainsTarget(LabelTarget target)
    {
        return Labels != null && Labels.ContainsKey(target);
    }

    internal bool TryGetLabelInfo(LabelTarget target, out LabelInfo info)
    {
        if (Labels != null)
        {
            return Labels.TryGetValue(target, out info);
        }

        info = null;
        return false;
    }
}