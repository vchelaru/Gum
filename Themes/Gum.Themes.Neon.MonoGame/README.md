# Gum.Themes.Neon

A neon / cyberpunk theme for Gum UI Forms controls. Dark Surface1 bodies, near-square corners, saturated cyan accent, and a Gaussian glow on focused/hovered controls. Body text uses Share Tech Mono; window title bars use Orbitron.

## Install

Pick the package matching your runtime:

- MonoGame: `dotnet add package Gum.Themes.Neon.MonoGame`
- KNI: `dotnet add package Gum.Themes.Neon.Kni`

## Usage

Call `NeonTheme.Apply(GraphicsDevice)` once after initializing Gum:

```csharp
using Gum.Themes.Neon;

protected override void Initialize()
{
    GumService.Default.Initialize(this);
    NeonTheme.Apply(GraphicsDevice);
    base.Initialize();
}
```

Every default Forms control now renders in the Neon style. For best effect, clear your back buffer to the Neon page background:

```csharp
GraphicsDevice.Clear(NeonColors.Background); // #060612
```

## Fonts

- `NeonTheme.FontFamily` — `"Share Tech Mono"` — body typeface, Regular only.
- `NeonTheme.TitleFontFamily` — `"Orbitron"` — title typeface, Regular + Bold (also Black available via direct Font override).
- `NeonTheme.IconFontFamily` — `"Neon Icons"` (DejaVu Sans Mono) — for ✓ ✕ ▼ ▲ ◀ ▶ glyphs the body typeface doesn't cover.

## Licensing

- Theme code: MIT (same as Gum).
- Share Tech Mono: SIL Open Font License (see `ShareTechMono-LICENSE.txt`).
- Orbitron: SIL Open Font License (see `Orbitron-LICENSE.txt`).
- DejaVu Sans Mono: Bitstream Vera / DejaVu license (see `DejaVuSansMono-LICENSE.txt`).
