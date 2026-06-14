# Migrating to 2026 June

## Introduction

This page discusses breaking changes and other considerations when migrating from `2026 May` to `2026 June`.

{% hint style="warning" %}
The `2026 June` version of Gum has not yet been released. This page is a work in progress and will be updated when the release is published. In the meantime, if you want to use the changes described below, you will need to build Gum from source.
{% endhint %}

## What Changed at a Glance

`2026 June` finishes the namespace unification that began in `2026 May`. The setup/boot API and the input API move out of the platform-specific `MonoGameGum` / `RaylibGum` namespaces and into the unified `Gum` / `Gum.Input` namespaces ([syntax version 3](syntax-versions.md)). The code generator now emits the unified `using` directives, and a bundled Roslyn analyzer rewrites most call sites for you.

For most projects, upgrading is a matter of swapping a few `using` directives:

| Replace | With |
|---|---|
| `using MonoGameGum;` (for `GumService`, `WindowZoomMode`, hot-reload types) | `using Gum;` |
| `using MonoGameGum.Forms.DefaultVisuals;` (and `MonoGameGum.ExtensionMethods` / `MonoGameGum.Renderables`) | `using Gum.Forms.DefaultVisuals;` (etc.) |
| `using MonoGameGum.Input;` (for `Keyboard`, gamepad types, deadzone enums) | `using Gum.Input;` |

### Steps for most projects

1. Upgrade the Gum tool and your Gum NuGet packages (see [Upgrading the Gum Tool](#upgrading-the-gum-tool) and [Upgrading the Runtime](#upgrading-the-runtime)).
2. In your code, replace the old `MonoGameGum.*` / `RaylibGum.*` `using` directives with the unified `Gum.*` equivalents above, removing the old directive so the two don't collide.
3. Run the bundled `Gum.Analyzers` "Fix all in solution" (analyzers **GUM001** / **GUM003**) to apply most of these rewrites automatically.

{% hint style="info" %}
**The runtime classes themselves already moved in `2026 May`, not June.** `TextRuntime`, `ContainerRuntime`, `SpriteRuntime`, `NineSliceRuntime`, the shape runtimes (`CircleRuntime`, `RectangleRuntime`, etc.), and the rest of the `GueDeriving` types are now canonically in the unified `Gum.GueDeriving` namespace. The old `MonoGameGum.GueDeriving` / `SkiaGum.GueDeriving` names remain only as `[Obsolete]` shims that forward to the unified classes. If your project still has `using MonoGameGum.GueDeriving;` directives, that move and its analyzer fix are covered in [Migrating to 2026 May](migrating-to-2026-may.md) and [Syntax Version 1](syntax-version-1.md).
{% endhint %}

### Severity reference

Most changes are **soft breaks**: a permanent `[Obsolete]` shim keeps the old name compiling with a `CS0618` warning until you migrate. A few are **hard breaks** that fail to compile and need a manual (or analyzer-assisted) edit — they are called out below.

| Change | Old | New | Break type |
|---|---|---|---|
| `GumService` | `MonoGameGum` / `RaylibGum` | `Gum` | Soft — obsolete shim (`CS0618`) |
| `WindowZoomMode` and hot-reload types | `MonoGameGum` / `RaylibGum` | `Gum` | **Hard** — `CS0246` (enums/types can't be shimmed) |
| `AddToRoot` / `RemoveFromRoot` | platform extension method | instance method on `GraphicalUiElement` | None — call sites unchanged |
| `ElementSaveExtensionMethods.ToGraphicalUiElement` | `MonoGameGum` | `Gum` | Soft — obsolete forwarder (`CS0618`) |
| `IReadOnlyListExtensionMethods`, default-filled/stroked renderables, default-visual runtimes | `MonoGameGum.ExtensionMethods` / `.Renderables` / `.Forms.DefaultVisuals` | `Gum.*` | Soft — obsolete shims (`CS0618`) |
| `Keyboard`, `KeyboardStateProcessor`, `AnalogButton`, deadzone enums, `IInputReceiverKeyboardMonoGame` | `MonoGameGum.Input` | `Gum.Input` | Namespace move — add `using Gum.Input;` (GUM001) |
| Gamepad button query arguments | XNA `Buttons` | `Gum.Input.GamepadButton` | **Hard** — `CS1503` (no implicit conversion; GUM003) |

## Upgrading the Gum Tool

{% tabs %}
{% tab title="Windows" %}


To upgrade the Gum tool:

1. Download Gum.zip from the release on Github (link will be added once published)
2. Delete the old tool from your machine
3. Unzip the gum tool to the same location as to not break any file associations
{% endtab %}

{% tab title="Linux" %}
Run the upgrade `gum upgrade` or `~/bin/gum upgrade`
{% endtab %}
{% endtabs %}

## Upgrading the Runtime

The `2026.6` NuGet packages have not yet been published. Once released, upgrade your Gum NuGet packages to the new version. For more information, see the NuGet packages for your particular platform:

* MonoGame - [https://www.nuget.org/packages/Gum.MonoGame/](https://www.nuget.org/packages/Gum.MonoGame/)
* KNI - [https://www.nuget.org/packages/Gum.KNI/](https://www.nuget.org/packages/Gum.KNI/)
* FNA - [https://www.nuget.org/packages/Gum.FNA/](https://www.nuget.org/packages/Gum.FNA/)
* raylib - [https://www.nuget.org/packages/Gum.raylib](https://www.nuget.org/packages/Gum.raylib)
* .NET MAUI - [https://www.nuget.org/packages/Gum.SkiaSharp.Maui](https://www.nuget.org/packages/Gum.SkiaSharp.Maui)
* SkiaSharp - [https://www.nuget.org/packages/Gum.SkiaSharp/](https://www.nuget.org/packages/Gum.SkiaSharp/)

If using GumCommon directly, you can update the GumCommon NuGet:

* GumCommon - [https://www.nuget.org/packages/FlatRedBall.GumCommon](https://www.nuget.org/packages/FlatRedBall.GumCommon)

If using the Apos.Shapes library, update the library for your target platform:

* Gum.Shapes.MonoGame - [https://www.nuget.org/packages/Gum.Shapes.MonoGame](https://www.nuget.org/packages/Gum.Shapes.MonoGame)
* Gum.Shapes.KNI - [https://www.nuget.org/packages/Gum.Shapes.KNI](https://www.nuget.org/packages/Gum.Shapes.KNI)

For other platforms you need to build Gum from source.

## Breaking Changes and Migrations

### Setup/boot API (`GumService`, `WindowZoomMode`, hot-reload) moved to the `Gum` Namespace

`GumService` (along with `WindowZoomMode`, `GumHotReloadManager`, and related hot-reload types) has been moved from the platform-specific `MonoGameGum` / `RaylibGum` namespace to the unified `Gum` namespace ([syntax version 3](syntax-versions.md)). The code generator now emits `using Gum;` instead of `using MonoGameGum;` when the detected runtime syntax version is 3 or higher.

#### `GumService` — soft break (obsolete shim)

A permanent `[Obsolete]` subclass shim keeps `MonoGameGum.GumService` / `RaylibGum.GumService` compiling. Existing code that declares `MonoGameGum.GumService gumService = ...` or calls `MonoGameGum.GumService.Default` continues to work but will produce a `CS0618` compiler warning:

```
warning CS0618: 'GumService' is obsolete: 'Use Gum.GumService instead.'
```

`GumService.Default` still returns the shim-typed instance, so stored declarations typed against the legacy name keep compiling. To silence the warning, change:

❌ Old:
```csharp
using MonoGameGum; // or using RaylibGum;
// ...
GumService.Default.Initialize(this);
```

✅ New:
```csharp
using Gum;
// ...
GumService.Default.Initialize(this);
```

#### `WindowZoomMode` — hard break (no shim for enums)

`WindowZoomMode` is an enum, and enums cannot have subclass shims. Code that references `MonoGameGum.WindowZoomMode` (or `RaylibGum.WindowZoomMode`) will produce a `CS0246` ("type or namespace not found") error after upgrading.

❌ Old:
```csharp
using MonoGameGum;
// ...
GumService.Default.ZoomToWindow(WindowZoomMode.HeightDominant);
```

✅ New:
```csharp
using Gum;
// ...
GumService.Default.ZoomToWindow(WindowZoomMode.HeightDominant);
```

If `using Gum;` conflicts with something in your project, use the fully-qualified name: `Gum.WindowZoomMode.HeightDominant`.

#### Hot-reload types — hard break

`GumHotReloadManager` and its companion types also moved to `Gum`. Code that references them by the old namespace name will produce a compile error. Add `using Gum;` and remove the old platform-specific `using`.

### `AddToRoot` / `RemoveFromRoot` Are Now Instance Methods

`AddToRoot()` and `RemoveFromRoot()` are now instance methods on `GraphicalUiElement`, dispatching through `IGumService.Default`. No `using` directive is needed at all — call sites that previously needed `using MonoGameGum;` (or `using RaylibGum;` / `using SokolGum;`) solely for these methods can drop that `using` if it is not needed for other reasons.

The per-platform `GraphicalUiElementExtensionMethods.AddToRoot / RemoveFromRoot` extension methods that previously lived in each platform namespace have been deleted. Since instance methods always win over extension methods in C#, call sites of the form `element.AddToRoot();` continue to compile and work identically — they now resolve to the instance method rather than the extension. No call-site changes are needed.

### `MonoGameGum.ElementSaveExtensionMethods.ToGraphicalUiElement` Is Now `[Obsolete]`

A back-compat forwarder in `MonoGameGum.ElementSaveExtensionMethods` now delegates to `Gum.ElementSaveExtensionMethods.ToGraphicalUiElement` and is marked `[Obsolete]`. Existing code keeps compiling with a `CS0618` warning. To migrate, add `using Gum;` and drop `using MonoGameGum;` if it is no longer needed for other symbols.

### More Public Types Moved to the Unified `Gum` Namespace

Continuing the namespace unification, several more public types have moved from `MonoGameGum.*` namespaces to their unified `Gum.*` equivalents so you can drop `using MonoGameGum;` from more of your code. Each is a **soft break**: a permanent `[Obsolete]` shim remains in the old `MonoGameGum.*` namespace, so existing code keeps compiling and produces a `CS0618` warning that names the new namespace.

| Old namespace | New namespace | Types |
|---|---|---|
| `MonoGameGum.ExtensionMethods` | `Gum.ExtensionMethods` | `IReadOnlyListExtensionMethods` |
| `MonoGameGum.Renderables` | `Gum.Renderables` | `DefaultFilledRectangleRenderable`, `DefaultStrokedCircleRenderable`, `DefaultStrokedRectangleRenderable` |
| `MonoGameGum.Forms.DefaultVisuals` | `Gum.Forms.DefaultVisuals` | `DefaultButtonRuntime`, `DefaultCheckboxRuntime`, … `DefaultWindowRuntime` (all default-visual runtimes) |

To migrate, change the `using` directive:

❌ Old:
```csharp
using MonoGameGum.Forms.DefaultVisuals;

// Initialize
var visual = new DefaultButtonRuntime();
```

✅ New:
```csharp
using Gum.Forms.DefaultVisuals;

// Initialize
var visual = new DefaultButtonRuntime();
```

Because the moved type and its shim share a name, importing **both** the new `Gum.*` namespace and the old `MonoGameGum.*` namespace at the same time produces a `CS0104` ambiguity. Remove the old `MonoGameGum.*` `using` directive.

{% hint style="info" %}
The runtime classes these default visuals are built from — `TextRuntime`, `ContainerRuntime`, and so on — already live in `Gum.GueDeriving` (moved in `2026 May`; see the note in [What Changed at a Glance](#what-changed-at-a-glance)). For source compatibility, the child-runtime properties on a default visual (`TextInstance`, `Background`, etc.) are still **typed** as the `[Obsolete]` `MonoGameGum.GueDeriving` shim aliases, which derive from the unified classes. So if you captured those properties into shim-typed variables, that code keeps compiling unchanged.
{% endhint %}

### Gamepad and Keyboard Input Consolidated into `Gum.Input`

The MonoGame runtime now reads input into the same platform-neutral `Gum.Input` types the Raylib runtime already used. The MonoGame-specific `MonoGameGum.Input.GamePad` and `MonoGameGum.Input.AnalogStick` classes have been **removed**, and the rest of the input group — `Keyboard`, `KeyboardStateProcessor`, `AnalogButton`, the `DeadzoneType` / `DeadzoneInterpolationType` enums, and `IInputReceiverKeyboardMonoGame` — has **moved** from `MonoGameGum.Input` to `Gum.Input`. After this change, a single `using Gum.Input;` covers the whole gamepad-and-keyboard surface.

`GumService.Default.Gamepads` and `FrameworkElement.GamePadsForUiControl` now hold `Gum.Input.GamePad` instances (which implement `IGamePad`). The common path — reading a gamepad and querying it for UI navigation — keeps working; the breaking changes below affect code that passed the XNA `Buttons` enum to a gamepad, or that named the moved types by their old `MonoGameGum.Input` namespace.

#### Gamepad button queries take `Gum.Input.GamepadButton` (hard break)

`ButtonDown`, `ButtonPushed`, `ButtonReleased`, and `ButtonRepeatRate` on a gamepad now take `Gum.Input.GamepadButton` instead of the XNA `Microsoft.Xna.Framework.Input.Buttons` enum. There is no implicit conversion (the neutral type lives in `GumCommon`, which has no XNA dependency), so this is a compile error (`CS1503`) at every call site. The enum members share names and values, so the migration is a type swap.

❌ Old:

```csharp
// Update
var gamepad = GumService.Default.Gamepads[0];
if (gamepad.ButtonDown(Microsoft.Xna.Framework.Input.Buttons.A))
{
    // ...
}
```

✅ New:

```csharp
// Update
var gamepad = GumService.Default.Gamepads[0];
if (gamepad.ButtonDown(Gum.Input.GamepadButton.A))
{
    // ...
}
```

{% hint style="info" %}
**Analyzer `GUM003`** flags each `Buttons.X` argument on a gamepad query method and offers a one-click fix that rewrites it to `GamepadButton.X`, so Visual Studio can perform the swap for you.
{% endhint %}

#### Input types moved to `Gum.Input` (namespace change)

`Keyboard`, `KeyboardStateProcessor`, `AnalogButton`, `DeadzoneType`, `DeadzoneInterpolationType`, and `IInputReceiverKeyboardMonoGame` now live in `Gum.Input`. Code that referenced them through `using MonoGameGum.Input;` needs `using Gum.Input;` added. This is a **partial** move: `Cursor`, `CursorExtensions`, and `KeyCombo` stay in `MonoGameGum.Input`, so if your file uses those too, keep both `using` directives — there is no name collision between the two namespaces.

❌ Old:

```csharp
using MonoGameGum.Input;

// Class scope
Keyboard keyboard;
```

✅ New:

```csharp
using Gum.Input;

// Class scope
Keyboard keyboard;
```

{% hint style="info" %}
**Analyzer `GUM001`** flags a `using MonoGameGum.Input;` directive whose types have moved and offers a fix that **adds** `using Gum.Input;` (keeping the old `using` for `Cursor` and `KeyCombo`). The warning clears once `Gum.Input` is imported.
{% endhint %}
