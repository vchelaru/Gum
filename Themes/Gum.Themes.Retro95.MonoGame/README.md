# Gum.Themes.Retro95

A retro Windows 95 / "Classic" theme for Gum UI Forms controls. Pairs the canonical battleship-gray (`#C0C0C0`) chrome with raised/sunken beveled borders and the navy-and-white selection band of the era.

## Install

Pick the package matching your runtime:

- MonoGame: `dotnet add package Gum.Themes.Retro95.MonoGame`
- KNI: `dotnet add package Gum.Themes.Retro95.Kni`

## Usage

Call `Retro95Theme.Apply(GraphicsDevice)` once after initializing Gum:

```csharp
using Gum.Themes.Retro95;

protected override void Initialize()
{
    GumService.Default.Initialize(this);
    Retro95Theme.Apply(GraphicsDevice);
    base.Initialize();
}
```

Every default Forms control now renders in the Retro95 style.

## Licensing

- Theme code: MIT (same as Gum).
- Bundled Nunito font: SIL Open Font License (see `OFL.txt` packed at the root of the NuGet). Used as a stand-in for the era-accurate MS Sans Serif, which is proprietary.
- Bundled DejaVu Sans Mono font (icons): Bitstream Vera / DejaVu license (see `DejaVuSansMono-LICENSE.txt`).
