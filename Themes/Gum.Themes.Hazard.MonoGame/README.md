# Gum.Themes.Hazard

An industrial space-salvage HUD theme for [Gum](https://github.com/vchelaru/Gum) UI — signature hazard-yellow on warm near-black, muted-gold borders, and square-cornered chrome (inspired by Hardspace: Shipbreaker). Provides styled visuals for Gum Forms controls.

The theme ships per rendering backend. Install the one matching your runtime:

- MonoGame: `dotnet add package Gum.Themes.Hazard.MonoGame`
- KNI: `dotnet add package Gum.Themes.Hazard.Kni`
- raylib: `dotnet add package Gum.Themes.Hazard.Raylib`

## Usage

Call the parameterless `HazardTheme.Apply()` once after initializing Gum — the same call on every backend:

```csharp
using Gum.Themes.Hazard;

protected override void Initialize()
{
    GumService.Default.Initialize(this, DefaultVisualsVersion.Newest);

    HazardTheme.Apply();

    var button = new Button();
    button.Text = "EXTRACT";
}
```

> On MonoGame/KNI a legacy `HazardTheme.Apply(GraphicsDevice)` overload remains for source
> compatibility; the graphics device is now resolved internally, so prefer `Apply()`.

Interactive states echo the source HUD: hover brightens the border and text, **pressing a button flashes the full hazard-yellow accent with black text**, and active states (a checked CheckBox, an On ToggleButton, a selected list row) fill with the accent and switch to ink-black text.

## Bundled Fonts

Two fonts are embedded in the assembly and registered automatically.

### `"Saira Condensed"` — user-facing text and labels

Saira Condensed (SIL Open Font License). Two weights:

| Gum properties  | TTF                          |
| --------------- | ---------------------------- |
| default         | `SairaCondensed-Regular.ttf`  |
| `IsBold = true` | `SairaCondensed-SemiBold.ttf` |

Saira Condensed ships no italic, and the Forms styling never requests one. SemiBold (600) is mapped to Gum's `IsBold = true` slot to match the design's uppercase label/heading weight. Override via `KernSmithFontCreator.RegisterFont("Saira Condensed", ttfBytes, style: "Bold")`.

See `Content/Fonts/SairaCondensed-OFL.txt` for the Saira license.

### `"Saira Condensed Icons"` — internal glyphs

DejaVu Sans Mono (Bitstream Vera / DejaVu license; redistribution permitted). Used by the theme's visuals for glyphs Saira Condensed doesn't cover — the CheckBox check (`✓`) and the ComboBox dropdown arrow (`▼`), which live in the Dingbats and Geometric Shapes Unicode blocks. The family name is also exposed as `HazardStyling.ActiveStyle.Text.IconFontFamily` if you need to render the same glyphs yourself.

See `Content/Fonts/DejaVuSansMono-LICENSE.txt` for the DejaVu license.
