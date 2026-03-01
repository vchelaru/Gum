---
name: gum-forms-controls
description: Reference guide for Forms controls — classes inheriting from FrameworkElement. Load this when working on Button, CheckBox, ListBox, ComboBox, TextBox, ScrollViewer, or any class in Gum.Forms.Controls (or FlatRedBall.Forms.Controls). Also load when working on FrameworkElement itself, the Visual/InteractiveGue relationship, state machines, DefaultVisuals, or ReactToVisualChanged.
---

# Gum Forms Controls Reference

## What They Are

Forms controls are classes inheriting from `FrameworkElement` (`MonoGameGum/Forms/Controls/FrameworkElement.cs`). Their names and API intentionally mirror WPF (Button, CheckBox, ListBox, TextBox, etc.), but the visual and layout engine is Gum (`GraphicalUiElement`/`InteractiveGue`), not WPF.

**WPF conventions that do NOT apply here:**
- No `Margin`, `Padding`, `HorizontalAlignment`, `VerticalAlignment` (in the WPF sense) on `FrameworkElement`
- No direct `Background`, `Foreground`, or `BorderBrush` properties — visual styling is done through states
- No WPF grid/dock/wrap panel auto-layout — sizing and positioning follow Gum's unit system (`X`, `Y`, `XUnits`, `YUnits`, `Width`, `Height`, `WidthUnits`, `HeightUnits`)

## The Visual / FrameworkElement Split

Every `FrameworkElement` has a `Visual` property of type `InteractiveGue` (which is `GraphicalUiElement`). The Visual owns all layout and rendering; the `FrameworkElement` is a logical/behavioral layer on top.

- `Visual.FormsControlAsObject` — back-link from the `InteractiveGue` to its `FrameworkElement`
- `FrameworkElement.X/Y/Width/Height/etc.` — all forward to `Visual`; there is no separate logical sizing
- `ActualWidth` / `ActualHeight` — computed pixel values, read from `Visual.GetAbsoluteWidth/Height()`

## Two Construction Paths

**Forms-first** (`new Button()`): The default constructor calls `GetGraphicalUiElementFor(this)` which looks up `DefaultFormsTemplates` (or the older `DefaultFormsComponents`) to find the registered visual type, instantiates it with `createFormsInternally: false`, and assigns it as `Visual`. This path requires the type to be registered before the constructor runs.

**Visual-first** (`new ButtonVisual()`): The `DefaultVisuals` classes (in `MonoGameGum/Forms/DefaultVisuals/`) are `InteractiveGue` subclasses that construct all child runtimes, set up the state machine, and then call `FormsControlAsObject = new Button(this)` in their constructor. The two-bool constructor `(bool fullInstantiation, bool tryCreateFormsObject)` controls this — `tryCreateFormsObject: false` skips creating the Forms object, used when the visual is being instantiated by a Forms-first control.

## ReactToVisualChanged

When `Visual` is assigned, `ReactToVisualChanged()` fires. Subclasses override this to grab references to named children:

```csharp
protected override void ReactToVisualChanged()
{
    textComponent = Visual.GetGraphicalUiElementByName("TextInstance");
    coreTextObject = textComponent?.RenderableComponent as IText;
    base.ReactToVisualChanged();
}
```

Named child lookup is the standard pattern — controls depend on specific child names being present in the visual. Properties like `Button.Text` silently no-op (or throw in `FULL_DIAGNOSTICS` mode) if the expected child is absent.

## Visual States (Not WPF Styles)

Appearance changes are driven by a `StateSaveCategory` on the Visual. `UpdateState()` is called whenever interaction state changes and applies the correct state by name:

```csharp
Visual.SetProperty("ButtonCategoryState", stateName);
```

Common state names are defined as constants on `FrameworkElement` (`EnabledStateName`, `DisabledStateName`, `HighlightedStateName`, `PushedStateName`, `FocusedStateName`, etc.). The `GetState(string stateName)` method searches all categories on the Visual.

To customize appearance, either replace the Visual with a custom one that has different state variable values, or get and modify states directly via `control.GetState(...)`.

## Class Hierarchy

```
FrameworkElement
├── ButtonBase (IInputReceiver)
│   ├── Button
│   ├── ToggleButton
│   ├── RadioButton
│   └── CheckBox
├── ItemsControl (→ ScrollViewer)
│   ├── ListBox
│   └── ComboBox
├── ScrollViewer
│   └── (also base of ItemsControl)
├── TextBoxBase (IInputReceiver)
│   ├── TextBox
│   └── PasswordBox
├── Panel
│   └── StackPanel
├── Label
├── Slider (RangeBase)
├── ScrollBar (RangeBase)
├── ListBoxItem
├── MenuItem
├── UserControl
└── Splitter
```

## Key Files

| Path | Purpose |
|------|---------|
| `MonoGameGum/Forms/Controls/FrameworkElement.cs` | Base class: Visual link, layout forwarding, state constants, construction |
| `MonoGameGum/Forms/Controls/Primitives/ButtonBase.cs` | Push/click/hold input handling |
| `MonoGameGum/Forms/Controls/ItemsControl.cs` | Items collection, InnerPanel management, ListBoxItemsInternal sync |
| `MonoGameGum/Forms/DefaultVisuals/` | Pre-built `InteractiveGue` subclasses that create state machines and Forms objects |
| `MonoGameGum/Forms/Controls/FrameworkElementExt.cs` | Extension helpers (AddToRoot, etc.) |

## Non-Obvious Behaviors

**Layout is Gum layout, not WPF layout.** A `Button` with default `Width = 128` and `WidthUnits = Absolute` is 128 pixels wide regardless of content — there is no WPF-style `Auto` sizing unless `WidthUnits = RelativeToChildren`. Do not expect WPF layout rules.

**`IsVisible` vs `Visibility`**: There is no `Visibility` enum. `IsVisible` maps directly to `Visual.Visible` (bool).

**No color shortcut on the control.** To change a button's color, either modify the state's variables on the Visual, or access the child runtime directly (`(listBox.Visual as ButtonVisual)?.Background.Color = ...`). There is no `Background` property on `Button`.

**`ReactToVisualChanged` can fire before child references exist.** If `Visual` is reassigned at runtime, all `GetGraphicalUiElementByName` lookups re-run. Code that caches Visual children must refresh inside `ReactToVisualChanged`, not in the constructor.

**`ParentFrameworkElement` walks the Visual parent chain**, skipping non-Forms Gue nodes, until it finds a Gue whose `FormsControlAsObject` is a `FrameworkElement`. It does not just return the direct parent.
