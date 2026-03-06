## \[Breaking] BindableGue Deprecated - Binding Moved to GraphicalUiElement

Previous versions of Gum provided binding support (data context, `SetBinding`, `BindingContext`) through a separate class called `BindableGue`, which sat between `GraphicalUiElement` and `InteractiveGue` in the inheritance hierarchy. This functionality has been moved directly into `GraphicalUiElement`, making binding available on all Gum runtime objects without requiring a specific base class.

The `BindableGue` class still exists and compiles, but it is now marked `[Obsolete]` and is simply a subclass of `GraphicalUiElement` with no additional functionality. It will be removed in a future version.

The following changes may be required depending on how your project uses `BindableGue`.

### How to Update

Most projects will see compiler warnings on any code that references `BindableGue` by name. The fix in all cases is to replace `BindableGue` with `GraphicalUiElement`.

#### Custom classes that inherit BindableGue

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

#### Variables typed as BindableGue

If you store runtime references in variables typed as `BindableGue`, change the type to `GraphicalUiElement`.

❌ Old:

```csharp
BindableGue currentScreen;
```

✅ New:

```csharp
GraphicalUiElement currentScreen;
```

#### SkiaGum Children collection and related methods

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

### Codegen Output

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
