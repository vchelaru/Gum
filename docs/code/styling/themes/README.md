# Themes

## Introduction

A **theme** is a per-backend NuGet package that restyles every default Gum Forms control with a single call. After calling `GumService.Default.Initialize(...)`, calling `<Name>Theme.Apply(GraphicsDevice)` swaps the default visuals for the theme's visuals — every `Button`, `TextBox`, `CheckBox`, `ComboBox`, etc. created afterward renders in that theme's style.

Themes ship one NuGet per rendering backend (for example `Gum.Themes.DarkPro.MonoGame` and `Gum.Themes.DarkPro.Kni`) so each package only carries the assets and references it needs.

## Usage

Install the package matching your runtime, then call the theme's `Apply` after `Initialize`:

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

The pattern is the same for every theme — only the `using` namespace, NuGet package, and `Apply` call change. Pick a theme from the table below and substitute its name in.

A few things to keep in mind:

- **Apply one theme per app.** `Apply` mutates the default visuals — calling more than one theme's `Apply` in the same process produces a mix of leftover state.
- **Call `Apply` after `Initialize` and before constructing Forms controls.** Controls capture their visuals at construction time, so any control built before `Apply` will keep the default styling.
- **Themes compose with per-control styling.** For tweaking a single control on top of a theme, see [Code-Only Styling](../code-only-styling/styling-using-activestyles.md) and [Control Customization in Gum Tool](../control-customization-in-gum-tool.md).

## Available themes

| Theme | NuGet (MonoGame / KNI) | Apply call | Preview |
|---|---|---|---|
| **[Editor](editor-theme.md)** — flat dark editor/tool chrome with `PropertyGridVisual` and `Expander` controls for tool UIs. See the [Editor Theme](editor-theme.md) page for those extra controls. | `Gum.Themes.Editor.MonoGame` / `Gum.Themes.Editor.Kni` | `EditorTheme.Apply(GraphicsDevice);` | <!-- Screenshot: editor.png --> |
| **DarkPro** — modern code-editor dark (VS Code / JetBrains feel). Bundled fonts: DM Mono (body) + DejaVu Sans Mono (icons). | `Gum.Themes.DarkPro.MonoGame` / `Gum.Themes.DarkPro.Kni` | `DarkProTheme.Apply(GraphicsDevice);` | <!-- Screenshot: dark-pro.png --> |
| **Bubblegum** — pastel pink casual-game look with rounded pill buttons and soft drop shadows. Bundled font: Nunito. | `Gum.Themes.Bubblegum.MonoGame` / `Gum.Themes.Bubblegum.Kni` | `BubblegumTheme.Apply(GraphicsDevice);` | <!-- Screenshot: bubblegum.png --> |
| **Neon** — cyberpunk / neon-glow dark theme with saturated cyan accent and a Gaussian glow on focus. Bundled fonts: Share Tech Mono (body) + Orbitron (titles) + DejaVu Sans Mono (icons). For the intended look, clear the back buffer to `NeonColors.Background` (`#060612`). | `Gum.Themes.Neon.MonoGame` / `Gum.Themes.Neon.Kni` | `NeonTheme.Apply(GraphicsDevice);` | <!-- Screenshot: neon.png --> |
| **Retro95** — Windows 95 "Classic" battleship-gray chrome with raised/sunken bevels and navy selection. Bundled fonts: Nunito (MS Sans Serif stand-in) + DejaVu Sans Mono (icons). | `Gum.Themes.Retro95.MonoGame` / `Gum.Themes.Retro95.Kni` | `Retro95Theme.Apply(GraphicsDevice);` | <!-- Screenshot: retro95.png --> |

All bundled fonts are SIL Open Font License or the Bitstream Vera / DejaVu license — both permit redistribution. License files ship inside each NuGet.

## Supported backends

Themes are currently published only for **MonoGame** and **KNI**. If you'd like to see a theme published for **FNA**, **Raylib**, or **Skia**, please open an issue or start a discussion on the [Gum GitHub repo](https://github.com/vchelaru/Gum).

## Requirements

- .NET 8.0+
- MonoGame 3.8+ (for the MonoGame packages) or KNI (for the KNI packages)
- [Gum.MonoGame](https://www.nuget.org/packages/Gum.MonoGame) or [Gum.Kni](https://www.nuget.org/packages/Gum.Kni)
- [KernSmith.MonoGameGum](https://www.nuget.org/packages/KernSmith.MonoGameGum) — optional, only needed when using runtime in-memory font generation
