using System.Dynamic;

namespace System.Runtime.CompilerServices;

internal class DynamicError
{
    public static Exception AmbiguousMatchInExpandoObject(string name)
    {
        throw new NotImplementedException();
    }

    public static Exception ArgCntMustBeGreaterThanNameCnt()
    {
        throw new NotImplementedException();
    }

    public static Exception BinderNotCompatibleWithCallSite(Type type, DynamicMetaObjectBinder dynamicMetaObjectBinder,
        Type returnLabelType)
    {
        throw new NotImplementedException();
    }

    public static Exception BindingCannotBeNull()
    {
        throw new NotImplementedException();
    }

    public static Exception CollectionModifiedWhileEnumerating()
    {
        throw new NotImplementedException();
    }

    public static Exception CollectionReadOnly()
    {
        throw new NotImplementedException();
    }

    public static Exception DynamicBinderResultNotAssignable(object type,
        DynamicMetaObjectBinder dynamicMetaObjectBinder, Type type1)
    {
        throw new NotImplementedException();
    }

    public static Exception DynamicBindingNeedsRestrictions(Type getType,
        DynamicMetaObjectBinder dynamicMetaObjectBinder)
    {
        throw new NotImplementedException();
    }

    public static Exception DynamicObjectResultNotAssignable(object type, Type getType,
        DynamicMetaObjectBinder dynamicMetaObjectBinder, Type type1)
    {
        throw new NotImplementedException();
    }

    public static Exception InvalidArgumentValue()
    {
        throw new NotImplementedException();
    }

    public static Exception InvalidMetaObjectCreated(Type getType)
    {
        throw new NotImplementedException();
    }

    public static Exception KeyDoesNotExistInExpando(string key)
    {
        throw new NotImplementedException();
    }

    public static Exception MethodPreconditionViolated()
    {
        throw new NotImplementedException();
    }

    public static Exception NonEmptyCollectionRequired()
    {
        throw new NotImplementedException();
    }

    public static Exception OutOfRange(string argsLength, int p1)
    {
        throw new NotImplementedException();
    }

    public static Exception SameKeyExistsInExpando(string name)
    {
        throw new NotImplementedException();
    }

    public static Exception TypeContainsGenericParameters(Type type)
    {
        throw new NotImplementedException();
    }

    public static Exception TypeIsGeneric(Type type)
    {
        throw new NotImplementedException();
    }
}