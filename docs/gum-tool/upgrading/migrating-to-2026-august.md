# Migrating to 2026 August

## Introduction

This page discusses breaking changes and other considerations when migrating from `2026 July` to `2026 August`.

## What Changed at a Glance

`2026 August` renames the explicit-font-object property on `TextRuntime` to `Typeface`, unifying a name that previously differed per backend (`BitmapFont` on MonoGame/KNI/FNA, `CustomFont` on raylib) and adding the same capability to SkiaSharp for the first time. This is a **soft break**: the old names still compile and work, but now emit a `CS0618` obsolete warning.

## Upgrading the Gum Tool

{% tabs %}
{% tab title="Windows" %}
To upgrade the Gum tool:

1. Download Gum.zip from the [PLACEHOLDER!!!! August 2026 release on GitHub](https://github.com/vchelaru/Gum/releases)
2. Delete the old tool from your machine
3. Unzip the gum tool to the same location as to not break any file associations
{% endtab %}

{% tab title="Linux" %}
Run the upgrade `gum upgrade` or `~/bin/gum upgrade`
{% endtab %}
{% endtabs %}

## Upgrading the Runtime

The `2026 August` runtime ships as NuGet version **`PLACEHOLDER!!!! version number`**. Upgrade your Gum NuGet packages to this version. For more information, see the NuGet packages for your particular platform:

* MonoGame - [https://www.nuget.org/packages/Gum.MonoGame/](https://www.nuget.org/packages/Gum.MonoGame/)
* KNI - [https://www.nuget.org/packages/Gum.KNI/](https://www.nuget.org/packages/Gum.KNI/)
* FNA - [https://www.nuget.org/packages/Gum.FNA/](https://www.nuget.org/packages/Gum.FNA/)
* raylib - [https://www.nuget.org/packages/Gum.raylib](https://www.nuget.org/packages/Gum.raylib)
* .NET MAUI - [https://www.nuget.org/packages/Gum.SkiaSharp.Maui](https://www.nuget.org/packages/Gum.SkiaSharp.Maui)
* SkiaSharp - [https://www.nuget.org/packages/Gum.SkiaSharp/](https://www.nuget.org/packages/Gum.SkiaSharp/)

If using GumCommon directly, you can update the GumCommon NuGet:

* GumCommon - [https://www.nuget.org/packages/FlatRedBall.GumCommon](https://www.nuget.org/packages/FlatRedBall.GumCommon)

## Breaking Changes and Migrations

### `BitmapFont` / `CustomFont` Renamed to `Typeface`

`TextRuntime` has an explicit-font-object property for assigning an already-loaded font directly, bypassing the normal `Font`/`FontSize` name-based resolution. This property is also what you read to inspect whichever font object is actually active, whether it was resolved normally or assigned explicitly. It previously had a different name per backend:

* **MonoGame / KNI / FNA** — `BitmapFont` (type `BitmapFont`)
* **raylib** — `CustomFont` (type `Raylib_cs.Font`)
* **SkiaSharp** — no equivalent existed at all

All three backends now expose the same property name, `Typeface`, typed to each backend's own font object (`BitmapFont`, `Raylib_cs.Font`, or `SkiaSharp.SKTypeface`). `BitmapFont` and `CustomFont` still compile and work, but now emit a `CS0618` warning:

```
warning CS0618: 'TextRuntime.BitmapFont' is obsolete: 'Use Typeface instead.'
```

To migrate, rename the property at your call sites:

❌ Old (MonoGame/KNI/FNA):
```csharp
// Initialize
textRuntime.BitmapFont = myFont;
```

❌ Old (raylib):
```csharp
// Initialize
textRuntime.CustomFont = myFont;
```

✅ New (all backends):
```csharp
// Initialize
textRuntime.Typeface = myFont;
```

The static, constructor-time default (`TextRuntime.DefaultCustomFont` on MonoGame/KNI/FNA and raylib) is renamed the same way, to `TextRuntime.DefaultTypeface`, and is now also available on SkiaSharp.

{% hint style="info" %}
On SkiaSharp, `Typeface` is a new capability, not a rename — there is no obsolete alias to migrate from, since the property didn't exist there before.
{% endhint %}
