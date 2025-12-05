# FrameworkElement

## Introduction

FrameworkElement is the base class for all Gum Forms controls. Gum Forms controls are a collection of controls which often are used to build up UI such as [Button](../button.md), [ListBox](../listbox.md), [Label](../label.md), and [TextBox](../textbox.md). FrameworkElement provides much of the common functionality across all controls.

The FrameworkElement class is usually not directly instantiated. Rather, derived types such as Button and TextBox are created. FrameworkElement can also be used as a base class for custom controls.

## Visual

FrameworkElements have a Visual property which is of type InteractiveGue. This Visual property is used to customize the appearance of the FrameworkElement. Common types of properties that can be modified through the Visual property include:

* Position such as `X`, `XUnits`, and `XOrigin`
* Size such as `Width`, `WidthUnits`, `MinWidth`, and `MaxWidth`
* Properties controlling how children are positioned such as `ChildrenLayout`, `AutoGridHorizontalCells`, and `StackSpacing`
* Shortcut methods for quick positioning and sizing such as `Anchor` and `Dock`

For example, to change the Width of a Button, the following code might be used.

```csharp
Button.Visual.Width = 100;
```

FrameworkElements offer a few common shortcut properties and methods for commonly-accessed properties. These shortcut methods are effectively the same thing as setting properties or calling methods on the Visual object. These properties are:

* X
* Y
* Width
* Height
* Anchor
* Dock

For example, the following two blocks of code are identical:

```
// Properties can be assigned through shortcuts...
Label.Dock(Dock.Left);
Label.X = 10;
// ...or they can be assigned directly on the Visual
Label.Visual.Dock(Dock.Left);
Label.Visual.X = 10;
```

Some properties, such as WidthUnits, must be directly assigned on the Visual object.
