using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Linq.Expressions
{
    // All private methods moved from Expression.cs into this partial class file.
    // No logic changed, only relocated to keep Expression.cs shorter.
    public abstract partial class Expression
    {
        private static PropertyInfo GetProperty(MethodInfo mi)
        {
            foreach (var property in mi.DeclaringType.GetProperties((BindingFlags)(48 | (mi.IsStatic ? 8 : 4))))
            {
                if (property.CanRead && CheckMethod(mi, property.GetGetMethod(true)) || property.CanWrite && CheckMethod(mi, property.GetSetMethod(true)))
                    return property;
            }
            throw Error.MethodNotPropertyAccessor(mi.DeclaringType, mi.Name);
        }

        private static bool CheckMethod(MethodInfo method, MethodInfo propertyMethod)
        {
            if (method == propertyMethod)
                return true;
            var declaringType = method.DeclaringType;
            return declaringType.IsInterface && method.Name == propertyMethod.Name && declaringType.GetMethod(method.Name) == propertyMethod;
        }

        private static void ValidateCallArgs(
            Expression instance,
            MethodInfo method,
            ref ReadOnlyCollection<Expression> arguments)
        {
            if (method == null)
                throw Error.ArgumentNull(nameof(method));
            if (arguments == null)
                throw Error.ArgumentNull(nameof(arguments));
            ValidateMethodInfo(method);
            if (!method.IsStatic)
            {
                if (instance == null)
                    throw Error.ArgumentNull(nameof(instance));
                ValidateCallInstanceType(instance.Type, method);
            }
            ValidateArgumentTypes(method, ref arguments);
        }

        private static void ValidateCallInstanceType(Type instanceType, MethodInfo method)
        {
            if (!AreReferenceAssignable(method.DeclaringType, instanceType))
            {
                if (instanceType.IsValueType)
                {
                    if (AreReferenceAssignable(method.DeclaringType, typeof(object)) || AreReferenceAssignable(method.DeclaringType, typeof(ValueType)) || instanceType.IsEnum && AreReferenceAssignable(method.DeclaringType, typeof(Enum)))
                        return;
                    if (method.DeclaringType.IsInterface)
                    {
                        foreach (var src in instanceType.GetInterfaces())
                        {
                            if (AreReferenceAssignable(method.DeclaringType, src))
                                return;
                        }
                    }
                }
                throw Error.MethodNotDefinedForType(method, instanceType);
            }
        }

        private static void ValidateArgumentTypes(
            MethodInfo method,
            ref ReadOnlyCollection<Expression> arguments)
        {
            var parameters = method.GetParameters();
            if (parameters.Length > 0)
            {
                if (parameters.Length != arguments.Count)
                    throw Error.IncorrectNumberOfMethodCallArguments(method);
                List<Expression> sequence = null;
                var index1 = 0;
                for (var length = parameters.Length; index1 < length; ++index1)
                {
                    var expression = arguments[index1];
                    var parameterInfo = parameters[index1];
                    if (expression == null)
                        throw Error.ArgumentNull(nameof(arguments));
                    var type = parameterInfo.ParameterType;
                    if (type.IsByRef)
                        type = type.GetElementType();
                    ValidateType(type);
                    if (!AreReferenceAssignable(type, expression.Type))
                        expression = IsSameOrSubclass(typeof(Expression), type) && AreAssignable(type, expression.GetType()) ? (Expression)Quote(expression) : throw Error.ExpressionTypeDoesNotMatchMethodParameter(expression.Type, type, method);
                    if (sequence == null && expression != arguments[index1])
                    {
                        sequence = new List<Expression>(arguments.Count);
                        for (var index2 = 0; index2 < index1; ++index2)
                            sequence.Add(arguments[index2]);
                    }
                    sequence?.Add(expression);
                }
                if (sequence == null)
                    return;
                arguments = sequence.ToReadOnlyCollection<Expression>();
            }
            else if (arguments.Count > 0)
                throw Error.IncorrectNumberOfMethodCallArguments(method);
        }

        private static MethodInfo FindMethod(
            Type type,
            string methodName,
            Type[] typeArgs,
            Expression[] args,
            BindingFlags flags)
        {
            MemberInfo[] members = type.FindMembers(MemberTypes.Method, flags, Type.FilterNameIgnoreCase, methodName);
            if (members == null || members.Length == 0)
                throw Error.MethodDoesNotExistOnType(methodName, type);
            MethodInfo method;
            var bestMethod = FindBestMethod(members.Cast<MethodInfo>(), typeArgs, args, out method);
            if (bestMethod == 0)
                throw Error.MethodWithArgsDoesNotExistOnType(methodName, type);
            if (bestMethod > 1)
                throw Error.MethodWithMoreThanOneMatch(methodName, type);
            return method;
        }

        private static int FindBestMethod(
            IEnumerable<MethodInfo> methods,
            Type[] typeArgs,
            Expression[] args,
            out MethodInfo method)
        {
            var bestMethod = 0;
            method = null;
            foreach (var method1 in methods)
            {
                var m = ApplyTypeArgs(method1, typeArgs);
                if (m != null && IsCompatible(m, args))
                {
                    if (method == null || !method.IsPublic && m.IsPublic)
                    {
                        method = m;
                        bestMethod = 1;
                    }
                    else if (method.IsPublic == m.IsPublic)
                        ++bestMethod;
                }
            }
            return bestMethod;
        }

        private static MethodInfo ApplyTypeArgs(MethodInfo m, Type[] typeArgs)
        {
            if (typeArgs == null || typeArgs.Length == 0)
            {
                if (!m.IsGenericMethodDefinition)
                    return m;
            }
            else if (m.IsGenericMethodDefinition && m.GetGenericArguments().Length == typeArgs.Length)
                return m.MakeGenericMethod(typeArgs);
            return null;
        }

        private static bool IsCompatible(MethodInfo m, Expression[] args)
        {
            var parameters = m.GetParameters();
            if (parameters.Length != args.Length)
                return false;
            for (var index = 0; index < args.Length; ++index)
            {
                var expression = args[index];
                var src = expression != null ? expression.Type : throw Error.ArgumentNull("argument");
                var type = parameters[index].ParameterType;
                if (type.IsByRef)
                    type = type.GetElementType();
                if (!AreReferenceAssignable(type, src) && (!IsSameOrSubclass(typeof(Expression), type) || !AreAssignable(type, expression.GetType())))
                    return false;
            }
            return true;
        }

        private static BinaryExpression GetEqualityComparisonOperator(
            ExpressionType binaryType,
            string opName,
            Expression left,
            Expression right,
            bool liftToNull)
        {
            if (left.Type == right.Type && (IsNumeric(left.Type) || left.Type == typeof(object)))
                return IsNullableType(left.Type) && liftToNull ? new BinaryExpression(binaryType, left, right, typeof(bool?)) : new BinaryExpression(binaryType, left, right, typeof(bool));
            var definedBinaryOperator = GetUserDefinedBinaryOperator(binaryType, opName, left, right, liftToNull);
            if (definedBinaryOperator != null)
                return definedBinaryOperator;
            if (!HasBuiltInEqualityOperator(left.Type, right.Type) && !IsNullComparison(left, right))
                throw Error.BinaryOperatorNotDefined(binaryType, left.Type, right.Type);
            return IsNullableType(left.Type) && liftToNull ? new BinaryExpression(binaryType, left, right, typeof(bool?)) : new BinaryExpression(binaryType, left, right, typeof(bool));
        }

        private static bool IsNullComparison(Expression left, Expression right)
        {
            if (IsNullConstant(left) && !IsNullConstant(right) && IsNullableType(right.Type))
                return true;
            return IsNullConstant(right) && !IsNullConstant(left) && IsNullableType(left.Type);
        }

        private static bool HasBuiltInEqualityOperator(Type left, Type right)
        {
            if (left.IsInterface && !right.IsValueType || right.IsInterface && !left.IsValueType || !left.IsValueType && !right.IsValueType && (AreReferenceAssignable(left, right) || AreReferenceAssignable(right, left)))
                return true;
            if (left != right)
                return false;
            var nonNullableType = GetNonNullableType(left);
            return nonNullableType == typeof(bool) || IsNumeric(nonNullableType) || nonNullableType.IsEnum;
        }

        private static BinaryExpression GetComparisonOperator(
            ExpressionType binaryType,
            string opName,
            Expression left,
            Expression right,
            bool liftToNull)
        {
            if (left.Type != right.Type || !IsNumeric(left.Type))
                return GetUserDefinedBinaryOperatorOrThrow(binaryType, opName, left, right, liftToNull);
            return IsNullableType(left.Type) && liftToNull ? new BinaryExpression(binaryType, left, right, typeof(bool?)) : new BinaryExpression(binaryType, left, right, typeof(bool));
        }

        private static UnaryExpression GetUserDefinedCoercionOrThrow(
            ExpressionType coercionType,
            Expression expression,
            Type convertToType)
        {
            return GetUserDefinedCoercion(coercionType, expression, convertToType) ?? throw Error.CoercionOperatorNotDefined(expression.Type, convertToType);
        }

        private static UnaryExpression GetUserDefinedCoercion(
            ExpressionType coercionType,
            Expression expression,
            Type convertToType)
        {
            var nonNullableType1 = GetNonNullableType(expression.Type);
            var nonNullableType2 = GetNonNullableType(convertToType);
            var methods1 = nonNullableType1.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var conversionOperator1 = FindConversionOperator(methods1, expression.Type, convertToType);
            if (conversionOperator1 != null)
                return new UnaryExpression(coercionType, expression, conversionOperator1, convertToType);
            var methods2 = nonNullableType2.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var conversionOperator2 = FindConversionOperator(methods2, expression.Type, convertToType);
            if (conversionOperator2 != null)
                return new UnaryExpression(coercionType, expression, conversionOperator2, convertToType);
            if (nonNullableType1 != expression.Type || nonNullableType2 != convertToType)
            {
                var method = FindConversionOperator(methods1, nonNullableType1, nonNullableType2) ?? FindConversionOperator(methods2, nonNullableType1, nonNullableType2);
                if (method != null)
                    return new UnaryExpression(coercionType, expression, method, convertToType);
            }
            return null;
        }

        private static MethodInfo FindConversionOperator(
            MethodInfo[] methods,
            Type typeFrom,
            Type typeTo)
        {
            foreach (var method in methods)
            {
                if ((!(method.Name != "op_Implicit") || !(method.Name != "op_Explicit")) && method.ReturnType == typeTo && method.GetParameters()[0].ParameterType == typeFrom)
                    return method;
            }
            return null;
        }

        private static UnaryExpression GetUserDefinedUnaryOperatorOrThrow(
            ExpressionType unaryType,
            string name,
            Expression operand)
        {
            var definedUnaryOperator = GetUserDefinedUnaryOperator(unaryType, name, operand);
            if (definedUnaryOperator == null)
                throw Error.UnaryOperatorNotDefined(unaryType, operand.Type);
            ValidateParamswithOperandsOrThrow(definedUnaryOperator.Method.GetParameters()[0].ParameterType, operand.Type, unaryType, name);
            return definedUnaryOperator;
        }

        private static UnaryExpression GetUserDefinedUnaryOperator(
            ExpressionType unaryType,
            string name,
            Expression operand)
        {
            var type = operand.Type;
            var types = new Type[1] { type };
            var nonNullableType = GetNonNullableType(type);
            var method1 = nonNullableType.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
            if (method1 != null)
                return new UnaryExpression(unaryType, operand, method1, method1.ReturnType);
            if (IsNullableType(type))
            {
                types[0] = nonNullableType;
                var method2 = nonNullableType.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
                if (method2 != null && method2.ReturnType.IsValueType && !IsNullableType(method2.ReturnType))
                    return new UnaryExpression(unaryType, operand, method2, GetNullableType(method2.ReturnType));
            }
            return null;
        }

        private static void ValidateParamswithOperandsOrThrow(
            Type paramType,
            Type operandType,
            ExpressionType exprType,
            string name)
        {
            if (IsNullableType(paramType) && !IsNullableType(operandType))
                throw Error.OperandTypesDoNotMatchParameters(exprType, name);
        }

        private static BinaryExpression GetUserDefinedBinaryOperatorOrThrow(
            ExpressionType binaryType,
            string name,
            Expression left,
            Expression right,
            bool liftToNull)
        {
            var definedBinaryOperator = GetUserDefinedBinaryOperator(binaryType, name, left, right, liftToNull);
            if (definedBinaryOperator == null)
                throw Error.BinaryOperatorNotDefined(binaryType, left.Type, right.Type);
            ValidateParamswithOperandsOrThrow(definedBinaryOperator.Method.GetParameters()[0].ParameterType, left.Type, binaryType, name);
            ValidateParamswithOperandsOrThrow(definedBinaryOperator.Method.GetParameters()[1].ParameterType, right.Type, binaryType, name);
            return definedBinaryOperator;
        }

        private static MethodInfo GetUserDefinedBinaryOperator(
            ExpressionType binaryType,
            Type leftType,
            Type rightType,
            string name)
        {
            var types = new Type[2] { leftType, rightType };
            var nonNullableType1 = GetNonNullableType(leftType);
            var nonNullableType2 = GetNonNullableType(rightType);
            var bindingAttr = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var method = nonNullableType1.GetMethod(name, bindingAttr, null, types, null) ?? nonNullableType2.GetMethod(name, bindingAttr, null, types, null);
            if (IsLiftingConditionalLogicalOperator(leftType, rightType, method, binaryType))
                method = GetUserDefinedBinaryOperator(binaryType, nonNullableType1, nonNullableType2, name);
            return method;
        }

        private static bool IsLiftingConditionalLogicalOperator(
            Type left,
            Type right,
            MethodInfo method,
            ExpressionType binaryType)
        {
            if (!IsNullableType(right) || !IsNullableType(left) || method != null)
                return false;
            return binaryType == ExpressionType.AndAlso || binaryType == ExpressionType.OrElse;
        }

        private static BinaryExpression GetUserDefinedBinaryOperator(
            ExpressionType binaryType,
            string name,
            Expression left,
            Expression right,
            bool liftToNull)
        {
            var definedBinaryOperator1 = GetUserDefinedBinaryOperator(binaryType, left.Type, right.Type, name);
            if (definedBinaryOperator1 != null)
                return new BinaryExpression(binaryType, left, right, definedBinaryOperator1, definedBinaryOperator1.ReturnType);
            if (IsNullableType(left.Type) && IsNullableType(right.Type))
            {
                var nonNullableType1 = GetNonNullableType(left.Type);
                var nonNullableType2 = GetNonNullableType(right.Type);
                var definedBinaryOperator2 = GetUserDefinedBinaryOperator(binaryType, nonNullableType1, nonNullableType2, name);
                if (definedBinaryOperator2 != null && definedBinaryOperator2.ReturnType.IsValueType && !IsNullableType(definedBinaryOperator2.ReturnType))
                    return definedBinaryOperator2.ReturnType != typeof(bool) || liftToNull ? new BinaryExpression(binaryType, left, right, definedBinaryOperator2, GetNullableType(definedBinaryOperator2.ReturnType)) : new BinaryExpression(binaryType, left, right, definedBinaryOperator2, typeof(bool));
            }
            return null;
        }

        private static void ValidateOperator(MethodInfo method)
        {
            ValidateMethodInfo(method);
            if (!method.IsStatic)
                throw Error.UserDefinedOperatorMustBeStatic(method);
            if (method.ReturnType == typeof(void))
                throw Error.UserDefinedOperatorMustNotBeVoid(method);
        }

        private static void ValidateUserDefinedConditionalLogicOperator(
            ExpressionType nodeType,
            Type left,
            Type right,
            MethodInfo method)
        {
            ValidateOperator(method);
            var parameters = method.GetParameters();
            if (parameters.Length != 2)
                throw Error.IncorrectNumberOfMethodCallArguments(method);
            if (!ParameterIsAssignable(parameters[0], left) && (!IsNullableType(left) || !ParameterIsAssignable(parameters[0], GetNonNullableType(left))))
                throw Error.OperandTypesDoNotMatchParameters(nodeType, method.Name);
            if (!ParameterIsAssignable(parameters[1], right) && (!IsNullableType(right) || !ParameterIsAssignable(parameters[1], GetNonNullableType(right))))
                throw Error.OperandTypesDoNotMatchParameters(nodeType, method.Name);
            if (parameters[0].ParameterType != parameters[1].ParameterType)
                throw Error.LogicalOperatorMustHaveConsistentTypes(nodeType, method.Name);
            if (method.ReturnType != parameters[0].ParameterType)
                throw Error.LogicalOperatorMustHaveConsistentTypes(nodeType, method.Name);
            if (IsValidLiftedConditionalLogicalOperator(left, right, parameters))
            {
                left = GetNonNullableType(left);
                right = GetNonNullableType(left);
            }
            var types = new Type[1]
            {
                parameters[0].ParameterType
            };
            var method1 = method.DeclaringType.GetMethod("op_True", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
            var method2 = method.DeclaringType.GetMethod("op_False", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
            if (method1 == null || method2 == null)
                throw Error.LogicalOperatorMustHaveBooleanOperators(nodeType, method.Name);
            if (method1.ReturnType != typeof(bool))
                throw Error.LogicalOperatorMustHaveBooleanOperators(nodeType, method.Name);
            if (method2.ReturnType != typeof(bool))
                throw Error.LogicalOperatorMustHaveBooleanOperators(nodeType, method.Name);
        }

        private static bool IsValidLiftedConditionalLogicalOperator(
            Type left,
            Type right,
            ParameterInfo[] pms)
        {
            return left == right && IsNullableType(right) && pms[1].ParameterType == GetNonNullableType(right);
        }

        private static UnaryExpression GetMethodBasedCoercionOperator(
            ExpressionType unaryType,
            Expression operand,
            Type convertToType,
            MethodInfo method)
        {
            ValidateOperator(method);
            var parameters = method.GetParameters();
            if (parameters.Length != 1)
                throw Error.IncorrectNumberOfMethodCallArguments(method);
            if (ParameterIsAssignable(parameters[0], operand.Type) && method.ReturnType == convertToType)
                return new UnaryExpression(unaryType, operand, method, method.ReturnType);
            if ((IsNullableType(operand.Type) || IsNullableType(convertToType)) && ParameterIsAssignable(parameters[0], GetNonNullableType(operand.Type)) && method.ReturnType == GetNonNullableType(convertToType))
                return new UnaryExpression(unaryType, operand, method, convertToType);
            throw Error.OperandTypesDoNotMatchParameters(unaryType, method.Name);
        }

        private static UnaryExpression GetMethodBasedUnaryOperator(
            ExpressionType unaryType,
            Expression operand,
            MethodInfo method)
        {
            ValidateOperator(method);
            var parameters = method.GetParameters();
            if (parameters.Length != 1)
                throw Error.IncorrectNumberOfMethodCallArguments(method);
            if (ParameterIsAssignable(parameters[0], operand.Type))
            {
                ValidateParamswithOperandsOrThrow(parameters[0].ParameterType, operand.Type, unaryType, method.Name);
                return new UnaryExpression(unaryType, operand, method, method.ReturnType);
            }
            if (IsNullableType(operand.Type) && ParameterIsAssignable(parameters[0], GetNonNullableType(operand.Type)) && method.ReturnType.IsValueType && !IsNullableType(method.ReturnType))
                return new UnaryExpression(unaryType, operand, method, GetNullableType(method.ReturnType));
            throw Error.OperandTypesDoNotMatchParameters(unaryType, method.Name);
        }

        private static BinaryExpression GetMethodBasedBinaryOperator(
            ExpressionType binaryType,
            Expression left,
            Expression right,
            MethodInfo method,
            bool liftToNull)
        {
            ValidateOperator(method);
            var parameters = method.GetParameters();
            if (parameters.Length != 2)
                throw Error.IncorrectNumberOfMethodCallArguments(method);
            if (ParameterIsAssignable(parameters[0], left.Type) && ParameterIsAssignable(parameters[1], right.Type))
            {
                ValidateParamswithOperandsOrThrow(parameters[0].ParameterType, left.Type, binaryType, method.Name);
                ValidateParamswithOperandsOrThrow(parameters[1].ParameterType, right.Type, binaryType, method.Name);
                return new BinaryExpression(binaryType, left, right, method, method.ReturnType);
            }
            if (!IsNullableType(left.Type) || !IsNullableType(right.Type) || !ParameterIsAssignable(parameters[0], GetNonNullableType(left.Type)) || !ParameterIsAssignable(parameters[1], GetNonNullableType(right.Type)) || !method.ReturnType.IsValueType || IsNullableType(method.ReturnType))
                throw Error.OperandTypesDoNotMatchParameters(binaryType, method.Name);
            return method.ReturnType != typeof(bool) || liftToNull ? new BinaryExpression(binaryType, left, right, method, GetNullableType(method.ReturnType)) : new BinaryExpression(binaryType, left, right, method, typeof(bool));
        }

        private static bool ParameterIsAssignable(ParameterInfo pi, Type argType)
        {
            var dest = pi.ParameterType;
            if (dest.IsByRef)
                dest = dest.GetElementType();
            return AreReferenceAssignable(dest, argType);
        }

        private static void ValidateIntegerArg(Type type)
        {
            if (!IsInteger(type))
                throw Error.ArgumentMustBeInteger();
        }

        private static void ValidateIntegerOrBoolArg(Type type)
        {
            if (!IsIntegerOrBool(type))
                throw Error.ArgumentMustBeIntegerOrBoolean();
        }

        private static void ValidateNumericArg(Type type)
        {
            if (!IsNumeric(type))
                throw Error.ArgumentMustBeNumeric();
        }

        private static void ValidateConvertibleArg(Type type)
        {
            if (!IsConvertible(type))
                throw Error.ArgumentMustBeConvertible();
        }

        private static void ValidateBoolArg(Type type)
        {
            if (!IsBool(type))
                throw Error.ArgumentMustBeBoolean();
        }

        private static Type ValidateCoalesceArgTypes(Type left, Type right)
        {
            var nonNullableType = GetNonNullableType(left);
            if (left.IsValueType && !IsNullableType(left))
                throw Error.CoalesceUsedOnNonNullType();
            if (IsNullableType(left) && IsImplicitlyConvertible(right, nonNullableType))
                return nonNullableType;
            if (IsImplicitlyConvertible(right, left))
                return left;
            return IsImplicitlyConvertible(nonNullableType, right) ? right : throw Error.ArgumentTypesMustMatch();
        }

        private static void ValidateSameArgTypes(Type left, Type right)
        {
            if (left != right)
                throw Error.ArgumentTypesMustMatch();
        }

        private static void ValidateElementInitAddMethodInfo(MethodInfo addMethod)
        {
            ValidateMethodInfo(addMethod);
            if (addMethod.GetParameters().Length == 0)
                throw Error.ElementInitializerMethodWithZeroArgs();
            if (!addMethod.Name.Equals("Add", StringComparison.OrdinalIgnoreCase))
                throw Error.ElementInitializerMethodNotAdd();
            if (addMethod.IsStatic)
                throw Error.ElementInitializerMethodStatic();
            foreach (var parameter in addMethod.GetParameters())
            {
                if (parameter.ParameterType.IsByRef)
                    throw Error.ElementInitializerMethodNoRefOutParam(parameter.Name, addMethod.Name);
            }
        }

        private static void ValidateMethodInfo(MethodInfo method)
        {
            if (method.IsGenericMethodDefinition)
                throw Error.MethodIsGeneric(method);
            if (method.ContainsGenericParameters)
                throw Error.MethodContainsGenericParameters(method);
        }

        private static void ValidateType(Type type)
        {
            if (type.IsGenericTypeDefinition)
                throw Error.TypeIsGeneric(type);
            if (type.ContainsGenericParameters)
                throw Error.TypeContainsGenericParameters(type);
        }

        private static void ValidateNewArgs(
            ConstructorInfo constructor,
            ref ReadOnlyCollection<Expression> arguments,
            ReadOnlyCollection<MemberInfo> members)
        {
            ParameterInfo[] parameters;
            if ((parameters = constructor.GetParameters()).Length > 0)
            {
                if (arguments.Count != parameters.Length)
                    throw Error.IncorrectNumberOfConstructorArguments();
                if (arguments.Count != members.Count)
                    throw Error.IncorrectNumberOfArgumentsForMembers();
                List<Expression> sequence = null;
                var index1 = 0;
                for (var count = arguments.Count; index1 < count; ++index1)
                {
                    var expression = arguments[index1];
                    if (expression == null)
                        throw Error.ArgumentNull("argument");
                    var member = members[index1];
                    if (member == null)
                        throw Error.ArgumentNull("member");
                    if (member.DeclaringType != constructor.DeclaringType)
                        throw Error.ArgumentMemberNotDeclOnType(member.Name, constructor.DeclaringType.Name);
                    Type memberType;
                    ValidateAnonymousTypeMember(member, out memberType);
                    if (!AreReferenceAssignable(expression.Type, memberType))
                        expression = IsSameOrSubclass(typeof(Expression), memberType) && AreAssignable(memberType, expression.GetType()) ? (Expression)Quote(expression) : throw Error.ArgumentTypeDoesNotMatchMember(expression.Type, memberType);
                    var type = parameters[index1].ParameterType;
                    if (type.IsByRef)
                        type = type.GetElementType();
                    if (!AreReferenceAssignable(type, expression.Type))
                    {
                        if (!IsSameOrSubclass(typeof(Expression), type) || !AreAssignable(type, expression.Type))
                            throw Error.ExpressionTypeDoesNotMatchConstructorParameter(expression.Type, type);
                        expression = Quote(expression);
                    }
                    if (sequence == null && expression != arguments[index1])
                    {
                        sequence = new List<Expression>(arguments.Count);
                        for (var index2 = 0; index2 < index1; ++index2)
                            sequence.Add(arguments[index2]);
                    }
                    sequence?.Add(expression);
                }
                if (sequence == null)
                    return;
                arguments = sequence.ToReadOnlyCollection<Expression>();
            }
            else
            {
                if (arguments != null && arguments.Count > 0)
                    throw Error.IncorrectNumberOfConstructorArguments();
                if (members != null && members.Count > 0)
                    throw Error.IncorrectNumberOfMembersForGivenConstructor();
            }
        }

        private static void ValidateNewArgs(
            Type type,
            ConstructorInfo constructor,
            ref ReadOnlyCollection<Expression> arguments)
        {
            if (type == null)
                throw Error.ArgumentNull(nameof(type));
            if (!type.IsValueType && constructor == null)
                throw Error.ArgumentNull(nameof(constructor));
            ParameterInfo[] parameters;
            if (constructor != null && (parameters = constructor.GetParameters()).Length > 0)
            {
                if (arguments.Count != parameters.Length)
                    throw Error.IncorrectNumberOfConstructorArguments();
                List<Expression> sequence = null;
                var index1 = 0;
                for (var count = arguments.Count; index1 < count; ++index1)
                {
                    var expression = arguments[index1];
                    var parameterInfo = parameters[index1];
                    if (expression == null)
                        throw Error.ArgumentNull(nameof(arguments));
                    var type1 = parameterInfo.ParameterType;
                    if (type1.IsByRef)
                        type1 = type1.GetElementType();
                    if (!AreReferenceAssignable(type1, expression.Type))
                        expression = IsSameOrSubclass(typeof(Expression), type1) && AreAssignable(type1, expression.GetType()) ? (Expression)Quote(expression) : throw Error.ExpressionTypeDoesNotMatchConstructorParameter(expression.Type, type1);
                    if (sequence == null && expression != arguments[index1])
                    {
                        sequence = new List<Expression>(arguments.Count);
                        for (var index2 = 0; index2 < index1; ++index2)
                            sequence.Add(arguments[index2]);
                    }
                    sequence?.Add(expression);
                }
                if (sequence == null)
                    return;
                arguments = sequence.ToReadOnlyCollection<Expression>();
            }
            else if (arguments != null && arguments.Count > 0)
                throw Error.IncorrectNumberOfConstructorArguments();
        }

        private static void ValidateSettableFieldOrPropertyMember(
            MemberInfo member,
            out Type memberType)
        {
            switch (member)
            {
                case FieldInfo fieldInfo:
                    memberType = fieldInfo.FieldType;
                    break;
                case PropertyInfo p0:
                    memberType = p0.CanWrite ? p0.PropertyType : throw Error.PropertyDoesNotHaveSetter(p0);
                    break;
                default:
                    throw Error.ArgumentMustBeFieldInfoOrPropertInfo();
            }
        }

        private static void ValidateAnonymousTypeMember(MemberInfo member, out Type memberType)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    var fieldInfo = member as FieldInfo;
                    memberType = !fieldInfo?.IsStatic ?? true ? fieldInfo!.FieldType : throw Error.ArgumentMustBeInstanceMember();
                    break;
                case MemberTypes.Method:
                    var methodInfo = member as MethodInfo;
                    memberType = !methodInfo?.IsStatic ?? true ? methodInfo!.ReturnType : throw Error.ArgumentMustBeInstanceMember();
                    break;
                case MemberTypes.Property:
                    var p0 = member as PropertyInfo;
                    if (!p0.CanRead)
                        throw Error.PropertyDoesNotHaveGetter(p0);
                    memberType = !p0.GetGetMethod().IsStatic ? p0.PropertyType : throw Error.ArgumentMustBeInstanceMember();
                    break;
                default:
                    throw Error.ArgumentMustBeFieldInfoOrPropertInfoOrMethod();
            }
        }

        private static void ValidateGettableFieldOrPropertyMember(
            MemberInfo member,
            out Type memberType)
        {
            switch (member)
            {
                case FieldInfo fieldInfo:
                    memberType = fieldInfo.FieldType;
                    break;
                case PropertyInfo p0:
                    memberType = p0.CanRead ? p0.PropertyType : throw Error.PropertyDoesNotHaveGetter(p0);
                    break;
                default:
                    throw Error.ArgumentMustBeFieldInfoOrPropertInfo();
            }
        }

        private static void ValidateMemberInitArgs(
            Type type,
            ReadOnlyCollection<MemberBinding> bindings)
        {
            var index = 0;
            for (var count = bindings.Count; index < count; ++index)
            {
                var binding = bindings[index];
                if (!AreAssignable(binding.Member.DeclaringType, type))
                    throw Error.NotAMemberOfType(binding.Member.Name, type);
            }
        }

        private static BinaryExpression GetComparisonOperator(
            ExpressionType binaryType,
            string opName,
            Expression left,
            Expression right,
            bool liftToNull)
        {
            if (left.Type != right.Type || !IsNumeric(left.Type))
                return GetUserDefinedBinaryOperatorOrThrow(binaryType, opName, left, right, liftToNull);
            return IsNullableType(left.Type) && liftToNull ? new BinaryExpression(binaryType, left, right, typeof(bool?)) : new BinaryExpression(binaryType, left, right, typeof(bool));
        }

        private static UnaryExpression GetUserDefinedCoercionOrThrow(
            ExpressionType coercionType,
            Expression expression,
            Type convertToType)
        {
            return GetUserDefinedCoercion(coercionType, expression, convertToType) ?? throw Error.CoercionOperatorNotDefined(expression.Type, convertToType);
        }

        // Note: many helper predicates and small validators were private in the original file
        // and are also moved here. They are left unchanged.

        private static bool IsNullConstant(Expression expr) => expr is ConstantExpression constantExpression && constantExpression.Value == null;

        private static bool IsUnSigned(Type type)
        {
            type = GetNonNullableType(type);
            if (type.IsEnum)
                return false;
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsArithmetic(Type type)
        {
            type = GetNonNullableType(type);
            if (type.IsEnum)
                return false;
            switch (Type.GetTypeCode(type))
            {
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

        private static bool IsNumeric(Type type)
        {
            type = GetNonNullableType(type);
            if (type.IsEnum)
                return false;
            switch (Type.GetTypeCode(type))
            {
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

        private static bool IsImplicitlyConvertible(Type source, Type destination) => IsIdentityConversion(source, destination) || IsImplicitNumericConversion(source, destination) || IsImplicitReferenceConversion(source, destination) || IsImplicitBoxingConversion(source, destination) || IsImplicitNullableConversion(source, destination);

        private static bool IsIdentityConversion(Type source, Type destination) => source == destination;

        private static bool IsImplicitNumericConversion(Type source, Type destination)
        {
            var typeCode1 = Type.GetTypeCode(source);
            var typeCode2 = Type.GetTypeCode(destination);
            switch (typeCode1)
            {
                case TypeCode.Char:
                    switch (typeCode2)
                    {
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                        default:
                            return false;
                    }
                case TypeCode.SByte:
                    switch (typeCode2)
                    {
                        case TypeCode.Int16:
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                        default:
                            return false;
                    }
                case TypeCode.Byte:
                    switch (typeCode2)
                    {
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                        default:
                            return false;
                    }
                case TypeCode.Int16:
                    switch (typeCode2)
                    {
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                        default:
                            return false;
                    }
                case TypeCode.UInt16:
                    switch (typeCode2)
                    {
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                        default:
                            return false;
                    }
                case TypeCode.Int32:
                    switch (typeCode2)
                    {
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                        default:
                            return false;
                    }
                case TypeCode.UInt32:
                    switch (typeCode2)
                    {
                        case TypeCode.UInt32:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                        default:
                            return false;
                    }
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    switch (typeCode2)
                    {
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                        default:
                            return false;
                    }
                case TypeCode.Single:
                    return typeCode2 == TypeCode.Double;
                default:
                    return false;
            }
        }

        private static bool IsImplicitReferenceConversion(Type source, Type destination) => AreAssignable(destination, source);

        private static bool IsImplicitBoxingConversion(Type source, Type destination) => source.IsValueType && (destination == typeof(object) || destination == typeof(ValueType)) || source.IsEnum && destination == typeof(Enum);

        private static bool IsImplicitNullableConversion(Type source, Type destination) => IsNullableType(destination) && IsImplicitlyConvertible(GetNonNullableType(source), GetNonNullableType(destination));

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

        private static bool IsIntegerOrBool(Type type)
        {
            type = GetNonNullableType(type);
            if (type.IsEnum)
                return false;
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
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

        private static bool IsBool(Type type)
        {
            type = GetNonNullableType(type);
            return type == typeof(bool);
        }

        // Create a dynamic module builder for emitted delegate types.
        private static ModuleBuilder EnsureDelegateModuleBuilder()
        {
            var asmName = new AssemblyName("ExpressionGeneratedDelegates");
            // Full framework
            var asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
            var moduleBuilder = asmBuilder.DefineDynamicModule("ExpressionGeneratedDelegatesModule");
            return moduleBuilder;
        }

        private static Type CreateDelegateTypeWithByRef(Type[] parameterTypes, Type returnType, string uniqueKey)
        {
            var module = EnsureDelegateModuleBuilder();

            var name = "Delegate_" + Math.Abs(uniqueKey.GetHashCode()).ToString("X");
            var tb = module.DefineType(name, TypeAttributes.Sealed | TypeAttributes.Public, typeof(MulticastDelegate));

            var ctor = tb.DefineConstructor(
                MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public,
                CallingConventions.Standard,
                new Type[] { typeof(object), typeof(IntPtr) });
            ctor.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            var mb = tb.DefineMethod(
                "Invoke",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                CallingConventions.Standard,
                returnType,
                parameterTypes);
            for (var i = 0; i < parameterTypes.Length; i++)
            {
                mb.DefineParameter(i + 1, ParameterAttributes.None, "arg" + i);
            }

            mb.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            var delegateType = tb.CreateType();
            return delegateType;
        }
    }
}