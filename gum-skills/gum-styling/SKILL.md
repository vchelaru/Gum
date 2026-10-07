---
name: gum-styling
description: Restyling Gum Forms controls without a Theme — global colors/fonts, one control's look, custom states. Triggers: Styling.ActiveStyle, ButtonVisual, BackgroundColor, States.Enabled.Apply, "make the button red", Styles component.
---

# Gum Styling

Styling changes how Forms controls look, either for every control at once or for one control. For
a complete pre-built look, use a Theme instead (see **gum-theming**). The state/category model this
builds on is introduced in **gum-forms-controls**. Docs:
<https://docs.flatredball.com/gum/code/styling>.

## First question: where do your controls' visuals come from?

| Your controls come from | Restyle with |
|---|---|
| Code only (`new Button()`, no Gum project), or a Theme package | `Styling.ActiveStyle` and the casted `Visual` (below) |
| Forms components in a Gum project (`.gumx`, added through **Add Forms**) | The project's **Styles** component and each component's states, edited in the Gum tool |

**Landmine:** a Forms component in a loaded Gum project replaces the code default visual for that
control type, so `Styling.ActiveStyle` has no effect on it. If a style change "does nothing,"
check this first. To change project styles from code, see
<https://docs.flatredball.com/gum/code/styling/runtime-variable-references>.

## Every control: `Styling.ActiveStyle`

```csharp
using Gum.Forms.DefaultVisuals.V3;

// After GumUI.Initialize, BEFORE creating any control:
Styling.ActiveStyle.Colors.Primary = Color.DarkGreen;      // button/checkbox fills
Styling.ActiveStyle.Colors.InputBackground = Color.Black;  // textbox/listbox fills
Styling.ActiveStyle.Colors.TextPrimary = Color.LimeGreen;
Styling.ActiveStyle.Text.Normal.SetValue("FontSize", 28, "int");
```

`Colors` holds the named colors, `Text` holds three font states (`Normal`, `Strong`, `Emphasis`),
and `NineSlice`/`Icons`/`SpriteSheet` hold the background textures. Hover and press shades are
derived from these colors, so you don't set them separately. The full list of colors and which
controls use each one is in the docs page "Styling Using ActiveStyles."

**Landmine: set styling before creating controls.** A control reads `ActiveStyle` when it is
constructed. Controls created earlier keep the old look, and nothing refreshes them.

Changing the font family with `SetValue("Font", "Consolas", "string")` needs runtime font
generation (KernSmith) to be set up. See the docs page "Font Strategies."

## One control: cast `Visual`

Each control's `Visual` casts to `<ControlName>Visual` (`ButtonVisual`, `TextBoxVisual`, ...),
which exposes color properties:

```csharp
var button = new Button();
button.AddToRoot();
var visual = (ButtonVisual)button.Visual;
visual.BackgroundColor = Color.Red;   // hover/press shades follow automatically
```

**Landmine: use the color property, not the child's color.** `visual.Background.Color = ...` looks
like it works, but the next hover or press resets it, because the states write the child's color
from `BackgroundColor`. Only set a child directly when you also replace the states.

The background is a `NineSliceRuntime`. Swap its look with
`visual.Background.ApplyState(Styling.ActiveStyle.NineSlice.Bordered)` or a custom `Texture`. The
control's color multiplies the texture, so use white or grayscale art.

## Custom behavior on hover/press: states

```csharp
var enabled = visual.States.Enabled;
enabled.Clear();
enabled.Apply = () => visual.Background.Color = Color.Green;
// ...same for Highlighted, Pushed, Focused, Disabled...
button.UpdateState();   // show the change now
```

`Clear()` removes the default variables. Assign `Apply` with `=` so the default color logic is
replaced. A state can change any property (size, font scale), not just color. Each visual has a
different state set (CheckBox has On/Off/Indeterminate variants of each). See the docs page
"Styling Using States" for the full table.

## Replacing a control's visual for every instance

```csharp
FrameworkElement.DefaultFormsTemplates[typeof(Button)] =
    new VisualTemplate(() => new MyButtonVisual());
```

`MyButtonVisual` usually subclasses the V3 visual (`ButtonVisual`) and replaces its children. This
is how Themes are built. If you are restyling most controls this way, consider starting from a
Theme instead (**gum-theming**).

## In the Gum tool

Duplicate a Forms component (for example `ButtonStandard` → `OrangeButton`) and edit the colors in
**every** state of its category, plus the Default state the tool previews with. Shared colors and
fonts live in the **Styles** component. Docs: "Control Customization in Gum Tool."
