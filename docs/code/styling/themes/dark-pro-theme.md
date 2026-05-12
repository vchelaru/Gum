# DarkPro Theme

## Introduction

The **Gum.Themes.DarkPro** package provides a flat dark theme for Gum Forms controls, inspired by modern code editors like VS Code and JetBrains' dark themes. Calm dark grays, a single accent color, and the DM Mono typeface for a code-editor mood.

The theme ships per rendering backend:

* `Gum.Themes.DarkPro.MonoGame` — for MonoGame projects
* `Gum.Themes.DarkPro.Kni` — for KNI projects

## Installation

Install the package matching your runtime. For MonoGame:

```
dotnet add package Gum.Themes.DarkPro.MonoGame
```

For KNI:

```
dotnet add package Gum.Themes.DarkPro.Kni
```

## Usage

Call `DarkProTheme.Apply` after initializing Gum. All Forms controls created afterward will use the DarkPro theme:

```csharp
using Gum.Themes.DarkPro;

protected override void Initialize()
{
    GumService.Default.Initialize(this, DefaultVisualsVersion.Newest);

    DarkProTheme.Apply(GraphicsDevice);

    var button = new Button();
    button.Text = "Click Me";
}
```

## Included Controls

The theme provides styled visuals for the standard Gum Forms controls:

* **Button**
* **TextBox** / **PasswordBox**
* **CheckBox**
* **ComboBox**
* **ListBox** / **ListBoxItem**
* **ScrollBar**
* **ScrollViewer**
* **Slider**
* **RadioButton**
* **ToggleButton**
* **Tooltip**
* **Menu** / **MenuItem**
* **Splitter**
* **Window**

## Bundled Fonts

Two fonts are embedded in the assembly and registered automatically.

### `"DM Mono"` — user-facing text

DM Mono (SIL Open Font License). Four weights:

| Gum properties              | TTF                       |
| --------------------------- | ------------------------- |
| default                     | `DMMono-Regular.ttf`      |
| `IsBold = true`             | `DMMono-Medium.ttf`       |
| `IsItalic = true`           | `DMMono-Italic.ttf`       |
| `IsBold = true, IsItalic = true` | `DMMono-MediumItalic.ttf` |

DM Mono's true `Bold` weight (700) does not ship with this theme — `Medium` (500) is mapped to Gum's `IsBold = true` slot because the design intent is "Medium for emphasis." Replace via `KernSmithFontCreator.RegisterFont("DM Mono", ttfBytes, style: "Bold")` to override.

The DM Mono license is bundled in the NuGet's `Content/Fonts/OFL.txt`.

### `"DM Mono Icons"` — internal glyphs

DejaVu Sans Mono (Bitstream Vera / DejaVu license; redistribution permitted). Used by the theme's visuals for glyphs DM Mono doesn't cover — check marks, close buttons, combo and scrollbar arrows (Dingbats and Geometric Shapes Unicode blocks). The family name is also exposed as `DarkProTheme.IconFontFamily` if you need to render the same glyphs yourself.

The DejaVu license is bundled in the NuGet's `Content/Fonts/DejaVuSansMono-LICENSE.txt`.

## Requirements

* .NET 8.0+
* MonoGame 3.8+ (for the MonoGame package) or KNI (for the KNI package)
* [Gum.MonoGame](https://www.nuget.org/packages/Gum.MonoGame) or [Gum.Kni](https://www.nuget.org/packages/Gum.Kni)
* [KernSmith.MonoGameGum](https://www.nuget.org/packages/KernSmith.MonoGameGum) — optional, only needed when using runtime in-memory font generation
