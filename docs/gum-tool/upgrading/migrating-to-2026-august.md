# Migrating to 2026 August

## Introduction

This page discusses breaking changes and other considerations when migrating from `2026 July` to `2026 August`.

## What Changed at a Glance

Two changes need your attention:

* `GraphicalUiElement`'s `GetAbsoluteWidth()` and `GetAbsoluteHeight()` methods are renamed to the `AbsoluteWidth` and `AbsoluteHeight` properties, matching the existing `AbsoluteLeft`/`AbsoluteTop`/`AbsoluteRight`/`AbsoluteBottom` properties. This is a **soft break**: the old methods still compile and work, but now emit a `CS0618` obsolete warning.
* `MonoGameGum.csproj`'s iOS and Android target frameworks changed from opt-out to opt-in. This affects you only if you reference the MonoGameGum **source project** directly. If you use the `Gum.MonoGame` NuGet package, nothing changes.

## Upgrading the Gum Tool

{% tabs %}
{% tab title="Windows" %}
To upgrade the Gum tool:

1. Download Gum.zip from the [August 3, 2026 release on GitHub](https://github.com/vchelaru/Gum/releases/tag/Release_August_03_2026)
2. Delete the old tool from your machine
3. Unzip the gum tool to the same location as to not break any file associations
{% endtab %}

{% tab title="Linux" %}
Run the upgrade `gum upgrade` or `~/bin/gum upgrade`
{% endtab %}
{% endtabs %}

## Upgrading the Runtime

This release's runtime ships as NuGet version **`2026.8.3.1`**. Upgrade your Gum NuGet packages to this version. For more information, see the NuGet packages for your particular platform:

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

### MonoGameGum's iOS and Android Target Frameworks Are Now Opt-In

This section applies only if you reference `MonoGameGum.csproj` directly (source linking). If you use the `Gum.MonoGame` NuGet package, you can skip it: the published package still ships both mobile target frameworks and nothing changes for you.

`MonoGameGum.csproj` used to build its iOS and Android target frameworks by default, with `ExcludeIOS` and `ExcludeAndroid` properties available to turn them off. Those properties did not propagate through a `ProjectReference` from your own project, so on a .NET 9 or newer SDK every desktop-only source-linked build pulled in `net9.0-ios` and `net9.0-android` and failed with mobile workload errors that had no project-level fix.

The flags are now opt-in, matching `KniGum.csproj`:

| Old (opt-out)     | New (opt-in)     |
| ----------------- | ---------------- |
| `ExcludeIOS`      | `IncludeIOS`     |
| `ExcludeAndroid`  | `IncludeAndroid` |

Desktop-only builds now work with no flags at all. If you were deliberately building the mobile target frameworks from source, pass the new properties on the command line or as environment variables:

```
dotnet build MonoGameGum/MonoGameGum.csproj -p:IncludeIOS=true -p:IncludeAndroid=true
```

Setting them inside your own `.csproj` does not work. Only real global properties propagate through a `ProjectReference`.
