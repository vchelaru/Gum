# Finding Elements

## Introduction

Once you have a reference to a Forms control or a visual, you often need to grab something underneath it: a child control, a specific runtime visual, or an ancestor window. Gum provides extension methods on both `FrameworkElement` (the Forms layer) and `GraphicalUiElement` (the visual layer) for this. They live in the `Gum.Forms` and `Gum.Wireframe` namespaces respectively.

## Working With Forms Controls

Most game UIs are built from Forms controls (`Button`, `TextBox`, `ListBox`, `Window`, etc.). The Forms-side extensions on `FrameworkElement` come in two flavors: **`Find*`** to find other controls, and **`FindVisual*`** to drop down into the underlying visual.

### Finding a Visual Inside a Control

A Forms `Button` is composed of visuals — a `TextRuntime` for its label, a background, and so on. To set a property that the Forms control itself doesn't expose, use `FindVisual<T>`:

```csharp
// Initialize
Button okButton = new();
TextRuntime label = okButton.FindVisual<TextRuntime>()!;
label.Color = Microsoft.Xna.Framework.Color.LightGreen;
```

`FindVisual<T>(name)` adds a name filter, and `FindVisualByName(name)` looks up by name only. All three are sugar over the equivalent calls on `okButton.Visual`.

### Finding Another Control

When a control contains other controls — for example a `Window` containing several `Button`s — `Find<T>` walks the descendant Forms controls and returns the first match:

```csharp
// Initialize
Window dialog = new();
Button cancelButton = new();
cancelButton.Visual.Name = "CancelButton";
dialog.AddChild(cancelButton);

Button found = dialog.Find<Button>("CancelButton")!;
```

`Find<T>` matches subclasses (any `T` or anything derived from `T`). `FindByName(name)` matches on the underlying `Visual.Name`.

### Walking Up

`Ancestors()` returns the containing controls, nearest-first. Useful for asking "which window am I in?" from a deeply-nested control:

```csharp
// Initialize
Window? owningWindow = nestedButton.Ancestors().OfType<Window>().FirstOrDefault();
```

`Descendants()`, `DescendantsAndSelf()`, and `AncestorsAndSelf()` are also available for LINQ-composable traversal — for example `dialog.Descendants().OfType<Button>().ToList()` to gather every button.

## Working With Visuals Directly

If you're operating on `GraphicalUiElement` instances directly — building a screen in code, or working below the Forms layer — the same primitives exist on GUE. Use them via a `using Gum.Wireframe;` directive:

```csharp
// Initialize
ContainerRuntime root = new();
// ... populate root ...

TextRuntime? title = root.Find<TextRuntime>("TitleInstance");
List<SpriteRuntime> icons = root.Descendants().OfType<SpriteRuntime>().ToList();
```

The full set: `Find<T>`, `Find<T>(name)`, `FindByName(name)`, `Descendants`, `DescendantsAndSelf`, `Ancestors`, `AncestorsAndSelf`. Same shape, same semantics.

## Ordering and Performance

`Descendants()` and the `Find*` methods enumerate **shallowest-first**. Two consequences:

* When several descendants match, the one closest to the search root wins. A `ListBox` has a `FocusedIndicator` near its root and one inside each `ListBoxItem`; shallowest-first returns the outer one, which is what you want.
* `Find*` short-circuits as soon as it finds a match, so common lookups don't descend into deep subtrees like a populated `ListBox`'s item panel.

`Ancestors()` enumerates nearest-first.

## Type Matching

All generic methods (`Find<T>`, `OfType<T>`) use `is T` semantics — they match `T` and any subclass. If you specifically need an exact-type match, add an explicit filter: `Descendants().Where(x => x.GetType() == typeof(Button)).FirstOrDefault()`.
