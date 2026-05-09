# Migrating to 2026 May

## Introduction

This page discusses breaking changes and other considerations when migrating from `2026 April` to `2026 May`.

{% hint style="warning" %}
The `2026 May` version of Gum has not yet been released. This page is a work in progress and will be updated when the release is published. In the meantime, if you want to use the changes described below, you will need to build Gum from source.
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

The `2026.5` NuGet packages have not yet been published. Once released, upgrade your Gum NuGet packages to the new version. For more information, see the NuGet packages for your particular platform:

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

### MonoGameGum.GueDeriving and SkiaGum.GueDeriving Namespaces Obsolete

Runtime classes (`SpriteRuntime`, `TextRuntime`, `ContainerRuntime`, etc.) have been unified under the single `Gum.GueDeriving` namespace, regardless of backend. The old `MonoGameGum.GueDeriving` and `SkiaGum.GueDeriving` namespaces still exist and still expose every runtime class, but each is now a `[Obsolete]` shim that forwards to the real class in `Gum.GueDeriving`.

Existing code continues to compile, but you will see compiler warnings (`CS0618`) on each old-namespace reference, plus an analyzer warning (`GUM001`) from the bundled `Gum.Analyzers` package. To migrate, change:

```csharp
using MonoGameGum.GueDeriving;
```

to:

```csharp
using Gum.GueDeriving;
```

The `Gum.Analyzers` package ships a one-click code fix for `using` directives — place the cursor on the warning, trigger the lightbulb (Ctrl+.), and choose **Change to 'using Gum.GueDeriving'**. Use **Fix all in solution** to migrate the entire project at once.

The compatibility shims will remain in place until at least the November 2026 release. After that window, they will be marked `[Obsolete(error: true)]` in a subsequent release, breaking compilation for any code still using them.

For full details, including handling of fully-qualified references and a `RenderingLibrary` namespace-shadowing gotcha, see [Syntax Version 1](syntax-version-1.md).

### GraphicalUiElement tree-traversal methods replaced

Five recursive lookup methods on `GraphicalUiElement` are now `[Obsolete]` in favor of LINQ-friendly extension methods that compose more cleanly. Existing calls keep working, but they now produce `CS0618` compiler warnings.

The replaced methods:

* `GetChildByNameRecursively(string)` → `FindByName(string)`
* `GetChildByTypeRecursively(Type)` → `Find<T>()`
* `GetParentByNameRecursively(string)` → `Ancestors().FirstOrDefault(a => a.Name == name)`
* `GetParentByTypeRecursively(Type)` → `Ancestors().OfType<T>().FirstOrDefault()`
* `FillListWithChildrenByTypeRecursively<T>(...)` → `Descendants().OfType<T>().ToList()`

The new generic methods (`Find<T>`, `OfType<T>`) match subclasses (`is T` semantics). The old methods only matched the exact type. If your code relied on the exact-type behavior, add an explicit `GetType() == typeof(T)` filter to the LINQ pipeline.

❌Old:

```csharp
var textInstance = (TextRuntime)textBox.Visual.GetChildByNameRecursively("TextInstance")!;
```

✅New:

```csharp
TextRuntime textInstance = textBox.Visual.Find<TextRuntime>("TextInstance")!;
```

For the full set of new methods and how they compose with LINQ, see [Finding Elements](../../code/visual-tree/finding-elements.md).
