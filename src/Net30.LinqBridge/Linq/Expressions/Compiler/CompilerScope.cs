#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Compiler;

internal sealed class CompilerScope
{
    private readonly Dictionary<ParameterExpression, Storage> _locals = new();
    internal readonly Dictionary<ParameterExpression, VariableStorageKind> Definitions = new();
    internal readonly bool IsMethod;
    internal readonly object Node;
    private HoistedLocals _closureHoistedLocals;
    private HoistedLocals _hoistedLocals;
    private CompilerScope _parent;
    internal Set<object> MergedScopes;
    internal bool NeedsClosure;
    internal Dictionary<ParameterExpression, int> ReferenceCount;

    internal CompilerScope(object node, bool isMethod)
    {
        Node = node;
        IsMethod = isMethod;
        var variables = GetVariables(node);
        Definitions = new Dictionary<ParameterExpression, VariableStorageKind>(variables.Count);
        foreach (var key in variables)
        {
            Definitions.Add(key, VariableStorageKind.Local);
        }
    }

    internal HoistedLocals NearestHoistedLocals => _hoistedLocals ?? _closureHoistedLocals;

    private string CurrentLambdaName
    {
        get
        {
            LambdaExpression node;
            var compilerScope = this;
            do
            {
                node = compilerScope.Node as LambdaExpression;
            } while (node == null);

            return node.Name;
        }
    }

    internal void AddLocal(LambdaCompiler gen, ParameterExpression variable)
    {
        _locals.Add(variable, new LocalStorage(gen, variable));
    }

    internal void EmitAddressOf(ParameterExpression variable)
    {
        ResolveVariable(variable).EmitAddress();
    }

    internal void EmitGet(ParameterExpression variable)
    {
        ResolveVariable(variable).EmitLoad();
    }

    internal void EmitSet(ParameterExpression variable)
    {
        ResolveVariable(variable).EmitStore();
    }

    internal void EmitVariableAccess(LambdaCompiler lc, ReadOnlyCollection<ParameterExpression> vars)
    {
        if (NearestHoistedLocals != null)
        {
            var longList = new List<long>(vars.Count);
            foreach (var var in vars)
            {
                ulong num1 = 0;
                HoistedLocals hoistedLocals;
                for (hoistedLocals = NearestHoistedLocals;
                     !hoistedLocals.Indexes.ContainsKey(var);
                     hoistedLocals = hoistedLocals.Parent)
                {
                    ++num1;
                }

                var num2 = (num1 << 32) /*0x20*/ | (uint)hoistedLocals.Indexes[var];
                longList.Add((long)num2);
            }

            if (longList.Count > 0)
            {
                EmitGet(NearestHoistedLocals.SelfVariable);
                lc.EmitConstantArray(longList.ToArray());
                lc.IL.Emit(OpCodes.Call, typeof(RuntimeOps).GetMethod("CreateRuntimeVariables", new Type[2]
                {
                    typeof(object[]),
                    typeof(long[])
                }));
                return;
            }
        }

        lc.IL.Emit(OpCodes.Call, typeof(RuntimeOps).GetMethod("CreateRuntimeVariables", Type.EmptyTypes));
    }

    internal CompilerScope Enter(LambdaCompiler lc, CompilerScope parent)
    {
        SetParent(lc, parent);
        AllocateLocals(lc);
        if (IsMethod && _closureHoistedLocals != null)
        {
            EmitClosureAccess(lc, _closureHoistedLocals);
        }

        EmitNewHoistedLocals(lc);
        if (IsMethod)
        {
            EmitCachedVariables();
        }

        return this;
    }

    internal CompilerScope Exit()
    {
        if (!IsMethod)
        {
            foreach (var storage in _locals.Values)
            {
                storage.FreeLocal();
            }
        }

        var parent = _parent;
        _parent = null;
        _hoistedLocals = null;
        _closureHoistedLocals = null;
        _locals.Clear();
        return parent;
    }

    private void AllocateLocals(LambdaCompiler lc)
    {
        foreach (var variable in GetVariables())
        {
            if (Definitions[variable] == VariableStorageKind.Local)
            {
                var storage = !IsMethod || !lc.Parameters.Contains(variable)
                    ? new LocalStorage(lc, variable)
                    : (Storage)new ArgumentStorage(lc, variable);
                _locals.Add(variable, storage);
            }
        }
    }

    private void CacheBoxToLocal(LambdaCompiler lc, ParameterExpression v)
    {
        var localBoxStorage = new LocalBoxStorage(lc, v);
        localBoxStorage.EmitStoreBox();
        _locals.Add(v, localBoxStorage);
    }

    private void EmitCachedVariables()
    {
        if (ReferenceCount == null)
        {
            return;
        }

        foreach (var keyValuePair in ReferenceCount)
        {
            if (ShouldCache(keyValuePair.Key, keyValuePair.Value) &&
                ResolveVariable(keyValuePair.Key) is ElementBoxStorage elementBoxStorage)
            {
                elementBoxStorage.EmitLoadBox();
                CacheBoxToLocal(elementBoxStorage.Compiler, keyValuePair.Key);
            }
        }
    }

    private void EmitClosureAccess(LambdaCompiler lc, HoistedLocals locals)
    {
        if (locals == null)
        {
            return;
        }

        EmitClosureToVariable(lc, locals);
        while ((locals = locals.Parent) != null)
        {
            var selfVariable = locals.SelfVariable;
            var localStorage = new LocalStorage(lc, selfVariable);
            localStorage.EmitStore(ResolveVariable(selfVariable));
            _locals.Add(selfVariable, localStorage);
        }
    }

    private void EmitClosureToVariable(LambdaCompiler lc, HoistedLocals locals)
    {
        lc.EmitClosureArgument();
        lc.IL.Emit(OpCodes.Ldfld, typeof(Closure).GetField("Locals"));
        AddLocal(lc, locals.SelfVariable);
        EmitSet(locals.SelfVariable);
    }

    private void EmitNewHoistedLocals(LambdaCompiler lc)
    {
        if (_hoistedLocals == null)
        {
            return;
        }

        lc.IL.EmitInt(_hoistedLocals.Variables.Count);
        lc.IL.Emit(OpCodes.Newarr, typeof(object));
        var num = 0;
        foreach (var variable in _hoistedLocals.Variables)
        {
            lc.IL.Emit(OpCodes.Dup);
            lc.IL.EmitInt(num++);
            var type = typeof(StrongBox<>).MakeGenericType(variable.Type);
            if (IsMethod && lc.Parameters.Contains(variable))
            {
                var index = lc.Parameters.IndexOf(variable);
                lc.EmitLambdaArgument(index);
                lc.IL.Emit(OpCodes.Newobj, type.GetConstructor(new Type[1]
                {
                    variable.Type
                }));
            }
            else if (variable == _hoistedLocals.ParentVariable)
            {
                ResolveVariable(variable, _closureHoistedLocals).EmitLoad();
                lc.IL.Emit(OpCodes.Newobj, type.GetConstructor(new Type[1]
                {
                    variable.Type
                }));
            }
            else
            {
                lc.IL.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
            }

            if (ShouldCache(variable))
            {
                lc.IL.Emit(OpCodes.Dup);
                CacheBoxToLocal(lc, variable);
            }

            lc.IL.Emit(OpCodes.Stelem_Ref);
        }

        EmitSet(_hoistedLocals.SelfVariable);
    }

    private IList<ParameterExpression> GetVariables()
    {
        var variables1 = GetVariables(Node);
        if (MergedScopes == null)
        {
            return variables1;
        }

        var variables2 = new List<ParameterExpression>(variables1);
        foreach (var mergedScope in MergedScopes)
        {
            variables2.AddRange(GetVariables(mergedScope));
        }

        return variables2;
    }

    private static IList<ParameterExpression> GetVariables(object scope)
    {
        switch (scope)
        {
            case LambdaExpression lambdaExpression:
                return lambdaExpression.Parameters;
            case BlockExpression blockExpression:
                return blockExpression.Variables;
            default:
                return new ParameterExpression[1]
                {
                    ((CatchBlock)scope).Variable
                };
        }
    }

    private Storage ResolveVariable(ParameterExpression variable)
    {
        return ResolveVariable(variable, NearestHoistedLocals);
    }

    private Storage ResolveVariable(
        ParameterExpression variable,
        HoistedLocals hoistedLocals)
    {
        for (var compilerScope = this; compilerScope != null; compilerScope = compilerScope._parent)
        {
            Storage storage;
            if (compilerScope._locals.TryGetValue(variable, out storage))
            {
                return storage;
            }

            if (compilerScope.IsMethod)
            {
                break;
            }
        }

        for (var hoistedLocals1 = hoistedLocals; hoistedLocals1 != null; hoistedLocals1 = hoistedLocals1.Parent)
        {
            int index;
            if (hoistedLocals1.Indexes.TryGetValue(variable, out index))
            {
                return new ElementBoxStorage(ResolveVariable(hoistedLocals1.SelfVariable, hoistedLocals), index,
                    variable);
            }
        }

        throw Error.UndefinedVariable(variable.Name, variable.Type, CurrentLambdaName);
    }

    private void SetParent(LambdaCompiler lc, CompilerScope parent)
    {
        _parent = parent;
        if (NeedsClosure && _parent != null)
        {
            _closureHoistedLocals = _parent.NearestHoistedLocals;
        }

        var vars = GetVariables().Where(p => Definitions[p] == VariableStorageKind.Hoisted).ToReadOnly();
        if (vars.Count <= 0)
        {
            return;
        }

        _hoistedLocals = new HoistedLocals(_closureHoistedLocals, vars);
        AddLocal(lc, _hoistedLocals.SelfVariable);
    }

    private bool ShouldCache(ParameterExpression v, int refCount)
    {
        return refCount > 2 && !_locals.ContainsKey(v);
    }

    private bool ShouldCache(ParameterExpression v)
    {
        int refCount;
        return ReferenceCount != null && ReferenceCount.TryGetValue(v, out refCount) && ShouldCache(v, refCount);
    }

    private abstract class Storage
    {
        internal readonly LambdaCompiler Compiler;
        internal readonly ParameterExpression Variable;

        internal Storage(LambdaCompiler compiler, ParameterExpression variable)
        {
            Compiler = compiler;
            Variable = variable;
        }

        internal abstract void EmitAddress();

        internal abstract void EmitLoad();

        internal abstract void EmitStore();

        internal virtual void EmitStore(Storage value)
        {
            value.EmitLoad();
            EmitStore();
        }

        internal virtual void FreeLocal()
        {
        }
    }

    private sealed class LocalStorage : Storage
    {
        private readonly LocalBuilder _local;

        internal LocalStorage(LambdaCompiler compiler, ParameterExpression variable)
            : base(compiler, variable)
        {
            _local = compiler.GetNamedLocal(variable.IsByRef ? variable.Type.MakeByRefType() : variable.Type, variable);
        }

        internal override void EmitAddress()
        {
            Compiler.IL.Emit(OpCodes.Ldloca, _local);
        }

        internal override void EmitLoad()
        {
            Compiler.IL.Emit(OpCodes.Ldloc, _local);
        }

        internal override void EmitStore()
        {
            Compiler.IL.Emit(OpCodes.Stloc, _local);
        }
    }

    private sealed class ArgumentStorage : Storage
    {
        private readonly int _argument;

        internal ArgumentStorage(LambdaCompiler compiler, ParameterExpression p)
            : base(compiler, p)
        {
            _argument = compiler.GetLambdaArgument(compiler.Parameters.IndexOf(p));
        }

        internal override void EmitAddress()
        {
            Compiler.IL.EmitLoadArgAddress(_argument);
        }

        internal override void EmitLoad()
        {
            Compiler.IL.EmitLoadArg(_argument);
        }

        internal override void EmitStore()
        {
            Compiler.IL.EmitStoreArg(_argument);
        }
    }

    private sealed class ElementBoxStorage : Storage
    {
        private readonly Storage _array;
        private readonly Type _boxType;
        private readonly FieldInfo _boxValueField;
        private readonly int _index;

        internal ElementBoxStorage(
            Storage array,
            int index,
            ParameterExpression variable)
            : base(array.Compiler, variable)
        {
            _array = array;
            _index = index;
            _boxType = typeof(StrongBox<>).MakeGenericType(variable.Type);
            _boxValueField = _boxType.GetField("Value");
        }

        internal override void EmitAddress()
        {
            EmitLoadBox();
            Compiler.IL.Emit(OpCodes.Ldflda, _boxValueField);
        }

        internal override void EmitLoad()
        {
            EmitLoadBox();
            Compiler.IL.Emit(OpCodes.Ldfld, _boxValueField);
        }

        internal void EmitLoadBox()
        {
            _array.EmitLoad();
            Compiler.IL.EmitInt(_index);
            Compiler.IL.Emit(OpCodes.Ldelem_Ref);
            Compiler.IL.Emit(OpCodes.Castclass, _boxType);
        }

        internal override void EmitStore()
        {
            var local = Compiler.GetLocal(Variable.Type);
            Compiler.IL.Emit(OpCodes.Stloc, local);
            EmitLoadBox();
            Compiler.IL.Emit(OpCodes.Ldloc, local);
            Compiler.FreeLocal(local);
            Compiler.IL.Emit(OpCodes.Stfld, _boxValueField);
        }

        internal override void EmitStore(Storage value)
        {
            EmitLoadBox();
            value.EmitLoad();
            Compiler.IL.Emit(OpCodes.Stfld, _boxValueField);
        }
    }

    private sealed class LocalBoxStorage : Storage
    {
        private readonly LocalBuilder _boxLocal;
        private readonly Type _boxType;
        private readonly FieldInfo _boxValueField;

        internal LocalBoxStorage(LambdaCompiler compiler, ParameterExpression variable)
            : base(compiler, variable)
        {
            _boxType = typeof(StrongBox<>).MakeGenericType(variable.Type);
            _boxValueField = _boxType.GetField("Value");
            _boxLocal = compiler.GetNamedLocal(_boxType, variable);
        }

        internal override void EmitAddress()
        {
            Compiler.IL.Emit(OpCodes.Ldloc, _boxLocal);
            Compiler.IL.Emit(OpCodes.Ldflda, _boxValueField);
        }

        internal override void EmitLoad()
        {
            Compiler.IL.Emit(OpCodes.Ldloc, _boxLocal);
            Compiler.IL.Emit(OpCodes.Ldfld, _boxValueField);
        }

        internal override void EmitStore()
        {
            var local = Compiler.GetLocal(Variable.Type);
            Compiler.IL.Emit(OpCodes.Stloc, local);
            Compiler.IL.Emit(OpCodes.Ldloc, _boxLocal);
            Compiler.IL.Emit(OpCodes.Ldloc, local);
            Compiler.FreeLocal(local);
            Compiler.IL.Emit(OpCodes.Stfld, _boxValueField);
        }

        internal override void EmitStore(Storage value)
        {
            Compiler.IL.Emit(OpCodes.Ldloc, _boxLocal);
            value.EmitLoad();
            Compiler.IL.Emit(OpCodes.Stfld, _boxValueField);
        }

        internal void EmitStoreBox()
        {
            Compiler.IL.Emit(OpCodes.Stloc, _boxLocal);
        }
    }
}