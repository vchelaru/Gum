# WPF

## Introduction

This page assumes you have an existing WPF project. This can be a new or existing project.

## Adding Source

As of November 2025 there is not a Gum NuGet package for WPF, so projects must link directly to source.

To add source, first clone the Gum repository: [https://github.com/vchelaru/Gum](https://github.com/vchelaru/Gum)

If you have already added the Gum NuGet package to your project, remove it.

Add the following projects to your solution:

* \<Gum Root>/Runtimes/SkiaGum.Wpf.csproj
* \<Gum Root>/SkiaGum/SkiaGum.csproj
* \<GumRoot>/GumCommon/GumCommon.csproj

Next, add SkiaGum.Maui as a project reference in your game project. Your project might look like this depending on the location of the Gum repository relative to your game project:

```xml
<ProjectReference Include="..\Gum\Runtimes\SkiaGum.Wpf.csproj" />
```

## Adding a ColoredCircle (Testing the Setup)

You can add `GumSKElement` instances to any page or component. GumSKElement is a view which inherits from `SKElement` so it can be used as a regular Skia canvas, but it also allows adding of Gum runtime elements. To add a `GumSKElement`:

UNDER CONSTRUCTION
