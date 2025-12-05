# Migrating to 2025 July 28

## Introduction

This page discusses breaking changes when migrating from `2025 June 27` to `2025 July 28`.

## Upgrading Gum Tool

To upgrade the Gum tool:

1. Download Gum.zip from the release on Github: [https://github.com/vchelaru/Gum/releases/tag/Release\_July\_28\_2025](https://github.com/vchelaru/Gum/releases/tag/Release_July_28_2025)
2. Delete the old tool from your machine
3. Unzip the gum tool to the same location as to not break any file associations

## Upgrading Runtime

Upgrade your Gum NuGet packages to version 2025.7.27.1. For more information, see the NuGet packages for your particular platform:

* MonoGame - [https://www.nuget.org/packages/Gum.MonoGame/](https://www.nuget.org/packages/Gum.MonoGame/)
* KNI - [https://www.nuget.org/packages/Gum.KNI/](https://www.nuget.org/packages/Gum.KNI/)
* FNA - [https://www.nuget.org/packages/Gum.FNA/](https://www.nuget.org/packages/Gum.FNA/)
* raylib - [https://www.nuget.org/packages/Gum.raylib](https://www.nuget.org/packages/Gum.raylib)
* SkiaSharp - [https://www.nuget.org/packages/Gum.SkiaSharp/](https://www.nuget.org/packages/Gum.SkiaSharp/)

For other platforms you need to build Gum from source

See below for breaking changes and updates.

## Removal of ToolsUtilitiesStandard and GumDataTypesNet6 Projects

This version of Gum removes usage of the following two libraries/NuGet packages:

* ToolsUtilitiesStandard
* GumDataTypesNet6

All code from these libraries has been migrated into GumCommon. Whether you need to adjust to this change depends on how you have your project set up. The sections below provide steps for each project type.

This change provides the following benefits:

* Projects linking to Gum now only need to link (usually) two projects instead of four.
* NuGet packages now have fewer dependencies, which can make it easier to manually track referenced .dlls
* Adding new platforms and working with the raw Gum layout without platform-specific libraries now requires linking a single .csproj file instead of three
* Future changes to the location of .cs files now has a reduced chance of breaking projects
* Removes projects with confusing names

### ✅ Projects Linking NuGet Packages

If your game links the default package for your platform (such as MonoGame or SkiaGum), then you do not need to make any changes to your project. The platform-specific project continues to exist just like before.

### ✅ FlatRedBall Projects

FlatRedBall projects do not need to react to this change in any way, although this change does enable moving classes around which may affect FlatRedBall in the future.

### ❌ Platform-Specific Projects Linking Source

If your game references a platform-specific library such as MonoGame, Kni, FNA, or Skia, then your project should remove the GumDataTypesNet6 and ToolsUtilitiesStandard projects from the .sln, and remove any references to these projects from your game project.

If you do not do this, you may get error messages indicating that a class is defined in two separate projects causing ambiguity.

For more information, review the document for [adding source to your game project](/broken/pages/brEbOftKY9q9zolHNbMF).
