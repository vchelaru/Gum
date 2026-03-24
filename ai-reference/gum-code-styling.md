# Gum Code-Only Styling Reference

This document teaches AI agents how to style Gum Forms controls in code-only MonoGame projects. Styling ranges from global color theming to per-control customization. Always start with the simplest approach (global colors) and only reach for lower-level techniques when needed.

## Global Styling with ActiveStyle (First Choice)

Set `Styling.ActiveStyle.Colors` properties **before** creating controls. All controls created afterward use these colors automatically.

```csharp
using Gum.Forms;

// In Initialize, after GumUI.Initialize(...)
Styling.ActiveStyle.Colors.Primary = Color.DarkGreen;
Styling.ActiveStyle.Colors.InputBackground = Color.Black;
Styling.ActiveStyle.Colors.TextPrimary = Color.LimeGreen;
Styling.ActiveStyle.Colors.Accent = Color.Yellow;

// Now create controls -- they pick up the new colors
var button = new Button();
```

### Available Color Properties

| Property | Controls Affected |
|----------|------------------|
| `Primary` | Button/CheckBox/ComboBox/RadioButton backgrounds, Slider thumb, TextBox caret, Window border |
| `InputBackground` | ComboBox, ListBox, Menu, PasswordBox, ScrollViewer, Slider track, Splitter, TextBox backgrounds |
| `TextPrimary` | Text on Button, CheckBox, ComboBox, Label, ListBoxItem, MenuItem, PasswordBox, RadioButton, TextBox |
| `TextMuted` | Placeholder text on PasswordBox, TextBox |
| `Accent` | ListBoxItem/MenuItem highlight background |
| `Warning` | Focus indicators on all controls |
| `SurfaceVariant` | ScrollBar track background |
| `IconDefault` | CheckBox check, RadioButton radio, ScrollBar arrows |

### Important: Styling Order Matters

ActiveStyle only affects controls created **after** the style is set. Controls created before the change keep their original style. Always set ActiveStyle colors before creating any controls.

## Per-Control Styling (Second Choice)

When you need a specific control to look different from the global style, cast its `Visual` to the control-specific Visual type and set color properties.

```csharp
var button = new Button();
stackPanel.AddChild(button);
var buttonVisual = (ButtonVisual)button.Visual;
buttonVisual.BackgroundColor = Color.Red;
buttonVisual.ForegroundColor = Color.White;
```

The Visual type name follows the pattern `{ControlName}Visual` (e.g., `ButtonVisual`, `TextBoxVisual`, `ListBoxVisual`). These types live in `Gum.Forms.DefaultVisuals.V3`.

### Common Styling Properties by Visual Type

Most visuals expose `BackgroundColor` and `FocusedIndicatorColor`. Controls with text also expose `ForegroundColor`. Some controls have additional properties:

- **TextBoxVisual / PasswordBoxVisual:** `SelectionBackgroundColor`, `PlaceholderColor`, `CaretColor`
- **CheckBoxVisual:** `CheckColor`
- **RadioButtonVisual:** `RadioColor`
- **ComboBoxVisual:** `DropdownIndicatorColor`
- **SliderVisual:** `TrackBackgroundColor`
- **ListBoxItemVisual:** `HighlightedBackgroundColor`, `SelectedBackgroundColor`

## Changing Background Shape

Controls use NineSlice backgrounds by default. You can swap the built-in background style:

```csharp
var button = new Button();
stackPanel.AddChild(button);
var visual = (ButtonVisual)button.Visual;
visual.Background.ApplyState(Styling.ActiveStyle.NineSlice.Outlined);
```

Built-in NineSlice styles: `Solid`, `Bordered`, `BracketVertical`, `BracketHorizontal`, `Tab`, `TabBordered`, `Outlined`, `OutlinedHeavy`, `Panel`, `CircleSolid`, `CircleBordered`, `CircleOutlined`, `CircleOutlinedHeavy`.

## Custom State Behavior (Advanced)

Controls change appearance in response to hover, press, focus, etc. via states. To customize this behavior, cast the Visual, clear the state, and assign a new `Apply` action:

```csharp
var button = new Button();
stackPanel.AddChild(button);
var buttonVisual = (ButtonVisual)button.Visual;

buttonVisual.States.Enabled.Clear();
buttonVisual.States.Enabled.Apply = () =>
{
    buttonVisual.Background.Color = Color.Green;
};

buttonVisual.States.Highlighted.Clear();
buttonVisual.States.Highlighted.Apply = () =>
{
    buttonVisual.Background.Color = Color.Yellow;
};

buttonVisual.States.Pushed.Clear();
buttonVisual.States.Pushed.Apply = () =>
{
    buttonVisual.Background.Color = Color.DarkBlue;
};

button.UpdateState(); // apply immediately
```

Common states across most controls: `Enabled`, `Disabled`, `Highlighted`, `Pushed`, `Focused`. CheckBox and RadioButton have combined states like `EnabledOn`, `HighlightedOff`, etc.

## Adding Visual Children (Advanced)

You can add standard visuals (from `MonoGameGum.GueDeriving`) as children of a control's Visual to add decorative elements:

```csharp
var button = new Button();
stackPanel.AddChild(button);
var buttonVisual = (ButtonVisual)button.Visual;

var indicator = new ColoredRectangleRuntime();
buttonVisual.AddChild(indicator);
indicator.Color = Color.Red;
indicator.Anchor(Anchor.Left);
indicator.X = 8;
indicator.Width = 8;
indicator.Height = 8;
```

## Common Mistakes for AI-Generated Code

1. **Don't set `.Background.Color` directly** -- it gets overwritten by state changes (hover, press). Use `BackgroundColor` on the typed Visual instead, which persists across states.
2. **Set ActiveStyle colors before creating controls.** Controls created before the style change don't pick up new colors.
3. **Don't forget to cast the Visual.** `button.Visual` is a `GraphicalUiElement` -- cast to `ButtonVisual` (or the appropriate type) to access styling properties.
4. **Call `UpdateState()` after modifying states** to see the effect immediately.
5. **State Apply lambdas replace default behavior.** If you `Clear()` a state and only set background color, you lose default text color changes for that state. Set all properties you care about in each Apply lambda.
6. **Gum tints textures by multiplying colors.** If using custom textures with `BackgroundColor`, use white/grayscale textures. For full-color textures, set `BackgroundColor = Color.White` to prevent tinting.

## Docs Reference

Full styling guide: https://docs.flatredball.com/gum/code/styling/code-only-styling
