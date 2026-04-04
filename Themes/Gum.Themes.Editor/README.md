# Gum.Themes.Editor

An editor-focused theme for [Gum](https://github.com/vchelaru/Gum) UI. Provides styled visuals for building tool and editor interfaces with MonoGame and Gum.

## Installation

```
dotnet add package Gum.Themes.Editor
```

Or via the NuGet Package Manager:

```
Install-Package Gum.Themes.Editor
```

## Usage

Call `EditorTheme.Apply` after initializing Gum:

```csharp
using Gum.Themes.Editor;

protected override void Initialize()
{
    GumService.Default.Initialize(this, DefaultVisualsVersion.Newest);

    EditorTheme.Apply(GraphicsDevice);

    // Now create your UI — all controls will use the editor theme
    var button = new Button();
    button.Text = "Click Me";
}
```

## Included Controls

The theme provides styled visuals for:

- **Button** — flat style with outline on hover/press
- **TextBox** — dark background with outline states
- **CheckBox** — outline on hover/press
- **ComboBox** — outline on hover/press
- **ListBox** / **ListBoxItem** — dark background with highlight and selection colors
- **ScrollBar** — dark track with styled thumb
- **Slider** — narrow thumb with adjusted track
- **Expander** — collapsible header/content control with arrow indicator
- **PropertyGridVisual** — two-column label/control grid with alternating row backgrounds

## PropertyGridVisual

A two-column layout control that displays label/control pairs in rows with alternating background colors — similar to a Unity Inspector or WPF PropertyGrid.

```csharp
var grid = new PropertyGridVisual();
grid.AddToRoot();
grid.Width = 400;
grid.WidthUnits = DimensionUnitType.Absolute;

grid.AddRow("Name", new TextBox());
grid.AddRow("Visible", new CheckBox());
grid.AddRow("Speed", new Slider());

// Works with any FrameworkElement, including complex controls
var comboBox = new ComboBox();
comboBox.Items.Add("Option A");
comboBox.Items.Add("Option B");
grid.AddRow("Mode", comboBox);
```

Each row consists of a fixed-width label on the left and the control filling the remaining space on the right. Rows automatically size to fit their content and alternate between two background colors for readability.

## Expander

A collapsible header/content control. Clicking the header toggles visibility of the content area. The `Expander` Forms control lives in `Gum.Forms.Controls`.

```csharp
var expander = new Expander();
expander.Header = "Advanced Settings";
expander.AddContent(new CheckBox());
expander.AddContent(new Slider());
```

### Properties

| Property | Type | Description |
|---|---|---|
| `Header` | `string` | The text displayed in the header row. |
| `IsExpanded` | `bool` | Gets or sets whether the content area is visible. Defaults to `false`. |

### Events

| Event | Description |
|---|---|
| `Expanded` | Raised when `IsExpanded` changes to `true`. |
| `Collapsed` | Raised when `IsExpanded` changes to `false`. |

### Programmatic control

```csharp
// Start expanded
expander.IsExpanded = true;

// React to expand/collapse
expander.Expanded += (sender, e) => Console.WriteLine("Opened");
expander.Collapsed += (sender, e) => Console.WriteLine("Closed");
```

### Inside a PropertyGridVisual

The Expander works as a row control in a PropertyGridVisual:

```csharp
var expander = new Expander();
expander.Header = "More Options";
expander.AddContent(new CheckBox());
expander.AddContent(new Slider());
grid.AddRow("Expander", expander);
```

## Requirements

- .NET 8.0+
- MonoGame 3.8+
- [Gum.MonoGame](https://www.nuget.org/packages/Gum.MonoGame)
- [KernSmith.MonoGameGum](https://www.nuget.org/packages/KernSmith.MonoGameGum) (for in-memory font rendering)
