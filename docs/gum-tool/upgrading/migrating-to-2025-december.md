# Migrating to 2025 December

## Introduction

This page discusses breaking changes and other considerations when migrating from `2025 November` to `2025 December` .

## Upgrading Runtime

Upgrade your Gum NuGet packages to version 2025.12.9.1. For more information, see the NuGet packages for your particular platform:

* MonoGame - [https://www.nuget.org/packages/Gum.MonoGame/](https://www.nuget.org/packages/Gum.MonoGame/)
* KNI - [https://www.nuget.org/packages/Gum.KNI/](https://www.nuget.org/packages/Gum.KNI/)
* FNA - [https://www.nuget.org/packages/Gum.FNA/](https://www.nuget.org/packages/Gum.FNA/)
* raylib - [https://www.nuget.org/packages/Gum.raylib](https://www.nuget.org/packages/Gum.raylib)
* SkiaSharp - [https://www.nuget.org/packages/Gum.SkiaSharp/](https://www.nuget.org/packages/Gum.SkiaSharp/)

For other platforms you need to build Gum from source

See below for breaking changes and updates.

## \[Breaking] Changed GraphicalUiElement Children Type

In previous versions the GraphicalUiElement (base class for all Visuals) included the following property:

```csharp
// old:
public ObservableCollection<IRenderableIpso>? Children { get; }
```

The new property has changed to:

```csharp
//new:
public ObservableCollection<GraphicalUiElement> Children { get; }
```

This change brings the following benefits:

* Children are now already casted to GraphicalUiElement, simplifying for loops and access by element.
* Children is now always non-null, simplifying code to no longer need to check for nulls before accessing Children

For example, the following blocks shows how old and new.

❌ Old:

```csharp
var visual = myControl.Visual;
if(visual.Children != null)
{
    foreach(var item in visual.Children)
    {
        if(item is GraphicalUiElement asGraphicalUiElement)
        {
            DoSomethingTo(asGraphicalUiElement);
        }
    }
}
```

✅New:

```csharp
var visual = myControl.Visual;

foreach(var item in visual.Children)
{
    DoSomethingTo(asGraphicalUiElement);
}
```

Any code that operates on the Children by taking an `ObservableCollection<IPositionedSizedObject>`  needs to update to take an `ObservableCollection<GraphicalUiElement>`.&#x20;

❌Old:

```csharp
DoSomethingTo(myControl.Visual.Children);

void DoSomethingTo(ObservableCollection<IPositionedSizedObject> children)
{
    // ...
}
```

✅New:

```csharp
DoSomethingTo(myControl.Visual.Children);

void DoSomethingTo(ObservableCollection<GraphicalUiElement> children)
{
    // ...
}
```

Alternatively if it is difficult to change calls which work on Children, you can still access the Children by casting the visual to an `IPositionedSizedObject` as shown in the following block:

```csharp
var asIpso = (IPositionedSizedObject)myControl.Visual;

// If the GraphicalUiElement is cased to an IPositionedSizedObject, then
// the Children property is still of type ObservableCollection<GraphicalUiElement>
DoSomething(asIpso.Children);

void DoSomethingTo(ObservableCollection<IPositionedSizedObject> children)
{
    // ...
}
```

## \[Breaking] Removed FrameworkElement.FrameworkElement.Activity

`FrameworkElement.Activity` method was removed from all runtimes except FlatRedBall. This method was incorrectly suggesting that it could be called every frame, but this has never been implemented in any other runtime besides FlatRedBall, so it has been removed to avoid further confusion.



