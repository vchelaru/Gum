# Migrating to 2025 September

## Introduction

This page discusses breaking changes when migrating from `2025 August` to `2025 September`.

## Upgrading Gum Tool

To upgrade the Gum tool:

1. Download Gum.zip from the release on Github: \
   [https://github.com/vchelaru/Gum/releases/tag/Release\_September\_27\_2025](https://github.com/vchelaru/Gum/releases/tag/Release_September_27_2025)
2. Delete the old tool from your machine
3. Unzip the gum tool to the same location as to not break any file associations

## Upgrading Runtime

Upgrade your Gum NuGet packages to version 2025.9.27.1. For more information, see the NuGet packages for your particular platform:

* MonoGame - [https://www.nuget.org/packages/Gum.MonoGame/](https://www.nuget.org/packages/Gum.MonoGame/)
* KNI - [https://www.nuget.org/packages/Gum.KNI/](https://www.nuget.org/packages/Gum.KNI/)
* FNA - [https://www.nuget.org/packages/Gum.FNA/](https://www.nuget.org/packages/Gum.FNA/)
* raylib - [https://www.nuget.org/packages/Gum.raylib](https://www.nuget.org/packages/Gum.raylib)
* SkiaSharp - [https://www.nuget.org/packages/Gum.SkiaSharp/](https://www.nuget.org/packages/Gum.SkiaSharp/)

For other platforms you need to build Gum from source

See below for breaking changes and updates.

## Removal of MonoGameGum.Forms Namespace in Generated Code

The previous release of Gum marked the `MonoGameGum.Forms` namespace as obsolete. Most of the existing classes in `MonoGameGum.Forms` continue to live in both the old namespace as well as the new `Gum.Forms` namespace. For details about which types are supported in the old namespace, see the previous release notes: [Migrating to 2025 August](migrating-to-2025-august.md#removal-of-monogamegum.forms-namespace)

This version continues the migration by removing the usage of `MonoGameGum.Forms` from generated code.&#x20;

If your project has already upgraded runtimes to August 2025 or newer, then this change will not break your project, but it will remove many warnings from generated code.

If your project has not upgraded runtimes to August 2025 or newer, then you should either:

* Upgrade your NuGet package to the latest version of Gum runtimes
* Do not upgrade the Gum tool to the latest version

## Removal of GraphicalUiElement Text/Font Properties

This version of Gum removes properties which were previously included in GraphicalUiElement. These have been migrated to TextRuntime. Most projects will not be affected by this, but be aware that these will break your project if you explicitly casted an object to GraphicalUiElement and referenced any of the following properties:

* UseCustomFont
* CustomFontFile
* Font
* FontSize
* IsItalic
* IsBold
* UseFontSmoothing
* OutlineThickness

{% hint style="info" %}
Projects targeting FlatRedBall continue to have access to these properties through GraphicalUiElement. These properties will be removed from FlatRedBall projects in the future, once FlatRedBall no longer uses code generation for its TextRuntime class.
{% endhint %}

