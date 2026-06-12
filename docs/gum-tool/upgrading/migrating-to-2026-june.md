# Migrating to 2026 June

## Introduction

This page discusses breaking changes and other considerations when migrating from `2026 May` to `2026 June`.

{% hint style="warning" %}
The `2026 June` version of Gum has not yet been released. This page is a work in progress and will be updated when the release is published. In the meantime, if you want to use the changes described below, you will need to build Gum from source.
{% endhint %}

## Upgrading Gum Tool

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

## Upgrading Runtime

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

See below for breaking changes and updates.

### `GumService`, `WindowZoomMode`, and Hot-Reload Types Moved to `Gum` Namespace

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
