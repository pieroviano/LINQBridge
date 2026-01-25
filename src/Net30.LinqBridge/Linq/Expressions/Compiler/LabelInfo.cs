#nullable disable
using System.Collections.Generic;
using System.Dynamic.Utils;
using System.Reflection.Emit;

namespace System.Linq.Expressions.Compiler;

internal sealed class LabelInfo
{
    private readonly Set<LabelScopeInfo> _definitions = new();
    private readonly ILGenerator _ilg;
    private readonly LabelTarget _node;
    private readonly List<LabelScopeInfo> _references = new();
    private bool _acrossBlockJump;
    private Label _label;
    private bool _labelDefined;
    private OpCode _opCode = OpCodes.Leave;
    private LocalBuilder _value;

    internal LabelInfo(ILGenerator il, LabelTarget node, bool canReturn)
    {
        _ilg = il;
        _node = node;
        CanReturn = canReturn;
    }

    internal Label Label
    {
        get
        {
            EnsureLabelAndValue();
            return _label;
        }
    }

    internal bool CanReturn { get; }

    internal bool CanBranch => _opCode != OpCodes.Leave;

    internal void Define(LabelScopeInfo block)
    {
        for (var labelScopeInfo = block; labelScopeInfo != null; labelScopeInfo = labelScopeInfo.Parent)
        {
            if (labelScopeInfo.ContainsTarget(_node))
            {
                throw Error.LabelTargetAlreadyDefined(_node.Name);
            }
        }

        _definitions.Add(block);
        block.AddLabelInfo(_node, this);
        if (_definitions.Count == 1)
        {
            foreach (var reference in _references)
            {
                ValidateJump(reference);
            }
        }
        else
        {
            if (_acrossBlockJump)
            {
                throw Error.AmbiguousJump(_node.Name);
            }

            _labelDefined = false;
        }
    }

    internal void EmitJump()
    {
        if (_opCode == OpCodes.Ret)
        {
            _ilg.Emit(OpCodes.Ret);
        }
        else
        {
            StoreValue();
            _ilg.Emit(_opCode, Label);
        }
    }

    internal void Mark()
    {
        if (CanReturn)
        {
            if (!_labelDefined)
            {
                return;
            }

            _ilg.Emit(OpCodes.Ret);
        }
        else
        {
            StoreValue();
        }

        MarkWithEmptyStack();
    }

    internal void MarkWithEmptyStack()
    {
        _ilg.MarkLabel(Label);
        if (_value == null)
        {
            return;
        }

        _ilg.Emit(OpCodes.Ldloc, _value);
    }

    internal void Reference(LabelScopeInfo block)
    {
        _references.Add(block);
        if (_definitions.Count <= 0)
        {
            return;
        }

        ValidateJump(block);
    }

    internal void ValidateFinish()
    {
        if (_references.Count > 0 && _definitions.Count == 0)
        {
            throw Error.LabelTargetUndefined(_node.Name);
        }
    }

    private void EnsureLabelAndValue()
    {
        if (_labelDefined)
        {
            return;
        }

        _labelDefined = true;
        _label = _ilg.DefineLabel();
        if (_node == null || !(_node.Type != typeof(void)))
        {
            return;
        }

        _value = _ilg.DeclareLocal(_node.Type);
    }

    private void StoreValue()
    {
        EnsureLabelAndValue();
        if (_value == null)
        {
            return;
        }

        _ilg.Emit(OpCodes.Stloc, _value);
    }

    private void ValidateJump(LabelScopeInfo reference)
    {
        _opCode = CanReturn ? OpCodes.Ret : OpCodes.Br;
        for (var labelScopeInfo = reference; labelScopeInfo != null; labelScopeInfo = labelScopeInfo.Parent)
        {
            if (_definitions.Contains(labelScopeInfo))
            {
                return;
            }

            if (labelScopeInfo.Kind != LabelScopeKind.Finally && labelScopeInfo.Kind != LabelScopeKind.Filter)
            {
                if (labelScopeInfo.Kind == LabelScopeKind.Try || labelScopeInfo.Kind == LabelScopeKind.Catch)
                {
                    _opCode = OpCodes.Leave;
                }
            }
            else
            {
                break;
            }
        }

        _acrossBlockJump = true;
        if (_node != null && _node.Type != typeof(void))
        {
            throw Error.NonLocalJumpWithValue(_node.Name);
        }

        var first = _definitions.Count <= 1 ? _definitions.First() : throw Error.AmbiguousJump(_node.Name);
        var labelScopeInfo1 =
            Helpers.CommonNode(first, reference, (Func<LabelScopeInfo, LabelScopeInfo>)(b => b.Parent));
        _opCode = CanReturn ? OpCodes.Ret : OpCodes.Br;
        for (var labelScopeInfo2 = reference;
             labelScopeInfo2 != labelScopeInfo1;
             labelScopeInfo2 = labelScopeInfo2.Parent)
        {
            if (labelScopeInfo2.Kind == LabelScopeKind.Finally)
            {
                throw Error.ControlCannotLeaveFinally();
            }

            if (labelScopeInfo2.Kind == LabelScopeKind.Filter)
            {
                throw Error.ControlCannotLeaveFilterTest();
            }

            if (labelScopeInfo2.Kind == LabelScopeKind.Try || labelScopeInfo2.Kind == LabelScopeKind.Catch)
            {
                _opCode = OpCodes.Leave;
            }
        }

        for (var labelScopeInfo3 = first; labelScopeInfo3 != labelScopeInfo1; labelScopeInfo3 = labelScopeInfo3.Parent)
        {
            if (!labelScopeInfo3.CanJumpInto)
            {
                if (labelScopeInfo3.Kind == LabelScopeKind.Expression)
                {
                    throw Error.ControlCannotEnterExpression();
                }

                throw Error.ControlCannotEnterTry();
            }
        }
    }
}