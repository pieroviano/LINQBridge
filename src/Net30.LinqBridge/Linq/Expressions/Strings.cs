#nullable disable
using System.Globalization;
using System.Properties;

namespace System.Linq.Expressions;

internal class Strings
{
    internal static string MethodPreconditionViolated => LinqBridge.MethodPreconditionViolated;

    internal static string InvalidArgumentValue => LinqBridge.InvalidArgumentValue;

    internal static string NonEmptyCollectionRequired => LinqBridge.NonEmptyCollectionRequired;

    internal static string ArgCntMustBeGreaterThanNameCnt => LinqBridge.ArgCntMustBeGreaterThanNameCnt;

    internal static string ReducibleMustOverrideReduce => LinqBridge.ReducibleMustOverrideReduce;

    internal static string MustReduceToDifferent => LinqBridge.MustReduceToDifferent;

    internal static string ReducedNotCompatible => LinqBridge.ReducedNotCompatible;

    internal static string SetterHasNoParams => LinqBridge.SetterHasNoParams;

    internal static string PropertyCannotHaveRefType => LinqBridge.PropertyCannotHaveRefType;

    internal static string IndexesOfSetGetMustMatch => LinqBridge.IndexesOfSetGetMustMatch;

    internal static string AccessorsCannotHaveVarArgs => LinqBridge.AccessorsCannotHaveVarArgs;

    internal static string AccessorsCannotHaveByRefArgs => LinqBridge.AccessorsCannotHaveByRefArgs;

    internal static string BoundsCannotBeLessThanOne => LinqBridge.BoundsCannotBeLessThanOne;

    internal static string TypeMustNotBeByRef => LinqBridge.TypeMustNotBeByRef;

    internal static string TypeDoesNotHaveConstructorForTheSignature =>
        LinqBridge.TypeDoesNotHaveConstructorForTheSignature;

    internal static string CountCannotBeNegative => LinqBridge.CountCannotBeNegative;

    internal static string ArrayTypeMustBeArray => LinqBridge.ArrayTypeMustBeArray;

    internal static string SetterMustBeVoid => LinqBridge.SetterMustBeVoid;

    internal static string PropertyTyepMustMatchSetter => LinqBridge.PropertyTyepMustMatchSetter;

    internal static string BothAccessorsMustBeStatic => LinqBridge.BothAccessorsMustBeStatic;

    internal static string OnlyStaticFieldsHaveNullInstance => LinqBridge.OnlyStaticFieldsHaveNullInstance;

    internal static string OnlyStaticPropertiesHaveNullInstance => LinqBridge.OnlyStaticPropertiesHaveNullInstance;

    internal static string OnlyStaticMethodsHaveNullInstance => LinqBridge.OnlyStaticMethodsHaveNullInstance;

    internal static string PropertyTypeCannotBeVoid => LinqBridge.PropertyTypeCannotBeVoid;

    internal static string InvalidUnboxType => LinqBridge.InvalidUnboxType;

    internal static string ExpressionMustBeReadable => LinqBridge.ExpressionMustBeReadable;

    internal static string ExpressionMustBeWriteable => LinqBridge.ExpressionMustBeWriteable;

    internal static string ArgumentMustNotHaveValueType => LinqBridge.ArgumentMustNotHaveValueType;

    internal static string MustBeReducible => LinqBridge.MustBeReducible;

    internal static string AllTestValuesMustHaveSameType => LinqBridge.AllTestValuesMustHaveSameType;

    internal static string AllCaseBodiesMustHaveSameType => LinqBridge.AllCaseBodiesMustHaveSameType;

    internal static string DefaultBodyMustBeSupplied => LinqBridge.DefaultBodyMustBeSupplied;

    internal static string MethodBuilderDoesNotHaveTypeBuilder => LinqBridge.MethodBuilderDoesNotHaveTypeBuilder;

    internal static string TypeMustBeDerivedFromSystemDelegate => LinqBridge.TypeMustBeDerivedFromSystemDelegate;

    internal static string ArgumentTypeCannotBeVoid => LinqBridge.ArgumentTypeCannotBeVoid;

    internal static string LabelMustBeVoidOrHaveExpression => LinqBridge.LabelMustBeVoidOrHaveExpression;

    internal static string LabelTypeMustBeVoid => LinqBridge.LabelTypeMustBeVoid;

    internal static string QuotedExpressionMustBeLambda => LinqBridge.QuotedExpressionMustBeLambda;

    internal static string StartEndMustBeOrdered => LinqBridge.StartEndMustBeOrdered;

    internal static string FaultCannotHaveCatchOrFinally => LinqBridge.FaultCannotHaveCatchOrFinally;

    internal static string TryMustHaveCatchFinallyOrFault => LinqBridge.TryMustHaveCatchFinallyOrFault;

    internal static string BodyOfCatchMustHaveSameTypeAsBodyOfTry => LinqBridge.BodyOfCatchMustHaveSameTypeAsBodyOfTry;

    internal static string ConversionIsNotSupportedForArithmeticTypes =>
        LinqBridge.ConversionIsNotSupportedForArithmeticTypes;

    internal static string ArgumentMustBeArray => LinqBridge.ArgumentMustBeArray;

    internal static string ArgumentMustBeBoolean => LinqBridge.ArgumentMustBeBoolean;

    internal static string ArgumentMustBeFieldInfoOrPropertInfo => LinqBridge.ArgumentMustBeFieldInfoOrPropertInfo;

    internal static string ArgumentMustBeFieldInfoOrPropertInfoOrMethod =>
        LinqBridge.ArgumentMustBeFieldInfoOrPropertInfoOrMethod;

    internal static string ArgumentMustBeInstanceMember => LinqBridge.ArgumentMustBeInstanceMember;

    internal static string ArgumentMustBeInteger => LinqBridge.ArgumentMustBeInteger;

    internal static string ArgumentMustBeArrayIndexType => LinqBridge.ArgumentMustBeArrayIndexType;

    internal static string ArgumentMustBeSingleDimensionalArrayType =>
        LinqBridge.ArgumentMustBeSingleDimensionalArrayType;

    internal static string ArgumentTypesMustMatch => LinqBridge.ArgumentTypesMustMatch;

    internal static string CoalesceUsedOnNonNullType => LinqBridge.CoalesceUsedOnNonNullType;

    internal static string IncorrectNumberOfIndexes => LinqBridge.IncorrectNumberOfIndexes;

    internal static string IncorrectNumberOfLambdaArguments => LinqBridge.IncorrectNumberOfLambdaArguments;

    internal static string IncorrectNumberOfLambdaDeclarationParameters =>
        LinqBridge.IncorrectNumberOfLambdaDeclarationParameters;

    internal static string IncorrectNumberOfConstructorArguments => LinqBridge.IncorrectNumberOfConstructorArguments;

    internal static string IncorrectNumberOfMembersForGivenConstructor =>
        LinqBridge.IncorrectNumberOfMembersForGivenConstructor;

    internal static string IncorrectNumberOfArgumentsForMembers => LinqBridge.IncorrectNumberOfArgumentsForMembers;

    internal static string LambdaTypeMustBeDerivedFromSystemDelegate =>
        LinqBridge.LambdaTypeMustBeDerivedFromSystemDelegate;

    internal static string ListInitializerWithZeroMembers => LinqBridge.ListInitializerWithZeroMembers;

    internal static string ElementInitializerMethodNotAdd => LinqBridge.ElementInitializerMethodNotAdd;

    internal static string ElementInitializerMethodWithZeroArgs => LinqBridge.ElementInitializerMethodWithZeroArgs;

    internal static string ElementInitializerMethodStatic => LinqBridge.ElementInitializerMethodStatic;

    internal static string UnexpectedCoalesceOperator => LinqBridge.UnexpectedCoalesceOperator;

    internal static string UnhandledBinding => LinqBridge.UnhandledBinding;

    internal static string UnknownBindingType => LinqBridge.UnknownBindingType;

    internal static string IncorrectNumberOfTypeArgsForFunc => LinqBridge.IncorrectNumberOfTypeArgsForFunc;

    internal static string IncorrectNumberOfTypeArgsForAction => LinqBridge.IncorrectNumberOfTypeArgsForAction;

    internal static string ArgumentCannotBeOfTypeVoid => LinqBridge.ArgumentCannotBeOfTypeVoid;

    internal static string NoOrInvalidRuleProduced => LinqBridge.NoOrInvalidRuleProduced;

    internal static string FirstArgumentMustBeCallSite => LinqBridge.FirstArgumentMustBeCallSite;

    internal static string BindingCannotBeNull => LinqBridge.BindingCannotBeNull;

    internal static string QueueEmpty => LinqBridge.QueueEmpty;

    internal static string ControlCannotLeaveFinally => LinqBridge.ControlCannotLeaveFinally;

    internal static string ControlCannotLeaveFilterTest => LinqBridge.ControlCannotLeaveFilterTest;

    internal static string ControlCannotEnterTry => LinqBridge.ControlCannotEnterTry;

    internal static string ControlCannotEnterExpression => LinqBridge.ControlCannotEnterExpression;

    internal static string ExtensionNotReduced => LinqBridge.ExtensionNotReduced;

    internal static string CannotCompileDynamic => LinqBridge.CannotCompileDynamic;

    internal static string InvalidOutputDir => LinqBridge.InvalidOutputDir;

    internal static string InvalidAsmNameOrExtension => LinqBridge.InvalidAsmNameOrExtension;

    internal static string CollectionReadOnly => LinqBridge.CollectionReadOnly;

    internal static string RethrowRequiresCatch => LinqBridge.RethrowRequiresCatch;

    internal static string TryNotAllowedInFilter => LinqBridge.TryNotAllowedInFilter;

    internal static string CollectionModifiedWhileEnumerating => LinqBridge.CollectionModifiedWhileEnumerating;

    internal static string EnumerationIsDone => LinqBridge.EnumerationIsDone;

    internal static string HomogenousAppDomainRequired => LinqBridge.HomogenousAppDomainRequired;

    internal static string PdbGeneratorNeedsExpressionCompiler => LinqBridge.PdbGeneratorNeedsExpressionCompiler;

    public static string ArgumentArrayHasTooManyElements(object p0)
    {
        throw new InvalidOperationException(LinqBridge.ArgumentArrayHasTooManyElements);
    }

    public static string ArgumentNotIEnumerableGeneric(object p0)
    {
        throw new InvalidOperationException(LinqBridge.ArgumentNotIEnumerableGeneric);
    }

    public static string ArgumentNotLambda(object p0)
    {
        throw new InvalidOperationException(LinqBridge.ArgumentNotLambda);
    }

    public static string ArgumentNotSequence(object p0)
    {
        throw new InvalidOperationException(LinqBridge.ArgumentNotSequence);
    }

    public static string ArgumentNotValid(object p0)
    {
        throw new InvalidOperationException(LinqBridge.ArgumentNotValid);
    }

    public static string DynamicBindingNeedsRestrictions(object p0, object p1)
    {
        throw new InvalidOperationException(LinqBridge.DynamicBindingNeedsRestrictions);
    }

    public static string EmptyEnumerable()
    {
        throw new InvalidOperationException(LinqBridge.EmptyEnumerable);
    }

    public static string GetString(string name)
    {
        string s;
        try
        {
            s = LinqBridge.ResourceManager.GetString(name, CultureInfo.CurrentCulture);
        }
        catch (Exception e)
        {
            s = name;
        }

        if (string.IsNullOrEmpty(s))
        {
            s = NameFormatter.FormatPascalName(name);
        }

        return s;
    }

    public static string IncompatibleElementTypes()
    {
        throw new InvalidOperationException(LinqBridge.IncompatibleElementTypes);
    }

    public static string MoreThanOneElement()
    {
        throw new InvalidOperationException(LinqBridge.MoreThanOneElement);
    }

    public static string MoreThanOneMatch()
    {
        throw new InvalidOperationException(LinqBridge.MoreThanOneMatch);
    }

    public static string NoArgumentMatchingMethodsInQueryable(object p0)
    {
        throw new InvalidOperationException(LinqBridge.NoArgumentMatchingMethodsInQueryable);
    }

    public static string NoElements()
    {
        throw new InvalidOperationException(LinqBridge.NoElements);
    }

    public static string NoMatch()
    {
        throw new InvalidOperationException(LinqBridge.NoMatch);
    }

    public static string NoMethodOnType(object p0, object p1)
    {
        throw new InvalidOperationException(LinqBridge.NoMethodOnType);
    }

    public static string NoMethodOnTypeMatchingArguments(object p0, object p1)
    {
        throw new InvalidOperationException(LinqBridge.NoMethodOnTypeMatchingArguments);
    }

    public static string NoNameMatchingMethodsInQueryable(object p0)
    {
        throw new InvalidOperationException(LinqBridge.NoNameMatchingMethodsInQueryable);
    }

    public static string ParallelPartitionable_IncorretElementCount()
    {
        throw new InvalidOperationException(LinqBridge.ParallelPartitionable_IncorretElementCount);
    }

    public static string ParallelPartitionable_NullElement()
    {
        throw new InvalidOperationException(LinqBridge.ParallelPartitionable_NullElement);
    }

    public static string ParallelPartitionable_NullReturn()
    {
        throw new InvalidOperationException(LinqBridge.ParallelPartitionable_NullReturn);
    }

    public static string ParallelQuery_DuplicateDOP()
    {
        throw new InvalidOperationException(LinqBridge.ParallelQuery_DuplicateDOP);
    }

    public static string ParallelQuery_DuplicateExecutionMode()
    {
        throw new InvalidOperationException(LinqBridge.ParallelQuery_DuplicateExecutionMode);
    }

    public static string ParallelQuery_DuplicateMergeOptions()
    {
        throw new InvalidOperationException(LinqBridge.ParallelQuery_DuplicateMergeOptions);
    }

    public static string ParallelQuery_DuplicateTaskScheduler()
    {
        throw new InvalidOperationException(LinqBridge.ParallelQuery_DuplicateTaskScheduler);
    }

    public static string ParallelQuery_DuplicateWithCancellation()
    {
        throw new InvalidOperationException(LinqBridge.ParallelQuery_DuplicateWithCancellation);
    }

    public static string PLINQ_CommonEnumerator_Current_NotStarted()
    {
        throw new InvalidOperationException(LinqBridge.PLINQ_CommonEnumerator_Current_NotStarted);
    }

    public static string PLINQ_DisposeRequested()
    {
        throw new InvalidOperationException(LinqBridge.PLINQ_DisposeRequested);
    }

    public static string PLINQ_EnumerationPreviouslyFailed()
    {
        throw new InvalidOperationException(LinqBridge.PLINQ_EnumerationPreviouslyFailed);
    }

    public static string PLINQ_ExternalCancellationRequested()
    {
        throw new InvalidOperationException(LinqBridge.PLINQ_ExternalCancellationRequested);
    }

    internal static string AmbiguousJump(object p0)
    {
        return string.Format(LinqBridge.AmbiguousJump, p0);
    }

    internal static string AmbiguousMatchInExpandoObject(object p0)
    {
        return string.Format(LinqBridge.AmbiguousMatchInExpandoObject, p0);
    }

    internal static string ArgumentMemberNotDeclOnType(object p0, object p1)
    {
        return string.Format(LinqBridge.ArgumentMemberNotDeclOnType, p0, p1);
    }

    internal static string ArgumentTypeDoesNotMatchMember(object p0, object p1)
    {
        return string.Format(LinqBridge.ArgumentTypeDoesNotMatchMember, p0, p1);
    }

    internal static string BinaryOperatorNotDefined(object p0, object p1, object p2)
    {
        return string.Format(LinqBridge.BinaryOperatorNotDefined, p0, p1, p2);
    }

    internal static string BinderNotCompatibleWithCallSite(object p0, object p1, object p2)
    {
        return string.Format(LinqBridge.BinderNotCompatibleWithCallSite, p0, p1, p2);
    }

    internal static string CannotAutoInitializeValueTypeElementThroughProperty(object p0)
    {
        return string.Format(LinqBridge.CannotAutoInitializeValueTypeElementThroughProperty, p0);
    }

    internal static string CannotAutoInitializeValueTypeMemberThroughProperty(object p0)
    {
        return string.Format(LinqBridge.CannotAutoInitializeValueTypeMemberThroughProperty, p0);
    }

    internal static string CannotCloseOverByRef(object p0, object p1)
    {
        return string.Format(LinqBridge.CannotCloseOverByRef, p0, p1);
    }

    internal static string CannotCompileConstant(object p0)
    {
        return string.Format(LinqBridge.CannotCompileConstant, p0);
    }

    internal static string CoercionOperatorNotDefined(object p0, object p1)
    {
        return string.Format(LinqBridge.CoercionOperatorNotDefined, p0, p1);
    }

    internal static string DuplicateVariable(object p0)
    {
        return string.Format(LinqBridge.DuplicateVariable, p0);
    }

    internal static string DynamicBinderResultNotAssignable(object p0, object p1, object p2)
    {
        return string.Format(LinqBridge.DynamicBinderResultNotAssignable, p0, p1, p2);
    }

    internal static string DynamicBindingNeedStringsestrictions(object p0, object p1)
    {
        return string.Format(LinqBridge.DynamicBindingNeedStringsestrictions, p0, p1);
    }

    internal static string DynamicObjectResultNotAssignable(
        object p0,
        object p1,
        object p2,
        object p3)
    {
        return string.Format(LinqBridge.DynamicObjectResultNotAssignable, p0, p1, p2, p3);
    }

    internal static string ElementInitializerMethodNoRefOutParam(object p0, object p1)
    {
        return string.Format(LinqBridge.ElementInitializerMethodNoRefOutParam, p0, p1);
    }

    internal static string EqualityMustReturnBoolean(object p0)
    {
        return string.Format(LinqBridge.EqualityMustReturnBoolean, p0);
    }

    internal static string ExpressionTypeCannotInitializeArrayType(object p0, object p1)
    {
        return string.Format(LinqBridge.ExpressionTypeCannotInitializeArrayType, p0, p1);
    }

    internal static string ExpressionTypeDoesNotMatchAssignment(object p0, object p1)
    {
        return string.Format(LinqBridge.ExpressionTypeDoesNotMatchAssignment, p0, p1);
    }

    internal static string ExpressionTypeDoesNotMatchConstructorParameter(object p0, object p1)
    {
        return string.Format(LinqBridge.ExpressionTypeDoesNotMatchConstructorParameter, p0, p1);
    }

    internal static string ExpressionTypeDoesNotMatchLabel(object p0, object p1)
    {
        return string.Format(LinqBridge.ExpressionTypeDoesNotMatchLabel, p0, p1);
    }

    internal static string ExpressionTypeDoesNotMatchMethodParameter(object p0, object p1, object p2)
    {
        return string.Format(LinqBridge.ExpressionTypeDoesNotMatchMethodParameter, p0, p1, p2);
    }

    internal static string ExpressionTypeDoesNotMatchParameter(object p0, object p1)
    {
        return string.Format(LinqBridge.ExpressionTypeDoesNotMatchParameter, p0, p1);
    }

    internal static string ExpressionTypeDoesNotMatchReturn(object p0, object p1)
    {
        return string.Format(LinqBridge.ExpressionTypeDoesNotMatchReturn, p0, p1);
    }

    internal static string ExpressionTypeNotInvocable(object p0)
    {
        return string.Format(LinqBridge.ExpressionTypeNotInvocable, p0);
    }

    internal static string ExtensionNodeMustOverrideProperty(object p0)
    {
        return string.Format(LinqBridge.ExtensionNodeMustOverrideProperty, p0);
    }

    internal static string FieldInfoNotDefinedForType(object p0, object p1, object p2)
    {
        return string.Format(LinqBridge.FieldInfoNotDefinedForType, p0, p1, p2);
    }

    internal static string FieldNotDefinedForType(object p0, object p1)
    {
        return string.Format(LinqBridge.FieldNotDefinedForType, p0, p1);
    }

    internal static string GenericMethodWithArgsDoesNotExistOnType(object p0, object p1)
    {
        return string.Format(LinqBridge.GenericMethodWithArgsDoesNotExistOnType, p0, p1);
    }

    internal static string IllegalNewGenericParams(object p0)
    {
        return string.Format(LinqBridge.IllegalNewGenericParams, p0);
    }

    internal static string IncorrectNumberOfMethodCallArguments(object p0)
    {
        return string.Format(LinqBridge.IncorrectNumberOfMethodCallArguments, p0);
    }

    internal static string IncorrectTypeForTypeAs(object p0)
    {
        return string.Format(LinqBridge.IncorrectTypeForTypeAs, p0);
    }

    internal static string InstanceAndMethodTypeMismatch(object p0, object p1, object p2)
    {
        return string.Format(LinqBridge.InstanceAndMethodTypeMismatch, p0, p1, p2);
    }

    internal static string InstanceFieldNotDefinedForType(object p0, object p1)
    {
        return string.Format(LinqBridge.InstanceFieldNotDefinedForType, p0, p1);
    }

    internal static string InstancePropertyNotDefinedForType(object p0, object p1)
    {
        return string.Format(LinqBridge.InstancePropertyNotDefinedForType, p0, p1);
    }

    internal static string InstancePropertyWithoutParameterNotDefinedForType(object p0, object p1)
    {
        return string.Format(LinqBridge.InstancePropertyWithoutParameterNotDefinedForType, p0, p1);
    }

    internal static string InstancePropertyWithSpecifiedParametersNotDefinedForType(
        object p0,
        object p1,
        object p2)
    {
        return string.Format(LinqBridge.InstancePropertyWithSpecifiedParametersNotDefinedForType, p0, p1, p2);
    }

    internal static string InvalidCast(object p0, object p1)
    {
        return string.Format(LinqBridge.InvalidCast, p0, p1);
    }

    internal static string InvalidLvalue(object p0)
    {
        return string.Format(LinqBridge.InvalidLvalue, p0);
    }

    internal static string InvalidMemberType(object p0)
    {
        return string.Format(LinqBridge.InvalidMemberType, p0);
    }

    internal static string InvalidMetaObjectCreated(object p0)
    {
        return string.Format(LinqBridge.InvalidMetaObjectCreated, p0);
    }

    internal static string InvalidNullValue(object p0)
    {
        return string.Format(LinqBridge.InvalidNullValue, p0);
    }

    internal static string InvalidObjectType(object p0, object p1)
    {
        return string.Format(LinqBridge.InvalidObjectType, p0, p1);
    }

    internal static string InvalidOperation(object p0)
    {
        return string.Format(LinqBridge.InvalidOperation, p0);
    }

    internal static string KeyDoesNotExistInExpando(object p0)
    {
        return string.Format(LinqBridge.KeyDoesNotExistInExpando, p0);
    }

    internal static string LabelTargetAlreadyDefined(object p0)
    {
        return string.Format(LinqBridge.LabelTargetAlreadyDefined, p0);
    }

    internal static string LabelTargetUndefined(object p0)
    {
        return string.Format(LinqBridge.LabelTargetUndefined, p0);
    }

    internal static string LogicalOperatorMustHaveBooleanOperators(object p0, object p1)
    {
        return string.Format(LinqBridge.LogicalOperatorMustHaveBooleanOperators, p0, p1);
    }

    internal static string MemberNotFieldOrProperty(object p0)
    {
        return string.Format(LinqBridge.MemberNotFieldOrProperty, p0);
    }

    internal static string MethodContainsGenericParameters(object p0)
    {
        return string.Format(LinqBridge.MethodContainsGenericParameters, p0);
    }

    internal static string MethodDoesNotExistOnType(object p0, object p1)
    {
        return string.Format(LinqBridge.MethodDoesNotExistOnType, p0, p1);
    }

    internal static string MethodIsGeneric(object p0)
    {
        return string.Format(LinqBridge.MethodIsGeneric, p0);
    }

    internal static string MethodNotPropertyAccessor(object p0, object p1)
    {
        return string.Format(LinqBridge.MethodNotPropertyAccessor, p0, p1);
    }

    internal static string MethodWithArgsDoesNotExistOnType(object p0, object p1)
    {
        return string.Format(LinqBridge.MethodWithArgsDoesNotExistOnType, p0, p1);
    }

    internal static string MethodWithMoreThanOneMatch(object p0, object p1)
    {
        return string.Format(LinqBridge.MethodWithMoreThanOneMatch, p0, p1);
    }

    internal static string MustRewriteChildToSameType(object p0, object p1, object p2)
    {
        return string.Format(LinqBridge.MustRewriteChildToSameType, p0, p1, p2);
    }

    internal static string MustRewriteToSameNode(object p0, object p1, object p2)
    {
        return string.Format(LinqBridge.MustRewriteToSameNode, p0, p1, p2);
    }

    internal static string MustRewriteWithoutMethod(object p0, object p1)
    {
        return string.Format(LinqBridge.MustRewriteWithoutMethod, p0, p1);
    }

    internal static string NonLocalJumpWithValue(object p0)
    {
        return string.Format(LinqBridge.NonLocalJumpWithValue, p0);
    }

    internal static string NotAMemberOfType(object p0, object p1)
    {
        return string.Format(LinqBridge.NotAMemberOfType, p0, p1);
    }

    internal static string OperandTypesDoNotMatchParameters(object p0, object p1)
    {
        return string.Format(LinqBridge.OperandTypesDoNotMatchParameters, p0, p1);
    }

    internal static string OperatorNotImplementedForType(object p0, object p1)
    {
        return string.Format(LinqBridge.OperatorNotImplementedForType, p0, p1);
    }

    internal static string OutOfRange(object p0, object p1)
    {
        return string.Format(LinqBridge.OutOfRange, p0, p1);
    }

    internal static string OverloadOperatorTypeDoesNotMatchConversionType(object p0, object p1)
    {
        return string.Format(LinqBridge.OverloadOperatorTypeDoesNotMatchConversionType, p0, p1);
    }

    internal static string ParameterExpressionNotValidAsDelegate(object p0, object p1)
    {
        return string.Format(LinqBridge.ParameterExpressionNotValidAsDelegate, p0, p1);
    }

    internal static string PropertyDoesNotHaveAccessor(object p0)
    {
        return string.Format(LinqBridge.PropertyDoesNotHaveAccessor, p0);
    }

    internal static string PropertyDoesNotHaveGetter(object p0)
    {
        return string.Format(LinqBridge.PropertyDoesNotHaveGetter, p0);
    }

    internal static string PropertyDoesNotHaveSetter(object p0)
    {
        return string.Format(LinqBridge.PropertyDoesNotHaveSetter, p0);
    }

    internal static string PropertyNotDefinedForType(object p0, object p1)
    {
        return string.Format(LinqBridge.PropertyNotDefinedForType, p0, p1);
    }

    internal static string PropertyWithMoreThanOneMatch(object p0, object p1)
    {
        return string.Format(LinqBridge.PropertyWithMoreThanOneMatch, p0, p1);
    }

    internal static string ReferenceEqualityNotDefined(object p0, object p1)
    {
        return string.Format(LinqBridge.ReferenceEqualityNotDefined, p0, p1);
    }

    internal static string SameKeyExistsInExpando(object p0)
    {
        return string.Format(LinqBridge.SameKeyExistsInExpando, p0);
    }

    internal static string SwitchValueTypeDoesNotMatchComparisonMethodParameter(object p0, object p1)
    {
        return string.Format(LinqBridge.SwitchValueTypeDoesNotMatchComparisonMethodParameter, p0, p1);
    }

    internal static string TestValueTypeDoesNotMatchComparisonMethodParameter(object p0, object p1)
    {
        return string.Format(LinqBridge.TestValueTypeDoesNotMatchComparisonMethodParameter, p0, p1);
    }

    internal static string TryNotSupportedForMethodsWithRefArgs(object p0)
    {
        return string.Format(LinqBridge.TryNotSupportedForMethodsWithRefArgs, p0);
    }

    internal static string TryNotSupportedForValueTypeInstances(object p0)
    {
        return string.Format(LinqBridge.TryNotSupportedForValueTypeInstances, p0);
    }

    internal static string TypeContainsGenericParameters(object p0)
    {
        return string.Format(LinqBridge.TypeContainsGenericParameters, p0);
    }

    internal static string TypeIsGeneric(object p0)
    {
        return string.Format(LinqBridge.TypeIsGeneric, p0);
    }

    internal static string TypeMissingDefaultConstructor(object p0)
    {
        return string.Format(LinqBridge.TypeMissingDefaultConstructor, p0);
    }

    internal static string TypeNotIEnumerable(object p0)
    {
        return string.Format(LinqBridge.TypeNotIEnumerable, p0);
    }

    internal static string TypeParameterIsNotDelegate(object p0)
    {
        return string.Format(LinqBridge.TypeParameterIsNotDelegate, p0);
    }

    internal static string UnaryOperatorNotDefined(object p0, object p1)
    {
        return string.Format(LinqBridge.UnaryOperatorNotDefined, p0, p1);
    }

    internal static string UndefinedVariable(object p0, object p1, object p2)
    {
        return string.Format(LinqBridge.UndefinedVariable, p0, p1, p2);
    }

    internal static string UnexpectedVarArgsCall(object p0)
    {
        return string.Format(LinqBridge.UnexpectedVarArgsCall, p0);
    }

    internal static string UnhandledBinary(object p0)
    {
        return string.Format(LinqBridge.UnhandledBinary, p0);
    }

    internal static string UnhandledBindingType(object p0)
    {
        return string.Format(LinqBridge.UnhandledBindingType, p0);
    }

    internal static string UnhandledConvert(object p0)
    {
        return string.Format(LinqBridge.UnhandledConvert, p0);
    }

    internal static string UnhandledExpressionType(object p0)
    {
        return string.Format(LinqBridge.UnhandledExpressionType, p0);
    }

    internal static string UnhandledUnary(object p0)
    {
        return string.Format(LinqBridge.UnhandledUnary, p0);
    }

    internal static string UnknownLiftType(object p0)
    {
        return string.Format(LinqBridge.UnknownLiftType, p0);
    }

    internal static string UserDefinedOperatorMustBeStatic(object p0)
    {
        return string.Format(LinqBridge.UserDefinedOperatorMustBeStatic, p0);
    }

    internal static string UserDefinedOperatorMustNotBeVoid(object p0)
    {
        return string.Format(LinqBridge.UserDefinedOperatorMustNotBeVoid, p0);
    }

    internal static string UserDefinedOpMustHaveConsistentTypes(object p0, object p1)
    {
        return string.Format(LinqBridge.UserDefinedOpMustHaveConsistentTypes, p0, p1);
    }

    internal static string UserDefinedOpMustHaveValidReturnType(object p0, object p1)
    {
        return string.Format(LinqBridge.UserDefinedOpMustHaveValidReturnType, p0, p1);
    }

    internal static string VariableMustNotBeByRef(object p0, object p1)
    {
        return string.Format(LinqBridge.VariableMustNotBeByRef, p0, p1);
    }
}