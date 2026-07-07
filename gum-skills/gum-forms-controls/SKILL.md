---
name: gum-forms-controls
description: Gum Forms controls (Button, Label, TextBox, CheckBox, ListBox, StackPanel…) vs raw visuals, and the state/category styling system. Triggers: adding a button/textbox/list, WPF-style properties not working, IsVisible, styling a control, DefaultVisuals.
---

# Gum Forms Controls

Forms controls are interactive UI classes whose names mirror WPF (`Button`,
`CheckBox`, `TextBox`, …), but they are laid out and rendered by **Gum**, not
WPF. Reach for a Forms control for anything the player clicks, types in, or
scrolls. For non-interactive HUD/decoration, use raw visuals instead (see
below). Docs: <https://docs.flatredball.com/gum/code/controls>.

## Common controls

`Button`, `ToggleButton`, `RadioButton`, `CheckBox`, `Label`, `TextBox`,
`PasswordBox`, `ComboBox`, `ListBox`, `Slider`, `ScrollBar`, `ScrollViewer`,
`StackPanel`, `Panel`, `Menu`, `Window`, `Splitter`.

## Using a control in code

```csharp
using Gum.Forms.Controls;

var button = new Button();
button.Text = "Play";
button.AddToRoot();               // adds it to the screen
button.Click += (_, _) => StartGame();
```

Text-bearing controls share this shape — `new Label()`, `new TextBox()`, etc.
all expose `.Text`. Set position/size with Gum's unit system (see
**gum-layout**), not WPF layout.

## Composing controls (parenting in code)

`AddToRoot()` attaches a control to the screen. To nest one control inside
another — buttons inside a panel, anything inside a container — call `AddChild`
on the parent. There is **no** `.Children.Add(...)` on a Forms control.

```csharp
var menu = new StackPanel();   // stacks its children; vertical by default
menu.Spacing = 8;              // gap between children (StackPanel.Spacing)
menu.AddToRoot();

var play = new Button { Text = "Play" };
var quit = new Button { Text = "Quit" };
menu.AddChild(play);           // parents the control under the panel
menu.AddChild(quit);
```

`AddChild(child)` sets the child's visual parent; the parent's layout then
positions it — a `StackPanel` stacks them, and any container with
`ChildrenLayout` set arranges them. `RemoveChild` undoes it. `StackPanel`
defaults to `Orientation.Vertical`; set `Orientation = Orientation.Horizontal`
for a row.

## Forms vs raw visuals

| Need | Use |
|------|-----|
| Player interaction (click/type/scroll) | A Forms control |
| Static text | `TextRuntime` |
| Solid color block | `ColoredRectangleRuntime` |
| Image | `SpriteRuntime` |
| Scalable panel/border | `NineSliceRuntime` |
| Plain layout container | `ContainerRuntime` |

Raw visuals: <https://docs.flatredball.com/gum/code/standard-visuals>.

## Landmine: WPF conventions do not carry over

The names match WPF; the property model does not. On a Forms control:

- **No** `Margin`, `Padding`, or WPF-style `HorizontalAlignment`/`VerticalAlignment`.
  Position and size come from Gum units (`X`, `XUnits`, `Width`, `WidthUnits`, …).
- **No** `Background`, `Foreground`, or `BorderBrush`. Color and appearance come
  from **states** (below), not brush properties.
- **No** `Visibility` enum. Use the `IsVisible` bool.
- **No** WPF `Auto` sizing. A `Button` with `Width = 128` is 128 px wide
  regardless of its text unless `WidthUnits = RelativeToChildren`. See
  **gum-layout**.

For a fuller WPF-to-Gum translation:
<https://docs.flatredball.com/gum/code/about/for-wpf-users>.

## The state / category system (how styling works)

Appearance is driven by named **states** grouped into a **category** on the
control's visual, not by per-property setters. A `Button`'s visual, for example,
carries a category with `Enabled`, `Highlighted`, `Pushed`, `Focused`, and
`Disabled` states; the control switches between them automatically as the player
interacts. Each state sets a batch of variables (colors, sizes) at once.

To recognize this in code: applying a state name changes the look; the control
does this for you on hover/press/focus. To *customize* appearance you edit the
states (in the tool, or via the styling APIs) or swap in a different visual —
you do **not** look for a `Color` property on the control. Don't panic when you
see categories and states; they are Gum's equivalent of a style sheet. Styling
guide: <https://docs.flatredball.com/gum/code/styling>.

## Default visuals

A newly-constructed control (`new Button()`) gets a built-in default visual so
it renders without any project. Replacing or restyling that visual is the
styling story above. If a control looks unstyled or blank, its visual — not the
control class — is what to inspect.
