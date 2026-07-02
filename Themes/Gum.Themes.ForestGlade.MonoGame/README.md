# Gum.Themes.ForestGlade

A lush canopy / forest theme for Gum UI Forms controls. Deep teal-green canopy backdrop, saturated leaf-green fills, sunlit yellow border tints, and a signature leaf-shaped corner silhouette (sharp top-left / bottom-right, rounded top-right / bottom-left). Body text uses Nunito; the Window title bar uses Fraunces italic.

The leaf silhouette uses the per-corner radii API on Gum's `RectangleRuntime` (requires Apos.Shapes 0.6.9 or later for the rounded-corner rendering).

## Install

Pick the package matching your runtime:

- MonoGame: `dotnet add package Gum.Themes.ForestGlade.MonoGame`
- KNI: `dotnet add package Gum.Themes.ForestGlade.Kni`
- raylib: `dotnet add package Gum.Themes.ForestGlade.Raylib`

## Usage

Call the parameterless `ForestGladeTheme.Apply()` once after initializing Gum — the same call on every backend:

```csharp
using Gum.Themes.ForestGlade;

protected override void Initialize()
{
    GumService.Default.Initialize(this);
    ForestGladeTheme.Apply();
    base.Initialize();
}
```

> On MonoGame/KNI a legacy `ForestGladeTheme.Apply(GraphicsDevice)` overload remains for source
> compatibility; the graphics device is now resolved internally, so prefer `Apply()`.

Every default Forms control now renders in the Forest Glade style. For best effect, clear your back buffer to the canopy background:

```csharp
GraphicsDevice.Clear(ForestGladeStyling.ActiveStyle.Colors.CanopyDeep); // #053239
```

Colors and fonts are mutable — set `ForestGladeStyling.ActiveStyle.Colors.*` / `.Text.*` before calling `Apply()` to restyle the theme without forking its source.

## Fonts

- `ForestGladeStyling.ActiveStyle.Text.FontFamily` — defaults to `"Nunito"` — body typeface, Regular and Bold.
- `ForestGladeStyling.ActiveStyle.Text.TitleFontFamily` — defaults to `"Fraunces"` — display face used on the Window title bar (italic only).
- `ForestGladeStyling.ActiveStyle.Text.IconFontFamily` — defaults to `"Forest Glade Icons"` (DejaVu Sans Mono) — for ✓ ✕ ▼ ▴ ▲ ◀ ▶ ✿ glyphs the body typeface doesn't cover.

## The leaf silhouette

Every chrome surface in the theme uses the same four-corner radii pattern — sharp on the top-left and bottom-right corners, rounded on the top-right and bottom-left. Sizes scale with surface size (`--leaf-sm/md/lg/xl` in the CSS spec):

| Token | Sharp | Rounded | Used by                          |
| ----- | ----- | ------- | -------------------------------- |
| sm    | 2     | 8       | CheckBox box, small surfaces     |
| md    | 2     | 12      | TextBox, ComboBox, badge chips   |
| lg    | 4     | 18      | Button, ListBox, ComboBox open   |
| xl    | 6     | 24      | Window                            |

If you compose custom visuals on top of this theme and want them to match, the same helper is available in code: `ForestGladeLeaf.ApplySmall / Medium / Large / ExtraLarge` (internal — copy the same per-corner pattern into your own visual).

## Licensing

- Theme code: MIT (same as Gum).
- Nunito: SIL Open Font License (see `Nunito-LICENSE.txt`).
- Fraunces: SIL Open Font License (see `Fraunces-LICENSE.txt`).
- DejaVu Sans Mono: Bitstream Vera / DejaVu license (see `DejaVuSansMono-LICENSE.txt`).
