# Migrating to 2025 October

## Introduction

This page discusses breaking changes when migrating from `2025 September` to `2025 October`.

## Upgrading Gum Tool

To upgrade the Gum tool:

1. Download Gum.zip from the release on Github: [https://github.com/vchelaru/Gum/releases/tag/Release\_October\_31\_2025](https://github.com/vchelaru/Gum/releases/tag/Release_October_31_2025)
2. Delete the old tool from your machine
3. Unzip the gum tool to the same location as to not break any file associations

## Upgrading Runtime

Upgrade your Gum NuGet packages to version 2025.10.31.1. For more information, see the NuGet packages for your particular platform:

* MonoGame - [https://www.nuget.org/packages/Gum.MonoGame/](https://www.nuget.org/packages/Gum.MonoGame/)
* KNI - [https://www.nuget.org/packages/Gum.KNI/](https://www.nuget.org/packages/Gum.KNI/)
* FNA - [https://www.nuget.org/packages/Gum.FNA/](https://www.nuget.org/packages/Gum.FNA/)
* raylib - [https://www.nuget.org/packages/Gum.raylib](https://www.nuget.org/packages/Gum.raylib)
* SkiaSharp - [https://www.nuget.org/packages/Gum.SkiaSharp/](https://www.nuget.org/packages/Gum.SkiaSharp/)

For other platforms you need to build Gum from source

See below for breaking changes and updates.

## Animation Interpolation of True/False (bool) Values

Previously when an animation interpolated from one true/false value to another, the second keyframe would immediately apply its value when the interpolation started. Now, the value is only applied when the animation has completely interpolated.

If the interpolation is immediate, then the second keyframe still applies its values immediately.

## MonoGameGum.Forms Namespace Error

Previously the `MonoGameGum.Forms` namespace was marked as obsolete, allowing projects to migrate to the new `Gum.Forms` namespace. This version makes all of the obsolete classes report compile errors. These errors indicate which class should be used but the easiest fix is to replace namespaces as shown in the following block of code:

```diff
-using MonoGameGum.Forms;
+using Gum.Forms;
-using MonoGameGum.Forms.Controls;
+using Gum.Forms.Controls;
-using MonoGameGum.Forms.Controls.Primitives;
+using Gum.Forms.Controls.Primitives;
```

## raylib Texture Nullable Types

Previously SpriteRuntime and NineSliceRuntime properties were of type `Texture` (non-nullable). This version changes these to be nullable, bringing the syntax aligned with MonoGame Gum.
