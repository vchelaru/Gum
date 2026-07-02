# Gum.Themes.DarkPro

A flat dark theme for [Gum](https://github.com/vchelaru/Gum) UI inspired by modern code editors (VS Code, JetBrains dark). Provides styled visuals for Gum Forms controls.

The theme ships per rendering backend. Install the one matching your runtime:

```
dotnet add package Gum.Themes.DarkPro.MonoGame
dotnet add package Gum.Themes.DarkPro.Kni
dotnet add package Gum.Themes.DarkPro.Raylib
```

## Usage

Call the parameterless `DarkProTheme.Apply()` after initializing Gum — the same call on every backend:

```csharp
using Gum.Themes.DarkPro;

// after GumService.Default.Initialize(...)
DarkProTheme.Apply();

var button = new Button();
button.Text = "Click Me";
```

> On MonoGame/KNI a legacy `DarkProTheme.Apply(GraphicsDevice)` overload remains for source
> compatibility; the graphics device is now resolved internally, so prefer `Apply()`.

## Bundled Fonts

Two fonts are embedded in the assembly and registered automatically.

### `"DM Mono"` — user-facing text

DM Mono (SIL Open Font License). Four weights:

| Gum properties              | TTF                       |
| --------------------------- | ------------------------- |
| default                     | `DMMono-Regular.ttf`      |
| `IsBold = true`             | `DMMono-Medium.ttf`       |
| `IsItalic = true`           | `DMMono-Italic.ttf`       |
| `IsBold = true, IsItalic = true` | `DMMono-MediumItalic.ttf` |

DM Mono's true `Bold` weight (700) does not ship with this theme — `Medium` (500) is mapped to Gum's `IsBold = true` slot because the design intent is "Medium for emphasis." Replace via `KernSmithFontCreator.RegisterFont("DM Mono", ttfBytes, style: "Bold")` to override.

See `Content/Fonts/OFL.txt` for the DM Mono license.

### `"DM Mono Icons"` — internal glyphs

DejaVu Sans Mono (Bitstream Vera / DejaVu license; redistribution permitted). Used by the theme's visuals for glyphs DM Mono doesn't cover — check marks, close buttons, combo and scrollbar arrows (Dingbats and Geometric Shapes Unicode blocks). The family name is also exposed as `DarkProStyling.ActiveStyle.Text.IconFontFamily` if you need to render the same glyphs yourself.

See `Content/Fonts/DejaVuSansMono-LICENSE.txt` for the DejaVu license.
