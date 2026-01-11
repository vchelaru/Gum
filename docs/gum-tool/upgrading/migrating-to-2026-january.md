# Migrating to 2026 January

## Introduction

This page discusses breaking changes and other considerations when migrating from `2025 December` to `2026 January` .

## Upgrading Runtime

Upgrade your Gum NuGet packages to version 2026.1.3.1. For more information, see the NuGet packages for your particular platform:

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

For other platforms you need to build Gum from source

See below for breaking changes and updates.

## \[Breaking] Changed GraphicalUiElement.Parent Type

Previous versions of GraphicalUiElement (base class for all Visuals) included the Parent property of type `IPositionedSizedObject`. This has been replaced with a Parent property of type `GraphicalUiElement`. All Parents were of type `GraphicalUiElement` already, so this change makes it easier to work with parents, avoiding the need to cast.

```csharp
// old:
IPositionedSizedObject parent = SomeVisual.Parent;
```

This is now a GraphicalUiElement:

```csharp
// new:
GraphicalUiElement parent = SomeVisual.Parent;
```

This change results in X, Y, Width, and Height values being reported considering the object's pixel values.

This may result in code breaking if the code depended on X, Y, Width, and Height being reported in pixel values.

The following code shows a breaking change:

```csharp
StackPanel panel = new();
panel.AddToRoot();
panel.Anchor(Gum.Wireframe.Anchor.Center);

Button button = new Button();
panel.AddChild(button);

// Previously this would have returned a value
// like 300 (depending on screen size), which would
// have been the pixels from left
// Now it returns 0, which considers the XUnits and XOrigin
float parentX = button.Visual.Parent.X;
```

The old functionality can be preserved, but the calls must be explicit.&#x20;

```csharp
StackPanel panel = new();
panel.AddToRoot();
panel.Anchor(Gum.Wireframe.Anchor.Center);

Button button = new Button();
panel.AddChild(button);

// Previously this would have returned a value
// like 300 (depending on screen size), which would
// have been the pixels from left
// Now it returns 0, which considers the XUnits and XOrigin
float parentX = button.Visual.AbsoluteLeft;
```

Alternatively, the parent can also be casted to IPositionedSizedObject to get the same behavior as before:

```csharp
StackPanel panel = new();
panel.AddToRoot();
panel.Anchor(Gum.Wireframe.Anchor.Center);

Button button = new Button();
panel.AddChild(button);

// Previously this would have returned a value
// like 300 (depending on screen size), which would
// have been the pixels from left
// Now it returns 0, which considers the XUnits and XOrigin
float parentX = ((IPositionedSizedObject)button.Visual).AbsoluteLeft;
```

## \[Breaking] PolygonRuntime Default Size Change

Previously, new `PoylgonRuntime` instances created a 10x10 square polygon. The January version of Gum changes this to a 32x32 polygon which matches the dimensions of the default polygon in the Gum tool.

Users who would like to keep the default `Polygon` behavior can explicitly set the points:

```csharp
polygon.SetPoints(new List<System.Numerics.Vector2>
{
    new (0,0),
    new (10,0),
    new (10,10),
    new (0,10),
    new (0,0)
}); 
```

Practically speaking most `Polygons` are set either through the Gum tool, or their points are modified after creation so this change has a low likelihood of breaking projects.

## ContainerRuntime Alpha is now int instead of float

Previous versions of Gum runtime included a `ContainerRuntime` which has an `Alpha` value that was `float`. This caused confusion because the base class for `ContainerRuntime` also had an `Alpha` value of type int. The `float` version has been removed, so now only the `int` type remains.

## Deprecated

This section lists all deprecated types and members. Projects should migrate to the recommended types and members to avoid breaking changes in future versions.

### Update&#x20;

In MonoGame/KNI/FNA, GumService, often accessed through the GumUi property, no longer takes a Game instance as its first parameter. All versions of Update which take a Game instance are now obsolete.

This change is being made to simplify the syntax, and to unify the syntax between XNA-likes and raylib.

❌Old:

```csharp
// Update which takes a gameTime only
GumUi.Update(this, gameTime);

// Update which takes a custom GraphicalUiElement root
GumUi.Update(this, gameTime, customRoot);

// Update which takes a list of elements to update
GumUi.Update(this, gameTime, listOfElements);
```

✅New:

```csharp
// Update which takes a gameTime only
GumUi.Update(gameTime);

// Update which takes a custom GraphicalUiElement root
GumUi.Update(gameTime, customRoot);

// Update which takes a list of elements to update
GumUi.Update(gameTime, listOfElements);
```
