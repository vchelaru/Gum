# Editor Theme

## Introduction

The **Gum.Themes.Editor** package provides a styled, editor-focused theme for Gum Forms controls. It is intended for building tool and editor interfaces — flat surfaces, dark backgrounds, and outline-on-hover states similar to Unity's Inspector or a WPF property grid.

The theme ships per rendering backend:

* `Gum.Themes.Editor.MonoGame` — for MonoGame projects
* `Gum.Themes.Editor.Kni` — for KNI projects

## Installation

Install the package matching your runtime. For MonoGame:

```
dotnet add package Gum.Themes.Editor.MonoGame
```

For KNI:

```
dotnet add package Gum.Themes.Editor.Kni
```

## Usage

Call `EditorTheme.Apply` after initializing Gum. All Forms controls created afterward will use the editor theme:

```csharp
using Gum.Themes.Editor;

protected override void Initialize()
{
    GumService.Default.Initialize(this, DefaultVisualsVersion.Newest);

    EditorTheme.Apply(GraphicsDevice);

    var button = new Button();
    button.Text = "Click Me";
}
```

## Included Controls

The theme provides styled visuals for:

* **Button** — flat style with outline on hover/press
* **TextBox** — dark background with outline states
* **CheckBox** — outline on hover/press
* **ComboBox** — outline on hover/press
* **ListBox** / **ListBoxItem** — dark background with highlight and selection colors
* **ScrollBar** — dark track with styled thumb
* **ScrollViewer** — uses the styled ScrollBar
* **Slider** — narrow thumb with adjusted track
* **Expander** — collapsible header/content control
* **PropertyGridVisual** — two-column label/control grid with alternating row backgrounds

## PropertyGridVisual

A two-column layout that displays label/control pairs in rows with alternating background colors — similar to a Unity Inspector or WPF PropertyGrid.

```csharp
var grid = new PropertyGridVisual();
grid.AddToRoot();
grid.Width = 400;
grid.WidthUnits = DimensionUnitType.Absolute;

grid.AddRow("Name", new TextBox());
grid.AddRow("Visible", new CheckBox());
grid.AddRow("Speed", new Slider());

var comboBox = new ComboBox();
comboBox.Items.Add("Option A");
comboBox.Items.Add("Option B");
grid.AddRow("Mode", comboBox);
```

Each row has a fixed-width label on the left and the control filling the remaining space on the right. Rows automatically size to fit their content and alternate between two background colors for readability.

## Expander

A collapsible header/content control. Clicking the header toggles visibility of the content area. The `Expander` Forms control lives in the `Gum.Forms.Controls` namespace and is shipped with the runtime — the editor theme provides its visuals.

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
expander.IsExpanded = true;

expander.Expanded += (sender, e) => Console.WriteLine("Opened");
expander.Collapsed += (sender, e) => Console.WriteLine("Closed");
```

### Inside a PropertyGridVisual

The Expander works as a row control:

```csharp
var expander = new Expander();
expander.Header = "More Options";
expander.AddContent(new CheckBox());
expander.AddContent(new Slider());
grid.AddRow("Expander", expander);
```

## Requirements

* .NET 8.0+
* MonoGame 3.8+ (for the MonoGame package) or KNI (for the KNI package)
* [Gum.MonoGame](https://www.nuget.org/packages/Gum.MonoGame) or [Gum.Kni](https://www.nuget.org/packages/Gum.Kni)
* [KernSmith.MonoGameGum](https://www.nuget.org/packages/KernSmith.MonoGameGum) — optional, only needed when using runtime in-memory font generation
