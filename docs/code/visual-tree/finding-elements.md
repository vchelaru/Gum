# Finding Elements

## Introduction

Once you have a reference to a Forms control or a visual, three patterns of "find something else" come up constantly:

* **Reach into a control's visuals** — `myButton.FindVisual<TextRuntime>()` to grab the runtime visuals that make up a Forms control's appearance.
* **Find another control** — `dialog.Find<Button>("CancelButton")` to locate a descendant Forms control by type and/or name.
* **Walk up to a container** — `nested.Ancestors().OfType<Window>().FirstOrDefault()` to find the containing window from somewhere deep inside it.

These extension methods live in `Gum.Forms` (for `FrameworkElement`) and `Gum.Wireframe` (for `GraphicalUiElement`). All `Find*` methods return `null` if nothing matches — they don't throw. The examples below use `!` only because the result is known to exist for that example.

## Working With Forms Controls

Most game UIs are built from Forms controls (`Button`, `TextBox`, `ListBox`, `Window`, etc.), so most queries start from a `FrameworkElement`. The extensions group into three patterns: finding other controls, walking up to a container, and dropping down into a control's underlying visual.

### Finding Another Control

When a control contains other controls — for example a `Window` containing several `Button`s — `Find<T>` walks the descendants and returns the first match:

```csharp
// Initialize
Window dialog = new();
Button cancel = new();
cancel.Visual.Name = "CancelButton";
dialog.AddChild(cancel);

Button found = dialog.Find<Button>("CancelButton")!;
```

`FindByName(name)` matches on the underlying `Visual.Name` without a type filter. `Find<T>()` (no name) returns the first descendant of type `T`.

When you need every match instead of the first, use `Descendants()` and compose with LINQ:

```csharp
// Initialize
List<Button> buttons = dialog.Descendants().OfType<Button>().ToList();
```

`Descendants()` returns `IEnumerable<FrameworkElement>` lazily, so the LINQ pipeline can short-circuit without walking the whole tree — `Find*` and `.FirstOrDefault(...)` stop as soon as they hit a match, which keeps lookups cheap even on populated trees like a `ListBox`'s item panel.

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

### Reaching From a Visual to a Forms Control

`Find<T>` on `GraphicalUiElement` stays in the visual layer — it walks visual descendants and returns visuals. When you have a visual root (typically a screen or component) and need to reach a Forms control wrapping one of its visual descendants, use `FindFormsControl<T>` instead. It walks the visual tree, projects each node to its underlying Forms control (skipping pure visuals with no Forms wrapper), and returns the first match:

```csharp
// Initialize
TextBox nameBox = screenRoot.FindFormsControl<TextBox>("NameTextBox")!;
Button firstButton = screenRoot.FindFormsControl<Button>()!;
FrameworkElement anyControl = screenRoot.FindFormsControlByName("OkButton")!;
```

`FindFormsControl<T>()` returns the first descendant whose Forms control is assignable to `T`. `FindFormsControl<T>(name)` adds a name filter on the underlying visual. `FindFormsControlByName(name)` matches on name only, without a type filter. All three return `null` when no match is found.

This is the cross-layer counterpart to `FindVisual<T>` on `FrameworkElement` — one goes visual → Forms, the other goes Forms → visual.

{% hint style="warning" %}
`FindFormsControl<T>`, `FindFormsControl<T>(name)`, and `FindFormsControlByName(name)` are available starting in the 2026 June release.
{% endhint %}

## Ordering

`Descendants()` and the `Find*` methods enumerate **shallowest-first**, so the closest match wins. A `ListBox` has a `FocusedIndicator` near its root and another inside each `ListBoxItem`; shallowest-first returns the outer one, which is what you want.

`Ancestors()` enumerates nearest-first.

## Type Matching

All generic methods (`Find<T>`, `OfType<T>`) use `is T` semantics — they match `T` and any subclass. If you specifically need an exact-type match, add an explicit filter: `Descendants().Where(x => x.GetType() == typeof(Button)).FirstOrDefault()`.
