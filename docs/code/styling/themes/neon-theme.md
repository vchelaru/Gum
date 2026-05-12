# Neon Theme

## Introduction

The **Gum.Themes.Neon** package provides a neon / cyberpunk theme for Gum Forms controls. Dark `Surface1` bodies, near-square corners, a saturated cyan accent, and a Gaussian glow on focused or hovered controls. Body text uses Share Tech Mono; window title bars use Orbitron.

The theme ships per rendering backend:

* `Gum.Themes.Neon.MonoGame` — for MonoGame projects
* `Gum.Themes.Neon.Kni` — for KNI projects

## Installation

Install the package matching your runtime. For MonoGame:

```
dotnet add package Gum.Themes.Neon.MonoGame
```

For KNI:

```
dotnet add package Gum.Themes.Neon.Kni
```

## Usage

Call `NeonTheme.Apply` after initializing Gum. All Forms controls created afterward will use the Neon theme:

```csharp
using Gum.Themes.Neon;

protected override void Initialize()
{
    GumService.Default.Initialize(this, DefaultVisualsVersion.Newest);

    NeonTheme.Apply(GraphicsDevice);

    var button = new Button();
    button.Text = "Click Me";
}
```

For the intended look, clear your back buffer to the Neon page background so the glow reads against a near-black surface:

```csharp
GraphicsDevice.Clear(NeonColors.Background); // #060612
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

Three font families are embedded in the assembly and registered automatically:

* `NeonTheme.FontFamily` — `"Share Tech Mono"` — body typeface, Regular only.
* `NeonTheme.TitleFontFamily` — `"Orbitron"` — title typeface, Regular + Bold (Black is available via direct font override).
* `NeonTheme.IconFontFamily` — `"Neon Icons"` (DejaVu Sans Mono) — for `✓ ✕ ▼ ▲ ◀ ▶` glyphs the body typeface doesn't cover.

## Licensing

* Theme code: MIT (same as Gum).
* Share Tech Mono: SIL Open Font License (see `ShareTechMono-LICENSE.txt` in the NuGet).
* Orbitron: SIL Open Font License (see `Orbitron-LICENSE.txt` in the NuGet).
* DejaVu Sans Mono: Bitstream Vera / DejaVu license (see `DejaVuSansMono-LICENSE.txt` in the NuGet).

## Requirements

* .NET 8.0+
* MonoGame 3.8+ (for the MonoGame package) or KNI (for the KNI package)
* [Gum.MonoGame](https://www.nuget.org/packages/Gum.MonoGame) or [Gum.Kni](https://www.nuget.org/packages/Gum.Kni)
* [KernSmith.MonoGameGum](https://www.nuget.org/packages/KernSmith.MonoGameGum) — optional, only needed when using runtime in-memory font generation
