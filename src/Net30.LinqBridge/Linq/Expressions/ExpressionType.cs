namespace System.Linq.Expressions;

/// <summary>Describes the node types for the nodes of an expression tree.</summary>
public enum ExpressionType
{
    /// <summary>An addition operation, such as a + b, without overflow checking, for numeric operands.</summary>
    Add,

    /// <summary>An addition operation, such as (a + b), with overflow checking, for numeric operands.</summary>
    AddChecked,

    /// <summary>A bitwise or logical AND operation, such as (a &amp; b) in C# and (a And b) in Visual Basic.</summary>
    And,

    /// <summary>
    ///     A conditional AND operation that evaluates the second operand only if the first operand evaluates to true. It
    ///     corresponds to (a &amp;&amp; b) in C# and (a AndAlso b) in Visual Basic.
    /// </summary>
    AndAlso,

    /// <summary>An operation that obtains the length of a one-dimensional array, such as array.Length.</summary>
    ArrayLength,

    /// <summary>An indexing operation in a one-dimensional array, such as array[index] in C# or array(index) in Visual Basic.</summary>
    ArrayIndex,

    /// <summary>A method call, such as in the obj.sampleMethod() expression.</summary>
    Call,

    /// <summary>A node that represents a null coalescing operation, such as (a ?? b) in C# or If(a, b) in Visual Basic.</summary>
    Coalesce,

    /// <summary>A conditional operation, such as a &gt; b ? a : b in C# or If(a &gt; b, a, b) in Visual Basic.</summary>
    Conditional,

    /// <summary>A constant value.</summary>
    Constant,

    /// <summary>
    ///     A cast or conversion operation, such as (SampleType)obj in C#or CType(obj, SampleType) in Visual Basic. For a
    ///     numeric conversion, if the converted value is too large for the destination type, no exception is thrown.
    /// </summary>
    Convert,

    /// <summary>
    ///     A cast or conversion operation, such as (SampleType)obj in C#or CType(obj, SampleType) in Visual Basic. For a
    ///     numeric conversion, if the converted value does not fit the destination type, an exception is thrown.
    /// </summary>
    ConvertChecked,

    /// <summary>A division operation, such as (a / b), for numeric operands.</summary>
    Divide,

    /// <summary>A node that represents an equality comparison, such as (a == b) in C# or (a = b) in Visual Basic.</summary>
    Equal,

    /// <summary>A bitwise or logical XOR operation, such as (a ^ b) in C# or (a Xor b) in Visual Basic.</summary>
    ExclusiveOr,

    /// <summary>A "greater than" comparison, such as (a &gt; b).</summary>
    GreaterThan,

    /// <summary>A "greater than or equal to" comparison, such as (a &gt;= b).</summary>
    GreaterThanOrEqual,

    /// <summary>An operation that invokes a delegate or lambda expression, such as sampleDelegate.Invoke().</summary>
    Invoke,

    /// <summary>A lambda expression, such as a =&gt; a + a in C# or Function(a) a + a in Visual Basic.</summary>
    Lambda,

    /// <summary>A bitwise left-shift operation, such as (a &lt;&lt; b).</summary>
    LeftShift,

    /// <summary>A "less than" comparison, such as (a &lt; b).</summary>
    LessThan,

    /// <summary>A "less than or equal to" comparison, such as (a &lt;= b).</summary>
    LessThanOrEqual,

    /// <summary>
    ///     An operation that creates a new <see cref="T:System.Collections.IEnumerable" /> object and initializes it from
    ///     a list of elements, such as new List&lt;SampleType&gt;(){ a, b, c } in C# or Dim sampleList = { a, b, c } in Visual
    ///     Basic.
    /// </summary>
    ListInit,

    /// <summary>An operation that reads from a field or property, such as obj.SampleProperty.</summary>
    MemberAccess,

    /// <summary>
    ///     An operation that creates a new object and initializes one or more of its members, such as new Point { X = 1,
    ///     Y = 2 } in C# or New Point With {.X = 1, .Y = 2} in Visual Basic.
    /// </summary>
    MemberInit,

    /// <summary>An arithmetic remainder operation, such as (a % b) in C# or (a Mod b) in Visual Basic.</summary>
    Modulo,

    /// <summary>A multiplication operation, such as (a * b), without overflow checking, for numeric operands.</summary>
    Multiply,

    /// <summary>An multiplication operation, such as (a * b), that has overflow checking, for numeric operands.</summary>
    MultiplyChecked,

    /// <summary>An arithmetic negation operation, such as (-a). The object a should not be modified in place.</summary>
    Negate,

    /// <summary>
    ///     A unary plus operation, such as (+a). The result of a predefined unary plus operation is the value of the
    ///     operand, but user-defined implementations might have unusual results.
    /// </summary>
    UnaryPlus,

    /// <summary>
    ///     An arithmetic negation operation, such as (-a), that has overflow checking. The object a should not be
    ///     modified in place.
    /// </summary>
    NegateChecked,

    /// <summary>An operation that calls a constructor to create a new object, such as new SampleType().</summary>
    New,

    /// <summary>
    ///     An operation that creates a new one-dimensional array and initializes it from a list of elements, such as new
    ///     SampleType[]{a, b, c} in C# or New SampleType(){a, b, c} in Visual Basic.
    /// </summary>
    NewArrayInit,

    /// <summary>
    ///     An operation that creates a new array, in which the bounds for each dimension are specified, such as new
    ///     SampleType[dim1, dim2] in C# or New SampleType(dim1, dim2) in Visual Basic.
    /// </summary>
    NewArrayBounds,

    /// <summary>
    ///     A bitwise complement or logical negation operation. In C#, it is equivalent to (~a) for integral types and to
    ///     (!a) for Boolean values. In Visual Basic, it is equivalent to (Not a). The object a should not be modified in
    ///     place.
    /// </summary>
    Not,

    /// <summary>An inequality comparison, such as (a != b) in C# or (a &lt;&gt; b) in Visual Basic.</summary>
    NotEqual,

    /// <summary>A bitwise or logical OR operation, such as (a | b) in C# or (a Or b) in Visual Basic.</summary>
    Or,

    /// <summary>A short-circuiting conditional OR operation, such as (a || b) in C# or (a OrElse b) in Visual Basic.</summary>
    OrElse,

    /// <summary>
    ///     A reference to a parameter or variable that is defined in the context of the expression. For more information,
    ///     see <see cref="T:System.Linq.Expressions.ParameterExpression" />.
    /// </summary>
    Parameter,

    /// <summary>A mathematical operation that raises a number to a power, such as (a ^ b) in Visual Basic.</summary>
    Power,

    /// <summary>
    ///     An expression that has a constant value of type <see cref="T:System.Linq.Expressions.Expression" />. A
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Quote" /> node can contain references to parameters that are
    ///     defined in the context of the expression it represents.
    /// </summary>
    Quote,

    /// <summary>A bitwise right-shift operation, such as (a &gt;&gt; b).</summary>
    RightShift,

    /// <summary>A subtraction operation, such as (a - b), without overflow checking, for numeric operands.</summary>
    Subtract,

    /// <summary>An arithmetic subtraction operation, such as (a - b), that has overflow checking, for numeric operands.</summary>
    SubtractChecked,

    /// <summary>
    ///     An explicit reference or boxing conversion in which null is supplied if the conversion fails, such as (obj as
    ///     SampleType) in C# or TryCast(obj, SampleType) in Visual Basic.
    /// </summary>
    TypeAs,

    /// <summary>A type test, such as obj is SampleType in C# or TypeOf obj is SampleType in Visual Basic.</summary>
    TypeIs,

    /// <summary>An assignment operation, such as (a = b).</summary>
    Assign,

    /// <summary>A block of expressions.</summary>
    Block,

    /// <summary>Debugging information.</summary>
    DebugInfo,

    /// <summary>
    ///     A unary decrement operation, such as (a - 1) in C# and Visual Basic. The object a should not be modified in
    ///     place.
    /// </summary>
    Decrement,

    /// <summary>A dynamic operation.</summary>
    Dynamic,

    /// <summary>A default value.</summary>
    Default,

    /// <summary>An extension expression.</summary>
    Extension,

    /// <summary>A "go to" expression, such as goto Label in C# or GoTo Label in Visual Basic.</summary>
    Goto,

    /// <summary>
    ///     A unary increment operation, such as (a + 1) in C# and Visual Basic. The object a should not be modified in
    ///     place.
    /// </summary>
    Increment,

    /// <summary>An index operation or an operation that accesses a property that takes arguments. </summary>
    Index,

    /// <summary>A label.</summary>
    Label,

    /// <summary>
    ///     A list of run-time variables. For more information, see
    ///     <see cref="T:System.Linq.Expressions.RuntimeVariablesExpression" />.
    /// </summary>
    RuntimeVariables,

    /// <summary>A loop, such as for or while.</summary>
    Loop,

    /// <summary>A switch operation, such as switch in C# or Select Case in Visual Basic.</summary>
    Switch,

    /// <summary>An operation that throws an exception, such as throw new Exception().</summary>
    Throw,

    /// <summary>A try-catch expression.</summary>
    Try,

    /// <summary>An unbox value type operation, such as unbox and unbox.any instructions in MSIL. </summary>
    Unbox,

    /// <summary>An addition compound assignment operation, such as (a += b), without overflow checking, for numeric operands.</summary>
    AddAssign,

    /// <summary>A bitwise or logical AND compound assignment operation, such as (a &amp;= b) in C#.</summary>
    AndAssign,

    /// <summary>An division compound assignment operation, such as (a /= b), for numeric operands.</summary>
    DivideAssign,

    /// <summary>A bitwise or logical XOR compound assignment operation, such as (a ^= b) in C#.</summary>
    ExclusiveOrAssign,

    /// <summary>A bitwise left-shift compound assignment, such as (a &lt;&lt;= b).</summary>
    LeftShiftAssign,

    /// <summary>An arithmetic remainder compound assignment operation, such as (a %= b) in C#.</summary>
    ModuloAssign,

    /// <summary>
    ///     A multiplication compound assignment operation, such as (a *= b), without overflow checking, for numeric
    ///     operands.
    /// </summary>
    MultiplyAssign,

    /// <summary>A bitwise or logical OR compound assignment, such as (a |= b) in C#.</summary>
    OrAssign,

    /// <summary>A compound assignment operation that raises a number to a power, such as (a ^= b) in Visual Basic.</summary>
    PowerAssign,

    /// <summary>A bitwise right-shift compound assignment operation, such as (a &gt;&gt;= b).</summary>
    RightShiftAssign,

    /// <summary>
    ///     A subtraction compound assignment operation, such as (a -= b), without overflow checking, for numeric
    ///     operands.
    /// </summary>
    SubtractAssign,

    /// <summary>An addition compound assignment operation, such as (a += b), with overflow checking, for numeric operands.</summary>
    AddAssignChecked,

    /// <summary>
    ///     A multiplication compound assignment operation, such as (a *= b), that has overflow checking, for numeric
    ///     operands.
    /// </summary>
    MultiplyAssignChecked,

    /// <summary>
    ///     A subtraction compound assignment operation, such as (a -= b), that has overflow checking, for numeric
    ///     operands.
    /// </summary>
    SubtractAssignChecked,

    /// <summary>A unary prefix increment, such as (++a). The object a should be modified in place.</summary>
    PreIncrementAssign,

    /// <summary>A unary prefix decrement, such as (--a). The object a should be modified in place.</summary>
    PreDecrementAssign,

    /// <summary>A unary postfix increment, such as (a++). The object a should be modified in place.</summary>
    PostIncrementAssign,

    /// <summary>A unary postfix decrement, such as (a--). The object a should be modified in place.</summary>
    PostDecrementAssign,

    /// <summary>An exact type test.</summary>
    TypeEqual,

    /// <summary>A ones complement operation, such as (~a) in C#.</summary>
    OnesComplement,

    /// <summary>A true condition value.</summary>
    IsTrue,

    /// <summary>A false condition value.</summary>
    IsFalse
}