# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

`ConfigureAwait.Fody` is a [Fody](https://github.com/Fody/Home/) add-in. It is a post-compile IL weaver that rewrites async methods to inject `ConfigureAwait(bool)` calls, so that the continue-on-captured-context behaviour can be set once at the assembly/class/method level instead of on every `await`. Explicitly configured awaits in source are left untouched.

The weaver runs against an already-compiled assembly via Mono.Cecil; it does not see or modify C# source.

## Commands

- Build: `dotnet build`
- Test: `dotnet test` (build first, or it builds implicitly)
- Single test class: `dotnet test --filter "FullyQualifiedName~ClassWithAttributeTests"`
- Single test: `dotnet test --filter "FullyQualifiedName~ModuleWeaverTests.EnsureNoErrorsAndNoMessages"`
- CI build (AppVeyor) is `dotnet build --configuration Release` then `dotnet test --configuration Release --no-build --no-restore`; NuGets are emitted to `nugets/`.

`global.json` pins a .NET 11 preview SDK (`allowPrerelease`, `rollForward: latestFeature`). The repo targets the latest SDK on purpose because it supports .NET 11 runtime-async (see below); an older SDK will not build the `net11.0` targets.

## Projects

- **`ConfigureAwait`** — the marker-attribute package shipped to consumers. Contains only `Fody.ConfigureAwaitAttribute`. Multi-targets `net452;netstandard2.0;netstandard2.1`, strong-named (`key.snk`). This is the package users reference.
- **`ConfigureAwait.Fody`** — the weaver itself (`ModuleWeaver`). `netstandard2.0`, references `FodyHelpers`. All the real logic lives here.
- **`AssemblyToProcess`** — test fixture assembly the weaver is run against. `DisableFody=true` so it is *not* woven during its own build. Targets `net472;net10.0;net11.0`; the `net11.0` target turns on runtime-async (`EnablePreviewFeatures`, `Features=runtime-async=on`, `NET11_0` define) to exercise the new code path.
- **`Tests`** — xUnit v3 + [Verify](https://github.com/VerifyTests/Verify) snapshot tests. Targets `net472;net10.0;net11.0`.

`Directory.Build.props` applies repo-wide: `TreatWarningsAsErrors`, `ImplicitUsings`, `LangVersion=latest`, and the package `Version`.

## Weaver architecture

`ModuleWeaver` is a `partial class` split across files. `Execute()` (in `ModuleWeaver.cs`) is the entry point: read XML config → `FindRuntimeTypes()` → resolve the effective ConfigureAwait value for each type/method → rewrite → `AttributeCleaner.Run()`.

Configuration is resolved hierarchically in `CecilExtensions.GetConfigureAwaitConfig`: method attribute overrides class/struct attribute, which overrides assembly attribute, which falls back to the XML `ContinueOnCapturedContext` setting. A `null` result means "leave this method alone".

There are **two distinct weaving strategies**, picked per method by `ProcessType`:

1. **Classic state-machine async** (normal C# `async`/`await`, which the compiler lowers to an `IAsyncStateMachine`). The weaver edits the generated state-machine type, retargeting `Task`/`ValueTask` (and their `` `1 `` generic forms) so awaits go through the `Configured*Awaitable`/`Configured*Awaiter` types:
   - `ModuleWeaver.cs` — `AddAwaitConfigToAsyncMethod(TypeDefinition)`, plus `TryRedirectMethodInstruction` / `TryRedirectTypeInstruction` (redirect `GetAwaiter` calls and awaiter type references, and rewrite `AwaitUnsafeOnCompleted<TAwaiter,…>` generic arguments).
   - `ModuleWeaver_Variables.cs` — converts awaiter locals to `Configured*Awaiter`, adds a paired awaitable local, and inserts the `ConfigureAwait(bool)` call before each `GetAwaiter`.
   - `ModuleWeaver_Fields.cs` — converts awaiter fields on the state machine.

2. **Runtime-async** (.NET 11 preview feature; methods compiled with `runtime-async=on` are *not* lowered to a state machine but call `System.Runtime.CompilerServices.AsyncHelpers.Await(task)`):
   - `ModuleWeaver_CompilerServices.cs` — `AddAwaitConfigToAsyncMethod(MethodDefinition)` finds each `AsyncHelpers.Await(...)` call, inlines a `ConfigureAwait(value)` before it, and retargets the call to the `Await(Configured*Awaitable)` overload. Value-type awaitables (`ValueTask`/`ValueTask<T>`) need a temp local + `ldloca`/`call` because `ConfigureAwait` is a struct instance method; reference types use `callvirt` directly.

Supporting pieces:
- `ModuleWeaver_TypeFinder.cs` — `FindRuntimeTypes()` resolves every `Task`/`ValueTask`/`Configured*` type and `ConfigureAwait` method reference up front. `ValueTask` types are looked up with `TryFindTypeDefinition` because they are absent on older target frameworks.
- `AttributeCleaner.cs` — removes `Fody.ConfigureAwaitAttribute` from the output so it does not leak into the woven assembly.
- `CecilExtensions.cs` — attribute lookup + the config-resolution helper.

All four awaitable shapes are handled throughout: `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>`.

## Tests

`ModuleWeaverTests` weaves `AssemblyToProcess.dll` once in a static ctor (`ModuleWeaverTests.cs`) via `weaver.ExecuteTestRun(...)`. Two kinds of test then run against the woven assembly:

- **Behavioural** — `ClassWithAttributeTests`, `MethodWithAttributeTests`, `DoNotWeaveTests`, `CatchAndFinallyTests`, etc. load a woven type with `testResult.GetInstance(...)`, invoke a method under a `FlagSynchronizationContext`, and assert the flag stays `false` (i.e. the continuation did *not* resume on the captured context). Each fixture in `AssemblyToProcess/` has a matching `*Tests.cs`. `LoggingTests` asserts the weave produced no info/error messages.
- **IL snapshot** — `VerifyTest.cs` decompiles selected woven types with `Ildasm.Decompile` and snapshots the IL via Verify.

Snapshots live directly in `Tests/` and are keyed by build configuration *and* runtime: `UniqueForRuntimeAndVersion()` + `UniqueForAssemblyConfiguration()` give names like `ModuleWeaverTests.DecompileExample.Debug.DotNet11_0.verified.txt` and `....Release.Net4_7.verified.txt`. The same logical test therefore has a separate verified file per (Debug/Release × Net4_7/DotNet10_0/DotNet11_0). The `Decompile*` tests use `settings.AutoVerify()`, so received output is auto-promoted locally — review snapshot diffs in git before committing. On CI failure, `*.received.*` files are uploaded as artifacts.

`#if NET` blocks in the fixtures add the `ValueTask`/`ValueTask<T>` cases (absent on `net452`/`net472`). The `DotNet11_0` snapshots are much smaller than `DotNet10_0` because they exercise the runtime-async path (`AsyncHelpers.Await`) rather than full state-machine lowering.

When adding a behaviour: add a fixture class to `AssemblyToProcess`, a corresponding test in `Tests`, and accept the new verified snapshots for each (configuration × runtime) combination.
