# Migrating to 2026 July

## Introduction

This page discusses breaking changes and other considerations when migrating from `2026 June` to `2026 July`.

## What Changed at a Glance

`2026 July` ships two breaking changes. First, `Gum.SkiaSharp` (the SkiaGum core rendering library) is now rendering/layout only — `GumService` has moved out to each SkiaSharp host package (`Gum.SkiaSharp.Maui`, `SkiaGum.Wpf`, or a standalone host) instead of living on `Gum.SkiaSharp` itself. This is a **hard break**: the type is removed outright with no `[Obsolete]` shim, though real-world impact is expected to be minimal since there are no known external consumers of `Gum.SkiaSharp`'s `GumService` today. Second, `2026 July` marks the V1 and V2 Gum Forms default visuals as `[Obsolete]`. This is a **soft break**: the old default-visual classes and the `DefaultVisualsVersion.V1` / `DefaultVisualsVersion.V2` enum values still compile and work, but they now emit a `CS0618` warning and are slated for removal in a future release. `DefaultVisualsVersion.V3` (equivalently `DefaultVisualsVersion.Newest`) is the supported path going forward.

## Upgrading the Gum Tool

{% tabs %}
{% tab title="Windows" %}


To upgrade the Gum tool:

1. Download Gum.zip from the [July 6, 2026 release on GitHub](https://github.com/vchelaru/Gum/releases/tag/Release_July_06_2026)
2. Delete the old tool from your machine
3. Unzip the gum tool to the same location as to not break any file associations
{% endtab %}

{% tab title="Linux" %}
Run the upgrade `gum upgrade` or `~/bin/gum upgrade`
{% endtab %}
{% endtabs %}

## Upgrading the Runtime

The `2026 July` runtime ships as NuGet version **`2026.7.6.1`**. Upgrade your Gum NuGet packages to this version. For more information, see the NuGet packages for your particular platform:

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

### `Gum.SkiaSharp` (SkiaGum) Is Now Rendering/Layout Only

`Gum.SkiaSharp` (the SkiaGum project, covering the SkiaSharp rendering and layout code) no longer contains `GumService`. `GumService` has moved out of the core rendering package and into each SkiaSharp **host** package instead — the host being whatever windowing/canvas layer actually presents the Skia surface:

* **.NET MAUI** — the `Gum.SkiaSharp.Maui` NuGet package ships its own `GumService`.
* **WPF** — the `SkiaGum.Wpf` project ships its own `GumService`.
* **A custom or bring-your-own-canvas host** (for example Silk.NET) with no dedicated Gum package — `GumService` for these hosts is currently shared **source** rather than a compiled NuGet package; see the `Runtimes/SkiaGum.Standalone` folder in the Gum repo for the file to link into your project.

`GumService` keeps the same `Gum` namespace and the same API in every host — this is not a namespace or API-shape change, only a change in which package provides the type. Existing `using Gum;` directives and `GumService.Default.Initialize(...)` call sites are unaffected once the type is available again from the right host package.

{% hint style="warning" %}
**This is a hard break, not a soft one.** Unlike the V1/V2 Forms visuals change below, `Gum.SkiaSharp`'s `GumService` is removed outright — there is no `[Obsolete]` shim. Code that referenced `GumService` through `Gum.SkiaSharp` alone fails to compile (`CS0246`) after upgrading, until you add a reference to the appropriate host package. Real-world impact is expected to be near-zero: there are no known projects outside the Gum repo that referenced `Gum.SkiaSharp`'s `GumService` directly before this release.
{% endhint %}

### V1 and V2 Forms Default Visuals Are Now `[Obsolete]`

Gum Forms controls get their appearance from a *default visual* class per control. There have been three generations of these default visuals:

* **V1** — the `Default*Runtime` classes (`DefaultButtonRuntime`, `DefaultCheckboxRuntime`, and so on). These are the original Forms visuals, built on solid-color `ColoredRectangle` backgrounds.
* **V2** — the `*Visual` classes (`ButtonVisual`, `CheckBoxVisual`, and so on). These use nine-slice textured backgrounds with centralized styling.
* **V3** — the current generation, with color-driven styling. This is the only generation wired up completely across all platforms.

The V1 (`Default*Runtime`) and V2 (`*Visual`) classes, along with the `DefaultVisualsVersion.V1` and `DefaultVisualsVersion.V2` enum values, are now marked `[Obsolete]`. They still compile and work, but each use produces a `CS0618` compiler warning:

```
warning CS0618: 'DefaultVisualsVersion.V2' is obsolete
```

They are slated for removal in a future release. To migrate, pass `DefaultVisualsVersion.V3` (or `DefaultVisualsVersion.Newest`) to `GumService.Initialize` / `FormsUtilities.InitializeDefaults`:

❌ Old:
```csharp
// Initialize
GumService.Default.Initialize(
    this,
    defaultVisualsVersion: DefaultVisualsVersion.V2);
```

✅ New:
```csharp
// Initialize
GumService.Default.Initialize(
    this,
    defaultVisualsVersion: DefaultVisualsVersion.V3);
```

{% hint style="info" %}
**Switching visual versions is not purely cosmetic.** The different generations build **different visual trees** — different child structure and different named children. Projects that reach into a control's visual tree by name, or that customize the default visuals, may need adjustments beyond swapping the enum value. This is why V1 and V2 are only deprecated, not removed: you can keep compiling against them while you migrate the surrounding code, then move to V3 once your visual-tree customizations are updated.
{% endhint %}
