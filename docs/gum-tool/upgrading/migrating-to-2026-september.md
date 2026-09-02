# Migrating to 2026 September

## Introduction

This page discusses breaking changes and other considerations when migrating from `2026 August` to `2026 September`.

## What Changed at a Glance

Two changes need your attention:

* The V1 and V2 default Forms visuals are removed. This is a **hard break**: `DefaultVisualsVersion.V1`/`.V2` and their backing classes no longer exist. This only affects you if you explicitly requested V1 or V2; everyone else was already on V3.
* `Gum.SkiaSharp` now compiles `GumService` (and its new `GumServiceSkiaBase`) directly instead of requiring host projects to file-link `GumService.cs`. This affects you only if you previously file-linked `Runtimes/SkiaGum.Standalone/GumService.cs` into your own project.

## Upgrading the Gum Tool

{% tabs %}
{% tab title="Windows" %}
To upgrade the Gum tool:

1. Download Gum.zip from the [September 2, 2026 release on GitHub](https://github.com/vchelaru/Gum/releases/tag/Release_September_02_2026)
2. Delete the old tool from your machine
3. Unzip the gum tool to the same location as to not break any file associations
{% endtab %}

{% tab title="Linux" %}
Run the upgrade `gum upgrade` or `~/bin/gum upgrade`
{% endtab %}
{% endtabs %}

## Upgrading the Runtime

This release's runtime ships as NuGet version **`PLACEHOLDER!!!! NuGet version`**. Upgrade your Gum NuGet packages to this version. For more information, see the NuGet packages for your particular platform:

* MonoGame - [https://www.nuget.org/packages/Gum.MonoGame/](https://www.nuget.org/packages/Gum.MonoGame/)
* KNI - [https://www.nuget.org/packages/Gum.KNI/](https://www.nuget.org/packages/Gum.KNI/)
* FNA - [https://www.nuget.org/packages/Gum.FNA/](https://www.nuget.org/packages/Gum.FNA/)
* raylib - [https://www.nuget.org/packages/Gum.raylib](https://www.nuget.org/packages/Gum.raylib)
* .NET MAUI - [https://www.nuget.org/packages/Gum.SkiaSharp.Maui](https://www.nuget.org/packages/Gum.SkiaSharp.Maui)
* SkiaSharp - [https://www.nuget.org/packages/Gum.SkiaSharp/](https://www.nuget.org/packages/Gum.SkiaSharp/)

If using GumCommon directly, you can update the GumCommon NuGet:

* GumCommon - [https://www.nuget.org/packages/FlatRedBall.GumCommon](https://www.nuget.org/packages/FlatRedBall.GumCommon)

## Breaking Changes and Migrations

### V1 and V2 Default Forms Visuals Are Removed

The 2026 July release (see [Migrating to 2026 July](migrating-to-2026-july.md)) marked the V1 (`Default*Runtime`) and V2 (non-V3 `*Visual`) Forms default visuals `[Obsolete]`. This release removes them outright: `DefaultVisualsVersion.V1`/`.V2` and their backing classes no longer exist. Passing either value, or referencing a V1/V2 class directly, is now a compile error rather than a `CS0618` warning.

This only affects you if you explicitly passed `DefaultVisualsVersion.V1` or `.V2` to `GumService.Initialize` / `FormsUtilities.InitializeDefaults`, or referenced a V1/V2 visual class (e.g. `DefaultButtonRuntime`, the non-V3 `ButtonVisual`) directly, for example to build a custom visual around one. Every backend already defaulted to V3 in practice, so most projects need no changes.

To migrate, pass `DefaultVisualsVersion.V3` (or `.Newest`):

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

If you subclassed a V1/V2 visual (e.g. `internal class MyButton : DefaultButtonRuntime`), switch to its V3 equivalent under `Gum.Forms.DefaultVisuals.V3` (e.g. `Gum.Forms.DefaultVisuals.V3.ButtonVisual`). V3's visual tree and state system are not identical to V1/V2's: V3 drives per-state appearance through a `StateSave.Apply` lambda rather than a `Variables` list, so a subclass that customized state behavior against V1/V2 internals needs more than a type swap.

{% hint style="info" %}
`FormsUtilities.InitializeDefaults`'s XNA-only `Game` overload also dropped its unused `game` parameter as part of this change. This only affects direct callers of `FormsUtilities.InitializeDefaults` that passed `game`; `GumService.Initialize` callers are unaffected.
{% endhint %}

### `GumService` Is Now Compiled Directly Into `Gum.SkiaSharp`

The render-only `Gum.GumService` used by WPF, MAUI, and any bring-your-own-`SKCanvas` project (Silk.NET has its own, separate `GumService` and is unaffected) used to live as shared **source** (`Runtimes/SkiaGum.Standalone/GumService.cs`), file-linked into each host project. It is now a real, compiled member of the `Gum.SkiaSharp` package (`SkiaGum.csproj`), reached through the `ProjectReference`/NuGet reference every host already has. The public API is unchanged: `GumService.Default.Initialize(canvas, ...)`, `.Update(...)`, `.Draw()`, and `.HandleResize(...)` all work exactly as before.

If you never file-linked `GumService.cs` yourself, this affects you only through a real behavior fix: `GumService.Initialize` now calls `FormsUtilities.InitializeDefaults()` unconditionally, matching every other backend. Previously, a code-only Forms control (`Button`, etc.) got no default `Visual` on this render-only path unless a `.gumx` project happened to be loaded; now it does.

If you did file-link `Runtimes/SkiaGum.Standalone/GumService.cs` into your own project (for example, following an older version of the [SkiaSharp (General Canvas)](../../code/getting-started/setup/adding-initializing-gum/skiasharp-general-canvas.md) setup, or mirroring how `SkiaGum.Wpf`/`SkiaGum.Maui` used to do it), you'll get a duplicate-type build error once you upgrade to a `Gum.SkiaSharp` version that includes the type natively:

```
error CS0433: The type 'GumService' exists in both 'YourProject' and 'Gum.SkiaSharp'
```

To fix it, delete the file-link from your `.csproj`:

❌ Old:
```xml
<ItemGroup>
  <Compile Include="..\SkiaGum.Standalone\GumService.cs" Link="GumService.cs" />
</ItemGroup>
```

✅ New: delete the `ItemGroup` above entirely. `GumService` now comes from your existing `Gum.SkiaSharp` reference.

`Runtimes/SkiaGum.Standalone/` itself is removed from the Gum repository; there is no longer a file to link.
