# Syntax Versions

## What Is a Syntax Version?

A syntax version is an integer stamped on each Gum runtime assembly (MonoGameGum, RaylibGum, SkiaGum, SilkNetGum) via the `GumSyntaxVersionAttribute`. It tells the Gum tool's code generator which namespaces and conventions to use when emitting C# code for your project.

When the Gum team makes a breaking namespace change (such as moving enums to a unified namespace), the syntax version is incremented. The code generator reads the syntax version from the runtime your project references and emits code that matches.

## How It Works

The Gum tool auto-detects the syntax version from your project:

- **NuGet users:** The tool finds your Gum PackageReference in the `.csproj`, locates the DLL in the NuGet cache, and reads the attribute.
- **Direct project reference (source linking):** The tool follows the ProjectReference path and reads the version from `AssemblyAttributes.cs`.
- **Manual override:** If auto-detection does not work for your setup, you can set the `SyntaxVersion` field in your `ProjectCodeSettings.codsj` file to an explicit number instead of `"*"` (auto-detect).

The detected version is displayed in the Code tab under "Project-Wide Code Generation."

## Version Table

| Version | Introduced | Changes |
|---|---|---|
| 0 | 2026.4.x | Baseline. Attribute system introduced. No breaking changes from prior releases. All existing namespace conventions are preserved. |
| 1 | 2026.6.x | Runtime class namespace unification (non-breaking). Runtime classes like `TextRuntime`, `SpriteRuntime`, etc. move to the unified `Gum.GueDeriving` namespace. Old namespaces (`MonoGameGum.GueDeriving`, `SkiaGum.GueDeriving`) continue to work via compatibility shims marked `[Obsolete]`. See [Syntax Version 1](syntax-version-1.md) for details. |
| 2 | 2026.6.x | Shape runtime collapse. `ColoredCircleRuntime`, `ColoredRectangleRuntime`, and `RoundedRectangleRuntime` are collapsed into `CircleRuntime` and `RectangleRuntime`, which now expose `FillColor`, `StrokeColor`, and `StrokeWidth` (and `CornerRadius` on `RectangleRuntime`). See PR #2769 / issue #2768. |
| 3 | 2026.6.x | Setup/boot API namespace unification (non-breaking). `GumService` (with its companions `WindowZoomMode` and the hot-reload types) moves from `MonoGameGum` / `RaylibGum` to the unified `Gum` namespace; generated code emits `using Gum;` instead of `using MonoGameGum;`. The legacy names continue to work via permanent `[Obsolete]` subclass shims, and `GumService.Default` still returns the legacy-named type so existing declarations keep compiling. `AddToRoot` / `RemoveFromRoot` become instance methods on `GraphicalUiElement` (no `using` needed at all). See issue #3119. |
| 4 | TBD | Layout enum namespace unification (breaking). Enums like `DimensionUnitType`, `ChildrenLayout`, `HorizontalAlignment`, etc. move to a unified namespace. The bundled Roslyn analyzer provides one-click migration. |

When a new syntax version is introduced, the corresponding monthly migration page will document the specific changes.

## What Happens When You Upgrade

If you upgrade your Gum NuGet package to a version with a higher syntax version:

1. The Gum tool auto-detects the new version.
2. Code generation uses the new namespaces.
3. Your existing hand-written code may have `using` statements that reference old namespaces. The Gum Roslyn analyzer (bundled with the NuGet package) will show warnings with one-click fixes in your IDE. Use "Fix all in solution" to update everything at once.

If you reference Gum via direct project reference, the same flow applies — the tool reads the version from the source and the analyzer runs at compile time.

## Absent Attribute (Pre-2026.4 Assemblies)

If the Gum runtime assembly does not have a `GumSyntaxVersionAttribute` (because it predates the system), the code generator treats it as pre-version-0 and emits the original namespaces. No action is needed — everything works as before.
