# Themes

## Introduction

A **theme** is a per-backend NuGet package that restyles every default Gum Forms control with a single call. After calling `GumService.Default.Initialize(...)`, calling `<Name>Theme.Apply(GraphicsDevice)` swaps the default visuals for the theme's visuals — every `Button`, `TextBox`, `CheckBox`, `ComboBox`, etc. created afterward renders in that theme's style.

Themes ship one NuGet per rendering backend (e.g. `Gum.Themes.DarkPro.MonoGame`, `Gum.Themes.DarkPro.Kni`) so each package only carries the assets and references it needs.

## How to use a theme

Install the package matching your runtime and theme, then call `Apply` after `Initialize`:

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

See each theme's page for its install command, bundled fonts, and theme-specific notes.

## Available themes

| Theme | Vibe |
|---|---|
| [Editor](editor-theme.md) | Flat dark editor/tool chrome — Unity-Inspector / WPF property-grid feel, with `PropertyGridVisual` and `Expander` controls for tool UIs. |
| [DarkPro](dark-pro-theme.md) | Modern code-editor dark — VS Code / JetBrains feel using the DM Mono typeface. |
| [Bubblegum](bubblegum-theme.md) | Pastel pink casual-game look — rounded pill buttons, soft drop shadows, Nunito typeface. |
| [Neon](neon-theme.md) | Cyberpunk / neon — dark near-black surfaces, saturated cyan accent, Gaussian glow on focus, Share Tech Mono + Orbitron. |
| [Retro95](retro95-theme.md) | Windows 95 "Classic" — battleship-gray `#C0C0C0` chrome with raised/sunken bevels and navy selection. |

## Previews

Screenshots will be added for each theme. In the meantime, here is a one- or two-sentence description of each:

- **Editor** — Flat, low-saturation dark chrome with outline-on-hover states. Reads as a tool window, not a game UI.
  <!-- Screenshot: themes/editor.png -->
- **DarkPro** — Calm dark grays with a single accent color and the DM Mono typeface. Square-ish corners, no glow, code-editor mood.
  <!-- Screenshot: themes/dark-pro.png -->
- **Bubblegum** — Pastel pinks and creams with rounded pill buttons and soft drop shadows. Friendly and casual-game-ready.
  <!-- Screenshot: themes/bubblegum.png -->
- **Neon** — Dark `#060612` backgrounds with saturated cyan accents and a Gaussian glow on hovered or focused controls. Near-square corners, cyberpunk mood.
  <!-- Screenshot: themes/neon.png -->
- **Retro95** — Battleship-gray surfaces with raised/sunken beveled borders, navy-and-white selection band, and a Nunito stand-in for MS Sans Serif. Strictly square corners.
  <!-- Screenshot: themes/retro95.png -->

## Supported backends

Themes are currently published only for **MonoGame** and **KNI**.

If you'd like to see a theme published for **FNA**, **Raylib**, or **Skia**, please open an issue or start a discussion on the [Gum GitHub repo](https://github.com/vchelaru/Gum).

## Switching or customizing

A few things to keep in mind:

- **Apply one theme per app.** `Apply` mutates the default visuals — calling more than one theme's `Apply` in the same process produces a mix of leftover state. Pick one.
- **Call `Apply` after `Initialize` and before constructing Forms controls.** Controls capture their visuals at construction time, so any control built before `Apply` will keep the default styling.
- **For finer-grained tweaking** — restyling a single control, overriding colors, or changing layout — see [Code-Only Styling](../code-only-styling/styling-using-activestyles.md) and [Control Customization in Gum Tool](../control-customization-in-gum-tool.md). Themes and per-control styling compose; you can apply a theme and then further customize individual controls.
