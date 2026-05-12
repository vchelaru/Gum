# Bubblegum Theme

## Introduction

The **Gum.Themes.Bubblegum** package provides a pastel pink "casual game" theme for Gum Forms controls. Rounded pill buttons, soft drop shadows, and the Nunito typeface combine for a friendly, approachable look that fits casual or mobile-style games.

The theme ships per rendering backend:

* `Gum.Themes.Bubblegum.MonoGame` — for MonoGame projects
* `Gum.Themes.Bubblegum.Kni` — for KNI projects

## Installation

Install the package matching your runtime. For MonoGame:

```
dotnet add package Gum.Themes.Bubblegum.MonoGame
```

For KNI:

```
dotnet add package Gum.Themes.Bubblegum.Kni
```

## Usage

Call `BubblegumTheme.Apply` after initializing Gum. All Forms controls created afterward will use the Bubblegum theme:

```csharp
using Gum.Themes.Bubblegum;

protected override void Initialize()
{
    GumService.Default.Initialize(this, DefaultVisualsVersion.Newest);

    BubblegumTheme.Apply(GraphicsDevice);

    var button = new Button();
    button.Text = "Click Me";
}
```

## Included Controls

The theme provides styled visuals for the standard Gum Forms controls:

* **Button** — rounded pill shape with drop shadow
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

The Nunito typeface (SIL Open Font License) is embedded in the assembly and registered automatically as the theme's body font. The license file `OFL.txt` is packed at the root of the NuGet.

## Licensing

* Theme code: MIT (same as Gum).
* Bundled Nunito font: SIL Open Font License.

## Requirements

* .NET 8.0+
* MonoGame 3.8+ (for the MonoGame package) or KNI (for the KNI package)
* [Gum.MonoGame](https://www.nuget.org/packages/Gum.MonoGame) or [Gum.Kni](https://www.nuget.org/packages/Gum.Kni)
* [KernSmith.MonoGameGum](https://www.nuget.org/packages/KernSmith.MonoGameGum) — optional, only needed when using runtime in-memory font generation
