# Gum.Themes.DarkPro

A flat dark theme for [Gum](https://github.com/vchelaru/Gum) UI inspired by modern code editors (VS Code, JetBrains dark). Provides styled visuals for Gum Forms controls.

The theme ships per rendering backend. Install the one matching your runtime:

```
dotnet add package Gum.Themes.DarkPro.MonoGame
```

## Usage

Call `DarkProTheme.Apply` after initializing Gum:

```csharp
using Gum.Themes.DarkPro;

protected override void Initialize()
{
    GumService.Default.Initialize(this, DefaultVisualsVersion.Newest);

    DarkProTheme.Apply(GraphicsDevice);

    var button = new Button();
    button.Text = "Click Me";
}
```

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

DejaVu Sans Mono (Bitstream Vera / DejaVu license; redistribution permitted). Used by the theme's visuals for glyphs DM Mono doesn't cover — check marks, close buttons, combo and scrollbar arrows (Dingbats and Geometric Shapes Unicode blocks). The family name is also exposed as `DarkProTheme.IconFontFamily` if you need to render the same glyphs yourself.

See `Content/Fonts/DejaVuSansMono-LICENSE.txt` for the DejaVu license.
