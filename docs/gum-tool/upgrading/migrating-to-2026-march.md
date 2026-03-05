# Migrating to 2026 March

## Introduction

This page discusses breaking changes and other considerations when migrating from `2026 February` to `2026 March` .

The March version of Gum is currently only available in source code and has not been released as binary/NuGet files yet.

### \[Breaking] Blend Property is Now Nullable

The `Blend` property on runtime objects now returns `Blend?` (nullable) instead of `Blend`. Previously, if a runtime object had a custom `BlendState` assigned that did not correspond to any known `Blend` enum value, the `Blend` getter would silently return `Blend.Normal`. This was incorrect — `Blend.Normal` is a meaningful value (NonPremultiplied blending), and returning it for an unrecognized state made it impossible to distinguish between "this object uses Normal blending" and "this object has an unknown blend state."

The following runtime objects are affected:

* `SpriteRuntime`
* `TextRuntime`
* `NineSliceRuntime`
* `ColoredRectangleRuntime`
* `ContainerRuntime`

In practice, a `null` return value only occurs when a custom `BlendState` that does not map to a known `Blend` enum value has been programmatically assigned to a renderable. Under all standard usage — including the Gum tool, state application, and the built-in `BlendState` constants — the `Blend` getter will continue to return a non-null value.

#### How to Update

If your code reads the `Blend` property and assigns it to a non-nullable variable, you will receive a compiler error or warning. The recommended fix depends on how you use the value.

If you want to preserve the previous fallback behavior of defaulting to `Blend.Normal` for unknown states, use the null-coalescing operator:

❌ Old:

```csharp
Gum.RenderingLibrary.Blend blend = mySprite.Blend;
```

✅ New:

```csharp
Gum.RenderingLibrary.Blend blend = mySprite.Blend ?? Gum.RenderingLibrary.Blend.Normal;
```

If you want to handle the unknown state explicitly:

```csharp
if (mySprite.Blend is Gum.RenderingLibrary.Blend blend)
{
    // blend is a known value
}
else
{
    // BlendState does not correspond to a known Blend enum value
}
```

Code that only writes to the `Blend` property (setter) is not affected by this change.

### \[Breaking] BindableGue Deprecated - Binding Moved to GraphicalUiElement

Previous versions of Gum provided binding support (data context, `SetBinding`, `BindingContext`) through a separate class called `BindableGue`, which sat between `GraphicalUiElement` and `InteractiveGue` in the inheritance hierarchy. This functionality has been moved directly into `GraphicalUiElement`, making binding available on all Gum runtime objects without requiring a specific base class.

The `BindableGue` class still exists and compiles, but it is now marked `[Obsolete]` and is simply a subclass of `GraphicalUiElement` with no additional functionality. It will be removed in a future version.

The following changes may be required depending on how your project uses `BindableGue`.

#### How to Update

Most projects will see compiler warnings on any code that references `BindableGue` by name. The fix in all cases is to replace `BindableGue` with `GraphicalUiElement`.

**Custom classes that inherit BindableGue**

If you have custom runtime classes that inherit from `BindableGue`, change the base class to `GraphicalUiElement`.

❌ Old:

```csharp
public class MyCustomRuntime : BindableGue
{
    // ...
}
```

✅ New:

```csharp
public class MyCustomRuntime : GraphicalUiElement
{
    // ...
}
```

**Variables typed as BindableGue**

If you store runtime references in variables typed as `BindableGue`, change the type to `GraphicalUiElement`.

❌ Old:

```csharp
BindableGue currentScreen;
```

✅ New:

```csharp
GraphicalUiElement currentScreen;
```

**SkiaGum Children collection and related methods**

The `Children` collection and child management methods on `GumSKElement` (SkiaGum WPF) and `SkiaGumCanvasView` (SkiaGum MAUI) previously used `BindableGue` as their element type. These now use `GraphicalUiElement`.

❌ Old:

```csharp
ObservableCollection<BindableGue> children = myGumSKElement.Children;
mySkiaCanvas.AddChild(myBindableGue);
mySkiaCanvas.RemoveChild(myBindableGue);
```

✅ New:

```csharp
ObservableCollection<GraphicalUiElement> children = myGumSKElement.Children;
mySkiaCanvas.AddChild(myGraphicalUiElement);
mySkiaCanvas.RemoveChild(myGraphicalUiElement);
```

Since all existing `BindableGue` instances are also `GraphicalUiElement` instances, passing them to these methods continues to work without any casting.

#### Codegen Output

The Gum tool's code generation previously produced classes that inherit from `Gum.Wireframe.BindableGue`. Regenerating your runtime classes with the updated Gum tool will produce classes that inherit from `Gum.Wireframe.GraphicalUiElement` instead.

❌ Old (generated):

```csharp
public partial class MyScreenRuntime : Gum.Wireframe.BindableGue
```

✅ New (generated):

```csharp
public partial class MyScreenRuntime : Gum.Wireframe.GraphicalUiElement
```

Existing generated files that still reference `Gum.Wireframe.BindableGue` will continue to compile during the deprecation period since `BindableGue` still exists. However, regenerating the files is recommended to avoid deprecation warnings and to prepare for the eventual removal of `BindableGue`.
