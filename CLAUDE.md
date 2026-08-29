# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

LINQBridge is a re-implementation of .NET Framework 3.5's `System.Core` for **CLR 2.0** targets, so C# 3.0+
language features (extension methods, lambdas, query comprehensions) can be used on Framework 2.0/3.0.
See `README.md` for the original rationale.

This fork (`pieroviano/LINQBridge`, package `Net20.LinqBridge`) goes beyond the classic LINQBridge, which only
provided LINQ to Objects: it also ships a full `System.Linq.Expressions` implementation — expression tree types
plus an IL-emitting `ExpressionCompiler` — and `IQueryable`/`Queryable`, so `Expression<TDelegate>.Compile()`
and queryable providers work on net20/net30 as well. Everything lives in the **real BCL namespaces**
(`System`, `System.Linq`, `System.Linq.Expressions`, `System.Collections.Generic`,
`System.Runtime.CompilerServices`, `System.ComponentModel.DataAnnotations`) with `<RootNamespace>System</RootNamespace>`,
so consumers switch to `System.Core` later with no code edits.

## Build and test

`dotnet build` **does not work** for this repo — net20/net30/net35 need `ResGen.exe` and the v2.0/v3.0
reference assemblies, which .NET Core MSBuild has not. Always use full MSBuild:

```bash
# path on this machine; adjust if VS lives elsewhere
MSBUILD="/c/Program Files/Microsoft Visual Studio/18/Enterprise/MSBuild/Current/Bin/MSBuild.exe"

"$MSBUILD" Net20.LinqBridge.sln -t:restore,build -v:m          # whole solution
"$MSBUILD" src/Net20.LinqBridge.csproj -t:build -v:m           # library only
"$MSBUILD" Net20.LinqBridge.sln -t:clean
```

`GeneratePackageOnBuild` is on, so every build also writes `Packages/Net20.LinqBridge.<version>.nupkg`
(+ `.snupkg`). Version is `1.6.0` (`Directory.Nuget.Props`) plus a date-derived `VersionSuffix` (`yyDDD`),
so the package filename changes daily — that is expected, not a bug.

Tests are **NUnit 2.6.4** targeting net30/net35; there is no VSTest adapter, so `dotnet test` is not usable.
Build the test project, then run the v2 console runner:

```bash
"$MSBUILD" test/LINQBridge.Tests/LINQBridge.Tests.csproj -t:restore,build -v:m
NUNIT="$HOME/.nuget/packages/nunit.runners/2.7.0/tools/nunit-console.exe"

"$NUNIT" test/LINQBridge.Tests/bin/Debug/net30/LinqBridge.Tests.dll -nologo        # all (~250 tests)
"$NUNIT" test/LINQBridge.Tests/bin/Debug/net30/LinqBridge.Tests.dll \
         -run:LinqBridge.Tests.EnumerableFixture.Where_ValidArguments -nologo      # single test
```

The runner drops `TestResult.xml` in the working directory (git-ignored).

## Test layout — the two-project trick

`test/LINQBridge.Tests` (net30) compiles every fixture against **this library**.
`test/LINQ.Tests` (net35) `<Compile Include>`s most of the *same source files* by link and compiles them against
the **real `System.Core`**. Those fixtures are therefore differential tests: they must pass identically on both,
which is how "does LINQBridge match Framework 3.5?" is answered. When editing a linked fixture, remember both
projects consume it — never use a LINQBridge-only or Framework 4.0 API in one.

| Fixture | Covers | Linked into LINQ.Tests? |
| --- | --- | --- |
| `EnumerableFixture` | LINQ to Objects operators | yes |
| `ExpressionFixture` | `Expression` factories + `Compile()` (the IL emitter) | yes |
| `QueryableFixture` | `Queryable` + EnumerableQuery/Rewriter/Executor | yes |
| `HashSetFixture` | `HashSet<T>` | yes |
| `BridgeTypesFixture` | bridge-only types (see below) | **no** |

`BridgeTypesFixture` is deliberately excluded because its subjects have no 3.5 counterpart to compare against:
`OrderedEnumerable`, `Key`/`KeyComparer`, `DelegatingComparer`, `Net20Interlocked`, the resource loaders,
`ReadOnlyCollectionBuilder` (a 4.0 type), and the DataAnnotations subset (a separate assembly in 3.5).

Current totals: 568 tests in LINQBridge.Tests, 516 in LINQ.Tests, all passing.

`test/TestResultsWiki` is a small console tool that reads NUnit XML output files and emits a wiki table
comparing runs (`TestResultsWiki.exe Bridge=a.xml Framework=b.xml`).

## Multi-targeting: source is compiled only for net20/net30

`src/Net20.LinqBridge.csproj` targets `net20;net30;net35;net40;net45;netstandard2.0`, but:

```xml
<DefaultItemExcludes Condition="'$(TargetFramework)'=='net35' or ... 'net45' or 'netstandard2.0'">
  $(DefaultItemExcludes);**\*.cs</DefaultItemExcludes>
```

For net35 and above **no C# is compiled at all** — those TFMs produce a near-empty assembly (~47 KB of
resources/metadata vs ~330 KB for net20/net30) so a consumer multi-targeting the same way gets the real
`System.Core` types on modern frameworks and the bridge only where the framework lacks them. Consequences:

- A new `.cs` file only affects net20/net30. If a net35+ build "doesn't see" your type, that is by design.
- Anything that must exist on all TFMs has to be a resource or an explicit `<Compile Include>`.
- `Action.cs` is `<Compile Remove>`d: `Func`/`Action` come from the `Net35.Actions` package reference instead.

## Code organisation

- `src/Linq/Enumerable.cs` — hand-written LINQ to Objects operators. `Enumerable.g.cs` is **generated** from
  `Enumerable.g.tt` (T4, run by Visual Studio's `TextTemplatingFileGenerator`) and holds the numeric overload
  explosion for `Sum`/`Min`/`Max`/`Average` over `int/long/float/double/decimal` and their nullable forms.
  Edit the `.tt`, not the `.g.cs`.
- `src/Linq/Queryable.cs`, `EnumerableQuery.cs`, `EnumerableRewriter.cs`, `EnumerableExecutor.cs` — the
  `IQueryable` side: `EnumerableRewriter` rewrites a `Queryable` expression tree into `Enumerable` calls so
  `AsQueryable()` executes in memory.
- `src/Linq/Expressions/` (~10.8k lines, the bulk of the repo) — expression tree node types mirroring the BCL
  layout (`Block2`…`BlockN`, `Scope1`…`ScopeN`, `TrueReadOnlyCollection`, `TypeUtils`), plus
  `ExpressionCompiler.cs` which emits IL through `DynamicMethod` + `ExecutionScope`.
- `src/Runtime/CompilerServices/` — `ExtensionAttribute` (what makes extension methods compile on 2.0),
  `ExecutionScope`, `StrongBox`, `ReadOnlyCollectionBuilder`.
- `src/Collections/Generic/HashSet.cs`, `src/ComponentModel/DataAnnotations/`, `src/Threading/Net20Interlocked.cs` —
  further 3.5-era BCL pieces backported for the same reason.
- String resources: `Core.resx` → `CoreStringResources.cs`, `Linq/Expressions/Expressions.resx` →
  `StringResources.cs`/`Strings.cs`. These `.resx` files are why full MSBuild is required.

Much of `Expressions/`, `Queryable.cs` and `EnumerableRewriter.cs` reads like decompiled BCL code
(`(Expression)Expression.Call(...)` casts, `_underscore` fields, XML docs copied from MSDN). That is deliberate —
it mirrors reference-source behaviour. Match the surrounding style rather than "cleaning it up", and keep public
signatures byte-identical to the Framework 3.5 originals; a divergence breaks the drop-in-replacement contract
that `test/LINQ.Tests` exists to police.

## Build infrastructure

- `Directory.Build.Props` imports `Directory.Nuget.Props` (version constants), enables SourceLink, symbol
  packages and `GeneratePackageOnBuild`.
- The csproj imports `NuGet.Utility.props`/`.targets` from the `Net4x.NuGetUtility` package
  (`$(NuGetPackageRoot)net4x.nugetutility/$(NuGetUtilityVersion)/build/`) — a shared convention across the
  sibling repos under `D:\CommonLibrary`. It also pulls in Obfuscar for Release. The imports are `Condition`ed
  on `Exists(...)`, so a missing package degrades quietly rather than failing.
- `NuGet.config` is a symlink to `D:\CommonLibrary\NuGet.Config`.
- The assembly is strong-named with `src/LinqBridge.snk`.
- The working tree is mid-migration: the legacy `build.cmd`/`pack.cmd`/`onefile.fsx`/`pkg/*.nuspec` toolchain and
  the old `Net30.LinqBridge.sln` are deleted but not yet committed. Don't resurrect them; packaging now happens
  through the SDK-style csproj.
