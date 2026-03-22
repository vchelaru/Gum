# Gum Forms Controls Reference

This document teaches AI agents how to use Gum Forms controls to build interactive UI in code-only MonoGame projects. Forms controls are high-level widgets that handle input, state, and visual hierarchy automatically.

## Available Controls

| Control | Purpose |
|---------|---------|
| `Button` | Clickable button with text |
| `CheckBox` | Toggleable bool with text label |
| `ComboBox` | Dropdown selection from a list |
| `Image` | Displays a texture/sprite |
| `Label` | Read-only text display |
| `ListBox` | Scrollable list with selectable items |
| `Menu` / `MenuItem` | Hierarchical menus |
| `Panel` | Generic container |
| `PasswordBox` | Masked text input |
| `RadioButton` | Mutually exclusive option (grouped by parent container) |
| `ScrollBar` | Standalone scroll bar |
| `ScrollViewer` | Scrollable container for content that exceeds available space |
| `Slider` | Value selection between min/max |
| `Splitter` | Resizable split between two regions |
| `StackPanel` | Auto-stacking container (vertical by default) |
| `TextBox` | Editable text input |
| `ToggleButton` | Button that stays pressed/unpressed |

All controls live in `Gum.Forms.Controls`.

## Creating and Parenting

- Instantiate with `new Button()`, `new TextBox()`, etc. -- the constructor creates the full visual tree automatically.
- Top-level elements: call `element.AddToRoot()` (extension method from `GumService`).
- Child elements: call `parent.AddChild(child)` where `parent` is any `FrameworkElement`.
- `StackPanel` is the standard container for forms layouts.

## Common Control Patterns

**Button:**
```csharp
var button = new Button();
stackPanel.AddChild(button);
button.Text = "Click Me";
button.Click += (_, _) => { /* handle */ };
```

**TextBox:**
```csharp
var textBox = new TextBox();
stackPanel.AddChild(textBox);
textBox.Placeholder = "Enter text here...";
textBox.Width = 200;
textBox.TextChanged += (_, _) => { /* handle */ };
```

**ListBox:**
```csharp
var listBox = new ListBox();
stackPanel.AddChild(listBox);
listBox.Visual.Width = 150;
listBox.Visual.Height = 300;
for (int i = 0; i < 20; i++)
    listBox.Items.Add($"Item {i}");
listBox.SelectionChanged += (_, _) => { /* use listBox.SelectedObject, listBox.SelectedIndex */ };
```

**CheckBox:**
```csharp
var checkBox = new CheckBox();
stackPanel.AddChild(checkBox);
checkBox.Text = "Enable Option";
checkBox.Checked += (_, _) => { /* handle */ };
checkBox.Unchecked += (_, _) => { /* handle */ };
```

**Slider:**
```csharp
var slider = new Slider();
stackPanel.AddChild(slider);
slider.Width = 200;
slider.Minimum = 0;
slider.Maximum = 100;
slider.ValueChanged += (_, _) => { /* use slider.Value */ };
```

**RadioButton (grouped by parent):**
```csharp
var group = new StackPanel();
stackPanel.AddChild(group);
var optionA = new RadioButton();
optionA.Text = "Option A";
group.AddChild(optionA);
var optionB = new RadioButton();
optionB.Text = "Option B";
group.AddChild(optionB);
```

## Visual Property Access

- Every Forms control has a `.Visual` property (type `GraphicalUiElement`) giving access to all layout properties.
- `button.Width` and `button.Visual.Width` are equivalent -- convenience shortcuts exist for `X`, `Y`, `Width`, `Height` only.
- For `Dock` and `Anchor`, call on the control directly: `button.Dock(Dock.Fill)`.
- For layout units and origins, use `.Visual`:
  ```csharp
  button.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
  ```

## Data Binding

- Create a ViewModel class inheriting from `Gum.Mvvm.ViewModel`.
- Bind: `control.SetBinding(nameof(control.Text), nameof(viewModel.PlayerName))`.
- Set context: `control.BindingContext = viewModel`.
- `BindingContext` propagates to children -- set it on a parent and children inherit it.
- Two-way binding is default for input controls (`TextBox`, `CheckBox`, etc.).

## Common Mistakes for AI-Generated Code

1. **Do not manually construct a control's visual tree.** `new Button()` creates everything it needs.
2. **Always use `AddChild` for parenting,** not `Children.Add` or `.Parent =`.
3. **Do not forget `AddToRoot()`** on the top-level element.
4. **Use `ScrollViewer`** when content may exceed available space.
5. **RadioButtons are grouped by parent container** -- put related options in the same `StackPanel`.
6. **`Minimum`, `Maximum`, and `Value` on `Slider` are `double`,** not `float` or `int`.
7. **`ListBox.SelectionChanged` is `Action<object, SelectionChangedEventArgs>`,** not `EventHandler` -- the delegate signature differs from most other events.

## Docs Reference

Full documentation: https://docs.flatredball.com/gum/code/controls
