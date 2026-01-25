// Type: System.Dynamic.VarEnumSelector
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

#nullable disable
namespace System.Linq.Expressions;

internal class VarEnumSelector
{
  private readonly VariantBuilder[] _variantBuilders;
  private static readonly Dictionary<VarEnum, Type> _ComToManagedPrimitiveTypes = VarEnumSelector.CreateComToManagedPrimitiveTypes();
  private static readonly IList<IList<VarEnum>> _ComPrimitiveTypeFamilies = VarEnumSelector.CreateComPrimitiveTypeFamilies();
  private const VarEnum VT_DEFAULT = VarEnum.VT_RECORD;

  internal VarEnumSelector(Type[] explicitArgTypes)
  {
    this._variantBuilders = new VariantBuilder[explicitArgTypes.Length];
    for (int index = 0; index < explicitArgTypes.Length; ++index)
      this._variantBuilders[index] = this.GetVariantBuilder(explicitArgTypes[index]);
  }

  internal VariantBuilder[] VariantBuilders => this._variantBuilders;

  internal static Type GetManagedMarshalType(VarEnum varEnum)
  {
    if (varEnum == VarEnum.VT_CY)
      return typeof (CurrencyWrapper);
    if (Variant.IsPrimitiveType(varEnum))
      return VarEnumSelector._ComToManagedPrimitiveTypes[varEnum];
    switch (varEnum)
    {
      case VarEnum.VT_EMPTY:
      case VarEnum.VT_NULL:
      case VarEnum.VT_DISPATCH:
      case VarEnum.VT_VARIANT:
      case VarEnum.VT_UNKNOWN:
        return typeof (object);
      case VarEnum.VT_ERROR:
        return typeof (ErrorWrapper);
      default:
        throw Error.UnexpectedVarEnum((object) varEnum);
    }
  }

  private static Dictionary<VarEnum, Type> CreateComToManagedPrimitiveTypes()
  {
    return new Dictionary<VarEnum, Type>()
    {
      [VarEnum.VT_I1] = typeof (sbyte),
      [VarEnum.VT_I2] = typeof (short),
      [VarEnum.VT_I4] = typeof (int),
      [VarEnum.VT_I8] = typeof (long),
      [VarEnum.VT_UI1] = typeof (byte),
      [VarEnum.VT_UI2] = typeof (ushort),
      [VarEnum.VT_UI4] = typeof (uint),
      [VarEnum.VT_UI8] = typeof (ulong),
      [VarEnum.VT_INT] = typeof (IntPtr),
      [VarEnum.VT_UINT] = typeof (UIntPtr),
      [VarEnum.VT_BOOL] = typeof (bool),
      [VarEnum.VT_R4] = typeof (float),
      [VarEnum.VT_R8] = typeof (double),
      [VarEnum.VT_DECIMAL] = typeof (Decimal),
      [VarEnum.VT_DATE] = typeof (DateTime),
      [VarEnum.VT_BSTR] = typeof (string),
      [VarEnum.VT_CY] = typeof (CurrencyWrapper),
      [VarEnum.VT_ERROR] = typeof (ErrorWrapper)
    };
  }

  private static IList<IList<VarEnum>> CreateComPrimitiveTypeFamilies()
  {
    return (IList<IList<VarEnum>>) new VarEnum[11][]
    {
      new VarEnum[4]
      {
        VarEnum.VT_I8,
        VarEnum.VT_I4,
        VarEnum.VT_I2,
        VarEnum.VT_I1
      },
      new VarEnum[4]
      {
        VarEnum.VT_UI8,
        VarEnum.VT_UI4,
        VarEnum.VT_UI2,
        VarEnum.VT_UI1
      },
      new VarEnum[1]{ VarEnum.VT_INT },
      new VarEnum[1]{ VarEnum.VT_UINT },
      new VarEnum[1]{ VarEnum.VT_BOOL },
      new VarEnum[1]{ VarEnum.VT_DATE },
      new VarEnum[2]{ VarEnum.VT_R8, VarEnum.VT_R4 },
      new VarEnum[1]{ VarEnum.VT_DECIMAL },
      new VarEnum[1]{ VarEnum.VT_BSTR },
      new VarEnum[1]{ VarEnum.VT_CY },
      new VarEnum[1]{ VarEnum.VT_ERROR }
    };
  }

  private static List<VarEnum> GetConversionsToComPrimitiveTypeFamilies(Type argumentType)
  {
    List<VarEnum> primitiveTypeFamilies = new List<VarEnum>();
    foreach (IEnumerable<VarEnum> primitiveTypeFamily in (IEnumerable<IList<VarEnum>>) VarEnumSelector._ComPrimitiveTypeFamilies)
    {
      foreach (VarEnum key in primitiveTypeFamily)
      {
        Type managedPrimitiveType = VarEnumSelector._ComToManagedPrimitiveTypes[key];
        if (TypeUtils.IsImplicitlyConvertible(argumentType, managedPrimitiveType, true))
        {
          primitiveTypeFamilies.Add(key);
          break;
        }
      }
    }
    return primitiveTypeFamilies;
  }

  private static void CheckForAmbiguousMatch(Type argumentType, List<VarEnum> compatibleComTypes)
  {
    if (compatibleComTypes.Count > 1)
    {
      string p1 = "";
      for (int index = 0; index < compatibleComTypes.Count; ++index)
      {
        string name = VarEnumSelector._ComToManagedPrimitiveTypes[compatibleComTypes[index]].Name;
        if (index == compatibleComTypes.Count - 1)
          p1 += " and ";
        else if (index != 0)
          p1 += ", ";
        p1 += name;
      }
      throw Error.AmbiguousConversion((object) argumentType.Name, (object) p1);
    }
  }

  private static bool TryGetPrimitiveComType(Type argumentType, out VarEnum primitiveVarEnum)
  {
    switch (Type.GetTypeCode(argumentType))
    {
      case TypeCode.Boolean:
        primitiveVarEnum = VarEnum.VT_BOOL;
        return true;
      case TypeCode.Char:
        primitiveVarEnum = VarEnum.VT_UI2;
        return true;
      case TypeCode.SByte:
        primitiveVarEnum = VarEnum.VT_I1;
        return true;
      case TypeCode.Byte:
        primitiveVarEnum = VarEnum.VT_UI1;
        return true;
      case TypeCode.Int16:
        primitiveVarEnum = VarEnum.VT_I2;
        return true;
      case TypeCode.UInt16:
        primitiveVarEnum = VarEnum.VT_UI2;
        return true;
      case TypeCode.Int32:
        primitiveVarEnum = VarEnum.VT_I4;
        return true;
      case TypeCode.UInt32:
        primitiveVarEnum = VarEnum.VT_UI4;
        return true;
      case TypeCode.Int64:
        primitiveVarEnum = VarEnum.VT_I8;
        return true;
      case TypeCode.UInt64:
        primitiveVarEnum = VarEnum.VT_UI8;
        return true;
      case TypeCode.Single:
        primitiveVarEnum = VarEnum.VT_R4;
        return true;
      case TypeCode.Double:
        primitiveVarEnum = VarEnum.VT_R8;
        return true;
      case TypeCode.Decimal:
        primitiveVarEnum = VarEnum.VT_DECIMAL;
        return true;
      case TypeCode.DateTime:
        primitiveVarEnum = VarEnum.VT_DATE;
        return true;
      case TypeCode.String:
        primitiveVarEnum = VarEnum.VT_BSTR;
        return true;
      default:
        if (argumentType == typeof (CurrencyWrapper))
        {
          primitiveVarEnum = VarEnum.VT_CY;
          return true;
        }
        if (argumentType == typeof (ErrorWrapper))
        {
          primitiveVarEnum = VarEnum.VT_ERROR;
          return true;
        }
        if (argumentType == typeof (IntPtr))
        {
          primitiveVarEnum = VarEnum.VT_INT;
          return true;
        }
        if (argumentType == typeof (UIntPtr))
        {
          primitiveVarEnum = VarEnum.VT_UINT;
          return true;
        }
        primitiveVarEnum = VarEnum.VT_VOID;
        return false;
    }
  }

  private static bool TryGetPrimitiveComTypeViaConversion(
    Type argumentType,
    out VarEnum primitiveVarEnum)
  {
    List<VarEnum> primitiveTypeFamilies = VarEnumSelector.GetConversionsToComPrimitiveTypeFamilies(argumentType);
    VarEnumSelector.CheckForAmbiguousMatch(argumentType, primitiveTypeFamilies);
    if (primitiveTypeFamilies.Count == 1)
    {
      primitiveVarEnum = primitiveTypeFamilies[0];
      return true;
    }
    primitiveVarEnum = VarEnum.VT_VOID;
    return false;
  }

  private VarEnum GetComType(ref Type argumentType)
  {
    if (argumentType == typeof (Missing))
      return VarEnum.VT_RECORD;
    if (argumentType.IsArray)
      return VarEnum.VT_ARRAY;
    if (argumentType == typeof (UnknownWrapper))
      return VarEnum.VT_UNKNOWN;
    if (argumentType == typeof (DispatchWrapper))
      return VarEnum.VT_DISPATCH;
    if (argumentType == typeof (VariantWrapper))
      return VarEnum.VT_VARIANT;
    if (argumentType == typeof (BStrWrapper))
      return VarEnum.VT_BSTR;
    if (argumentType == typeof (ErrorWrapper))
      return VarEnum.VT_ERROR;
    if (argumentType == typeof (CurrencyWrapper))
      return VarEnum.VT_CY;
    if (argumentType.IsEnum)
    {
      argumentType = Enum.GetUnderlyingType(argumentType);
      return this.GetComType(ref argumentType);
    }
    if (argumentType.IsNullableType())
    {
      argumentType = TypeUtils.GetNonNullableType(argumentType);
      return this.GetComType(ref argumentType);
    }
    if (argumentType.IsGenericType)
      return VarEnum.VT_UNKNOWN;
    VarEnum primitiveVarEnum;
    return VarEnumSelector.TryGetPrimitiveComType(argumentType, out primitiveVarEnum) ? primitiveVarEnum : VarEnum.VT_RECORD;
  }

  private VariantBuilder GetVariantBuilder(Type argumentType)
  {
    if (argumentType == (Type) null)
      return new VariantBuilder(VarEnum.VT_EMPTY, (ArgBuilder) new NullArgBuilder());
    if (argumentType == typeof (DBNull))
      return new VariantBuilder(VarEnum.VT_NULL, (ArgBuilder) new NullArgBuilder());
    if (argumentType.IsByRef)
    {
      Type elementType = argumentType.GetElementType();
      VarEnum elementVarEnum = elementType == typeof (object) || elementType == typeof (DBNull) ? VarEnum.VT_VARIANT : this.GetComType(ref elementType);
      ArgBuilder simpleArgBuilder = (ArgBuilder) VarEnumSelector.GetSimpleArgBuilder(elementType, elementVarEnum);
      return new VariantBuilder(elementVarEnum | VarEnum.VT_BYREF, simpleArgBuilder);
    }
    VarEnum comType = this.GetComType(ref argumentType);
    ArgBuilder byValArgBuilder = VarEnumSelector.GetByValArgBuilder(argumentType, ref comType);
    return new VariantBuilder(comType, byValArgBuilder);
  }

  private static ArgBuilder GetByValArgBuilder(Type elementType, ref VarEnum elementVarEnum)
  {
    if (elementVarEnum == VarEnum.VT_RECORD)
    {
      VarEnum primitiveVarEnum;
      if (VarEnumSelector.TryGetPrimitiveComTypeViaConversion(elementType, out primitiveVarEnum))
      {
        elementVarEnum = primitiveVarEnum;
        Type managedMarshalType = VarEnumSelector.GetManagedMarshalType(elementVarEnum);
        return (ArgBuilder) new ConversionArgBuilder(elementType, VarEnumSelector.GetSimpleArgBuilder(managedMarshalType, elementVarEnum));
      }
      if (typeof (IConvertible).IsAssignableFrom(elementType))
        return (ArgBuilder) new ConvertibleArgBuilder();
    }
    return (ArgBuilder) VarEnumSelector.GetSimpleArgBuilder(elementType, elementVarEnum);
  }

  private static SimpleArgBuilder GetSimpleArgBuilder(Type elementType, VarEnum elementVarEnum)
  {
    SimpleArgBuilder simpleArgBuilder;
    switch (elementVarEnum)
    {
      case VarEnum.VT_CY:
        simpleArgBuilder = (SimpleArgBuilder) new CurrencyArgBuilder(elementType);
        break;
      case VarEnum.VT_DATE:
        simpleArgBuilder = (SimpleArgBuilder) new DateTimeArgBuilder(elementType);
        break;
      case VarEnum.VT_BSTR:
        simpleArgBuilder = (SimpleArgBuilder) new StringArgBuilder(elementType);
        break;
      case VarEnum.VT_DISPATCH:
        simpleArgBuilder = (SimpleArgBuilder) new DispatchArgBuilder(elementType);
        break;
      case VarEnum.VT_ERROR:
        simpleArgBuilder = (SimpleArgBuilder) new ErrorArgBuilder(elementType);
        break;
      case VarEnum.VT_BOOL:
        simpleArgBuilder = (SimpleArgBuilder) new BoolArgBuilder(elementType);
        break;
      case VarEnum.VT_VARIANT:
      case VarEnum.VT_RECORD:
      case VarEnum.VT_ARRAY:
        simpleArgBuilder = (SimpleArgBuilder) new VariantArgBuilder(elementType);
        break;
      case VarEnum.VT_UNKNOWN:
        simpleArgBuilder = (SimpleArgBuilder) new UnknownArgBuilder(elementType);
        break;
      default:
        Type managedMarshalType = VarEnumSelector.GetManagedMarshalType(elementVarEnum);
        simpleArgBuilder = !(elementType == managedMarshalType) ? (SimpleArgBuilder) new ConvertArgBuilder(elementType, managedMarshalType) : new SimpleArgBuilder(elementType);
        break;
    }
    return simpleArgBuilder;
  }
}
