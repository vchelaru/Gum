# Gum.Themes.ForestGlade

A lush canopy / forest theme for Gum UI Forms controls. Deep teal-green canopy backdrop, saturated leaf-green fills, sunlit yellow border tints, and a signature leaf-shaped corner silhouette (sharp top-left / bottom-right, rounded top-right / bottom-left). Body text uses Nunito; the Window title bar uses Fraunces italic.

The leaf silhouette uses the per-corner radii API added to Gum's `RoundedRectangleRuntime` in #2721 (requires Apos.Shapes 0.6.9 or later).

## Install

Pick the package matching your runtime:

- MonoGame: `dotnet add package Gum.Themes.ForestGlade.MonoGame`
- KNI: `dotnet add package Gum.Themes.ForestGlade.Kni`

## Usage

Call `ForestGladeTheme.Apply(GraphicsDevice)` once after initializing Gum:

```csharp
using Gum.Themes.ForestGlade;

protected override void Initialize()
{
    GumService.Default.Initialize(this);
    ForestGladeTheme.Apply(GraphicsDevice);
    base.Initialize();
}
```

Every default Forms control now renders in the Forest Glade style. For best effect, clear your back buffer to the canopy background:

```csharp
GraphicsDevice.Clear(ForestGladeColors.CanopyDeep); // #053239
```

## Fonts

- `ForestGladeTheme.FontFamily` — `"Nunito"` — body typeface, Regular and Bold.
- `ForestGladeTheme.TitleFontFamily` — `"Fraunces"` — display face used on the Window title bar (italic only).
- `ForestGladeTheme.IconFontFamily` — `"Forest Glade Icons"` (DejaVu Sans Mono) — for ✓ ✕ ▼ ▴ ▲ ◀ ▶ ✿ glyphs the body typeface doesn't cover.

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
