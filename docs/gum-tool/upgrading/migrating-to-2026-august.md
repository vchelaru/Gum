# Migrating to 2026 August

## Introduction

This page discusses breaking changes and other considerations when migrating from `2026 July` to `2026 August`.

## What Changed at a Glance

This release renames `GraphicalUiElement`'s `GetAbsoluteWidth()` and `GetAbsoluteHeight()` methods to the `AbsoluteWidth` and `AbsoluteHeight` properties, matching the existing `AbsoluteLeft`/`AbsoluteTop`/`AbsoluteRight`/`AbsoluteBottom` properties. This is a **soft break**: the old methods still compile and work, but now emit a `CS0618` obsolete warning.

## Upgrading the Gum Tool

{% tabs %}
{% tab title="Windows" %}
To upgrade the Gum tool:

1. Download Gum.zip from the [August 2, 2026 release on GitHub](https://github.com/vchelaru/Gum/releases/tag/Release_August_02_2026)
2. Delete the old tool from your machine
3. Unzip the gum tool to the same location as to not break any file associations
{% endtab %}

{% tab title="Linux" %}
Run the upgrade `gum upgrade` or `~/bin/gum upgrade`
{% endtab %}
{% endtabs %}

## Upgrading the Runtime

This release's runtime ships as NuGet version **`2026.8.2.1`**. Upgrade your Gum NuGet packages to this version. For more information, see the NuGet packages for your particular platform:

* MonoGame - [https://www.nuget.org/packages/Gum.MonoGame/](https://www.nuget.org/packages/Gum.MonoGame/)
* KNI - [https://www.nuget.org/packages/Gum.KNI/](https://www.nuget.org/packages/Gum.KNI/)
* FNA - [https://www.nuget.org/packages/Gum.FNA/](https://www.nuget.org/packages/Gum.FNA/)
* raylib - [https://www.nuget.org/packages/Gum.raylib](https://www.nuget.org/packages/Gum.raylib)
* .NET MAUI - [https://www.nuget.org/packages/Gum.SkiaSharp.Maui](https://www.nuget.org/packages/Gum.SkiaSharp.Maui)
* SkiaSharp - [https://www.nuget.org/packages/Gum.SkiaSharp/](https://www.nuget.org/packages/Gum.SkiaSharp/)

If using GumCommon directly, you can update the GumCommon NuGet:

* GumCommon - [https://www.nuget.org/packages/FlatRedBall.GumCommon](https://www.nuget.org/packages/FlatRedBall.GumCommon)

## Breaking Changes and Migrations

### `GetAbsoluteWidth()` / `GetAbsoluteHeight()` Renamed to `AbsoluteWidth` / `AbsoluteHeight`

`GraphicalUiElement` exposes the final, computed pixel dimensions of an element (as opposed to its authored `WidthUnits`/`HeightUnits`-relative values) through two methods, `GetAbsoluteWidth()` and `GetAbsoluteHeight()`. These are now properties, `AbsoluteWidth` and `AbsoluteHeight`, matching the naming of the existing `AbsoluteLeft`, `AbsoluteTop`, `AbsoluteRight`, and `AbsoluteBottom` properties.

The old methods still compile and work, but now emit a `CS0618` warning:

```
warning CS0618: 'GraphicalUiElement.GetAbsoluteWidth()' is obsolete: 'Use AbsoluteWidth instead.'
```

To migrate, replace the method call with the property at your call sites:

❌ Old:
```csharp
var width = graphicalUiElement.GetAbsoluteWidth();
var height = graphicalUiElement.GetAbsoluteHeight();
```

✅ New:
```csharp
var width = graphicalUiElement.AbsoluteWidth;
var height = graphicalUiElement.AbsoluteHeight;
```
