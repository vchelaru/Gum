# Finding Elements

## Introduction

Once you have a reference to a Forms control — or, less commonly, to a visual — you often need to find something else in the same UI: a child control, a containing window, or one of the runtime visuals that make up a control's appearance. Gum provides extension methods on both `FrameworkElement` (the Forms layer) and `GraphicalUiElement` (the visual layer) for this.

## Working With Forms Controls

Most game UIs are built from Forms controls (`Button`, `TextBox`, `ListBox`, `Window`, etc.), so most of these queries start from a `FrameworkElement`. The extensions group into three patterns: finding other controls, walking up to a container, and dropping down into a control's underlying visual.

### Finding Another Control

When a control contains other controls — for example a `Window` containing several `Button`s — `Find<T>` walks the descendants and returns the first match:

```csharp
// Initialize
Button cancelButton = dialog.Find<Button>("CancelButton")!;
```

`FindByName(name)` matches on the underlying `Visual.Name` without a type filter. `Find<T>()` (no name) returns the first descendant of type `T`.

When you need every match instead of the first, use `Descendants()` and compose with LINQ:

```csharp
// Initialize
List<Button> buttons = dialog.Descendants().OfType<Button>().ToList();
```

`Descendants()` returns `IEnumerable<FrameworkElement>` lazily, so the LINQ pipeline can short-circuit (e.g. `.FirstOrDefault(...)`, `.Take(3)`) without walking the whole tree.

### Walking Up to a Containing Control

`Ancestors()` returns the containing controls, nearest-first. Useful for asking "which window am I in?" from a deeply-nested control:

```csharp
// Initialize
Window? owningWindow = nestedButton.Ancestors().OfType<Window>().FirstOrDefault();
```

`AncestorsAndSelf()` and `DescendantsAndSelf()` are available too, for cases where the search should include the starting element.

### Reaching Into a Control's Visual

A Forms `Button` is composed of visuals — a `TextRuntime` for its label, a background, and so on. To set a property that the Forms control itself doesn't expose, drop down to the visual with `FindVisual<T>`:

```csharp
// Initialize
Button okButton = new();
TextRuntime label = okButton.FindVisual<TextRuntime>()!;
label.Color = Microsoft.Xna.Framework.Color.LightGreen;
```

`FindVisual<T>(name)` adds a name filter, and `FindVisualByName(name)` looks up by name only. Each is shorthand for the equivalent call on `okButton.Visual` — the next section covers what's available there.

## Working With Visuals Directly

If you're operating on `GraphicalUiElement` instances directly — building a screen in code, or working below the Forms layer — the same method names exist on `GraphicalUiElement` itself. Add `using Gum.Wireframe;` (and `using System.Linq;` if you compose with LINQ) to bring them into scope:

```csharp
// Initialize
TextRuntime? title = root.Find<TextRuntime>("TitleInstance");
List<SpriteRuntime> icons = root.Descendants().OfType<SpriteRuntime>().ToList();
Window? containingWindow = someInnerVisual.Ancestors().OfType<Window>().FirstOrDefault();
```

The full set on `GraphicalUiElement`: `Find<T>`, `Find<T>(name)`, `FindByName(name)`, `Descendants`, `DescendantsAndSelf`, `Ancestors`, `AncestorsAndSelf`. The Forms-layer methods are thin projections built on top of these.

## Ordering and Performance

`Descendants()` and the `Find*` methods enumerate **shallowest-first**. Two consequences:

* When several descendants match, the one closest to the search root wins. A `ListBox` has a `FocusedIndicator` near its root and another inside each `ListBoxItem`; shallowest-first returns the outer one, which is what you want.
* `Find*` short-circuits as soon as it finds a match, so common lookups don't descend into deep subtrees like a populated `ListBox`'s item panel.

`Ancestors()` enumerates nearest-first.

## Type Matching

All generic methods (`Find<T>`, `OfType<T>`) use `is T` semantics — they match `T` and any subclass. If you specifically need an exact-type match, add an explicit filter: `Descendants().Where(x => x.GetType() == typeof(Button)).FirstOrDefault()`.
