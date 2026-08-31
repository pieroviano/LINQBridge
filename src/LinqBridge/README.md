# Net20.LinqBridge

LINQ, expression trees and `IQueryable` on .NET Framework 2.0 and 3.0.

C# 3.0 and Framework 3.5 run on the same CLR 2.0 as Framework 2.0: the compiler emits ordinary IL and
then looks for methods with the right names and signatures — it does not care which assembly they come
from. Framework 3.5 supplies them in `System.Core`; this package supplies the same types, in the same
namespaces, for targets that have no `System.Core` at all. Extension methods, lambdas, query
comprehensions, `var`, object and collection initialisers and anonymous types therefore all compile and
run on a 2.0 target.

```csharp
int[] numbers = { 5, 15, 7, 12 };

var query = from n in numbers            // query comprehension, on a net20 assembly
            where n > 10
            orderby n
            select n * 10;

// expression trees, and compiling one back to a delegate
Expression<Func<int, int>> f = x => x * 2 + 1;
int seven = f.Compile()(3);

// IQueryable, executed in memory
var q = numbers.AsQueryable().Where(n => n > 10).Select(n => n * 10);
```

## Installing

```
dotnet add package Net20.LinqBridge
```

`<LangVersion>latest</LangVersion>` is required in the consuming project: the target framework may be
2.0, but the *compiler* has to be one that knows the C# 3.0 language features.

| Target | What you get |
| --- | --- |
| net20, net30 | Full implementation, in the real BCL namespaces. |
| net35 and later, netstandard2.0 | An empty facade. The Framework's own `System.Core` provides these types, so referencing this package on a modern target changes nothing and cannot cause a `CS0433` ambiguity. |

That split is the point: a project that multi-targets `net20;net48` gets the bridge where the framework
lacks LINQ and the real thing everywhere else, with no `#if` in your code. And because everything lives
under the real namespaces, moving a project to 3.5 later is a matter of changing the target framework
and dropping the package reference — no code edits.

The assembly is strong-named. `Func<>` and `Action<>` come from the `Net35.Actions` package, which this
package depends on.

## What's in the box

| Namespace | Types |
| --- | --- |
| `System.Linq` | `Enumerable` — the complete set of Framework 3.5 standard query operators, including every numeric overload of `Sum`/`Min`/`Max`/`Average`; `Queryable`, `IQueryable`, `IQueryable<T>`, `IQueryProvider`, `IOrderedQueryable`; `IGrouping<K,T>`, `ILookup<K,T>`, `Lookup<K,T>`, `IOrderedEnumerable<T>` |
| `System.Linq.Expressions` | The expression tree node types (`Expression`, `Expression<TDelegate>`, `LambdaExpression`, `BinaryExpression`, `MethodCallExpression`, …), `ExpressionType`, `ExpressionVisitor`, and an IL-emitting compiler behind `LambdaExpression.Compile()` |
| `System.Runtime.CompilerServices` | `ExtensionAttribute` — what makes extension methods compile on 2.0 — plus `StrongBox<T>`, `IStrongBox`, `ExecutionScope` and `ReadOnlyCollectionBuilder<T>` |
| `System.Collections.Generic` | `HashSet<T>` |
| `System.ComponentModel.DataAnnotations` | `ValidationAttribute`, `RequiredAttribute`, `ValidationException` |
| `System.Threading` | `Net20Interlocked.CompareExchange<T>` |

Two things go beyond the classic LINQBridge, which was LINQ to Objects only:

- **Expression trees actually work.** `Expression<TDelegate>.Compile()` emits IL through
  `DynamicMethod`, so a compiled lambda runs at the speed of a normal delegate.
- **`IQueryable` works.** `EnumerableRewriter` rewrites a `Queryable` expression tree into the
  equivalent `Enumerable` calls, so `AsQueryable()` and any provider written against `IQueryProvider`
  execute in memory.

Behaviour is checked by a differential test suite: the same fixtures are compiled once against this
package (net30) and once against the real `System.Core` (net35), and must pass identically on both.

## Limitations

1. **LINQ to Objects only.** There is no LINQ to SQL, LINQ to XML, LINQ to Entities or PLINQ here; a
   remote provider is yours to write against the `IQueryProvider` this package supplies.
2. **`Compile()` needs `System.Reflection.Emit`.** The expression compiler emits a `DynamicMethod`, so
   it will not work on a platform without run-time code generation. Building and inspecting trees is
   unaffected.
3. **The expression node set is the 3.5 one**, plus the few 4.0 nodes `Net20.DynamicBridge` needs —
   `Assign`, `Block` and `Index`. The other 4.0 statement nodes (`Loop`, `Switch`, `TryCatch`, `Goto`)
   are not implemented.
4. **`System.ComponentModel.DataAnnotations` is a deliberate subset** — the three types above, not the
   whole 3.5 assembly.
5. **`ExpressionType`'s numeric values are a contract.** The C# compiler passes node kinds to a runtime
   binder as bare numeric constants, so members are appended, never inserted. If you fork this, do the
   same.

## See also

- [`Net20.DynamicBridge`](https://github.com/pieroviano/LINQBridge) — the C# `dynamic` keyword on
  net20/net30/net35, built on this package.
- `Net4x.AsyncBridge` — `async`/`await` on frameworks without it.

## Licence

New BSD. Copyright (c) 2007, Atif Aziz, Joseph Albahari. All rights reserved.
