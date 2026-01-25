// Type: System.Dynamic.UnsafeMethods
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

#nullable disable
namespace System.Linq.Expressions;

internal static class UnsafeMethods
{
  private static readonly object _lock = new object();
  private static volatile ModuleBuilder _dynamicModule;
  private const int _dummyMarker = 269488144 /*0x10101010*/;
  private static readonly UnsafeMethods.IUnknownReleaseDelegate _IUnknownRelease = UnsafeMethods.Create_IUnknownRelease();
  internal static readonly IntPtr NullInterfaceId = UnsafeMethods.GetNullInterfaceId();
  private static readonly UnsafeMethods.IDispatchInvokeDelegate _IDispatchInvoke = UnsafeMethods.Create_IDispatchInvoke(true);
  private static volatile UnsafeMethods.IDispatchInvokeDelegate _IDispatchInvokeNoResultImpl;

  [SecurityCritical]
  internal static unsafe IntPtr ConvertSByteByrefToPtr(ref sbyte value)
  {
    fixed (sbyte* numPtr = &value)
      return new IntPtr((void*) numPtr);
  }

  [SecurityCritical]
  internal static unsafe IntPtr ConvertInt16ByrefToPtr(ref short value)
  {
    fixed (short* numPtr = &value)
      return new IntPtr((void*) numPtr);
  }

  [SecurityCritical]
    public static unsafe IntPtr ConvertInt32ByrefToPtr(ref int value)
  {
    fixed (int* numPtr = &value)
      return new IntPtr((void*) numPtr);
  }

  [SecurityCritical]
  internal static unsafe IntPtr ConvertInt64ByrefToPtr(ref long value)
  {
    fixed (long* numPtr = &value)
      return new IntPtr((void*) numPtr);
  }

  [SecurityCritical]
  internal static unsafe IntPtr ConvertByteByrefToPtr(ref byte value)
  {
    fixed (byte* numPtr = &value)
      return new IntPtr((void*) numPtr);
  }

  [SecurityCritical]
  internal static unsafe IntPtr ConvertUInt16ByrefToPtr(ref ushort value)
  {
    fixed (ushort* numPtr = &value)
      return new IntPtr((void*) numPtr);
  }

  [SecurityCritical]
  internal static unsafe IntPtr ConvertUInt32ByrefToPtr(ref uint value)
  {
    fixed (uint* numPtr = &value)
      return new IntPtr((void*) numPtr);
  }

  [SecurityCritical]
  internal static unsafe IntPtr ConvertUInt64ByrefToPtr(ref ulong value)
  {
    fixed (ulong* numPtr = &value)
      return new IntPtr((void*) numPtr);
  }

  [SecurityCritical]
  internal static unsafe IntPtr ConvertIntPtrByrefToPtr(ref IntPtr value)
  {
    fixed (IntPtr* numPtr = &value)
      return new IntPtr((void*) numPtr);
  }

  [SecurityCritical]
  internal static unsafe IntPtr ConvertUIntPtrByrefToPtr(ref UIntPtr value)
  {
    fixed (UIntPtr* numPtr = &value)
      return new IntPtr((void*) numPtr);
  }

  [SecurityCritical]
  internal static unsafe IntPtr ConvertSingleByrefToPtr(ref float value)
  {
    fixed (float* numPtr = &value)
      return new IntPtr((void*) numPtr);
  }

  [SecurityCritical]
  internal static unsafe IntPtr ConvertDoubleByrefToPtr(ref double value)
  {
    fixed (double* numPtr = &value)
      return new IntPtr((void*) numPtr);
  }

  [SecurityCritical]
  internal static unsafe IntPtr ConvertDecimalByrefToPtr(ref Decimal value)
  {
    fixed (Decimal* numPtr = &value)
      return new IntPtr((void*) numPtr);
  }

  [SecurityCritical]
    public static unsafe IntPtr ConvertVariantByrefToPtr(ref Variant value)
  {
    fixed (Variant* variantPtr = &value)
      return new IntPtr((void*) variantPtr);
  }

  [SecurityCritical]
    internal static Variant GetVariantForObject(object obj)
  {
    Variant variant = new Variant();
    if (obj == null)
      return variant;
    UnsafeMethods.InitVariantForObject(obj, ref variant);
    return variant;
  }

  [SecurityCritical]
  internal static void InitVariantForObject(object obj, ref Variant variant)
  {
    if (obj is IDispatch)
      variant.AsDispatch = obj;
    else
      Marshal.GetNativeVariantForObject(obj, UnsafeMethods.ConvertVariantByrefToPtr(ref variant));
  }

  [SecurityCritical]
  [Obsolete("do not use this method", true)]
    public static object GetObjectForVariant(Variant variant)
  {
    return Marshal.GetObjectForNativeVariant(UnsafeMethods.ConvertVariantByrefToPtr(ref variant));
  }

  [Obsolete("do not use this method", true)]
    public static int IUnknownRelease(IntPtr interfacePointer)
  {
    return UnsafeMethods._IUnknownRelease(interfacePointer);
  }

  [Obsolete("do not use this method", true)]
    public static void IUnknownReleaseNotZero(IntPtr interfacePointer)
  {
    if (!(interfacePointer != IntPtr.Zero))
      return;
    UnsafeMethods.IUnknownRelease(interfacePointer);
  }

  [SecurityCritical]
  [Obsolete("do not use this method", true)]
    public static int IDispatchInvoke(
    IntPtr dispatchPointer,
    int memberDispId,
    System.Runtime.InteropServices.ComTypes.INVOKEKIND flags,
    ref System.Runtime.InteropServices.ComTypes.DISPPARAMS dispParams,
    out Variant result,
    out ExcepInfo excepInfo,
    out uint argErr)
  {
    int num = UnsafeMethods._IDispatchInvoke(dispatchPointer, memberDispId, flags, ref dispParams, out result, out excepInfo, out argErr);
    if (num == -2147352573 /*0x80020003*/ && (flags & System.Runtime.InteropServices.ComTypes.INVOKEKIND.INVOKE_FUNC) != (System.Runtime.InteropServices.ComTypes.INVOKEKIND) 0 && (flags & (System.Runtime.InteropServices.ComTypes.INVOKEKIND.INVOKE_PROPERTYPUT | System.Runtime.InteropServices.ComTypes.INVOKEKIND.INVOKE_PROPERTYPUTREF)) == (System.Runtime.InteropServices.ComTypes.INVOKEKIND) 0)
      num = UnsafeMethods._IDispatchInvokeNoResult(dispatchPointer, memberDispId, System.Runtime.InteropServices.ComTypes.INVOKEKIND.INVOKE_FUNC, ref dispParams, out result, out excepInfo, out argErr);
    return num;
  }

  [Obsolete("do not use this method", true)]
  [SecurityCritical]
    public static IntPtr GetIdsOfNamedParameters(
    IDispatch dispatch,
    string[] names,
    int methodDispId,
    out GCHandle pinningHandle)
  {
    pinningHandle = GCHandle.Alloc((object) null, GCHandleType.Pinned);
    int[] numArray = new int[names.Length];
    Guid empty = Guid.Empty;
    int idsOfNames = dispatch.TryGetIDsOfNames(ref empty, names, (uint) names.Length, 0, numArray);
    if (idsOfNames < 0)
      Marshal.ThrowExceptionForHR(idsOfNames);
    int[] arr = methodDispId == numArray[0] ? numArray.RemoveFirst<int>() : throw Error.GetIDsOfNamesInvalid((object) names[0]);
    pinningHandle.Target = (object) arr;
    return Marshal.UnsafeAddrOfPinnedArrayElement((Array) arr, 0);
  }

  [SecurityCritical]
  static UnsafeMethods()
  {
  }

  private static void EmitLoadArg(ILGenerator il, int index)
  {
    ContractUtils.Requires(index >= 0, nameof (index));
    switch (index)
    {
      case 0:
        il.Emit(OpCodes.Ldarg_0);
        break;
      case 1:
        il.Emit(OpCodes.Ldarg_1);
        break;
      case 2:
        il.Emit(OpCodes.Ldarg_2);
        break;
      case 3:
        il.Emit(OpCodes.Ldarg_3);
        break;
      default:
        if (index <= (int) byte.MaxValue)
        {
          il.Emit(OpCodes.Ldarg_S, (byte) index);
          break;
        }
        il.Emit(OpCodes.Ldarg, index);
        break;
    }
  }

  [Conditional("DEBUG")]
  [SecurityCritical]
  private static void AssertByrefPointsToStack(IntPtr ptr)
  {
    if (Marshal.ReadInt32(ptr) == 269488144 /*0x10101010*/)
      return;
    int num = 269488144 /*0x10101010*/;
    UnsafeMethods.ConvertInt32ByrefToPtr(ref num);
  }

  internal static ModuleBuilder DynamicModule
  {
    get
    {
      if ((Module) UnsafeMethods._dynamicModule != (Module) null)
        return UnsafeMethods._dynamicModule;
      lock (UnsafeMethods._lock)
      {
        if ((Module) UnsafeMethods._dynamicModule == (Module) null)
        {
          CustomAttributeBuilder[] assemblyAttributes = new CustomAttributeBuilder[2]
          {
            new CustomAttributeBuilder(typeof (UnverifiableCodeAttribute).GetConstructor(Type.EmptyTypes), new object[0]),
            new CustomAttributeBuilder(typeof (PermissionSetAttribute).GetConstructor(new Type[1]
            {
              typeof (SecurityAction)
            }), new object[1]
            {
              (object) SecurityAction.Demand
            }, new PropertyInfo[1]
            {
              typeof (PermissionSetAttribute).GetProperty("Unrestricted")
            }, new object[1]{ (object) true })
          };
          string str = typeof (VariantArray).Namespace + ".DynamicAssembly";
          AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(str), AssemblyBuilderAccess.Run, (IEnumerable<CustomAttributeBuilder>) assemblyAttributes);
          assemblyBuilder.DefineVersionInfoResource();
          UnsafeMethods._dynamicModule = assemblyBuilder.DefineDynamicModule(str);
        }
        return UnsafeMethods._dynamicModule;
      }
    }
  }

  private static UnsafeMethods.IUnknownReleaseDelegate Create_IUnknownRelease()
  {
    DynamicMethod dynamicMethod = new DynamicMethod("IUnknownRelease", typeof (int), new Type[1]
    {
      typeof (IntPtr)
    }, (Module) UnsafeMethods.DynamicModule);
    ILGenerator ilGenerator = dynamicMethod.GetILGenerator();
    ilGenerator.Emit(OpCodes.Ldarg_0);
    int num = 2 * Marshal.SizeOf(typeof (IntPtr));
    ilGenerator.Emit(OpCodes.Ldarg_0);
    ilGenerator.Emit(OpCodes.Ldind_I);
    ilGenerator.Emit(OpCodes.Ldc_I4, num);
    ilGenerator.Emit(OpCodes.Add);
    ilGenerator.Emit(OpCodes.Ldind_I);
    SignatureHelper methodSigHelper = SignatureHelper.GetMethodSigHelper(CallingConvention.Winapi, typeof (int));
    methodSigHelper.AddArgument(typeof (IntPtr));
    ilGenerator.Emit(OpCodes.Calli, methodSigHelper);
    ilGenerator.Emit(OpCodes.Ret);
    return (UnsafeMethods.IUnknownReleaseDelegate) dynamicMethod.CreateDelegate(typeof (UnsafeMethods.IUnknownReleaseDelegate));
  }

  [SecurityCritical]
  private static IntPtr GetNullInterfaceId()
  {
    int cb = Marshal.SizeOf((object) Guid.Empty);
    IntPtr ptr = Marshal.AllocHGlobal(cb);
    for (int ofs = 0; ofs < cb; ++ofs)
      Marshal.WriteByte(ptr, ofs, (byte) 0);
    return ptr;
  }

  private static UnsafeMethods.IDispatchInvokeDelegate _IDispatchInvokeNoResult
  {
    get
    {
      if (UnsafeMethods._IDispatchInvokeNoResultImpl == null)
      {
        lock (UnsafeMethods._IDispatchInvoke)
        {
          if (UnsafeMethods._IDispatchInvokeNoResultImpl == null)
            UnsafeMethods._IDispatchInvokeNoResultImpl = UnsafeMethods.Create_IDispatchInvoke(false);
        }
      }
      return UnsafeMethods._IDispatchInvokeNoResultImpl;
    }
  }

  private static UnsafeMethods.IDispatchInvokeDelegate Create_IDispatchInvoke(bool returnResult)
  {
    DynamicMethod dynamicMethod = new DynamicMethod("IDispatchInvoke", typeof (int), new Type[7]
    {
      typeof (IntPtr),
      typeof (int),
      typeof (System.Runtime.InteropServices.ComTypes.INVOKEKIND),
      typeof (System.Runtime.InteropServices.ComTypes.DISPPARAMS).MakeByRefType(),
      typeof (Variant).MakeByRefType(),
      typeof (ExcepInfo).MakeByRefType(),
      typeof (uint).MakeByRefType()
    }, (Module) UnsafeMethods.DynamicModule);
    ILGenerator ilGenerator = dynamicMethod.GetILGenerator();
    UnsafeMethods.EmitLoadArg(ilGenerator, 0);
    UnsafeMethods.EmitLoadArg(ilGenerator, 1);
    if (IntPtr.Size == 4)
      ilGenerator.Emit(OpCodes.Ldc_I4, UnsafeMethods.NullInterfaceId.ToInt32());
    else
      ilGenerator.Emit(OpCodes.Ldc_I8, UnsafeMethods.NullInterfaceId.ToInt64());
    ilGenerator.Emit(OpCodes.Conv_I);
    ilGenerator.Emit(OpCodes.Ldc_I4_0);
    UnsafeMethods.EmitLoadArg(ilGenerator, 2);
    UnsafeMethods.EmitLoadArg(ilGenerator, 3);
    if (returnResult)
      UnsafeMethods.EmitLoadArg(ilGenerator, 4);
    else
      ilGenerator.Emit(OpCodes.Ldsfld, typeof (IntPtr).GetField("Zero"));
    UnsafeMethods.EmitLoadArg(ilGenerator, 5);
    UnsafeMethods.EmitLoadArg(ilGenerator, 6);
    int num = 6 * Marshal.SizeOf(typeof (IntPtr));
    UnsafeMethods.EmitLoadArg(ilGenerator, 0);
    ilGenerator.Emit(OpCodes.Ldind_I);
    ilGenerator.Emit(OpCodes.Ldc_I4, num);
    ilGenerator.Emit(OpCodes.Add);
    ilGenerator.Emit(OpCodes.Ldind_I);
    SignatureHelper methodSigHelper = SignatureHelper.GetMethodSigHelper(CallingConvention.Winapi, typeof (int));
    Type[] arguments = new Type[9]
    {
      typeof (IntPtr),
      typeof (int),
      typeof (IntPtr),
      typeof (int),
      typeof (ushort),
      typeof (IntPtr),
      typeof (IntPtr),
      typeof (IntPtr),
      typeof (IntPtr)
    };
    methodSigHelper.AddArguments(arguments, (Type[][]) null, (Type[][]) null);
    ilGenerator.Emit(OpCodes.Calli, methodSigHelper);
    ilGenerator.Emit(OpCodes.Ret);
    return (UnsafeMethods.IDispatchInvokeDelegate) dynamicMethod.CreateDelegate(typeof (UnsafeMethods.IDispatchInvokeDelegate));
  }

  private delegate int IUnknownReleaseDelegate(IntPtr interfacePointer);

  private delegate int IDispatchInvokeDelegate(
    IntPtr dispatchPointer,
    int memberDispId,
    System.Runtime.InteropServices.ComTypes.INVOKEKIND flags,
    ref System.Runtime.InteropServices.ComTypes.DISPPARAMS dispParams,
    out Variant result,
    out ExcepInfo excepInfo,
    out uint argErr);
}
