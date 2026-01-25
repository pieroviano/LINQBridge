// Type: System.Dynamic.Variant
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;

#nullable disable
namespace System.Linq.Expressions;

[StructLayout(LayoutKind.Explicit)]
internal struct Variant
{
  [FieldOffset(0)]
  private Variant.TypeUnion _typeUnion;
  [FieldOffset(0)]
  private Decimal _decimal;

  public override string ToString()
  {
    return string.Format((IFormatProvider) CultureInfo.CurrentCulture, "Variant ({0})", new object[1]
    {
      (object) this.VariantType
    });
  }

  internal static bool IsPrimitiveType(VarEnum varEnum)
  {
    switch (varEnum)
    {
      case VarEnum.VT_I2:
      case VarEnum.VT_I4:
      case VarEnum.VT_R4:
      case VarEnum.VT_R8:
      case VarEnum.VT_CY:
      case VarEnum.VT_DATE:
      case VarEnum.VT_BSTR:
      case VarEnum.VT_ERROR:
      case VarEnum.VT_BOOL:
      case VarEnum.VT_DECIMAL:
      case VarEnum.VT_I1:
      case VarEnum.VT_UI1:
      case VarEnum.VT_UI2:
      case VarEnum.VT_UI4:
      case VarEnum.VT_I8:
      case VarEnum.VT_UI8:
      case VarEnum.VT_INT:
      case VarEnum.VT_UINT:
        return true;
      default:
        return false;
    }
  }

  [SecurityCritical]
    public object ToObject()
  {
    if (this.IsEmpty)
      return (object) null;
    switch (this.VariantType)
    {
      case VarEnum.VT_NULL:
        return (object) DBNull.Value;
      case VarEnum.VT_I2:
        return (object) this.AsI2;
      case VarEnum.VT_I4:
        return (object) this.AsI4;
      case VarEnum.VT_R4:
        return (object) this.AsR4;
      case VarEnum.VT_R8:
        return (object) this.AsR8;
      case VarEnum.VT_CY:
        return (object) this.AsCy;
      case VarEnum.VT_DATE:
        return (object) this.AsDate;
      case VarEnum.VT_BSTR:
        return (object) this.AsBstr;
      case VarEnum.VT_DISPATCH:
        return this.AsDispatch;
      case VarEnum.VT_ERROR:
        return (object) this.AsError;
      case VarEnum.VT_BOOL:
        return (object) this.AsBool;
      case VarEnum.VT_VARIANT:
        return this.AsVariant;
      case VarEnum.VT_UNKNOWN:
        return this.AsUnknown;
      case VarEnum.VT_DECIMAL:
        return (object) this.AsDecimal;
      case VarEnum.VT_I1:
        return (object) this.AsI1;
      case VarEnum.VT_UI1:
        return (object) this.AsUi1;
      case VarEnum.VT_UI2:
        return (object) this.AsUi2;
      case VarEnum.VT_UI4:
        return (object) this.AsUi4;
      case VarEnum.VT_I8:
        return (object) this.AsI8;
      case VarEnum.VT_UI8:
        return (object) this.AsUi8;
      case VarEnum.VT_INT:
        return (object) this.AsInt;
      case VarEnum.VT_UINT:
        return (object) this.AsUint;
      default:
        return this.AsVariant;
    }
  }

  [SecurityCritical]
    public void Clear()
  {
    VarEnum variantType = this.VariantType;
    if ((variantType & VarEnum.VT_BYREF) != VarEnum.VT_EMPTY)
      this.VariantType = VarEnum.VT_EMPTY;
    else if ((variantType & VarEnum.VT_ARRAY) != VarEnum.VT_EMPTY || variantType == VarEnum.VT_BSTR || variantType == VarEnum.VT_UNKNOWN || variantType == VarEnum.VT_DISPATCH || variantType == VarEnum.VT_RECORD)
      NativeMethods.VariantClear(UnsafeMethods.ConvertVariantByrefToPtr(ref this));
    else
      this.VariantType = VarEnum.VT_EMPTY;
  }

  public VarEnum VariantType
  {
    get => (VarEnum) this._typeUnion._vt;
    set => this._typeUnion._vt = (ushort) value;
  }

  internal bool IsEmpty => this._typeUnion._vt == (ushort) 0;

    public void SetAsNull() => this.VariantType = VarEnum.VT_NULL;

  [SecurityCritical]
    public void SetAsIConvertible(IConvertible value)
  {
    TypeCode typeCode = value.GetTypeCode();
    CultureInfo currentCulture = CultureInfo.CurrentCulture;
    switch (typeCode)
    {
      case TypeCode.Empty:
        break;
      case TypeCode.Object:
        this.AsUnknown = (object) value;
        break;
      case TypeCode.DBNull:
        this.SetAsNull();
        break;
      case TypeCode.Boolean:
        this.AsBool = value.ToBoolean((IFormatProvider) currentCulture);
        break;
      case TypeCode.Char:
        this.AsUi2 = (ushort) value.ToChar((IFormatProvider) currentCulture);
        break;
      case TypeCode.SByte:
        this.AsI1 = value.ToSByte((IFormatProvider) currentCulture);
        break;
      case TypeCode.Byte:
        this.AsUi1 = value.ToByte((IFormatProvider) currentCulture);
        break;
      case TypeCode.Int16:
        this.AsI2 = value.ToInt16((IFormatProvider) currentCulture);
        break;
      case TypeCode.UInt16:
        this.AsUi2 = value.ToUInt16((IFormatProvider) currentCulture);
        break;
      case TypeCode.Int32:
        this.AsI4 = value.ToInt32((IFormatProvider) currentCulture);
        break;
      case TypeCode.UInt32:
        this.AsUi4 = value.ToUInt32((IFormatProvider) currentCulture);
        break;
      case TypeCode.Int64:
        this.AsI8 = value.ToInt64((IFormatProvider) currentCulture);
        break;
      case TypeCode.UInt64:
        this.AsI8 = value.ToInt64((IFormatProvider) currentCulture);
        break;
      case TypeCode.Single:
        this.AsR4 = value.ToSingle((IFormatProvider) currentCulture);
        break;
      case TypeCode.Double:
        this.AsR8 = value.ToDouble((IFormatProvider) currentCulture);
        break;
      case TypeCode.Decimal:
        this.AsDecimal = value.ToDecimal((IFormatProvider) currentCulture);
        break;
      case TypeCode.DateTime:
        this.AsDate = value.ToDateTime((IFormatProvider) currentCulture);
        break;
      case TypeCode.String:
        this.AsBstr = value.ToString((IFormatProvider) currentCulture);
        break;
      default:
        throw Assert.Unreachable;
    }
  }

  public sbyte AsI1
  {
    get => this._typeUnion._unionTypes._i1;
    set
    {
      this.VariantType = VarEnum.VT_I1;
      this._typeUnion._unionTypes._i1 = value;
    }
  }

  [SecurityCritical]
    public void SetAsByrefI1(ref sbyte value)
  {
    this.VariantType = VarEnum.VT_I1 | VarEnum.VT_BYREF;
    this._typeUnion._unionTypes._byref = UnsafeMethods.ConvertSByteByrefToPtr(ref value);
  }

  public short AsI2
  {
    get => this._typeUnion._unionTypes._i2;
    set
    {
      this.VariantType = VarEnum.VT_I2;
      this._typeUnion._unionTypes._i2 = value;
    }
  }

  [SecurityCritical]
    public void SetAsByrefI2(ref short value)
  {
    this.VariantType = VarEnum.VT_I2 | VarEnum.VT_BYREF;
    this._typeUnion._unionTypes._byref = UnsafeMethods.ConvertInt16ByrefToPtr(ref value);
  }

  public int AsI4
  {
    get => this._typeUnion._unionTypes._i4;
    set
    {
      this.VariantType = VarEnum.VT_I4;
      this._typeUnion._unionTypes._i4 = value;
    }
  }

  [SecurityCritical]
    public void SetAsByrefI4(ref int value)
  {
    this.VariantType = VarEnum.VT_I4 | VarEnum.VT_BYREF;
    this._typeUnion._unionTypes._byref = UnsafeMethods.ConvertInt32ByrefToPtr(ref value);
  }

  public long AsI8
  {
    get => this._typeUnion._unionTypes._i8;
    set
    {
      this.VariantType = VarEnum.VT_I8;
      this._typeUnion._unionTypes._i8 = value;
    }
  }

  [SecurityCritical]
    public void SetAsByrefI8(ref long value)
  {
    this.VariantType = VarEnum.VT_I8 | VarEnum.VT_BYREF;
    this._typeUnion._unionTypes._byref = UnsafeMethods.ConvertInt64ByrefToPtr(ref value);
  }

  public byte AsUi1
  {
    get => this._typeUnion._unionTypes._ui1;
    set
    {
      this.VariantType = VarEnum.VT_UI1;
      this._typeUnion._unionTypes._ui1 = value;
    }
  }

  [SecurityCritical]
    public void SetAsByrefUi1(ref byte value)
  {
    this.VariantType = VarEnum.VT_UI1 | VarEnum.VT_BYREF;
    this._typeUnion._unionTypes._byref = UnsafeMethods.ConvertByteByrefToPtr(ref value);
  }

  public ushort AsUi2
  {
    get => this._typeUnion._unionTypes._ui2;
    set
    {
      this.VariantType = VarEnum.VT_UI2;
      this._typeUnion._unionTypes._ui2 = value;
    }
  }

  [SecurityCritical]
    public void SetAsByrefUi2(ref ushort value)
  {
    this.VariantType = VarEnum.VT_UI2 | VarEnum.VT_BYREF;
    this._typeUnion._unionTypes._byref = UnsafeMethods.ConvertUInt16ByrefToPtr(ref value);
  }

  public uint AsUi4
  {
    get => this._typeUnion._unionTypes._ui4;
    set
    {
      this.VariantType = VarEnum.VT_UI4;
      this._typeUnion._unionTypes._ui4 = value;
    }
  }

  [SecurityCritical]
    public void SetAsByrefUi4(ref uint value)
  {
    this.VariantType = VarEnum.VT_UI4 | VarEnum.VT_BYREF;
    this._typeUnion._unionTypes._byref = UnsafeMethods.ConvertUInt32ByrefToPtr(ref value);
  }

  public ulong AsUi8
  {
    get => this._typeUnion._unionTypes._ui8;
    set
    {
      this.VariantType = VarEnum.VT_UI8;
      this._typeUnion._unionTypes._ui8 = value;
    }
  }

  [SecurityCritical]
    public void SetAsByrefUi8(ref ulong value)
  {
    this.VariantType = VarEnum.VT_UI8 | VarEnum.VT_BYREF;
    this._typeUnion._unionTypes._byref = UnsafeMethods.ConvertUInt64ByrefToPtr(ref value);
  }

  public IntPtr AsInt
  {
    get => this._typeUnion._unionTypes._int;
    set
    {
      this.VariantType = VarEnum.VT_INT;
      this._typeUnion._unionTypes._int = value;
    }
  }

  [SecurityCritical]
    public void SetAsByrefInt(ref IntPtr value)
  {
    this.VariantType = VarEnum.VT_INT | VarEnum.VT_BYREF;
    this._typeUnion._unionTypes._byref = UnsafeMethods.ConvertIntPtrByrefToPtr(ref value);
  }

  public UIntPtr AsUint
  {
    get => this._typeUnion._unionTypes._uint;
    set
    {
      this.VariantType = VarEnum.VT_UINT;
      this._typeUnion._unionTypes._uint = value;
    }
  }

  [SecurityCritical]
    public void SetAsByrefUint(ref UIntPtr value)
  {
    this.VariantType = VarEnum.VT_UINT | VarEnum.VT_BYREF;
    this._typeUnion._unionTypes._byref = UnsafeMethods.ConvertUIntPtrByrefToPtr(ref value);
  }

  public bool AsBool
  {
    get => this._typeUnion._unionTypes._bool != (short) 0;
    set
    {
      this.VariantType = VarEnum.VT_BOOL;
      this._typeUnion._unionTypes._bool = value ? (short) -1 : (short) 0;
    }
  }

  [SecurityCritical]
    public void SetAsByrefBool(ref short value)
  {
    this.VariantType = VarEnum.VT_BOOL | VarEnum.VT_BYREF;
    this._typeUnion._unionTypes._byref = UnsafeMethods.ConvertInt16ByrefToPtr(ref value);
  }

  public int AsError
  {
    get => this._typeUnion._unionTypes._error;
    set
    {
      this.VariantType = VarEnum.VT_ERROR;
      this._typeUnion._unionTypes._error = value;
    }
  }

  [SecurityCritical]
    public void SetAsByrefError(ref int value)
  {
    this.VariantType = VarEnum.VT_ERROR | VarEnum.VT_BYREF;
    this._typeUnion._unionTypes._byref = UnsafeMethods.ConvertInt32ByrefToPtr(ref value);
  }

  public float AsR4
  {
    get => this._typeUnion._unionTypes._r4;
    set
    {
      this.VariantType = VarEnum.VT_R4;
      this._typeUnion._unionTypes._r4 = value;
    }
  }

  [SecurityCritical]
    public void SetAsByrefR4(ref float value)
  {
    this.VariantType = VarEnum.VT_R4 | VarEnum.VT_BYREF;
    this._typeUnion._unionTypes._byref = UnsafeMethods.ConvertSingleByrefToPtr(ref value);
  }

  public double AsR8
  {
    get => this._typeUnion._unionTypes._r8;
    set
    {
      this.VariantType = VarEnum.VT_R8;
      this._typeUnion._unionTypes._r8 = value;
    }
  }

  [SecurityCritical]
    public void SetAsByrefR8(ref double value)
  {
    this.VariantType = VarEnum.VT_R8 | VarEnum.VT_BYREF;
    this._typeUnion._unionTypes._byref = UnsafeMethods.ConvertDoubleByrefToPtr(ref value);
  }

  public Decimal AsDecimal
  {
    get
    {
      Variant variant = this;
      variant._typeUnion._vt = (ushort) 0;
      return variant._decimal;
    }
    set
    {
      this.VariantType = VarEnum.VT_DECIMAL;
      this._decimal = value;
      this._typeUnion._vt = (ushort) 14;
    }
  }

  [SecurityCritical]
    public void SetAsByrefDecimal(ref Decimal value)
  {
    this.VariantType = VarEnum.VT_DECIMAL | VarEnum.VT_BYREF;
    this._typeUnion._unionTypes._byref = UnsafeMethods.ConvertDecimalByrefToPtr(ref value);
  }

  public Decimal AsCy
  {
    get => Decimal.FromOACurrency(this._typeUnion._unionTypes._cy);
    set
    {
      this.VariantType = VarEnum.VT_CY;
      this._typeUnion._unionTypes._cy = Decimal.ToOACurrency(value);
    }
  }

  [SecurityCritical]
    public void SetAsByrefCy(ref long value)
  {
    this.VariantType = VarEnum.VT_CY | VarEnum.VT_BYREF;
    this._typeUnion._unionTypes._byref = UnsafeMethods.ConvertInt64ByrefToPtr(ref value);
  }

  public DateTime AsDate
  {
    get => DateTime.FromOADate(this._typeUnion._unionTypes._date);
    set
    {
      this.VariantType = VarEnum.VT_DATE;
      this._typeUnion._unionTypes._date = value.ToOADate();
    }
  }

  [SecurityCritical]
    public void SetAsByrefDate(ref double value)
  {
    this.VariantType = VarEnum.VT_DATE | VarEnum.VT_BYREF;
    this._typeUnion._unionTypes._byref = UnsafeMethods.ConvertDoubleByrefToPtr(ref value);
  }

  public string AsBstr
  {
    [SecurityCritical] get
    {
      return this._typeUnion._unionTypes._bstr != IntPtr.Zero ? Marshal.PtrToStringBSTR(this._typeUnion._unionTypes._bstr) : (string) null;
    }
    [SecurityCritical] set
    {
      this.VariantType = VarEnum.VT_BSTR;
      if (value == null)
        return;
      Marshal.GetNativeVariantForObject((object) value, UnsafeMethods.ConvertVariantByrefToPtr(ref this));
    }
  }

  [SecurityCritical]
    public void SetAsByrefBstr(ref IntPtr value)
  {
    this.VariantType = VarEnum.VT_BSTR | VarEnum.VT_BYREF;
    this._typeUnion._unionTypes._byref = UnsafeMethods.ConvertIntPtrByrefToPtr(ref value);
  }

  public object AsUnknown
  {
    [SecurityCritical] get
    {
      return this._typeUnion._unionTypes._dispatch != IntPtr.Zero ? Marshal.GetObjectForIUnknown(this._typeUnion._unionTypes._unknown) : (object) null;
    }
    [SecurityCritical] set
    {
      this.VariantType = VarEnum.VT_UNKNOWN;
      if (value == null)
        return;
      this._typeUnion._unionTypes._unknown = Marshal.GetIUnknownForObject(value);
    }
  }

  [SecurityCritical]
    public void SetAsByrefUnknown(ref IntPtr value)
  {
    this.VariantType = VarEnum.VT_UNKNOWN | VarEnum.VT_BYREF;
    this._typeUnion._unionTypes._byref = UnsafeMethods.ConvertIntPtrByrefToPtr(ref value);
  }

  public object AsDispatch
  {
    [SecurityCritical] get
    {
      return this._typeUnion._unionTypes._dispatch != IntPtr.Zero ? Marshal.GetObjectForIUnknown(this._typeUnion._unionTypes._dispatch) : (object) null;
    }
    [SecurityCritical] set
    {
      this.VariantType = VarEnum.VT_DISPATCH;
      if (value == null)
        return;
      this._typeUnion._unionTypes._unknown = Marshal.GetIDispatchForObject(value);
    }
  }

  [SecurityCritical]
    public void SetAsByrefDispatch(ref IntPtr value)
  {
    this.VariantType = VarEnum.VT_DISPATCH | VarEnum.VT_BYREF;
    this._typeUnion._unionTypes._byref = UnsafeMethods.ConvertIntPtrByrefToPtr(ref value);
  }

  public object AsVariant
  {
    [SecurityCritical] get
    {
      return Marshal.GetObjectForNativeVariant(UnsafeMethods.ConvertVariantByrefToPtr(ref this));
    }
    [SecurityCritical] set
    {
      if (value == null)
        return;
      UnsafeMethods.InitVariantForObject(value, ref this);
    }
  }

  [SecurityCritical]
    public void SetAsByrefVariant(ref Variant value)
  {
    this.VariantType = VarEnum.VT_VARIANT | VarEnum.VT_BYREF;
    this._typeUnion._unionTypes._byref = UnsafeMethods.ConvertVariantByrefToPtr(ref value);
  }

  [SecurityCritical]
    public void SetAsByrefVariantIndirect(ref Variant value)
  {
    switch (value.VariantType)
    {
      case VarEnum.VT_EMPTY:
      case VarEnum.VT_NULL:
        this.SetAsByrefVariant(ref value);
        return;
      case VarEnum.VT_DECIMAL:
        this._typeUnion._unionTypes._byref = UnsafeMethods.ConvertDecimalByrefToPtr(ref value._decimal);
        break;
      case VarEnum.VT_RECORD:
        this._typeUnion._unionTypes._record = value._typeUnion._unionTypes._record;
        break;
      default:
        this._typeUnion._unionTypes._byref = UnsafeMethods.ConvertIntPtrByrefToPtr(ref value._typeUnion._unionTypes._byref);
        break;
    }
    this.VariantType = value.VariantType | VarEnum.VT_BYREF;
  }

  internal static PropertyInfo GetAccessor(VarEnum varType)
  {
    switch (varType)
    {
      case VarEnum.VT_I2:
        return typeof (Variant).GetProperty("AsI2");
      case VarEnum.VT_I4:
        return typeof (Variant).GetProperty("AsI4");
      case VarEnum.VT_R4:
        return typeof (Variant).GetProperty("AsR4");
      case VarEnum.VT_R8:
        return typeof (Variant).GetProperty("AsR8");
      case VarEnum.VT_CY:
        return typeof (Variant).GetProperty("AsCy");
      case VarEnum.VT_DATE:
        return typeof (Variant).GetProperty("AsDate");
      case VarEnum.VT_BSTR:
        return typeof (Variant).GetProperty("AsBstr");
      case VarEnum.VT_DISPATCH:
        return typeof (Variant).GetProperty("AsDispatch");
      case VarEnum.VT_ERROR:
        return typeof (Variant).GetProperty("AsError");
      case VarEnum.VT_BOOL:
        return typeof (Variant).GetProperty("AsBool");
      case VarEnum.VT_VARIANT:
      case VarEnum.VT_RECORD:
      case VarEnum.VT_ARRAY:
        return typeof (Variant).GetProperty("AsVariant");
      case VarEnum.VT_UNKNOWN:
        return typeof (Variant).GetProperty("AsUnknown");
      case VarEnum.VT_DECIMAL:
        return typeof (Variant).GetProperty("AsDecimal");
      case VarEnum.VT_I1:
        return typeof (Variant).GetProperty("AsI1");
      case VarEnum.VT_UI1:
        return typeof (Variant).GetProperty("AsUi1");
      case VarEnum.VT_UI2:
        return typeof (Variant).GetProperty("AsUi2");
      case VarEnum.VT_UI4:
        return typeof (Variant).GetProperty("AsUi4");
      case VarEnum.VT_I8:
        return typeof (Variant).GetProperty("AsI8");
      case VarEnum.VT_UI8:
        return typeof (Variant).GetProperty("AsUi8");
      case VarEnum.VT_INT:
        return typeof (Variant).GetProperty("AsInt");
      case VarEnum.VT_UINT:
        return typeof (Variant).GetProperty("AsUint");
      default:
        throw Error.VariantGetAccessorNYI((object) varType);
    }
  }

  internal static MethodInfo GetByrefSetter(VarEnum varType)
  {
    switch (varType)
    {
      case VarEnum.VT_I2:
        return typeof (Variant).GetMethod("SetAsByrefI2");
      case VarEnum.VT_I4:
        return typeof (Variant).GetMethod("SetAsByrefI4");
      case VarEnum.VT_R4:
        return typeof (Variant).GetMethod("SetAsByrefR4");
      case VarEnum.VT_R8:
        return typeof (Variant).GetMethod("SetAsByrefR8");
      case VarEnum.VT_CY:
        return typeof (Variant).GetMethod("SetAsByrefCy");
      case VarEnum.VT_DATE:
        return typeof (Variant).GetMethod("SetAsByrefDate");
      case VarEnum.VT_BSTR:
        return typeof (Variant).GetMethod("SetAsByrefBstr");
      case VarEnum.VT_DISPATCH:
        return typeof (Variant).GetMethod("SetAsByrefDispatch");
      case VarEnum.VT_ERROR:
        return typeof (Variant).GetMethod("SetAsByrefError");
      case VarEnum.VT_BOOL:
        return typeof (Variant).GetMethod("SetAsByrefBool");
      case VarEnum.VT_VARIANT:
        return typeof (Variant).GetMethod("SetAsByrefVariant");
      case VarEnum.VT_UNKNOWN:
        return typeof (Variant).GetMethod("SetAsByrefUnknown");
      case VarEnum.VT_DECIMAL:
        return typeof (Variant).GetMethod("SetAsByrefDecimal");
      case VarEnum.VT_I1:
        return typeof (Variant).GetMethod("SetAsByrefI1");
      case VarEnum.VT_UI1:
        return typeof (Variant).GetMethod("SetAsByrefUi1");
      case VarEnum.VT_UI2:
        return typeof (Variant).GetMethod("SetAsByrefUi2");
      case VarEnum.VT_UI4:
        return typeof (Variant).GetMethod("SetAsByrefUi4");
      case VarEnum.VT_I8:
        return typeof (Variant).GetMethod("SetAsByrefI8");
      case VarEnum.VT_UI8:
        return typeof (Variant).GetMethod("SetAsByrefUi8");
      case VarEnum.VT_INT:
        return typeof (Variant).GetMethod("SetAsByrefInt");
      case VarEnum.VT_UINT:
        return typeof (Variant).GetMethod("SetAsByrefUint");
      case VarEnum.VT_RECORD:
      case VarEnum.VT_ARRAY:
        return typeof (Variant).GetMethod("SetAsByrefVariantIndirect");
      default:
        throw Error.VariantGetAccessorNYI((object) varType);
    }
  }

  private struct TypeUnion
  {
    internal ushort _vt;
    internal ushort _wReserved1;
    internal ushort _wReserved2;
    internal ushort _wReserved3;
    internal Variant.UnionTypes _unionTypes;
  }

  private struct Record
  {
    private IntPtr _record;
    private IntPtr _recordInfo;
  }

  [StructLayout(LayoutKind.Explicit)]
  private struct UnionTypes
  {
    [FieldOffset(0)]
    internal sbyte _i1;
    [FieldOffset(0)]
    internal short _i2;
    [FieldOffset(0)]
    internal int _i4;
    [FieldOffset(0)]
    internal long _i8;
    [FieldOffset(0)]
    internal byte _ui1;
    [FieldOffset(0)]
    internal ushort _ui2;
    [FieldOffset(0)]
    internal uint _ui4;
    [FieldOffset(0)]
    internal ulong _ui8;
    [FieldOffset(0)]
    internal IntPtr _int;
    [FieldOffset(0)]
    internal UIntPtr _uint;
    [FieldOffset(0)]
    internal short _bool;
    [FieldOffset(0)]
    internal int _error;
    [FieldOffset(0)]
    internal float _r4;
    [FieldOffset(0)]
    internal double _r8;
    [FieldOffset(0)]
    internal long _cy;
    [FieldOffset(0)]
    internal double _date;
    [FieldOffset(0)]
    internal IntPtr _bstr;
    [FieldOffset(0)]
    internal IntPtr _unknown;
    [FieldOffset(0)]
    internal IntPtr _dispatch;
    [FieldOffset(0)]
    internal IntPtr _byref;
    [FieldOffset(0)]
    internal Variant.Record _record;
  }
}
