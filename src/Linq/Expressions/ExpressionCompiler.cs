using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions;

internal class ExpressionCompiler
{
    private List<LambdaInfo> lambdas;
    private List<object> globals;
    private CompileScope scope;

    internal ExpressionCompiler()
    {
        lambdas = new List<LambdaInfo>();
        globals = new List<object>();
    }

    public D Compile<D>(Expression<D> lambda) where D:Delegate
    {
        if (!typeof(Delegate).IsAssignableFrom(typeof(D)))
            throw Error.TypeParameterIsNotDelegate(typeof(D));
        return (D)Compile((LambdaExpression)lambda);
    }

    public Delegate Compile(LambdaExpression lambda) => CompileDynamicLambda(lambda);

    private Delegate CompileDynamicLambda(LambdaExpression lambda)
    {
        lambdas = new List<LambdaInfo>();
        globals = new List<object>();
        var lambda1 = lambdas[GenerateLambda(lambda)];
        var target = new ExecutionScope(null, lambda1, globals.ToArray(), null);
        return ((DynamicMethod)lambda1.Method).CreateDelegate(lambda.Type, target);
    }

    private static void GenerateLoadExecutionScope(ILGenerator gen) => gen.Emit(OpCodes.Ldarg_0);

    private void GenerateLoadHoistedLocals(ILGenerator gen) => gen.Emit(OpCodes.Ldloc, scope.HoistedLocalsVar);

    private int GenerateLambda(LambdaExpression lambda)
    {
        scope = new CompileScope(scope, lambda);
        var method1 = lambda.Type.GetMethod("Invoke");
        new Hoister().Hoist(scope);
        var dynamicMethod = new DynamicMethod("lambda_method", method1.ReturnType, GetParameterTypes(method1), true);
        var ilGenerator = dynamicMethod.GetILGenerator();
        MethodInfo method2 = dynamicMethod;
        GenerateInitHoistedLocals(ilGenerator);
        var num = (int)Generate(ilGenerator, lambda.Body, StackType.Value);
        if (method1.ReturnType == typeof(void) && lambda.Body.Type != typeof(void))
            ilGenerator.Emit(OpCodes.Pop);
        ilGenerator.Emit(OpCodes.Ret);
        var count = lambdas.Count;
        lambdas.Add(new LambdaInfo(lambda, method2, scope.HoistedLocals, lambdas));
        scope = scope.Parent;
        return count;
    }

    private void GenerateInitHoistedLocals(ILGenerator gen)
    {
        if (scope.HoistedLocals.Count == 0)
            return;
        scope.HoistedLocalsVar = gen.DeclareLocal(typeof(object[]));
        GenerateLoadExecutionScope(gen);
        gen.Emit(OpCodes.Callvirt, typeof(ExecutionScope).GetMethod("CreateHoistedLocals", BindingFlags.Instance | BindingFlags.Public));
        gen.Emit(OpCodes.Stloc, scope.HoistedLocalsVar);
        var count = scope.Lambda.Parameters.Count;
        for (var index = 0; index < count; ++index)
        {
            var parameter = scope.Lambda.Parameters[index];
            if (IsHoisted(parameter))
            {
                PrepareInitLocal(gen, parameter);
                var argAccess = (int)GenerateArgAccess(gen, index + 1, StackType.Value);
                GenerateInitLocal(gen, parameter);
            }
        }
    }

    private bool IsHoisted(ParameterExpression p) => scope.HoistedLocals.ContainsKey(p);

    private void PrepareInitLocal(ILGenerator gen, ParameterExpression p)
    {
        int num;
        if (scope.HoistedLocals.TryGetValue(p, out num))
        {
            GenerateLoadHoistedLocals(gen);
            GenerateConstInt(gen, num);
        }
        else
        {
            var localBuilder = gen.DeclareLocal(p.Type);
            scope.Locals.Add(p, localBuilder);
        }
    }

    private static Type MakeStrongBoxType(Type type) => typeof(StrongBox<>).MakeGenericType(type);

    private void GenerateInitLocal(ILGenerator gen, ParameterExpression p)
    {
        if (scope.HoistedLocals.TryGetValue(p, out var _))
        {
            var constructor = MakeStrongBoxType(p.Type).GetConstructor(new Type[1]
            {
                p.Type
            });
            gen.Emit(OpCodes.Newobj, constructor);
            gen.Emit(OpCodes.Stelem_Ref);
        }
        else
        {
            LocalBuilder local;
            if (!scope.Locals.TryGetValue(p, out local))
                throw Error.NotSupported();
            gen.Emit(OpCodes.Stloc, local);
        }
    }

    private Type[] GetParameterTypes(MethodInfo mi)
    {
        var parameters = mi.GetParameters();
        var parameterTypes = new Type[parameters.Length + 1];
        var index = 0;
        for (var length = parameters.Length; index < length; ++index)
            parameterTypes[index + 1] = parameters[index].ParameterType;
        parameterTypes[0] = typeof(ExecutionScope);
        return parameterTypes;
    }

    private StackType Generate(
        ILGenerator gen,
        Expression node,
        StackType ask)
    {
        switch (node.NodeType)
        {
            case ExpressionType.Add:
            case ExpressionType.AddChecked:
            case ExpressionType.And:
            case ExpressionType.AndAlso:
            case ExpressionType.ArrayIndex:
            case ExpressionType.Coalesce:
            case ExpressionType.Divide:
            case ExpressionType.Equal:
            case ExpressionType.ExclusiveOr:
            case ExpressionType.GreaterThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.LeftShift:
            case ExpressionType.LessThan:
            case ExpressionType.LessThanOrEqual:
            case ExpressionType.Modulo:
            case ExpressionType.Multiply:
            case ExpressionType.MultiplyChecked:
            case ExpressionType.NotEqual:
            case ExpressionType.Or:
            case ExpressionType.OrElse:
            case ExpressionType.Power:
            case ExpressionType.RightShift:
            case ExpressionType.Subtract:
            case ExpressionType.SubtractChecked:
                return GenerateBinary(gen, (BinaryExpression)node, ask);
            case ExpressionType.ArrayLength:
            case ExpressionType.Negate:
            case ExpressionType.UnaryPlus:
            case ExpressionType.NegateChecked:
            case ExpressionType.Not:
            case ExpressionType.TypeAs:
                return GenerateUnary(gen, (UnaryExpression)node, ask);
            case ExpressionType.Call:
                return GenerateMethodCall(gen, (MethodCallExpression)node, ask);
            case ExpressionType.Conditional:
                return GenerateConditional(gen, (ConditionalExpression)node);
            case ExpressionType.Constant:
                return GenerateConstant(gen, (ConstantExpression)node, ask);
            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked:
                GenerateConvert(gen, (UnaryExpression)node);
                return StackType.Value;
            case ExpressionType.Invoke:
                return GenerateInvoke(gen, (InvocationExpression)node, ask);
            case ExpressionType.Lambda:
                GenerateCreateDelegate(gen, (LambdaExpression)node);
                return StackType.Value;
            case ExpressionType.ListInit:
                return GenerateListInit(gen, (ListInitExpression)node);
            case ExpressionType.MemberAccess:
                return GenerateMemberAccess(gen, (MemberExpression)node, ask);
            case ExpressionType.MemberInit:
                return GenerateMemberInit(gen, (MemberInitExpression)node);
            case ExpressionType.New:
                return GenerateNew(gen, (NewExpression)node, ask);
            case ExpressionType.NewArrayInit:
            case ExpressionType.NewArrayBounds:
                GenerateNewArray(gen, (NewArrayExpression)node);
                return StackType.Value;
            case ExpressionType.Parameter:
                return GenerateParameterAccess(gen, (ParameterExpression)node, ask);
            case ExpressionType.Quote:
                GenerateQuote(gen, (UnaryExpression)node);
                return StackType.Value;
            case ExpressionType.TypeIs:
                GenerateTypeIs(gen, (TypeBinaryExpression)node);
                return StackType.Value;
            default:
                throw Error.UnhandledExpressionType(node.NodeType);
        }
    }

    private StackType GenerateNew(
        ILGenerator gen,
        NewExpression nex,
        StackType ask)
    {
        LocalBuilder local = null;
        if (nex.Type.IsValueType)
            local = gen.DeclareLocal(nex.Type);
        if (nex.Constructor != null)
        {
            var parameters = nex.Constructor.GetParameters();
            GenerateArgs(gen, parameters, nex.Arguments);
            gen.Emit(OpCodes.Newobj, nex.Constructor);
            if (nex.Type.IsValueType)
                gen.Emit(OpCodes.Stloc, local);
        }
        else if (nex.Type.IsValueType)
        {
            gen.Emit(OpCodes.Ldloca, local);
            gen.Emit(OpCodes.Initobj, nex.Type);
        }
        else
        {
            var constructor = nex.Type.GetConstructor(Type.EmptyTypes);
            gen.Emit(OpCodes.Newobj, constructor);
        }
        return nex.Type.IsValueType ? ReturnFromLocal(gen, ask, local) : StackType.Value;
    }

    private StackType GenerateInvoke(
        ILGenerator gen,
        InvocationExpression invoke,
        StackType ask)
    {
        var lambdaExpression = invoke.Expression.NodeType == ExpressionType.Quote ? (LambdaExpression)((UnaryExpression)invoke.Expression).Operand : invoke.Expression as LambdaExpression;
        if (lambdaExpression != null)
        {
            var index = 0;
            for (var count = invoke.Arguments.Count; index < count; ++index)
            {
                var parameter = lambdaExpression.Parameters[index];
                PrepareInitLocal(gen, parameter);
                var num = (int)Generate(gen, invoke.Arguments[index], StackType.Value);
                GenerateInitLocal(gen, parameter);
            }
            return Generate(gen, lambdaExpression.Body, ask);
        }
        var instance = invoke.Expression;
        if (typeof(LambdaExpression).IsAssignableFrom(instance.Type))
            instance = Expression.Call(instance, instance.Type.GetMethod("Compile", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        Expression node = Expression.Call(instance, instance.Type.GetMethod("Invoke"), invoke.Arguments);
        return Generate(gen, node, ask);
    }

    private void GenerateQuote(ILGenerator gen, UnaryExpression quote)
    {
        GenerateLoadExecutionScope(gen);
        var iGlobal = AddGlobal(typeof(Expression), quote.Operand);
        var globalAccess = (int)GenerateGlobalAccess(gen, iGlobal, typeof(Expression), StackType.Value);
        if (scope.HoistedLocalsVar != null)
            GenerateLoadHoistedLocals(gen);
        else
            gen.Emit(OpCodes.Ldnull);
        var method = typeof(ExecutionScope).GetMethod("IsolateExpression", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        gen.Emit(OpCodes.Callvirt, method);
        var type = quote.Operand.GetType();
        if (type == typeof(Expression))
            return;
        gen.Emit(OpCodes.Castclass, type);
    }

    private void GenerateBinding(ILGenerator gen, MemberBinding binding, Type objectType)
    {
        switch (binding.BindingType)
        {
            case MemberBindingType.Assignment:
                GenerateMemberAssignment(gen, (MemberAssignment)binding, objectType);
                break;
            case MemberBindingType.MemberBinding:
                GenerateMemberMemberBinding(gen, (MemberMemberBinding)binding);
                break;
            case MemberBindingType.ListBinding:
                GenerateMemberListBinding(gen, (MemberListBinding)binding);
                break;
            default:
                throw Error.UnknownBindingType();
        }
    }

    private void GenerateMemberAssignment(
        ILGenerator gen,
        MemberAssignment binding,
        Type objectType)
    {
        var num = (int)Generate(gen, binding.Expression, StackType.Value);
        if (binding.Member is FieldInfo member1)
        {
            gen.Emit(OpCodes.Stfld, member1);
        }
        else
        {
            var member = binding.Member as PropertyInfo;
            var setMethod = member.GetSetMethod(true);
            if (member == null)
                throw Error.UnhandledBinding();
            if (UseVirtual(setMethod))
            {
                if (objectType.IsValueType)
                    gen.Emit(OpCodes.Constrained, objectType);
                gen.Emit(OpCodes.Callvirt, setMethod);
            }
            else
                gen.Emit(OpCodes.Call, setMethod);
        }
    }

    private void GenerateMemberMemberBinding(ILGenerator gen, MemberMemberBinding binding)
    {
        var memberType = GetMemberType(binding.Member);
        if (binding.Member is PropertyInfo && memberType.IsValueType)
            throw Error.CannotAutoInitializeValueTypeMemberThroughProperty(binding.Member);
        var ask = memberType.IsValueType ? StackType.Address : StackType.Value;
        if (GenerateMemberAccess(gen, binding.Member, ask) != ask && memberType.IsValueType)
        {
            var local = gen.DeclareLocal(memberType);
            gen.Emit(OpCodes.Stloc, local);
            gen.Emit(OpCodes.Ldloca, local);
        }
        if (binding.Bindings.Count == 0)
            gen.Emit(OpCodes.Pop);
        else
            GenerateMemberInit(gen, binding.Bindings, false, memberType);
    }

    private void GenerateMemberListBinding(ILGenerator gen, MemberListBinding binding)
    {
        var memberType = GetMemberType(binding.Member);
        if (binding.Member is PropertyInfo && memberType.IsValueType)
            throw Error.CannotAutoInitializeValueTypeElementThroughProperty(binding.Member);
        var ask = memberType.IsValueType ? StackType.Address : StackType.Value;
        if (GenerateMemberAccess(gen, binding.Member, ask) != StackType.Address && memberType.IsValueType)
        {
            var local = gen.DeclareLocal(memberType);
            gen.Emit(OpCodes.Stloc, local);
            gen.Emit(OpCodes.Ldloca, local);
        }
        GenerateListInit(gen, binding.Initializers, false, memberType);
    }

    private StackType GenerateMemberInit(
        ILGenerator gen,
        MemberInitExpression init)
    {
        var num = (int)Generate(gen, init.NewExpression, StackType.Value);
        LocalBuilder local = null;
        if (init.NewExpression.Type.IsValueType && init.Bindings.Count > 0)
        {
            local = gen.DeclareLocal(init.NewExpression.Type);
            gen.Emit(OpCodes.Stloc, local);
            gen.Emit(OpCodes.Ldloca, local);
        }
        GenerateMemberInit(gen, init.Bindings, local == null, init.NewExpression.Type);
        if (local != null)
            gen.Emit(OpCodes.Ldloc, local);
        return StackType.Value;
    }

    private void GenerateMemberInit(
        ILGenerator gen,
        ReadOnlyCollection<MemberBinding> bindings,
        bool keepOnStack,
        Type objectType)
    {
        var index = 0;
        for (var count = bindings.Count; index < count; ++index)
        {
            if (keepOnStack || index < count - 1)
                gen.Emit(OpCodes.Dup);
            GenerateBinding(gen, bindings[index], objectType);
        }
    }

    private StackType GenerateListInit(
        ILGenerator gen,
        ListInitExpression init)
    {
        var num = (int)Generate(gen, init.NewExpression, StackType.Value);
        LocalBuilder local = null;
        if (init.NewExpression.Type.IsValueType)
        {
            local = gen.DeclareLocal(init.NewExpression.Type);
            gen.Emit(OpCodes.Stloc, local);
            gen.Emit(OpCodes.Ldloca, local);
        }
        GenerateListInit(gen, init.Initializers, local == null, init.NewExpression.Type);
        if (local != null)
            gen.Emit(OpCodes.Ldloc, local);
        return StackType.Value;
    }

    private void GenerateListInit(
        ILGenerator gen,
        ReadOnlyCollection<ElementInit> initializers,
        bool keepOnStack,
        Type objectType)
    {
        var index = 0;
        for (var count = initializers.Count; index < count; ++index)
        {
            if (keepOnStack || index < count - 1)
                gen.Emit(OpCodes.Dup);
            GenerateMethodCall(gen, initializers[index].AddMethod, initializers[index].Arguments, objectType);
            if (initializers[index].AddMethod.ReturnType != typeof(void))
                gen.Emit(OpCodes.Pop);
        }
    }

    private Type GetMemberType(MemberInfo member)
    {
        switch (member)
        {
            case FieldInfo fieldInfo:
                return fieldInfo.FieldType;
            case PropertyInfo propertyInfo:
                return propertyInfo.PropertyType;
            default:
                throw Error.MemberNotFieldOrProperty(member);
        }
    }

    private void GenerateNewArray(ILGenerator gen, NewArrayExpression nex)
    {
        var elementType = nex.Type.GetElementType();
        if (nex.NodeType == ExpressionType.NewArrayInit)
        {
            GenerateConstInt(gen, nex.Expressions.Count);
            gen.Emit(OpCodes.Newarr, elementType);
            var index = 0;
            for (var count = nex.Expressions.Count; index < count; ++index)
            {
                gen.Emit(OpCodes.Dup);
                GenerateConstInt(gen, index);
                var num = (int)Generate(gen, nex.Expressions[index], StackType.Value);
                GenerateArrayAssign(gen, elementType);
            }
        }
        else
        {
            var types = new Type[nex.Expressions.Count];
            var index1 = 0;
            for (var length = types.Length; index1 < length; ++index1)
                types[index1] = typeof(int);
            var index2 = 0;
            for (var count = nex.Expressions.Count; index2 < count; ++index2)
            {
                var expression = nex.Expressions[index2];
                var num = (int)Generate(gen, expression, StackType.Value);
                if (expression.Type != typeof(int))
                    GenerateConvertToType(gen, expression.Type, typeof(int), true);
            }
            if (nex.Expressions.Count > 1)
            {
                var numArray = new int[nex.Expressions.Count];
                var constructor = Array.CreateInstance(elementType, numArray).GetType().GetConstructor(types);
                gen.Emit(OpCodes.Newobj, constructor);
            }
            else
                gen.Emit(OpCodes.Newarr, elementType);
        }
    }

    private void GenerateConvert(ILGenerator gen, UnaryExpression u)
    {
        if (u.Method != null)
        {
            if (u.IsLifted && (!u.Type.IsValueType || !u.Operand.Type.IsValueType))
            {
                var parameters = u.Method.GetParameters();
                var parameterType = parameters[0].ParameterType;
                if (parameterType.IsByRef)
                    parameterType.GetElementType();
                Expression node = Expression.Convert(Expression.Call(null, u.Method, Expression.Convert(u.Operand, parameters[0].ParameterType)), u.Type);
                var num = (int)Generate(gen, node, StackType.Value);
            }
            else
            {
                var unaryMethod = (int)GenerateUnaryMethod(gen, u, StackType.Value);
            }
        }
        else
        {
            var num = (int)Generate(gen, u.Operand, StackType.Value);
            GenerateConvertToType(gen, u.Operand.Type, u.Type, u.NodeType == ExpressionType.ConvertChecked);
        }
    }

    private void GenerateCreateDelegate(ILGenerator gen, LambdaExpression lambda)
    {
        var lambda1 = GenerateLambda(lambda);
        GenerateLoadExecutionScope(gen);
        GenerateConstInt(gen, lambda1);
        if (scope.HoistedLocalsVar != null)
            GenerateLoadHoistedLocals(gen);
        else
            gen.Emit(OpCodes.Ldnull);
        gen.Emit(OpCodes.Callvirt, typeof(ExecutionScope).GetMethod("CreateDelegate", BindingFlags.Instance | BindingFlags.Public));
        gen.Emit(OpCodes.Castclass, lambda.Type);
    }

    private StackType GenerateMethodCall(
        ILGenerator gen,
        MethodCallExpression mc,
        StackType ask)
    {
        var methodCall = StackType.Value;
        var method = mc.Method;
        if (!mc.Method.IsStatic)
        {
            var ask1 = mc.Object.Type.IsValueType ? StackType.Address : StackType.Value;
            if (Generate(gen, mc.Object, ask1) != ask1)
            {
                var local = gen.DeclareLocal(mc.Object.Type);
                gen.Emit(OpCodes.Stloc, local);
                gen.Emit(OpCodes.Ldloca, local);
            }
            if (ask == StackType.Address && mc.Object.Type.IsArray && method == mc.Object.Type.GetMethod("Get", BindingFlags.Instance | BindingFlags.Public))
            {
                method = mc.Object.Type.GetMethod("Address", BindingFlags.Instance | BindingFlags.Public);
                methodCall = StackType.Address;
            }
        }
        GenerateMethodCall(gen, method, mc.Arguments, mc.Object == null ? null : mc.Object.Type);
        return methodCall;
    }

    private void GenerateMethodCall(
        ILGenerator gen,
        MethodInfo mi,
        ReadOnlyCollection<Expression> args,
        Type objectType)
    {
        var parameters = mi.GetParameters();
        var args1 = GenerateArgs(gen, parameters, args);
        var opcode = UseVirtual(mi) ? OpCodes.Callvirt : OpCodes.Call;
        if (opcode == OpCodes.Callvirt && objectType.IsValueType)
            gen.Emit(OpCodes.Constrained, objectType);
        if (mi.CallingConvention == CallingConventions.VarArgs)
        {
            var optionalParameterTypes = new Type[args.Count];
            var index = 0;
            for (var length = optionalParameterTypes.Length; index < length; ++index)
                optionalParameterTypes[index] = args[index].Type;
            gen.EmitCall(opcode, mi, optionalParameterTypes);
        }
        else
            gen.Emit(opcode, mi);
        foreach (var writeback in args1)
            GenerateWriteBack(gen, writeback);
    }

    private List<WriteBack> GenerateArgs(
        ILGenerator gen,
        ParameterInfo[] pis,
        ReadOnlyCollection<Expression> args)
    {
        var args1 = new List<WriteBack>();
        var index = 0;
        for (var length = pis.Length; index < length; ++index)
        {
            var pi = pis[index];
            var node = args[index];
            var ask = pi.ParameterType.IsByRef ? StackType.Address : StackType.Value;
            var stackType = Generate(gen, node, ask);
            if (ask == StackType.Address && stackType != StackType.Address)
            {
                var localBuilder = gen.DeclareLocal(node.Type);
                gen.Emit(OpCodes.Stloc, localBuilder);
                gen.Emit(OpCodes.Ldloca, localBuilder);
                if (args[index] is MemberExpression)
                    args1.Add(new WriteBack(localBuilder, args[index]));
            }
        }
        return args1;
    }

    private StackType GenerateLift(
        ILGenerator gen,
        ExpressionType nodeType,
        Type resultType,
        MethodCallExpression mc,
        IEnumerable<ParameterExpression> parameters,
        IEnumerable<Expression> arguments)
    {
        var readOnlyCollection1 = parameters.ToReadOnlyCollection<ParameterExpression>();
        var readOnlyCollection2 = arguments.ToReadOnlyCollection<Expression>();
        switch (nodeType)
        {
            case ExpressionType.Equal:
            case ExpressionType.NotEqual:
                if (resultType != Expression.GetNullableType(mc.Type))
                {
                    var label1 = gen.DefineLabel();
                    var label2 = gen.DefineLabel();
                    var label3 = gen.DefineLabel();
                    var local1 = gen.DeclareLocal(typeof(bool));
                    var local2 = gen.DeclareLocal(typeof(bool));
                    gen.Emit(OpCodes.Ldc_I4_0);
                    gen.Emit(OpCodes.Stloc, local1);
                    gen.Emit(OpCodes.Ldc_I4_1);
                    gen.Emit(OpCodes.Stloc, local2);
                    var index = 0;
                    for (var count = readOnlyCollection1.Count; index < count; ++index)
                    {
                        var p = readOnlyCollection1[index];
                        var node = readOnlyCollection2[index];
                        PrepareInitLocal(gen, p);
                        if (IsNullable(node.Type))
                        {
                            if (Generate(gen, node, StackType.Address) == StackType.Value)
                            {
                                var local3 = gen.DeclareLocal(node.Type);
                                gen.Emit(OpCodes.Stloc, local3);
                                gen.Emit(OpCodes.Ldloca, local3);
                            }
                            gen.Emit(OpCodes.Dup);
                            GenerateHasValue(gen, node.Type);
                            gen.Emit(OpCodes.Ldc_I4_0);
                            gen.Emit(OpCodes.Ceq);
                            gen.Emit(OpCodes.Dup);
                            gen.Emit(OpCodes.Ldloc, local1);
                            gen.Emit(OpCodes.Or);
                            gen.Emit(OpCodes.Stloc, local1);
                            gen.Emit(OpCodes.Ldloc, local2);
                            gen.Emit(OpCodes.And);
                            gen.Emit(OpCodes.Stloc, local2);
                            GenerateGetValueOrDefault(gen, node.Type);
                        }
                        else
                        {
                            var num = (int)Generate(gen, node, StackType.Value);
                            if (!node.Type.IsValueType)
                            {
                                gen.Emit(OpCodes.Dup);
                                gen.Emit(OpCodes.Ldnull);
                                gen.Emit(OpCodes.Ceq);
                                gen.Emit(OpCodes.Dup);
                                gen.Emit(OpCodes.Ldloc, local1);
                                gen.Emit(OpCodes.Or);
                                gen.Emit(OpCodes.Stloc, local1);
                                gen.Emit(OpCodes.Ldloc, local2);
                                gen.Emit(OpCodes.And);
                                gen.Emit(OpCodes.Stloc, local2);
                            }
                            else
                            {
                                gen.Emit(OpCodes.Ldc_I4_0);
                                gen.Emit(OpCodes.Stloc, local2);
                            }
                        }
                        GenerateInitLocal(gen, p);
                    }
                    gen.Emit(OpCodes.Ldloc, local2);
                    gen.Emit(OpCodes.Brtrue, label2);
                    gen.Emit(OpCodes.Ldloc, local1);
                    gen.Emit(OpCodes.Brtrue, label3);
                    var num1 = (int)Generate(gen, mc, StackType.Value);
                    if (IsNullable(resultType) && resultType != mc.Type)
                    {
                        var constructor = resultType.GetConstructor(new Type[1]
                        {
                            mc.Type
                        });
                        gen.Emit(OpCodes.Newobj, constructor);
                    }
                    gen.Emit(OpCodes.Br_S, label1);
                    gen.MarkLabel(label2);
                    var flag1 = nodeType == ExpressionType.Equal;
                    var constant1 = (int)GenerateConstant(gen, Expression.Constant(flag1), StackType.Value);
                    gen.Emit(OpCodes.Br_S, label1);
                    gen.MarkLabel(label3);
                    var flag2 = nodeType == ExpressionType.NotEqual;
                    var constant2 = (int)GenerateConstant(gen, Expression.Constant(flag2), StackType.Value);
                    gen.MarkLabel(label1);
                    return StackType.Value;
                }
                break;
        }
        var label4 = gen.DefineLabel();
        var label5 = gen.DefineLabel();
        var local4 = gen.DeclareLocal(typeof(bool));
        var index1 = 0;
        for (var count = readOnlyCollection1.Count; index1 < count; ++index1)
        {
            var p = readOnlyCollection1[index1];
            var node = readOnlyCollection2[index1];
            if (IsNullable(node.Type))
            {
                PrepareInitLocal(gen, p);
                if (Generate(gen, node, StackType.Address) == StackType.Value)
                {
                    var local5 = gen.DeclareLocal(node.Type);
                    gen.Emit(OpCodes.Stloc, local5);
                    gen.Emit(OpCodes.Ldloca, local5);
                }
                gen.Emit(OpCodes.Dup);
                GenerateHasValue(gen, node.Type);
                gen.Emit(OpCodes.Ldc_I4_0);
                gen.Emit(OpCodes.Ceq);
                gen.Emit(OpCodes.Stloc, local4);
                GenerateGetValueOrDefault(gen, node.Type);
                GenerateInitLocal(gen, p);
            }
            else
            {
                PrepareInitLocal(gen, p);
                var num = (int)Generate(gen, node, StackType.Value);
                if (!node.Type.IsValueType)
                {
                    gen.Emit(OpCodes.Dup);
                    gen.Emit(OpCodes.Ldnull);
                    gen.Emit(OpCodes.Ceq);
                    gen.Emit(OpCodes.Stloc, local4);
                }
                GenerateInitLocal(gen, p);
            }
            gen.Emit(OpCodes.Ldloc, local4);
            gen.Emit(OpCodes.Brtrue, label5);
        }
        var num2 = (int)Generate(gen, mc, StackType.Value);
        if (IsNullable(resultType) && resultType != mc.Type)
        {
            var constructor = resultType.GetConstructor(new Type[1]
            {
                mc.Type
            });
            gen.Emit(OpCodes.Newobj, constructor);
        }
        gen.Emit(OpCodes.Br_S, label4);
        gen.MarkLabel(label5);
        if (resultType == Expression.GetNullableType(mc.Type))
        {
            if (resultType.IsValueType)
            {
                var local6 = gen.DeclareLocal(resultType);
                gen.Emit(OpCodes.Ldloca, local6);
                gen.Emit(OpCodes.Initobj, resultType);
                gen.Emit(OpCodes.Ldloc, local6);
            }
            else
                gen.Emit(OpCodes.Ldnull);
        }
        else
        {
            switch (nodeType)
            {
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                    gen.Emit(OpCodes.Ldc_I4_0);
                    break;
            }
        }
        gen.MarkLabel(label4);
        return StackType.Value;
    }

    private StackType GenerateMemberAccess(
        ILGenerator gen,
        MemberExpression m,
        StackType ask)
    {
        return GenerateMemberAccess(gen, m.Expression, m.Member, ask);
    }

    private StackType GenerateMemberAccess(
        ILGenerator gen,
        Expression expression,
        MemberInfo member,
        StackType ask)
    {
        switch (member)
        {
            case FieldInfo fieldInfo:
                if (!fieldInfo.IsStatic)
                {
                    var ask1 = expression.Type.IsValueType ? StackType.Address : StackType.Value;
                    if (Generate(gen, expression, ask1) != ask1)
                    {
                        var local = gen.DeclareLocal(expression.Type);
                        gen.Emit(OpCodes.Stloc, local);
                        gen.Emit(OpCodes.Ldloca, local);
                    }
                }
                return GenerateMemberAccess(gen, member, ask);
            case PropertyInfo propertyInfo:
                if (!propertyInfo.GetGetMethod(true).IsStatic)
                {
                    var ask2 = expression.Type.IsValueType ? StackType.Address : StackType.Value;
                    if (Generate(gen, expression, ask2) != ask2)
                    {
                        var local = gen.DeclareLocal(expression.Type);
                        gen.Emit(OpCodes.Stloc, local);
                        gen.Emit(OpCodes.Ldloca, local);
                    }
                }
                return GenerateMemberAccess(gen, member, ask);
            default:
                throw Error.UnhandledMemberAccess(member);
        }
    }

    private void GenerateWriteBack(ILGenerator gen, WriteBack writeback)
    {
        if (!(writeback.arg is MemberExpression memberExpression))
            return;
        GenerateMemberWriteBack(gen, memberExpression.Expression, memberExpression.Member, writeback.loc);
    }

    private void GenerateMemberWriteBack(
        ILGenerator gen,
        Expression expression,
        MemberInfo member,
        LocalBuilder loc)
    {
        switch (member)
        {
            case FieldInfo field:
                if (!field.IsStatic)
                {
                    var ask = expression.Type.IsValueType ? StackType.Address : StackType.Value;
                    var num = (int)Generate(gen, expression, ask);
                    gen.Emit(OpCodes.Ldloc, loc);
                    gen.Emit(OpCodes.Stfld, field);
                    break;
                }
                gen.Emit(OpCodes.Ldloc, loc);
                gen.Emit(OpCodes.Stsfld, field);
                break;
            case PropertyInfo propertyInfo:
                var setMethod = propertyInfo.GetSetMethod(true);
                if (setMethod == null)
                    break;
                if (!setMethod.IsStatic)
                {
                    var ask = expression.Type.IsValueType ? StackType.Address : StackType.Value;
                    var num = (int)Generate(gen, expression, ask);
                }
                gen.Emit(OpCodes.Ldloc, loc);
                gen.Emit(UseVirtual(setMethod) ? OpCodes.Callvirt : OpCodes.Call, setMethod);
                break;
            default:
                throw Error.UnhandledMemberAccess(member);
        }
    }

    private bool UseVirtual(MethodInfo mi) => !mi.IsStatic && !mi.DeclaringType.IsValueType;

    private void GenerateFieldAccess(
        ILGenerator gen,
        FieldInfo fi,
        StackType ask)
    {
        StackType stackType;
        if (fi.IsLiteral)
        {
            stackType = GenerateConstant(gen, fi.FieldType, fi.GetRawConstantValue(), ask);
        }
        else
        {
            OpCode opcode;
            if (ask == StackType.Value || fi.IsInitOnly)
            {
                opcode = fi.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld;
                stackType = StackType.Value;
            }
            else
            {
                opcode = fi.IsStatic ? OpCodes.Ldsflda : OpCodes.Ldflda;
                stackType = StackType.Address;
            }
            gen.Emit(opcode, fi);
        }
        if (ask != StackType.Address || stackType != StackType.Value)
            return;
        var local = gen.DeclareLocal(fi.FieldType);
        gen.Emit(OpCodes.Stloc, local);
        gen.Emit(OpCodes.Ldloca, local);
    }

    private StackType GenerateMemberAccess(
        ILGenerator gen,
        MemberInfo member,
        StackType ask)
    {
        switch (member)
        {
            case FieldInfo fi:
                GenerateFieldAccess(gen, fi, ask);
                return ask;
            case PropertyInfo propertyInfo:
                var getMethod = propertyInfo.GetGetMethod(true);
                gen.Emit(UseVirtual(getMethod) ? OpCodes.Callvirt : OpCodes.Call, getMethod);
                return StackType.Value;
            default:
                throw Error.UnhandledMemberAccess(member);
        }
    }

    private StackType GenerateParameterAccess(
        ILGenerator gen,
        ParameterExpression p,
        StackType ask)
    {
        LocalBuilder local;
        if (scope.Locals.TryGetValue(p, out local))
        {
            if (ask == StackType.Value)
                gen.Emit(OpCodes.Ldloc, local);
            else
                gen.Emit(OpCodes.Ldloca, local);
            return ask;
        }
        int hoistIndex;
        if (scope.HoistedLocals.TryGetValue(p, out hoistIndex))
        {
            GenerateLoadHoistedLocals(gen);
            return GenerateHoistedLocalAccess(gen, hoistIndex, p.Type, ask);
        }
        var index = 0;
        for (var count = scope.Lambda.Parameters.Count; index < count; ++index)
        {
            if (scope.Lambda.Parameters[index] == p)
                return GenerateArgAccess(gen, index + 1, ask);
        }
        GenerateLoadExecutionScope(gen);
        for (var parent = scope.Parent; parent != null; parent = parent.Parent)
        {
            if (parent.HoistedLocals.TryGetValue(p, out hoistIndex))
            {
                gen.Emit(OpCodes.Ldfld, typeof(ExecutionScope).GetField("Locals", BindingFlags.Instance | BindingFlags.Public));
                return GenerateHoistedLocalAccess(gen, hoistIndex, p.Type, ask);
            }
            gen.Emit(OpCodes.Ldfld, typeof(ExecutionScope).GetField("Parent", BindingFlags.Instance | BindingFlags.Public));
        }
        throw Error.LambdaParameterNotInScope();
    }

    private StackType GenerateConstant(
        ILGenerator gen,
        ConstantExpression c,
        StackType ask)
    {
        return GenerateConstant(gen, c.Type, c.Value, ask);
    }

    private StackType GenerateConstant(
        ILGenerator gen,
        Type type,
        object value,
        StackType ask)
    {
        if (value == null)
        {
            if (type.IsValueType)
            {
                var local = gen.DeclareLocal(type);
                gen.Emit(OpCodes.Ldloca, local);
                gen.Emit(OpCodes.Initobj, type);
                gen.Emit(OpCodes.Ldloc, local);
            }
            else
                gen.Emit(OpCodes.Ldnull);
        }
        else
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    GenerateConstInt(gen, (bool)value ? 1 : 0);
                    break;
                case TypeCode.SByte:
                    GenerateConstInt(gen, (sbyte)value);
                    gen.Emit(OpCodes.Conv_I1);
                    break;
                case TypeCode.Int16:
                    GenerateConstInt(gen, (short)value);
                    gen.Emit(OpCodes.Conv_I2);
                    break;
                case TypeCode.Int32:
                    GenerateConstInt(gen, (int)value);
                    break;
                case TypeCode.Int64:
                    gen.Emit(OpCodes.Ldc_I8, (long)value);
                    break;
                case TypeCode.Single:
                    gen.Emit(OpCodes.Ldc_R4, (float)value);
                    break;
                case TypeCode.Double:
                    gen.Emit(OpCodes.Ldc_R8, (double)value);
                    break;
                default:
                    var iGlobal = AddGlobal(type, value);
                    return GenerateGlobalAccess(gen, iGlobal, type, ask);
            }
        }
        return StackType.Value;
    }

    private StackType GenerateUnary(
        ILGenerator gen,
        UnaryExpression u,
        StackType ask)
    {
        if (u.Method != null)
            return GenerateUnaryMethod(gen, u, ask);
        if (u.NodeType == ExpressionType.NegateChecked && IsInteger(u.Operand.Type))
        {
            GenerateConstInt(gen, 0);
            GenerateConvertToType(gen, typeof(int), u.Operand.Type, false);
            var num = (int)Generate(gen, u.Operand, StackType.Value);
            return GenerateBinaryOp(gen, ExpressionType.SubtractChecked, u.Operand.Type, u.Operand.Type, u.Type, false, ask);
        }
        var num1 = (int)Generate(gen, u.Operand, StackType.Value);
        return GenerateUnaryOp(gen, u.NodeType, u.Operand.Type, u.Type, ask);
    }

    private static bool IsInteger(Type type)
    {
        type = GetNonNullableType(type);
        if (type.IsEnum)
            return false;
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.SByte:
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64:
                return true;
            default:
                return false;
        }
    }

    private StackType GenerateUnaryMethod(
        ILGenerator gen,
        UnaryExpression u,
        StackType ask)
    {
        if (u.IsLifted)
        {
            var parameterExpression = Expression.Parameter(Expression.GetNonNullableType(u.Operand.Type), null);
            var mc = Expression.Call(null, u.Method, parameterExpression);
            var nullableType = Expression.GetNullableType(mc.Type);
            var lift = (int)GenerateLift(gen, u.NodeType, nullableType, mc, new ParameterExpression[1]
            {
                parameterExpression
            }, new Expression[1]
            {
                u.Operand
            });
            GenerateConvertToType(gen, nullableType, u.Type, false);
            return StackType.Value;
        }
        var node = Expression.Call(null, u.Method, u.Operand);
        return Generate(gen, node, ask);
    }

    private StackType GenerateConditional(
        ILGenerator gen,
        ConditionalExpression b)
    {
        var label1 = gen.DefineLabel();
        var label2 = gen.DefineLabel();
        var num1 = (int)Generate(gen, b.Test, StackType.Value);
        gen.Emit(OpCodes.Brfalse, label1);
        var num2 = (int)Generate(gen, b.IfTrue, StackType.Value);
        gen.Emit(OpCodes.Br, label2);
        gen.MarkLabel(label1);
        var num3 = (int)Generate(gen, b.IfFalse, StackType.Value);
        gen.MarkLabel(label2);
        return StackType.Value;
    }

    private void GenerateCoalesce(ILGenerator gen, BinaryExpression b)
    {
        if (IsNullable(b.Left.Type))
        {
            GenerateNullableCoalesce(gen, b);
        }
        else
        {
            if (b.Left.Type.IsValueType)
                throw Error.CoalesceUsedOnNonNullType();
            if (b.Conversion != null)
                GenerateLambdaReferenceCoalesce(gen, b);
            else if (b.Method != null)
                GenerateUserDefinedReferenceCoalesce(gen, b);
            else
                GenerateReferenceCoalesceWithoutConversion(gen, b);
        }
    }

    private void GenerateNullableCoalesce(ILGenerator gen, BinaryExpression b)
    {
        var local = gen.DeclareLocal(b.Left.Type);
        var label1 = gen.DefineLabel();
        var label2 = gen.DefineLabel();
        var num1 = (int)Generate(gen, b.Left, StackType.Value);
        gen.Emit(OpCodes.Stloc, local);
        gen.Emit(OpCodes.Ldloca, local);
        GenerateHasValue(gen, b.Left.Type);
        gen.Emit(OpCodes.Brfalse, label1);
        var nonNullableType = GetNonNullableType(b.Left.Type);
        if (b.Method != null)
        {
            if (!b.Method.GetParameters()[0].ParameterType.IsAssignableFrom(b.Left.Type))
            {
                gen.Emit(OpCodes.Ldloca, local);
                GenerateGetValueOrDefault(gen, b.Left.Type);
            }
            else
                gen.Emit(OpCodes.Ldloc, local);
            gen.Emit(OpCodes.Call, b.Method);
        }
        else if (b.Conversion != null)
        {
            var parameter = b.Conversion.Parameters[0];
            PrepareInitLocal(gen, parameter);
            if (!parameter.Type.IsAssignableFrom(b.Left.Type))
            {
                gen.Emit(OpCodes.Ldloca, local);
                GenerateGetValueOrDefault(gen, b.Left.Type);
            }
            else
                gen.Emit(OpCodes.Ldloc, local);
            GenerateInitLocal(gen, parameter);
            var num2 = (int)Generate(gen, b.Conversion.Body, StackType.Value);
        }
        else if (b.Type != nonNullableType)
        {
            gen.Emit(OpCodes.Ldloca, local);
            GenerateGetValueOrDefault(gen, b.Left.Type);
            GenerateConvertToType(gen, nonNullableType, b.Type, true);
        }
        else
        {
            gen.Emit(OpCodes.Ldloca, local);
            GenerateGetValueOrDefault(gen, b.Left.Type);
        }
        gen.Emit(OpCodes.Br, label2);
        gen.MarkLabel(label1);
        var num3 = (int)Generate(gen, b.Right, StackType.Value);
        if (b.Right.Type != b.Type)
            GenerateConvertToType(gen, b.Right.Type, b.Type, true);
        gen.MarkLabel(label2);
    }

    private void GenerateLambdaReferenceCoalesce(ILGenerator gen, BinaryExpression b)
    {
        var label1 = gen.DefineLabel();
        var label2 = gen.DefineLabel();
        var num1 = (int)Generate(gen, b.Left, StackType.Value);
        gen.Emit(OpCodes.Dup);
        gen.Emit(OpCodes.Ldnull);
        gen.Emit(OpCodes.Ceq);
        gen.Emit(OpCodes.Brfalse, label2);
        gen.Emit(OpCodes.Pop);
        var num2 = (int)Generate(gen, b.Right, StackType.Value);
        gen.Emit(OpCodes.Br, label1);
        gen.MarkLabel(label2);
        var parameter = b.Conversion.Parameters[0];
        PrepareInitLocal(gen, parameter);
        GenerateInitLocal(gen, parameter);
        var num3 = (int)Generate(gen, b.Conversion.Body, StackType.Value);
        gen.MarkLabel(label1);
    }

    private void GenerateUserDefinedReferenceCoalesce(ILGenerator gen, BinaryExpression b)
    {
        var label1 = gen.DefineLabel();
        var label2 = gen.DefineLabel();
        var num1 = (int)Generate(gen, b.Left, StackType.Value);
        gen.Emit(OpCodes.Dup);
        gen.Emit(OpCodes.Ldnull);
        gen.Emit(OpCodes.Ceq);
        gen.Emit(OpCodes.Brfalse, label2);
        gen.Emit(OpCodes.Pop);
        var num2 = (int)Generate(gen, b.Right, StackType.Value);
        gen.Emit(OpCodes.Br_S, label1);
        gen.MarkLabel(label2);
        gen.Emit(OpCodes.Call, b.Method);
        gen.MarkLabel(label1);
    }

    private void GenerateReferenceCoalesceWithoutConversion(ILGenerator gen, BinaryExpression b)
    {
        var label1 = gen.DefineLabel();
        var label2 = gen.DefineLabel();
        var num1 = (int)Generate(gen, b.Left, StackType.Value);
        gen.Emit(OpCodes.Dup);
        gen.Emit(OpCodes.Ldnull);
        gen.Emit(OpCodes.Ceq);
        gen.Emit(OpCodes.Brfalse, label2);
        gen.Emit(OpCodes.Pop);
        var num2 = (int)Generate(gen, b.Right, StackType.Value);
        if (b.Right.Type != b.Type)
            gen.Emit(OpCodes.Castclass, b.Type);
        gen.Emit(OpCodes.Br_S, label1);
        gen.MarkLabel(label2);
        if (b.Left.Type != b.Type)
            gen.Emit(OpCodes.Castclass, b.Type);
        gen.MarkLabel(label1);
    }

    private StackType GenerateUserdefinedLiftedAndAlso(
        ILGenerator gen,
        BinaryExpression b,
        StackType ask)
    {
        var type = b.Left.Type;
        var nonNullableType = GetNonNullableType(type);
        gen.DefineLabel();
        var label1 = gen.DefineLabel();
        var label2 = gen.DefineLabel();
        var local1 = gen.DeclareLocal(type);
        var local2 = gen.DeclareLocal(type);
        var local3 = gen.DeclareLocal(nonNullableType);
        var local4 = gen.DeclareLocal(nonNullableType);
        var num1 = (int)Generate(gen, b.Left, StackType.Value);
        gen.Emit(OpCodes.Stloc, local1);
        var num2 = (int)Generate(gen, b.Right, StackType.Value);
        gen.Emit(OpCodes.Stloc, local2);
        gen.Emit(OpCodes.Ldloca, local1);
        GenerateHasValue(gen, type);
        gen.Emit(OpCodes.Brfalse, label2);
        gen.Emit(OpCodes.Ldloca, local1);
        GenerateGetValueOrDefault(gen, type);
        var types1 = new Type[1] { nonNullableType };
        var method1 = nonNullableType.GetMethod("op_False", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types1, null);
        gen.Emit(OpCodes.Call, method1);
        gen.Emit(OpCodes.Brtrue, label2);
        gen.Emit(OpCodes.Ldloca, local2);
        GenerateHasValue(gen, type);
        gen.Emit(OpCodes.Brfalse, label1);
        gen.Emit(OpCodes.Ldloca, local1);
        GenerateGetValueOrDefault(gen, type);
        gen.Emit(OpCodes.Stloc, local3);
        gen.Emit(OpCodes.Ldloca, local2);
        GenerateGetValueOrDefault(gen, type);
        gen.Emit(OpCodes.Stloc, local4);
        var types2 = new Type[2]
        {
            nonNullableType,
            nonNullableType
        };
        var method2 = nonNullableType.GetMethod("op_BitwiseAnd", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types2, null);
        gen.Emit(OpCodes.Ldloc, local3);
        gen.Emit(OpCodes.Ldloc, local4);
        gen.Emit(OpCodes.Call, method2);
        if (method2.ReturnType != type)
            GenerateConvertToType(gen, method2.ReturnType, type, true);
        gen.Emit(OpCodes.Stloc, local1);
        gen.Emit(OpCodes.Br, label2);
        gen.MarkLabel(label1);
        gen.Emit(OpCodes.Ldloc, local2);
        gen.Emit(OpCodes.Stloc, local1);
        gen.MarkLabel(label2);
        return ReturnFromLocal(gen, ask, local1);
    }

    private StackType GenerateLiftedAndAlso(
        ILGenerator gen,
        BinaryExpression b,
        StackType ask)
    {
        var type = typeof(bool?);
        var label1 = gen.DefineLabel();
        var label2 = gen.DefineLabel();
        var label3 = gen.DefineLabel();
        var label4 = gen.DefineLabel();
        var label5 = gen.DefineLabel();
        var local1 = gen.DeclareLocal(type);
        var local2 = gen.DeclareLocal(type);
        var num1 = (int)Generate(gen, b.Left, StackType.Value);
        gen.Emit(OpCodes.Stloc, local1);
        gen.Emit(OpCodes.Ldloca, local1);
        GenerateHasValue(gen, type);
        gen.Emit(OpCodes.Brfalse, label1);
        gen.Emit(OpCodes.Ldloca, local1);
        GenerateGetValueOrDefault(gen, type);
        gen.Emit(OpCodes.Ldc_I4_0);
        gen.Emit(OpCodes.Ceq);
        gen.Emit(OpCodes.Brtrue, label2);
        gen.MarkLabel(label1);
        var num2 = (int)Generate(gen, b.Right, StackType.Value);
        gen.Emit(OpCodes.Stloc, local2);
        gen.Emit(OpCodes.Ldloca, local2);
        GenerateHasValue(gen, type);
        gen.Emit(OpCodes.Brfalse_S, label3);
        gen.Emit(OpCodes.Ldloca, local2);
        GenerateGetValueOrDefault(gen, type);
        gen.Emit(OpCodes.Ldc_I4_0);
        gen.Emit(OpCodes.Ceq);
        gen.Emit(OpCodes.Brtrue_S, label2);
        gen.Emit(OpCodes.Ldloca, local1);
        GenerateHasValue(gen, type);
        gen.Emit(OpCodes.Brfalse, label3);
        gen.Emit(OpCodes.Ldc_I4_1);
        gen.Emit(OpCodes.Br_S, label4);
        gen.MarkLabel(label2);
        gen.Emit(OpCodes.Ldc_I4_0);
        gen.Emit(OpCodes.Br_S, label4);
        gen.MarkLabel(label4);
        var constructor = type.GetConstructor(new Type[1]
        {
            typeof (bool)
        });
        gen.Emit(OpCodes.Newobj, constructor);
        gen.Emit(OpCodes.Stloc, local1);
        gen.Emit(OpCodes.Br, label5);
        gen.MarkLabel(label3);
        gen.Emit(OpCodes.Ldloca, local1);
        gen.Emit(OpCodes.Initobj, type);
        gen.MarkLabel(label5);
        return ReturnFromLocal(gen, ask, local1);
    }

    private void GenerateMethodAndAlso(ILGenerator gen, BinaryExpression b)
    {
        var label = gen.DefineLabel();
        var num1 = (int)Generate(gen, b.Left, StackType.Value);
        gen.Emit(OpCodes.Dup);
        var parameterType = b.Method.GetParameters()[0].ParameterType;
        var types = new Type[1] { parameterType };
        var method = parameterType.GetMethod("op_False", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
        gen.Emit(OpCodes.Call, method);
        gen.Emit(OpCodes.Brtrue, label);
        var num2 = (int)Generate(gen, b.Right, StackType.Value);
        gen.Emit(OpCodes.Call, b.Method);
        gen.MarkLabel(label);
    }

    private void GenerateUnliftedAndAlso(ILGenerator gen, BinaryExpression b)
    {
        var num1 = (int)Generate(gen, b.Left, StackType.Value);
        var label = gen.DefineLabel();
        gen.Emit(OpCodes.Dup);
        gen.Emit(OpCodes.Brfalse, label);
        gen.Emit(OpCodes.Pop);
        var num2 = (int)Generate(gen, b.Right, StackType.Value);
        gen.MarkLabel(label);
    }

    private StackType GenerateAndAlso(
        ILGenerator gen,
        BinaryExpression b,
        StackType ask)
    {
        if (b.Method != null && !IsLiftedLogicalBinaryOperator(b.Left.Type, b.Right.Type, b.Method))
        {
            GenerateMethodAndAlso(gen, b);
        }
        else
        {
            if (b.Left.Type == typeof(bool?))
                return GenerateLiftedAndAlso(gen, b, ask);
            if (IsLiftedLogicalBinaryOperator(b.Left.Type, b.Right.Type, b.Method))
                return GenerateUserdefinedLiftedAndAlso(gen, b, ask);
            GenerateUnliftedAndAlso(gen, b);
        }
        return StackType.Value;
    }

    private static bool IsLiftedLogicalBinaryOperator(Type left, Type right, MethodInfo method) => right == left && IsNullable(left) && method != null && method.ReturnType == GetNonNullableType(left);

    private StackType GenerateUserdefinedLiftedOrElse(
        ILGenerator gen,
        BinaryExpression b,
        StackType ask)
    {
        var type = b.Left.Type;
        var nonNullableType = GetNonNullableType(type);
        gen.DefineLabel();
        var label1 = gen.DefineLabel();
        var label2 = gen.DefineLabel();
        var local1 = gen.DeclareLocal(type);
        var local2 = gen.DeclareLocal(type);
        var local3 = gen.DeclareLocal(nonNullableType);
        var local4 = gen.DeclareLocal(nonNullableType);
        var num1 = (int)Generate(gen, b.Left, StackType.Value);
        gen.Emit(OpCodes.Stloc, local1);
        var num2 = (int)Generate(gen, b.Right, StackType.Value);
        gen.Emit(OpCodes.Stloc, local2);
        gen.Emit(OpCodes.Ldloca, local1);
        GenerateHasValue(gen, type);
        gen.Emit(OpCodes.Brfalse, label1);
        gen.Emit(OpCodes.Ldloca, local1);
        GenerateGetValueOrDefault(gen, type);
        var types1 = new Type[1] { nonNullableType };
        var method1 = nonNullableType.GetMethod("op_True", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types1, null);
        gen.Emit(OpCodes.Call, method1);
        gen.Emit(OpCodes.Brtrue, label2);
        gen.Emit(OpCodes.Ldloca, local2);
        GenerateHasValue(gen, type);
        gen.Emit(OpCodes.Brfalse, label1);
        gen.Emit(OpCodes.Ldloca, local1);
        GenerateGetValueOrDefault(gen, type);
        gen.Emit(OpCodes.Stloc, local3);
        gen.Emit(OpCodes.Ldloca, local2);
        GenerateGetValueOrDefault(gen, type);
        gen.Emit(OpCodes.Stloc, local4);
        var types2 = new Type[2]
        {
            nonNullableType,
            nonNullableType
        };
        var method2 = nonNullableType.GetMethod("op_BitwiseOr", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types2, null);
        gen.Emit(OpCodes.Ldloc, local3);
        gen.Emit(OpCodes.Ldloc, local4);
        gen.Emit(OpCodes.Call, method2);
        if (method2.ReturnType != type)
            GenerateConvertToType(gen, method2.ReturnType, type, true);
        gen.Emit(OpCodes.Stloc, local1);
        gen.Emit(OpCodes.Br, label2);
        gen.MarkLabel(label1);
        gen.Emit(OpCodes.Ldloc, local2);
        gen.Emit(OpCodes.Stloc, local1);
        gen.MarkLabel(label2);
        return ReturnFromLocal(gen, ask, local1);
    }

    private StackType GenerateLiftedOrElse(
        ILGenerator gen,
        BinaryExpression b,
        StackType ask)
    {
        var type = typeof(bool?);
        var label1 = gen.DefineLabel();
        var label2 = gen.DefineLabel();
        var label3 = gen.DefineLabel();
        var label4 = gen.DefineLabel();
        var label5 = gen.DefineLabel();
        var local1 = gen.DeclareLocal(type);
        var local2 = gen.DeclareLocal(type);
        var num1 = (int)Generate(gen, b.Left, StackType.Value);
        gen.Emit(OpCodes.Stloc, local1);
        gen.Emit(OpCodes.Ldloca, local1);
        GenerateHasValue(gen, type);
        gen.Emit(OpCodes.Brfalse, label1);
        gen.Emit(OpCodes.Ldloca, local1);
        GenerateGetValueOrDefault(gen, type);
        gen.Emit(OpCodes.Ldc_I4_0);
        gen.Emit(OpCodes.Ceq);
        gen.Emit(OpCodes.Brfalse, label2);
        gen.MarkLabel(label1);
        var num2 = (int)Generate(gen, b.Right, StackType.Value);
        gen.Emit(OpCodes.Stloc, local2);
        gen.Emit(OpCodes.Ldloca, local2);
        GenerateHasValue(gen, type);
        gen.Emit(OpCodes.Brfalse_S, label3);
        gen.Emit(OpCodes.Ldloca, local2);
        GenerateGetValueOrDefault(gen, type);
        gen.Emit(OpCodes.Ldc_I4_0);
        gen.Emit(OpCodes.Ceq);
        gen.Emit(OpCodes.Brfalse_S, label2);
        gen.Emit(OpCodes.Ldloca, local1);
        GenerateHasValue(gen, type);
        gen.Emit(OpCodes.Brfalse, label3);
        gen.Emit(OpCodes.Ldc_I4_0);
        gen.Emit(OpCodes.Br_S, label4);
        gen.MarkLabel(label2);
        gen.Emit(OpCodes.Ldc_I4_1);
        gen.Emit(OpCodes.Br_S, label4);
        gen.MarkLabel(label4);
        var constructor = type.GetConstructor(new Type[1]
        {
            typeof (bool)
        });
        gen.Emit(OpCodes.Newobj, constructor);
        gen.Emit(OpCodes.Stloc, local1);
        gen.Emit(OpCodes.Br, label5);
        gen.MarkLabel(label3);
        gen.Emit(OpCodes.Ldloca, local1);
        gen.Emit(OpCodes.Initobj, type);
        gen.MarkLabel(label5);
        return ReturnFromLocal(gen, ask, local1);
    }

    private void GenerateUnliftedOrElse(ILGenerator gen, BinaryExpression b)
    {
        var num1 = (int)Generate(gen, b.Left, StackType.Value);
        var label = gen.DefineLabel();
        gen.Emit(OpCodes.Dup);
        gen.Emit(OpCodes.Brtrue, label);
        gen.Emit(OpCodes.Pop);
        var num2 = (int)Generate(gen, b.Right, StackType.Value);
        gen.MarkLabel(label);
    }

    private void GenerateMethodOrElse(ILGenerator gen, BinaryExpression b)
    {
        var label = gen.DefineLabel();
        var num1 = (int)Generate(gen, b.Left, StackType.Value);
        gen.Emit(OpCodes.Dup);
        var parameterType = b.Method.GetParameters()[0].ParameterType;
        var types = new Type[1] { parameterType };
        var method = parameterType.GetMethod("op_True", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
        gen.Emit(OpCodes.Call, method);
        gen.Emit(OpCodes.Brtrue, label);
        var num2 = (int)Generate(gen, b.Right, StackType.Value);
        gen.Emit(OpCodes.Call, b.Method);
        gen.MarkLabel(label);
    }

    private StackType GenerateOrElse(
        ILGenerator gen,
        BinaryExpression b,
        StackType ask)
    {
        if (b.Method != null && !IsLiftedLogicalBinaryOperator(b.Left.Type, b.Right.Type, b.Method))
        {
            GenerateMethodOrElse(gen, b);
        }
        else
        {
            if (b.Left.Type == typeof(bool?))
                return GenerateLiftedOrElse(gen, b, ask);
            if (IsLiftedLogicalBinaryOperator(b.Left.Type, b.Right.Type, b.Method))
                return GenerateUserdefinedLiftedOrElse(gen, b, ask);
            GenerateUnliftedOrElse(gen, b);
        }
        return StackType.Value;
    }

    private static bool IsNullConstant(Expression e) => e.NodeType == ExpressionType.Constant && ((ConstantExpression)e).Value == null;

    private StackType GenerateBinary(
        ILGenerator gen,
        BinaryExpression b,
        StackType ask)
    {
        switch (b.NodeType)
        {
            case ExpressionType.AndAlso:
                return GenerateAndAlso(gen, b, ask);
            case ExpressionType.Coalesce:
                GenerateCoalesce(gen, b);
                return StackType.Value;
            case ExpressionType.OrElse:
                return GenerateOrElse(gen, b, ask);
            default:
                if (b.Method != null)
                    return GenerateBinaryMethod(gen, b, ask);
                if ((b.NodeType == ExpressionType.Equal || b.NodeType == ExpressionType.NotEqual) && (b.Type == typeof(bool) || b.Type == typeof(bool?)))
                {
                    if (IsNullConstant(b.Left) && !IsNullConstant(b.Right) && IsNullable(b.Right.Type))
                        return GenerateNullEquality(gen, b.NodeType, b.Right, b.IsLiftedToNull);
                    if (IsNullConstant(b.Right) && !IsNullConstant(b.Left) && IsNullable(b.Left.Type))
                        return GenerateNullEquality(gen, b.NodeType, b.Left, b.IsLiftedToNull);
                }
                var num1 = (int)Generate(gen, b.Left, StackType.Value);
                var num2 = (int)Generate(gen, b.Right, StackType.Value);
                return GenerateBinaryOp(gen, b.NodeType, b.Left.Type, b.Right.Type, b.Type, b.IsLiftedToNull, ask);
        }
    }

    private StackType GenerateNullEquality(
        ILGenerator gen,
        ExpressionType op,
        Expression e,
        bool isLiftedToNull)
    {
        var num = (int)Generate(gen, e, StackType.Value);
        if (isLiftedToNull)
        {
            gen.Emit(OpCodes.Pop);
            var constant = (int)GenerateConstant(gen, Expression.Constant(null, typeof(bool?)), StackType.Value);
        }
        else
        {
            var local = gen.DeclareLocal(e.Type);
            gen.Emit(OpCodes.Stloc, local);
            gen.Emit(OpCodes.Ldloca, local);
            GenerateHasValue(gen, e.Type);
            if (op == ExpressionType.Equal)
            {
                gen.Emit(OpCodes.Ldc_I4_0);
                gen.Emit(OpCodes.Ceq);
            }
        }
        return StackType.Value;
    }

    private StackType GenerateBinaryMethod(
        ILGenerator gen,
        BinaryExpression b,
        StackType ask)
    {
        if (b.IsLifted)
        {
            var parameterExpression1 = Expression.Parameter(Expression.GetNonNullableType(b.Left.Type), null);
            var parameterExpression2 = Expression.Parameter(Expression.GetNonNullableType(b.Right.Type), null);
            var mc = Expression.Call(null, b.Method, parameterExpression1, parameterExpression2);
            Type resultType;
            if (b.IsLiftedToNull)
            {
                resultType = Expression.GetNullableType(mc.Type);
            }
            else
            {
                switch (b.NodeType)
                {
                    case ExpressionType.Equal:
                    case ExpressionType.GreaterThan:
                    case ExpressionType.GreaterThanOrEqual:
                    case ExpressionType.LessThan:
                    case ExpressionType.LessThanOrEqual:
                    case ExpressionType.NotEqual:
                        if (mc.Type != typeof(bool))
                            throw Error.ArgumentMustBeBoolean();
                        resultType = typeof(bool);
                        break;
                    default:
                        resultType = Expression.GetNullableType(mc.Type);
                        break;
                }
            }
            IEnumerable<ParameterExpression> parameters = new ParameterExpression[2]
            {
                parameterExpression1,
                parameterExpression2
            };
            IEnumerable<Expression> arguments = new Expression[2]
            {
                b.Left,
                b.Right
            };
            Expression.ValidateLift(parameters, arguments);
            return GenerateLift(gen, b.NodeType, resultType, mc, parameters, arguments);
        }
        var node = Expression.Call(null, b.Method, b.Left, b.Right);
        return Generate(gen, node, ask);
    }

    private void GenerateTypeIs(ILGenerator gen, TypeBinaryExpression b)
    {
        var num = (int)Generate(gen, b.Expression, StackType.Value);
        if (b.Expression.Type == typeof(void))
        {
            gen.Emit(OpCodes.Ldc_I4_0);
        }
        else
        {
            if (b.Expression.Type.IsValueType)
                gen.Emit(OpCodes.Box, b.Expression.Type);
            gen.Emit(OpCodes.Isinst, b.TypeOperand);
            gen.Emit(OpCodes.Ldnull);
            gen.Emit(OpCodes.Cgt_Un);
        }
    }

    private StackType GenerateHoistedLocalAccess(
        ILGenerator gen,
        int hoistIndex,
        Type type,
        StackType ask)
    {
        GenerateConstInt(gen, hoistIndex);
        gen.Emit(OpCodes.Ldelem_Ref);
        var cls = MakeStrongBoxType(type);
        gen.Emit(OpCodes.Castclass, cls);
        var field = cls.GetField("Value", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
        if (ask == StackType.Value)
            gen.Emit(OpCodes.Ldfld, field);
        else
            gen.Emit(OpCodes.Ldflda, field);
        return ask;
    }

    private StackType GenerateGlobalAccess(
        ILGenerator gen,
        int iGlobal,
        Type type,
        StackType ask)
    {
        GenerateLoadExecutionScope(gen);
        gen.Emit(OpCodes.Ldfld, typeof(ExecutionScope).GetField("Globals", BindingFlags.Instance | BindingFlags.Public));
        GenerateConstInt(gen, iGlobal);
        gen.Emit(OpCodes.Ldelem_Ref);
        var cls = MakeStrongBoxType(type);
        gen.Emit(OpCodes.Castclass, cls);
        var field = cls.GetField("Value", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
        if (ask == StackType.Value)
            gen.Emit(OpCodes.Ldfld, field);
        else
            gen.Emit(OpCodes.Ldflda, field);
        return ask;
    }

    private int AddGlobal(Type type, object value)
    {
        var count = globals.Count;
        globals.Add(Activator.CreateInstance(MakeStrongBoxType(type), value));
        return count;
    }

    private void GenerateCastToType(ILGenerator gen, Type typeFrom, Type typeTo)
    {
        if (!typeFrom.IsValueType && typeTo.IsValueType)
            gen.Emit(OpCodes.Unbox_Any, typeTo);
        else if (typeFrom.IsValueType && !typeTo.IsValueType)
        {
            gen.Emit(OpCodes.Box, typeFrom);
            if (typeTo == typeof(object))
                return;
            gen.Emit(OpCodes.Castclass, typeTo);
        }
        else
        {
            if (typeFrom.IsValueType || typeTo.IsValueType)
                throw Error.InvalidCast(typeFrom, typeTo);
            gen.Emit(OpCodes.Castclass, typeTo);
        }
    }

    private void GenerateNullableToNullableConversion(
        ILGenerator gen,
        Type typeFrom,
        Type typeTo,
        bool isChecked)
    {
        var label1 = new Label();
        var label2 = new Label();
        var local1 = gen.DeclareLocal(typeFrom);
        gen.Emit(OpCodes.Stloc, local1);
        var local2 = gen.DeclareLocal(typeTo);
        gen.Emit(OpCodes.Ldloca, local1);
        GenerateHasValue(gen, typeFrom);
        var label3 = gen.DefineLabel();
        gen.Emit(OpCodes.Brfalse_S, label3);
        gen.Emit(OpCodes.Ldloca, local1);
        GenerateGetValueOrDefault(gen, typeFrom);
        var nonNullableType1 = GetNonNullableType(typeFrom);
        var nonNullableType2 = GetNonNullableType(typeTo);
        GenerateConvertToType(gen, nonNullableType1, nonNullableType2, isChecked);
        var constructor = typeTo.GetConstructor(new Type[1]
        {
            nonNullableType2
        });
        gen.Emit(OpCodes.Newobj, constructor);
        gen.Emit(OpCodes.Stloc, local2);
        var label4 = gen.DefineLabel();
        gen.Emit(OpCodes.Br_S, label4);
        gen.MarkLabel(label3);
        gen.Emit(OpCodes.Ldloca, local2);
        gen.Emit(OpCodes.Initobj, typeTo);
        gen.MarkLabel(label4);
        gen.Emit(OpCodes.Ldloc, local2);
    }

    private void GenerateNonNullableToNullableConversion(
        ILGenerator gen,
        Type typeFrom,
        Type typeTo,
        bool isChecked)
    {
        var local = gen.DeclareLocal(typeTo);
        var nonNullableType = GetNonNullableType(typeTo);
        GenerateConvertToType(gen, typeFrom, nonNullableType, isChecked);
        var constructor = typeTo.GetConstructor(new Type[1]
        {
            nonNullableType
        });
        gen.Emit(OpCodes.Newobj, constructor);
        gen.Emit(OpCodes.Stloc, local);
        gen.Emit(OpCodes.Ldloc, local);
    }

    private void GenerateNullableToNonNullableConversion(
        ILGenerator gen,
        Type typeFrom,
        Type typeTo,
        bool isChecked)
    {
        if (typeTo.IsValueType)
            GenerateNullableToNonNullableStructConversion(gen, typeFrom, typeTo, isChecked);
        else
            GenerateNullableToReferenceConversion(gen, typeFrom);
    }

    private void GenerateNullableToNonNullableStructConversion(
        ILGenerator gen,
        Type typeFrom,
        Type typeTo,
        bool isChecked)
    {
        var local = gen.DeclareLocal(typeFrom);
        gen.Emit(OpCodes.Stloc, local);
        gen.Emit(OpCodes.Ldloca, local);
        GenerateGetValue(gen, typeFrom);
        var nonNullableType = GetNonNullableType(typeFrom);
        GenerateConvertToType(gen, nonNullableType, typeTo, isChecked);
    }

    private void GenerateNullableToReferenceConversion(ILGenerator gen, Type typeFrom) => gen.Emit(OpCodes.Box, typeFrom);

    private void GenerateNullableConversion(
        ILGenerator gen,
        Type typeFrom,
        Type typeTo,
        bool isChecked)
    {
        var flag1 = IsNullable(typeFrom);
        var flag2 = IsNullable(typeTo);
        if (flag1 && flag2)
            GenerateNullableToNullableConversion(gen, typeFrom, typeTo, isChecked);
        else if (flag1)
            GenerateNullableToNonNullableConversion(gen, typeFrom, typeTo, isChecked);
        else
            GenerateNonNullableToNullableConversion(gen, typeFrom, typeTo, isChecked);
    }

    private void GenerateNumericConversion(
        ILGenerator gen,
        Type typeFrom,
        Type typeTo,
        bool isChecked)
    {
        var flag = IsUnsigned(typeFrom);
        IsFloatingPoint(typeFrom);
        if (typeTo == typeof(float))
        {
            if (flag)
                gen.Emit(OpCodes.Conv_R_Un);
            gen.Emit(OpCodes.Conv_R4);
        }
        else if (typeTo == typeof(double))
        {
            if (flag)
                gen.Emit(OpCodes.Conv_R_Un);
            gen.Emit(OpCodes.Conv_R8);
        }
        else
        {
            var typeCode = Type.GetTypeCode(typeTo);
            if (isChecked)
            {
                if (flag)
                {
                    switch (typeCode)
                    {
                        case TypeCode.Char:
                        case TypeCode.UInt16:
                            gen.Emit(OpCodes.Conv_Ovf_U2_Un);
                            break;
                        case TypeCode.SByte:
                            gen.Emit(OpCodes.Conv_Ovf_I1_Un);
                            break;
                        case TypeCode.Byte:
                            gen.Emit(OpCodes.Conv_Ovf_U1_Un);
                            break;
                        case TypeCode.Int16:
                            gen.Emit(OpCodes.Conv_Ovf_I2_Un);
                            break;
                        case TypeCode.Int32:
                            gen.Emit(OpCodes.Conv_Ovf_I4_Un);
                            break;
                        case TypeCode.UInt32:
                            gen.Emit(OpCodes.Conv_Ovf_U4_Un);
                            break;
                        case TypeCode.Int64:
                            gen.Emit(OpCodes.Conv_Ovf_I8_Un);
                            break;
                        case TypeCode.UInt64:
                            gen.Emit(OpCodes.Conv_Ovf_U8_Un);
                            break;
                        default:
                            throw Error.UnhandledConvert(typeTo);
                    }
                }
                else
                {
                    switch (typeCode)
                    {
                        case TypeCode.Char:
                        case TypeCode.UInt16:
                            gen.Emit(OpCodes.Conv_Ovf_U2);
                            break;
                        case TypeCode.SByte:
                            gen.Emit(OpCodes.Conv_Ovf_I1);
                            break;
                        case TypeCode.Byte:
                            gen.Emit(OpCodes.Conv_Ovf_U1);
                            break;
                        case TypeCode.Int16:
                            gen.Emit(OpCodes.Conv_Ovf_I2);
                            break;
                        case TypeCode.Int32:
                            gen.Emit(OpCodes.Conv_Ovf_I4);
                            break;
                        case TypeCode.UInt32:
                            gen.Emit(OpCodes.Conv_Ovf_U4);
                            break;
                        case TypeCode.Int64:
                            gen.Emit(OpCodes.Conv_Ovf_I8);
                            break;
                        case TypeCode.UInt64:
                            gen.Emit(OpCodes.Conv_Ovf_U8);
                            break;
                        default:
                            throw Error.UnhandledConvert(typeTo);
                    }
                }
            }
            else if (flag)
            {
                switch (typeCode)
                {
                    case TypeCode.Char:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                        gen.Emit(OpCodes.Conv_U2);
                        break;
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                        gen.Emit(OpCodes.Conv_U1);
                        break;
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                        gen.Emit(OpCodes.Conv_U4);
                        break;
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        gen.Emit(OpCodes.Conv_U8);
                        break;
                    default:
                        throw Error.UnhandledConvert(typeTo);
                }
            }
            else
            {
                switch (typeCode)
                {
                    case TypeCode.Char:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                        gen.Emit(OpCodes.Conv_I2);
                        break;
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                        gen.Emit(OpCodes.Conv_I1);
                        break;
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                        gen.Emit(OpCodes.Conv_I4);
                        break;
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        gen.Emit(OpCodes.Conv_I8);
                        break;
                    default:
                        throw Error.UnhandledConvert(typeTo);
                }
            }
        }
    }

    private void GenerateConvertToType(
        ILGenerator gen,
        Type typeFrom,
        Type typeTo,
        bool isChecked)
    {
        if (typeFrom == typeTo)
            return;
        var flag1 = IsNullable(typeFrom);
        var flag2 = IsNullable(typeTo);
        var nonNullableType1 = GetNonNullableType(typeFrom);
        var nonNullableType2 = GetNonNullableType(typeTo);
        if (typeFrom.IsInterface || typeTo.IsInterface || typeFrom == typeof(object) || typeTo == typeof(object))
            GenerateCastToType(gen, typeFrom, typeTo);
        else if (flag1 || flag2)
            GenerateNullableConversion(gen, typeFrom, typeTo, isChecked);
        else if ((!IsConvertible(typeFrom) || !IsConvertible(typeTo)) && (nonNullableType1.IsAssignableFrom(nonNullableType2) || nonNullableType2.IsAssignableFrom(nonNullableType1)))
            GenerateCastToType(gen, typeFrom, typeTo);
        else if (typeFrom.IsArray && typeTo.IsArray)
            GenerateCastToType(gen, typeFrom, typeTo);
        else
            GenerateNumericConversion(gen, typeFrom, typeTo, isChecked);
    }

    private StackType ReturnFromLocal(
        ILGenerator gen,
        StackType ask,
        LocalBuilder local)
    {
        if (ask == StackType.Address)
            gen.Emit(OpCodes.Ldloca, local);
        else
            gen.Emit(OpCodes.Ldloc, local);
        return ask;
    }

    private StackType GenerateUnaryOp(
        ILGenerator gen,
        ExpressionType op,
        Type operandType,
        Type resultType,
        StackType ask)
    {
        var flag = IsNullable(operandType);
        if (op == ExpressionType.ArrayLength)
        {
            gen.Emit(OpCodes.Ldlen);
            return StackType.Value;
        }
        if (flag)
        {
            switch (op)
            {
                case ExpressionType.Negate:
                case ExpressionType.UnaryPlus:
                case ExpressionType.NegateChecked:
                    var label1 = gen.DefineLabel();
                    var label2 = gen.DefineLabel();
                    var local1 = gen.DeclareLocal(operandType);
                    gen.Emit(OpCodes.Stloc, local1);
                    gen.Emit(OpCodes.Ldloca, local1);
                    GenerateHasValue(gen, operandType);
                    gen.Emit(OpCodes.Brfalse_S, label1);
                    gen.Emit(OpCodes.Ldloca, local1);
                    GenerateGetValueOrDefault(gen, operandType);
                    var nonNullableType1 = GetNonNullableType(resultType);
                    var unaryOp1 = (int)GenerateUnaryOp(gen, op, nonNullableType1, nonNullableType1, StackType.Value);
                    var constructor1 = resultType.GetConstructor(new Type[1]
                    {
                        nonNullableType1
                    });
                    gen.Emit(OpCodes.Newobj, constructor1);
                    gen.Emit(OpCodes.Stloc, local1);
                    gen.Emit(OpCodes.Br_S, label2);
                    gen.MarkLabel(label1);
                    gen.Emit(OpCodes.Ldloca, local1);
                    gen.Emit(OpCodes.Initobj, resultType);
                    gen.MarkLabel(label2);
                    return ReturnFromLocal(gen, ask, local1);
                case ExpressionType.Not:
                    if (operandType == typeof(bool?))
                    {
                        gen.DefineLabel();
                        var label3 = gen.DefineLabel();
                        var local2 = gen.DeclareLocal(operandType);
                        gen.Emit(OpCodes.Stloc, local2);
                        gen.Emit(OpCodes.Ldloca, local2);
                        GenerateHasValue(gen, operandType);
                        gen.Emit(OpCodes.Brfalse_S, label3);
                        gen.Emit(OpCodes.Ldloca, local2);
                        GenerateGetValueOrDefault(gen, operandType);
                        var nonNullableType2 = GetNonNullableType(operandType);
                        var unaryOp2 = (int)GenerateUnaryOp(gen, op, nonNullableType2, typeof(bool), StackType.Value);
                        var constructor2 = resultType.GetConstructor(new Type[1]
                        {
                            typeof (bool)
                        });
                        gen.Emit(OpCodes.Newobj, constructor2);
                        gen.Emit(OpCodes.Stloc, local2);
                        gen.MarkLabel(label3);
                        return ReturnFromLocal(gen, ask, local2);
                    }
                    goto case ExpressionType.Negate;
                case ExpressionType.TypeAs:
                    gen.Emit(OpCodes.Box, operandType);
                    gen.Emit(OpCodes.Isinst, resultType);
                    if (IsNullable(resultType))
                        gen.Emit(OpCodes.Unbox_Any, resultType);
                    return StackType.Value;
                default:
                    throw Error.UnhandledUnary(op);
            }
        }
        else
        {
            switch (op)
            {
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                    gen.Emit(OpCodes.Neg);
                    break;
                case ExpressionType.UnaryPlus:
                    gen.Emit(OpCodes.Nop);
                    break;
                case ExpressionType.Not:
                    if (operandType == typeof(bool))
                    {
                        gen.Emit(OpCodes.Ldc_I4_0);
                        gen.Emit(OpCodes.Ceq);
                        break;
                    }
                    gen.Emit(OpCodes.Not);
                    break;
                case ExpressionType.TypeAs:
                    if (operandType.IsValueType)
                        gen.Emit(OpCodes.Box, operandType);
                    gen.Emit(OpCodes.Isinst, resultType);
                    if (IsNullable(resultType))
                    {
                        gen.Emit(OpCodes.Unbox_Any, resultType);
                        break;
                    }
                    break;
                default:
                    throw Error.UnhandledUnary(op);
            }
            return StackType.Value;
        }
    }

    private StackType GenerateLiftedBinaryArithmetic(
        ILGenerator gen,
        ExpressionType op,
        Type leftType,
        Type rightType,
        Type resultType,
        StackType ask)
    {
        var flag1 = IsNullable(leftType);
        var flag2 = IsNullable(rightType);
        var label1 = gen.DefineLabel();
        var label2 = gen.DefineLabel();
        var local1 = gen.DeclareLocal(leftType);
        var local2 = gen.DeclareLocal(rightType);
        var local3 = gen.DeclareLocal(resultType);
        gen.Emit(OpCodes.Stloc, local2);
        gen.Emit(OpCodes.Stloc, local1);
        if (flag1 && flag2)
        {
            gen.Emit(OpCodes.Ldloca, local1);
            GenerateHasValue(gen, leftType);
            gen.Emit(OpCodes.Ldloca, local2);
            GenerateHasValue(gen, rightType);
            gen.Emit(OpCodes.And);
            gen.Emit(OpCodes.Brfalse_S, label1);
        }
        else if (flag1)
        {
            gen.Emit(OpCodes.Ldloca, local1);
            GenerateHasValue(gen, leftType);
            gen.Emit(OpCodes.Brfalse_S, label1);
        }
        else if (flag2)
        {
            gen.Emit(OpCodes.Ldloca, local2);
            GenerateHasValue(gen, rightType);
            gen.Emit(OpCodes.Brfalse_S, label1);
        }
        if (flag1)
        {
            gen.Emit(OpCodes.Ldloca, local1);
            GenerateGetValueOrDefault(gen, leftType);
        }
        else
            gen.Emit(OpCodes.Ldloc, local1);
        if (flag2)
        {
            gen.Emit(OpCodes.Ldloca, local2);
            GenerateGetValueOrDefault(gen, rightType);
        }
        else
            gen.Emit(OpCodes.Ldloc, local2);
        var binaryOp = (int)GenerateBinaryOp(gen, op, GetNonNullableType(leftType), GetNonNullableType(rightType), GetNonNullableType(resultType), false, StackType.Value);
        var constructor = resultType.GetConstructor(new Type[1]
        {
            GetNonNullableType(resultType)
        });
        gen.Emit(OpCodes.Newobj, constructor);
        gen.Emit(OpCodes.Stloc, local3);
        gen.Emit(OpCodes.Br_S, label2);
        gen.MarkLabel(label1);
        gen.Emit(OpCodes.Ldloca, local3);
        gen.Emit(OpCodes.Initobj, resultType);
        gen.MarkLabel(label2);
        return ReturnFromLocal(gen, ask, local3);
    }

    private StackType GenerateLiftedRelational(
        ILGenerator gen,
        ExpressionType op,
        Type leftType,
        Type rightType,
        Type resultType,
        bool liftedToNull,
        StackType ask)
    {
        var label1 = gen.DefineLabel();
        var label2 = gen.DefineLabel();
        var label3 = gen.DefineLabel();
        var local1 = gen.DeclareLocal(leftType);
        var local2 = gen.DeclareLocal(rightType);
        gen.Emit(OpCodes.Stloc, local2);
        gen.Emit(OpCodes.Stloc, local1);
        switch (op)
        {
            case ExpressionType.Equal:
                gen.Emit(OpCodes.Ldloca, local1);
                GenerateHasValue(gen, leftType);
                gen.Emit(OpCodes.Ldc_I4_0);
                gen.Emit(OpCodes.Ceq);
                gen.Emit(OpCodes.Ldloca, local2);
                GenerateHasValue(gen, rightType);
                gen.Emit(OpCodes.Ldc_I4_0);
                gen.Emit(OpCodes.Ceq);
                gen.Emit(OpCodes.And);
                gen.Emit(OpCodes.Dup);
                if (liftedToNull)
                    gen.Emit(OpCodes.Brtrue_S, label1);
                else
                    gen.Emit(OpCodes.Brtrue_S, label2);
                gen.Emit(OpCodes.Pop);
                gen.Emit(OpCodes.Ldloca, local1);
                GenerateHasValue(gen, leftType);
                gen.Emit(OpCodes.Ldloca, local2);
                GenerateHasValue(gen, rightType);
                gen.Emit(OpCodes.And);
                gen.Emit(OpCodes.Dup);
                if (liftedToNull)
                    gen.Emit(OpCodes.Brfalse_S, label1);
                else
                    gen.Emit(OpCodes.Brfalse_S, label2);
                gen.Emit(OpCodes.Pop);
                break;
            case ExpressionType.NotEqual:
                gen.Emit(OpCodes.Ldloca, local1);
                GenerateHasValue(gen, leftType);
                gen.Emit(OpCodes.Ldloca, local2);
                GenerateHasValue(gen, rightType);
                gen.Emit(OpCodes.Or);
                gen.Emit(OpCodes.Dup);
                if (liftedToNull)
                    gen.Emit(OpCodes.Brfalse_S, label1);
                else
                    gen.Emit(OpCodes.Brfalse_S, label2);
                gen.Emit(OpCodes.Pop);
                gen.Emit(OpCodes.Ldloca, local1);
                GenerateHasValue(gen, leftType);
                gen.Emit(OpCodes.Ldc_I4_0);
                gen.Emit(OpCodes.Ceq);
                gen.Emit(OpCodes.Ldloca, local2);
                GenerateHasValue(gen, rightType);
                gen.Emit(OpCodes.Ldc_I4_0);
                gen.Emit(OpCodes.Ceq);
                gen.Emit(OpCodes.Or);
                gen.Emit(OpCodes.Dup);
                if (liftedToNull)
                    gen.Emit(OpCodes.Brtrue_S, label1);
                else
                    gen.Emit(OpCodes.Brtrue_S, label2);
                gen.Emit(OpCodes.Pop);
                break;
            default:
                gen.Emit(OpCodes.Ldloca, local1);
                GenerateHasValue(gen, leftType);
                gen.Emit(OpCodes.Ldloca, local2);
                GenerateHasValue(gen, rightType);
                gen.Emit(OpCodes.And);
                gen.Emit(OpCodes.Dup);
                if (liftedToNull)
                    gen.Emit(OpCodes.Brfalse_S, label1);
                else
                    gen.Emit(OpCodes.Brfalse_S, label2);
                gen.Emit(OpCodes.Pop);
                break;
        }
        gen.Emit(OpCodes.Ldloca, local1);
        GenerateGetValueOrDefault(gen, leftType);
        gen.Emit(OpCodes.Ldloca, local2);
        GenerateGetValueOrDefault(gen, rightType);
        var binaryOp = GenerateBinaryOp(gen, op, GetNonNullableType(leftType), GetNonNullableType(rightType), GetNonNullableType(resultType), false, ask);
        gen.MarkLabel(label2);
        if (resultType != GetNonNullableType(resultType))
            GenerateConvertToType(gen, GetNonNullableType(resultType), resultType, true);
        gen.Emit(OpCodes.Br, label3);
        gen.MarkLabel(label1);
        gen.Emit(OpCodes.Pop);
        gen.Emit(OpCodes.Ldnull);
        gen.Emit(OpCodes.Unbox_Any, resultType);
        gen.MarkLabel(label3);
        return binaryOp;
    }

    private StackType GenerateLiftedBooleanAnd(
        ILGenerator gen,
        StackType ask)
    {
        var type = typeof(bool?);
        var label1 = gen.DefineLabel();
        var label2 = gen.DefineLabel();
        var label3 = gen.DefineLabel();
        var label4 = gen.DefineLabel();
        var label5 = gen.DefineLabel();
        var local1 = gen.DeclareLocal(type);
        var local2 = gen.DeclareLocal(type);
        gen.Emit(OpCodes.Stloc, local2);
        gen.Emit(OpCodes.Stloc, local1);
        gen.Emit(OpCodes.Ldloca, local1);
        GenerateHasValue(gen, type);
        gen.Emit(OpCodes.Brfalse, label1);
        gen.Emit(OpCodes.Ldloca, local1);
        GenerateGetValueOrDefault(gen, type);
        gen.Emit(OpCodes.Ldc_I4_0);
        gen.Emit(OpCodes.Ceq);
        gen.Emit(OpCodes.Brtrue, label2);
        gen.MarkLabel(label1);
        gen.Emit(OpCodes.Ldloca, local2);
        GenerateHasValue(gen, type);
        gen.Emit(OpCodes.Brfalse_S, label3);
        gen.Emit(OpCodes.Ldloca, local2);
        GenerateGetValueOrDefault(gen, type);
        gen.Emit(OpCodes.Ldc_I4_0);
        gen.Emit(OpCodes.Ceq);
        gen.Emit(OpCodes.Brtrue_S, label2);
        gen.Emit(OpCodes.Ldloca, local1);
        GenerateHasValue(gen, type);
        gen.Emit(OpCodes.Brfalse, label3);
        gen.Emit(OpCodes.Ldc_I4_1);
        gen.Emit(OpCodes.Br_S, label4);
        gen.MarkLabel(label2);
        gen.Emit(OpCodes.Ldc_I4_0);
        gen.Emit(OpCodes.Br_S, label4);
        gen.MarkLabel(label4);
        var constructor = type.GetConstructor(new Type[1]
        {
            typeof (bool)
        });
        gen.Emit(OpCodes.Newobj, constructor);
        gen.Emit(OpCodes.Stloc, local1);
        gen.Emit(OpCodes.Br, label5);
        gen.MarkLabel(label3);
        gen.Emit(OpCodes.Ldloca, local1);
        gen.Emit(OpCodes.Initobj, type);
        gen.MarkLabel(label5);
        return ReturnFromLocal(gen, ask, local1);
    }

    private StackType GenerateLiftedBooleanOr(
        ILGenerator gen,
        StackType ask)
    {
        var type = typeof(bool?);
        var label1 = gen.DefineLabel();
        var label2 = gen.DefineLabel();
        var label3 = gen.DefineLabel();
        var label4 = gen.DefineLabel();
        var label5 = gen.DefineLabel();
        var local1 = gen.DeclareLocal(type);
        var local2 = gen.DeclareLocal(type);
        gen.Emit(OpCodes.Stloc, local2);
        gen.Emit(OpCodes.Stloc, local1);
        gen.Emit(OpCodes.Ldloca, local1);
        GenerateHasValue(gen, type);
        gen.Emit(OpCodes.Brfalse, label1);
        gen.Emit(OpCodes.Ldloca, local1);
        GenerateGetValueOrDefault(gen, type);
        gen.Emit(OpCodes.Ldc_I4_0);
        gen.Emit(OpCodes.Ceq);
        gen.Emit(OpCodes.Brfalse, label2);
        gen.MarkLabel(label1);
        gen.Emit(OpCodes.Ldloca, local2);
        GenerateHasValue(gen, type);
        gen.Emit(OpCodes.Brfalse_S, label3);
        gen.Emit(OpCodes.Ldloca, local2);
        GenerateGetValueOrDefault(gen, type);
        gen.Emit(OpCodes.Ldc_I4_0);
        gen.Emit(OpCodes.Ceq);
        gen.Emit(OpCodes.Brfalse_S, label2);
        gen.Emit(OpCodes.Ldloca, local1);
        GenerateHasValue(gen, type);
        gen.Emit(OpCodes.Brfalse, label3);
        gen.Emit(OpCodes.Ldc_I4_0);
        gen.Emit(OpCodes.Br_S, label4);
        gen.MarkLabel(label2);
        gen.Emit(OpCodes.Ldc_I4_1);
        gen.Emit(OpCodes.Br_S, label4);
        gen.MarkLabel(label4);
        var constructor = type.GetConstructor(new Type[1]
        {
            typeof (bool)
        });
        gen.Emit(OpCodes.Newobj, constructor);
        gen.Emit(OpCodes.Stloc, local1);
        gen.Emit(OpCodes.Br, label5);
        gen.MarkLabel(label3);
        gen.Emit(OpCodes.Ldloca, local1);
        gen.Emit(OpCodes.Initobj, type);
        gen.MarkLabel(label5);
        return ReturnFromLocal(gen, ask, local1);
    }

    private StackType GenerateLiftedBinaryOp(
        ILGenerator gen,
        ExpressionType op,
        Type leftType,
        Type rightType,
        Type resultType,
        bool liftedToNull,
        StackType ask)
    {
        switch (op)
        {
            case ExpressionType.Add:
            case ExpressionType.AddChecked:
            case ExpressionType.Divide:
            case ExpressionType.ExclusiveOr:
            case ExpressionType.LeftShift:
            case ExpressionType.Modulo:
            case ExpressionType.Multiply:
            case ExpressionType.MultiplyChecked:
            case ExpressionType.RightShift:
            case ExpressionType.Subtract:
            case ExpressionType.SubtractChecked:
                return GenerateLiftedBinaryArithmetic(gen, op, leftType, rightType, resultType, ask);
            case ExpressionType.And:
                return leftType == typeof(bool?) ? GenerateLiftedBooleanAnd(gen, ask) : GenerateLiftedBinaryArithmetic(gen, op, leftType, rightType, resultType, ask);
            case ExpressionType.Equal:
            case ExpressionType.GreaterThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.LessThan:
            case ExpressionType.LessThanOrEqual:
            case ExpressionType.NotEqual:
                return GenerateLiftedRelational(gen, op, leftType, rightType, resultType, liftedToNull, ask);
            case ExpressionType.Or:
                return leftType == typeof(bool?) ? GenerateLiftedBooleanOr(gen, ask) : GenerateLiftedBinaryArithmetic(gen, op, leftType, rightType, resultType, ask);
            default:
                return StackType.Value;
        }
    }

    private static void GenerateUnliftedEquality(ILGenerator gen, ExpressionType op, Type type)
    {
        if (!type.IsPrimitive && type.IsValueType && !type.IsEnum)
            throw Error.OperatorNotImplementedForType(op, type);
        gen.Emit(OpCodes.Ceq);
        if (op != ExpressionType.NotEqual)
            return;
        gen.Emit(OpCodes.Ldc_I4_0);
        gen.Emit(OpCodes.Ceq);
    }

    private StackType GenerateUnliftedBinaryOp(
        ILGenerator gen,
        ExpressionType op,
        Type leftType,
        Type rightType)
    {
        if (op == ExpressionType.Equal || op == ExpressionType.NotEqual)
        {
            GenerateUnliftedEquality(gen, op, leftType);
            return StackType.Value;
        }
        if (!leftType.IsPrimitive)
            throw Error.OperatorNotImplementedForType(op, leftType);
        switch (op)
        {
            case ExpressionType.Add:
                gen.Emit(OpCodes.Add);
                break;
            case ExpressionType.AddChecked:
                var local1 = gen.DeclareLocal(leftType);
                var local2 = gen.DeclareLocal(rightType);
                gen.Emit(OpCodes.Stloc, local2);
                gen.Emit(OpCodes.Stloc, local1);
                gen.Emit(OpCodes.Ldloc, local1);
                gen.Emit(OpCodes.Ldloc, local2);
                if (IsFloatingPoint(leftType))
                {
                    gen.Emit(OpCodes.Add);
                    break;
                }
                if (IsUnsigned(leftType))
                {
                    gen.Emit(OpCodes.Add_Ovf_Un);
                    break;
                }
                gen.Emit(OpCodes.Add_Ovf);
                break;
            case ExpressionType.And:
            case ExpressionType.AndAlso:
                gen.Emit(OpCodes.And);
                break;
            case ExpressionType.Divide:
                if (IsUnsigned(leftType))
                {
                    gen.Emit(OpCodes.Div_Un);
                    break;
                }
                gen.Emit(OpCodes.Div);
                break;
            case ExpressionType.ExclusiveOr:
                gen.Emit(OpCodes.Xor);
                break;
            case ExpressionType.GreaterThan:
                if (IsUnsigned(leftType))
                {
                    gen.Emit(OpCodes.Cgt_Un);
                    break;
                }
                gen.Emit(OpCodes.Cgt);
                break;
            case ExpressionType.GreaterThanOrEqual:
                var label1 = gen.DefineLabel();
                var label2 = gen.DefineLabel();
                if (IsUnsigned(leftType))
                    gen.Emit(OpCodes.Bge_Un_S, label1);
                else
                    gen.Emit(OpCodes.Bge_S, label1);
                gen.Emit(OpCodes.Ldc_I4_0);
                gen.Emit(OpCodes.Br_S, label2);
                gen.MarkLabel(label1);
                gen.Emit(OpCodes.Ldc_I4_1);
                gen.MarkLabel(label2);
                break;
            case ExpressionType.LeftShift:
                var nonNullableType1 = GetNonNullableType(rightType);
                if (nonNullableType1 != typeof(int))
                    GenerateConvertToType(gen, nonNullableType1, typeof(int), true);
                gen.Emit(OpCodes.Shl);
                break;
            case ExpressionType.LessThan:
                if (IsUnsigned(leftType))
                {
                    gen.Emit(OpCodes.Clt_Un);
                    break;
                }
                gen.Emit(OpCodes.Clt);
                break;
            case ExpressionType.LessThanOrEqual:
                var label3 = gen.DefineLabel();
                var label4 = gen.DefineLabel();
                if (IsUnsigned(leftType))
                    gen.Emit(OpCodes.Ble_Un_S, label3);
                else
                    gen.Emit(OpCodes.Ble_S, label3);
                gen.Emit(OpCodes.Ldc_I4_0);
                gen.Emit(OpCodes.Br_S, label4);
                gen.MarkLabel(label3);
                gen.Emit(OpCodes.Ldc_I4_1);
                gen.MarkLabel(label4);
                break;
            case ExpressionType.Modulo:
                if (IsUnsigned(leftType))
                {
                    gen.Emit(OpCodes.Rem_Un);
                    break;
                }
                gen.Emit(OpCodes.Rem);
                break;
            case ExpressionType.Multiply:
                gen.Emit(OpCodes.Mul);
                break;
            case ExpressionType.MultiplyChecked:
                var local3 = gen.DeclareLocal(leftType);
                var local4 = gen.DeclareLocal(rightType);
                gen.Emit(OpCodes.Stloc, local4);
                gen.Emit(OpCodes.Stloc, local3);
                gen.Emit(OpCodes.Ldloc, local3);
                gen.Emit(OpCodes.Ldloc, local4);
                if (IsFloatingPoint(leftType))
                {
                    gen.Emit(OpCodes.Mul);
                    break;
                }
                if (IsUnsigned(leftType))
                {
                    gen.Emit(OpCodes.Mul_Ovf_Un);
                    break;
                }
                gen.Emit(OpCodes.Mul_Ovf);
                break;
            case ExpressionType.Or:
            case ExpressionType.OrElse:
                gen.Emit(OpCodes.Or);
                break;
            case ExpressionType.RightShift:
                var nonNullableType2 = GetNonNullableType(rightType);
                if (nonNullableType2 != typeof(int))
                    GenerateConvertToType(gen, nonNullableType2, typeof(int), true);
                if (IsUnsigned(leftType))
                {
                    gen.Emit(OpCodes.Shr_Un);
                    break;
                }
                gen.Emit(OpCodes.Shr);
                break;
            case ExpressionType.Subtract:
                gen.Emit(OpCodes.Sub);
                break;
            case ExpressionType.SubtractChecked:
                var local5 = gen.DeclareLocal(leftType);
                var local6 = gen.DeclareLocal(rightType);
                gen.Emit(OpCodes.Stloc, local6);
                gen.Emit(OpCodes.Stloc, local5);
                gen.Emit(OpCodes.Ldloc, local5);
                gen.Emit(OpCodes.Ldloc, local6);
                if (IsFloatingPoint(leftType))
                {
                    gen.Emit(OpCodes.Sub);
                    break;
                }
                if (IsUnsigned(leftType))
                {
                    gen.Emit(OpCodes.Sub_Ovf_Un);
                    break;
                }
                gen.Emit(OpCodes.Sub_Ovf);
                break;
            default:
                throw Error.UnhandledBinary(op);
        }
        return StackType.Value;
    }

    private StackType GenerateBinaryOp(
        ILGenerator gen,
        ExpressionType op,
        Type leftType,
        Type rightType,
        Type resultType,
        bool liftedToNull,
        StackType ask)
    {
        var flag1 = IsNullable(leftType);
        var flag2 = IsNullable(rightType);
        switch (op)
        {
            case ExpressionType.ArrayIndex:
                if (flag2)
                {
                    var local = gen.DeclareLocal(rightType);
                    gen.Emit(OpCodes.Stloc, local);
                    gen.Emit(OpCodes.Ldloca, local);
                    GenerateGetValue(gen, rightType);
                }
                var nonNullableType = GetNonNullableType(rightType);
                if (nonNullableType != typeof(int))
                    GenerateConvertToType(gen, nonNullableType, typeof(int), true);
                return GenerateArrayAccess(gen, leftType.GetElementType(), ask);
            case ExpressionType.Coalesce:
                throw Error.UnexpectedCoalesceOperator();
            default:
                return flag1 ? GenerateLiftedBinaryOp(gen, op, leftType, rightType, resultType, liftedToNull, ask) : GenerateUnliftedBinaryOp(gen, op, leftType, rightType);
        }
    }

    private StackType GenerateArgAccess(
        ILGenerator gen,
        int iArg,
        StackType ask)
    {
        if (ask == StackType.Value)
        {
            switch (iArg)
            {
                case 0:
                    gen.Emit(OpCodes.Ldarg_0);
                    break;
                case 1:
                    gen.Emit(OpCodes.Ldarg_1);
                    break;
                case 2:
                    gen.Emit(OpCodes.Ldarg_2);
                    break;
                case 3:
                    gen.Emit(OpCodes.Ldarg_3);
                    break;
                default:
                    if (iArg < 128)
                    {
                        gen.Emit(OpCodes.Ldarg_S, (byte)iArg);
                        break;
                    }
                    gen.Emit(OpCodes.Ldarg, iArg);
                    break;
            }
        }
        else if (iArg < 128)
            gen.Emit(OpCodes.Ldarga_S, (byte)iArg);
        else
            gen.Emit(OpCodes.Ldarga, iArg);
        return ask;
    }

    private void GenerateConstInt(ILGenerator gen, int value)
    {
        switch (value)
        {
            case -1:
                gen.Emit(OpCodes.Ldc_I4_M1);
                break;
            case 0:
                gen.Emit(OpCodes.Ldc_I4_0);
                break;
            case 1:
                gen.Emit(OpCodes.Ldc_I4_1);
                break;
            case 2:
                gen.Emit(OpCodes.Ldc_I4_2);
                break;
            case 3:
                gen.Emit(OpCodes.Ldc_I4_3);
                break;
            case 4:
                gen.Emit(OpCodes.Ldc_I4_4);
                break;
            case 5:
                gen.Emit(OpCodes.Ldc_I4_5);
                break;
            case 6:
                gen.Emit(OpCodes.Ldc_I4_6);
                break;
            case 7:
                gen.Emit(OpCodes.Ldc_I4_7);
                break;
            case 8:
                gen.Emit(OpCodes.Ldc_I4_8);
                break;
            default:
                if (value >= -127 && value < 128)
                {
                    gen.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
                    break;
                }
                gen.Emit(OpCodes.Ldc_I4, value);
                break;
        }
    }

    private void GenerateArrayAssign(ILGenerator gen, Type type)
    {
        if (type.IsEnum)
        {
            gen.Emit(OpCodes.Stelem, type);
        }
        else
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                    gen.Emit(OpCodes.Stelem_I1);
                    break;
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    gen.Emit(OpCodes.Stelem_I2);
                    break;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    gen.Emit(OpCodes.Stelem_I4);
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    gen.Emit(OpCodes.Stelem_I8);
                    break;
                case TypeCode.Single:
                    gen.Emit(OpCodes.Stelem_R4);
                    break;
                case TypeCode.Double:
                    gen.Emit(OpCodes.Stelem_R8);
                    break;
                default:
                    if (type.IsValueType)
                    {
                        gen.Emit(OpCodes.Stelem, type);
                        break;
                    }
                    gen.Emit(OpCodes.Stelem_Ref);
                    break;
            }
        }
    }

    private StackType GenerateArrayAccess(
        ILGenerator gen,
        Type type,
        StackType ask)
    {
        if (ask == StackType.Address)
            gen.Emit(OpCodes.Ldelema, type);
        else if (!type.IsValueType)
            gen.Emit(OpCodes.Ldelem_Ref);
        else if (type.IsEnum)
        {
            gen.Emit(OpCodes.Ldelem, type);
        }
        else
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.SByte:
                    gen.Emit(OpCodes.Ldelem_I1);
                    break;
                case TypeCode.Int16:
                    gen.Emit(OpCodes.Ldelem_I2);
                    break;
                case TypeCode.Int32:
                    gen.Emit(OpCodes.Ldelem_I4);
                    break;
                case TypeCode.Int64:
                    gen.Emit(OpCodes.Ldelem_I8);
                    break;
                case TypeCode.Single:
                    gen.Emit(OpCodes.Ldelem_R4);
                    break;
                case TypeCode.Double:
                    gen.Emit(OpCodes.Ldelem_R8);
                    break;
                default:
                    gen.Emit(OpCodes.Ldelem, type);
                    break;
            }
        }
        return ask;
    }

    private void GenerateHasValue(ILGenerator gen, Type nullableType)
    {
        var method = nullableType.GetMethod("get_HasValue", BindingFlags.Instance | BindingFlags.Public);
        gen.Emit(OpCodes.Call, method);
    }

    private void GenerateGetValue(ILGenerator gen, Type nullableType)
    {
        var method = nullableType.GetMethod("get_Value", BindingFlags.Instance | BindingFlags.Public);
        gen.Emit(OpCodes.Call, method);
    }

    private void GenerateGetValueOrDefault(ILGenerator gen, Type nullableType)
    {
        var method = nullableType.GetMethod("GetValueOrDefault", Type.EmptyTypes);
        gen.Emit(OpCodes.Call, method);
    }

    private static bool IsNullable(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

    private static Type GetNonNullableType(Type type) => IsNullable(type) ? type.GetGenericArguments()[0] : type;

    private static bool IsConvertible(Type type)
    {
        type = GetNonNullableType(type);
        if (type.IsEnum)
            return true;
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Boolean:
            case TypeCode.Char:
            case TypeCode.SByte:
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64:
            case TypeCode.Single:
            case TypeCode.Double:
                return true;
            default:
                return false;
        }
    }

    private static bool IsUnsigned(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            type = type.GetGenericArguments()[0];
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Char:
            case TypeCode.Byte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
                return true;
            default:
                return false;
        }
    }

    private static bool IsFloatingPoint(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            type = type.GetGenericArguments()[0];
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Single:
            case TypeCode.Double:
                return true;
            default:
                return false;
        }
    }

    internal class LambdaInfo
    {
        internal LambdaExpression Lambda;
        internal List<LambdaInfo> Lambdas;
        internal MethodInfo Method;
        internal Dictionary<ParameterExpression, int> HoistedLocals;

        internal LambdaInfo(
            LambdaExpression lambda,
            MethodInfo method,
            Dictionary<ParameterExpression, int> hoistedLocals,
            List<LambdaInfo> lambdas)
        {
            Lambda = lambda;
            Method = method;
            HoistedLocals = hoistedLocals;
            Lambdas = lambdas;
        }
    }

    private class CompileScope
    {
        internal readonly CompileScope Parent;
        internal readonly LambdaExpression Lambda;
        internal readonly Dictionary<ParameterExpression, LocalBuilder> Locals;
        internal readonly Dictionary<ParameterExpression, int> HoistedLocals;
        internal LocalBuilder HoistedLocalsVar;

        internal CompileScope(CompileScope parent, LambdaExpression lambda)
        {
            Parent = parent;
            Lambda = lambda;
            Locals = new Dictionary<ParameterExpression, LocalBuilder>();
            HoistedLocals = new Dictionary<ParameterExpression, int>();
        }
    }

    private enum StackType
    {
        Value,
        Address,
    }

    private class Hoister : ExpressionVisitor
    {
        private CompileScope expressionScope;
        private LambdaExpression current;
        private List<ParameterExpression> locals;

        internal Hoister()
        {
        }

        internal void Hoist(CompileScope scope)
        {
            expressionScope = scope;
            current = scope.Lambda;
            locals = new List<ParameterExpression>(scope.Lambda.Parameters);
            Visit(scope.Lambda.Body);
        }

        internal override Expression VisitParameter(ParameterExpression p)
        {
            if (locals.Contains(p) && expressionScope.Lambda != current && !expressionScope.HoistedLocals.ContainsKey(p))
                expressionScope.HoistedLocals.Add(p, expressionScope.HoistedLocals.Count);
            return p;
        }

        internal override Expression VisitInvocation(InvocationExpression iv)
        {
            if (expressionScope.Lambda == current)
            {
                if (iv.Expression.NodeType == ExpressionType.Lambda)
                    locals.AddRange(((LambdaExpression)iv.Expression).Parameters);
                else if (iv.Expression.NodeType == ExpressionType.Quote && iv.Expression.Type.IsSubclassOf(typeof(LambdaExpression)))
                    locals.AddRange(((LambdaExpression)((UnaryExpression)iv.Expression).Operand).Parameters);
            }
            return base.VisitInvocation(iv);
        }

        internal override Expression VisitLambda(LambdaExpression l)
        {
            var current = this.current;
            this.current = l;
            Visit(l.Body);
            this.current = current;
            return l;
        }
    }

    private struct WriteBack
    {
        public readonly LocalBuilder loc;
        public readonly Expression arg;

        public WriteBack(LocalBuilder loc, Expression arg)
        {
            this.loc = loc;
            this.arg = arg;
        }
    }
}