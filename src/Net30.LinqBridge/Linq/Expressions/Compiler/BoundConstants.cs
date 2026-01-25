#nullable disable
using System.Collections.Generic;
using System.Dynamic.Utils;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Compiler;

internal sealed class BoundConstants
{
    private readonly Dictionary<TypedConstant, LocalBuilder> _cache = new();
    private readonly Dictionary<object, int> _indexes = new(ReferenceEqualityComparer<object>.Instance);
    private readonly Dictionary<TypedConstant, int> _references = new();
    private readonly List<object> _values = new();

    internal int Count => _values.Count;

    internal void AddReference(object value, Type type)
    {
        if (!_indexes.ContainsKey(value))
        {
            _indexes.Add(value, _values.Count);
            _values.Add(value);
        }

        Helpers.IncrementCount(new TypedConstant(value, type), _references);
    }

    internal void EmitCacheConstants(LambdaCompiler lc)
    {
        var num = 0;
        foreach (var reference in _references)
        {
            if (!lc.CanEmitBoundConstants)
            {
                throw Error.CannotCompileConstant(reference.Key.Value);
            }

            if (ShouldCache(reference.Value))
            {
                ++num;
            }
        }

        if (num == 0)
        {
            return;
        }

        EmitConstantsArray(lc);
        _cache.Clear();
        foreach (var reference in _references)
        {
            if (ShouldCache(reference.Value))
            {
                if (--num > 0)
                {
                    lc.IL.Emit(OpCodes.Dup);
                }

                var local = lc.IL.DeclareLocal(reference.Key.Type);
                EmitConstantFromArray(lc, reference.Key.Value, local.LocalType);
                lc.IL.Emit(OpCodes.Stloc, local);
                _cache.Add(reference.Key, local);
            }
        }
    }

    internal void EmitConstant(LambdaCompiler lc, object value, Type type)
    {
        if (!lc.CanEmitBoundConstants)
        {
            throw Error.CannotCompileConstant(value);
        }

        LocalBuilder local;
        if (_cache.TryGetValue(new TypedConstant(value, type), out local))
        {
            lc.IL.Emit(OpCodes.Ldloc, local);
        }
        else
        {
            EmitConstantsArray(lc);
            EmitConstantFromArray(lc, value, type);
        }
    }

    internal object[] ToArray()
    {
        return _values.ToArray();
    }

    private void EmitConstantFromArray(LambdaCompiler lc, object value, Type type)
    {
        int count;
        if (!_indexes.TryGetValue(value, out count))
        {
            _indexes.Add(value, count = _values.Count);
            _values.Add(value);
        }

        lc.IL.EmitInt(count);
        lc.IL.Emit(OpCodes.Ldelem_Ref);
        if (type.IsValueType)
        {
            lc.IL.Emit(OpCodes.Unbox_Any, type);
        }
        else
        {
            if (!(type != typeof(object)))
            {
                return;
            }

            lc.IL.Emit(OpCodes.Castclass, type);
        }
    }

    private static void EmitConstantsArray(LambdaCompiler lc)
    {
        lc.EmitClosureArgument();
        lc.IL.Emit(OpCodes.Ldfld, typeof(Closure).GetField("Constants"));
    }

    private static bool ShouldCache(int refCount)
    {
        return refCount > 2;
    }

    private struct TypedConstant : IEquatable<TypedConstant>
    {
        internal readonly object Value;
        internal readonly Type Type;

        internal TypedConstant(object value, Type type)
        {
            Value = value;
            Type = type;
        }

        public override int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(Value) ^ Type.GetHashCode();
        }

        public bool Equals(TypedConstant other)
        {
            return Value == other.Value && Type.Equals(other.Type);
        }

        public override bool Equals(object obj)
        {
            return obj is TypedConstant other && Equals(other);
        }
    }
}