# Gum.Themes.Neon

A neon / cyberpunk theme for Gum UI Forms controls. Dark Surface1 bodies, near-square corners, saturated cyan accent, and a Gaussian glow on focused/hovered controls. Body text uses Share Tech Mono; window title bars use Orbitron.

## Install

Pick the package matching your runtime:

- MonoGame: `dotnet add package Gum.Themes.Neon.MonoGame`
- KNI: `dotnet add package Gum.Themes.Neon.Kni`
- raylib: `dotnet add package Gum.Themes.Neon.Raylib`
- Skia/SilkNet: `dotnet add package Gum.Themes.Neon.SilkNet`

## Usage

Call the parameterless `NeonTheme.Apply()` once after initializing Gum — the same call on every backend:

```csharp
using Gum.Themes.Neon;

protected override void Initialize()
{
    GumService.Default.Initialize(this);
    NeonTheme.Apply();
    base.Initialize();
}
```

> On MonoGame/KNI a legacy `NeonTheme.Apply(GraphicsDevice)` overload remains for source
> compatibility; the graphics device is now resolved internally, so prefer `Apply()`.

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
