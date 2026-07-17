# Gum.Themes.Meadow

A cozy cottagecore theme for Gum UI Forms controls. Pairs chunky sky-blue pill buttons (with a flat "stacked card" drop shadow), dashed-outline cream panels, sage selection accents, and coral sliders, in the rounded Baloo 2 + Quicksand typefaces — for a warm, handmade look.

## Install

Pick the package matching your runtime:

- MonoGame: `dotnet add package Gum.Themes.Meadow.MonoGame`
- KNI: `dotnet add package Gum.Themes.Meadow.Kni`
- raylib: `dotnet add package Gum.Themes.Meadow.Raylib`
- Skia/SilkNet: `dotnet add package Gum.Themes.Meadow.SilkNet`

## Usage

Call the parameterless `MeadowTheme.Apply()` once after initializing Gum — the same call on every backend:

```csharp
using Gum.Themes.Meadow;

protected override void Initialize()
{
    GumService.Default.Initialize(this);
    MeadowTheme.Apply();
    base.Initialize();
}
```

Every default Forms control now renders in the Meadow style.

> On MonoGame/KNI a legacy `MeadowTheme.Apply(GraphicsDevice)` overload remains for source
> compatibility; the graphics device is now resolved internally, so prefer `Apply()`.

## Licensing

- Theme code: MIT (same as Gum).
- Bundled Baloo 2 and Quicksand fonts: SIL Open Font License (see `Baloo2-OFL.txt` and `Quicksand-OFL.txt`).
- Bundled DejaVu Sans Mono icon font: Bitstream Vera / DejaVu license (see `DejaVuSansMono-LICENSE.txt`).
