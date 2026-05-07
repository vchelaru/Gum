# Syntax Version 1: Runtime Namespace Unification

This page describes the changes introduced in **Gum syntax version 1**: a non-breaking move of runtime classes (`SpriteRuntime`, `TextRuntime`, `ContainerRuntime`, etc.) into a single unified namespace, `Gum.GueDeriving`.

## What Changed

Prior to version 1, runtime classes were split across backend-specific namespaces:

- `MonoGameGum.GueDeriving.*` (MonoGame, KNI, FNA, Apos.Shapes runtimes)
- `SkiaGum.GueDeriving.*` (Skia runtimes plus shared types like `TextRuntime`)
- `Gum.GueDeriving.*` (Raylib and Sokol)

In version 1, every runtime class moves to `Gum.GueDeriving`. The same `SpriteRuntime` is now available regardless of backend.

## Non-Breaking by Design

Existing user code is unaffected. The old `MonoGameGum.GueDeriving` and `SkiaGum.GueDeriving` namespaces still exist and still expose every runtime class — they just now contain thin `[Obsolete]` derived shims that forward to the real class in `Gum.GueDeriving`.

This means:

- `using MonoGameGum.GueDeriving;` still compiles.
- `new SpriteRuntime()` still works.
- You will see compiler warnings (`CS0618`) on each obsolete reference, plus an analyzer warning (`GUM001`) from the bundled Gum.Analyzers package.

## Auto-Migration via Roslyn Analyzer

The Gum NuGet packages ship with a Roslyn analyzer (`Gum.Analyzers`) that detects old-namespace `using` directives and offers a one-click code fix:

1. Place the cursor on the warning squiggle under `using MonoGameGum.GueDeriving;` or `using SkiaGum.GueDeriving;`.
2. Trigger the lightbulb (Ctrl+. in Visual Studio / Rider).
3. Choose **Change to 'using Gum.GueDeriving'**.
4. To migrate the entire solution at once, use **Fix all in solution**.

The analyzer rewrites `using` directives only. Fully-qualified references like `new MonoGameGum.GueDeriving.SpriteRuntime()` still raise `GUM001` but are not auto-fixed — update those by hand.

## Generated Code

When the Gum tool detects that your project references a runtime with `GumSyntaxVersion(Version = 1)` or higher, generated code emits `using Gum.GueDeriving;` and fully-qualified references like `global::Gum.GueDeriving.SpriteRuntime`. Older runtimes (version 0) continue to receive the legacy namespaces.

If auto-detection fails, you can pin a version manually by setting the `SyntaxVersion` field in `ProjectCodeSettings.codsj`. See [Syntax Versions](syntax-versions.md) for details.

## Deprecation Timeline

The compatibility shims will remain in place **until at least the November 2026 release** (six months after this change shipped in May 2026). After that window, the shim namespaces will be marked `[Obsolete(error: true)]` in a subsequent release, breaking compilation for any code still using them. The exact removal date will be announced in the corresponding monthly migration page.

## Before / After

Before:

```csharp
using MonoGameGum.GueDeriving;

public class MyScreen
{
    public void Setup()
    {
        SpriteRuntime sprite = new SpriteRuntime();
        sprite.Width = 100;
    }
}
```

After:

```csharp
using Gum.GueDeriving;

public class MyScreen
{
    public void Setup()
    {
        SpriteRuntime sprite = new SpriteRuntime();
        sprite.Width = 100;
    }
}
```

The only change is the `using` directive — the class API is unchanged.

## Troubleshooting

### `RenderingLibrary` references stop compiling after migration

If your code is declared **inside the `Gum.*` namespace tree** (for example, `namespace Gum.GueDeriving;` to add a custom runtime, or `namespace Gum.MyExtensions;` for helpers), unqualified references to the top-level `RenderingLibrary` namespace may stop resolving:

```csharp
namespace Gum.GueDeriving;

public class MyCustomRuntime : ContainerRuntime
{
    void Setup()
    {
        RenderingLibrary.Camera camera; // CS0234: 'Camera' does not exist
    }
}
```

This happens because there is also a `Gum.RenderingLibrary` namespace (a small set of blend-related helpers), and C# searches the containing namespace chain first — so when your file is inside `Gum.*`, `Gum.RenderingLibrary` shadows the top-level `RenderingLibrary`.

**Fix:** prefix the global namespace alias:

```csharp
global::RenderingLibrary.Camera camera;
```

Or move your code out of the `Gum.*` namespace tree (for example, into `MyGame.Extensions`) and import what you need with a `using` directive. That's the recommended structure for user code anyway.

Most users will not encounter this — code declared in your own namespace (`MyGame`, `Acme.Game`, etc.) and importing Gum types via `using Gum.GueDeriving;` is unaffected.
