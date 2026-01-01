# Migrating to 2026 January

## Introduction

This page discusses breaking changes and other considerations when migrating from `2025 December` to `2026 January` .

## Upgrading Runtime

NuGet packages are not yet available for the January release.

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

## ContainerRuntime Alpha is now int instead of float

Previous versions of Gum runtime included a `ContainerRuntime` which has an `Alpha` value that was `float`. This caused confusion because the base class for `ContainerRuntime` also had an `Alpha` value of type int. The `float` version has been removed, so now only the `int` type remains.

