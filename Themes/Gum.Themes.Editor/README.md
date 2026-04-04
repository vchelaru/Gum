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

A layout control for displaying labeled controls in a two-column grid:

```csharp
var grid = new PropertyGridVisual();
grid.AddToRoot();
grid.Width = 400;
grid.WidthUnits = DimensionUnitType.Absolute;

grid.AddRow("Name", new TextBox());
grid.AddRow("Visible", new CheckBox());
grid.AddRow("Speed", new Slider());
```

## Expander

A collapsible content control:

```csharp
var expander = new Expander();
expander.Header = "Advanced Settings";
expander.AddContent(new CheckBox());
expander.AddContent(new Slider());
```

## Requirements

- .NET 8.0+
- MonoGame 3.8+
- [Gum.MonoGame](https://www.nuget.org/packages/Gum.MonoGame)
- [KernSmith.MonoGameGum](https://www.nuget.org/packages/KernSmith.MonoGameGum) (for in-memory font rendering)
