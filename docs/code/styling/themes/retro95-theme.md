# Retro95 Theme

## Introduction

The **Gum.Themes.Retro95** package provides a retro Windows 95 / "Classic" theme for Gum Forms controls. Battleship-gray (`#C0C0C0`) chrome, raised and sunken beveled borders, and the navy-and-white selection band of the era. Strictly square corners.

The theme ships per rendering backend:

* `Gum.Themes.Retro95.MonoGame` — for MonoGame projects
* `Gum.Themes.Retro95.Kni` — for KNI projects

## Installation

Install the package matching your runtime. For MonoGame:

```
dotnet add package Gum.Themes.Retro95.MonoGame
```

For KNI:

```
dotnet add package Gum.Themes.Retro95.Kni
```

## Usage

Call `Retro95Theme.Apply` after initializing Gum. All Forms controls created afterward will use the Retro95 theme:

```csharp
using Gum.Themes.Retro95;

protected override void Initialize()
{
    GumService.Default.Initialize(this, DefaultVisualsVersion.Newest);

    Retro95Theme.Apply(GraphicsDevice);

    var button = new Button();
    button.Text = "Click Me";
}
```

## Included Controls

The theme provides styled visuals for the standard Gum Forms controls:

* **Button** — raised bevel by default, sunken when pressed
* **TextBox** / **PasswordBox** — sunken bevel
* **CheckBox**
* **ComboBox**
* **ListBox** / **ListBoxItem** — navy-and-white selection band
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

Two fonts are embedded in the assembly and registered automatically:

* **Nunito** — body typeface, used as a stand-in for the era-accurate MS Sans Serif (which is proprietary and cannot be redistributed). SIL Open Font License.
* **DejaVu Sans Mono** — used for icon glyphs (check marks, close buttons, arrows). Bitstream Vera / DejaVu license.

## Licensing

* Theme code: MIT (same as Gum).
* Bundled Nunito font: SIL Open Font License (see `OFL.txt` packed at the root of the NuGet).
* Bundled DejaVu Sans Mono font: Bitstream Vera / DejaVu license (see `DejaVuSansMono-LICENSE.txt`).

## Requirements

* .NET 8.0+
* MonoGame 3.8+ (for the MonoGame package) or KNI (for the KNI package)
* [Gum.MonoGame](https://www.nuget.org/packages/Gum.MonoGame) or [Gum.Kni](https://www.nuget.org/packages/Gum.Kni)
* [KernSmith.MonoGameGum](https://www.nuget.org/packages/KernSmith.MonoGameGum) — optional, only needed when using runtime in-memory font generation
