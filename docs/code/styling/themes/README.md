# Themes

## Introduction

A **theme** is a per-backend NuGet package that restyles every default Gum Forms control with a single call. After calling `GumService.Default.Initialize(...)`, calling `<Name>Theme.Apply(GraphicsDevice)` swaps the default visuals for the theme's visuals — every `Button`, `TextBox`, `CheckBox`, `ComboBox`, etc. created afterward renders in that theme's style.

Themes ship one NuGet per rendering backend (for example `Gum.Themes.DarkPro.MonoGame` and `Gum.Themes.DarkPro.Kni`) so each package only carries the assets and references it needs.

{% hint style="warning" %}
All themes install and initialize KernSmith for dynamic font generation. Also, all themes except `Editor` install and initialize Apos.Shapes for vector art rendering. For more information see the [KernSmith](../../files-and-fonts/font-strategies.md#dynamic-kernsmith-generation) and [Apos.Shapes](../../standard-visuals/shapes-apos.shapes.md) pages.
{% endhint %}

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

The pattern is the same for every theme — only the `using` namespace, NuGet package, and `Apply` call change. Pick a theme from the sections below and substitute its name in.

A few things to keep in mind:

* **Apply one theme per app.** `Apply` mutates the default visuals — calling more than one theme's `Apply` in the same process produces a mix of leftover state.
* **Call `Apply` after `Initialize` and before constructing Forms controls.** Controls capture their visuals at construction time, so any control built before `Apply` will keep the default styling.
* **Themes compose with per-control styling.** For tweaking a single control on top of a theme, see [Code-Only Styling](../code-only-styling/styling-using-activestyles.md) and [Control Customization in Gum Tool](../control-customization-in-gum-tool.md).

## Available themes

### Editor

<figure><img src="../../../.gitbook/assets/EditorThemeScreenshot.png" alt="Editor theme preview"><figcaption><p>The Editor theme applied to a sample settings panel.</p></figcaption></figure>

Flat dark editor/tool chrome — intended for tool and editor interfaces rather than in-game UI. Ships with two extra controls beyond the standard Forms set: `PropertyGridVisual` and `Expander`. See the [Editor Theme](editor-theme.md) page for details on those controls.

```csharp
// Initialize
using Gum.Themes.Editor;

EditorTheme.Apply(GraphicsDevice);
```

NuGet packages: `Gum.Themes.Editor.MonoGame`, `Gum.Themes.Editor.Kni`

### DarkPro

<figure><img src="../../../.gitbook/assets/DarkProThemeScreenshot.png" alt="DarkPro theme preview"><figcaption><p>The DarkPro theme applied to a sample settings panel.</p></figcaption></figure>

Modern code-editor dark theme with a VS Code / JetBrains feel. Bundled fonts: DM Mono (body) and DejaVu Sans Mono (icons).

```csharp
// Initialize
using Gum.Themes.DarkPro;

DarkProTheme.Apply(GraphicsDevice);
```

NuGet packages: `Gum.Themes.DarkPro.MonoGame`, `Gum.Themes.DarkPro.Kni`

### Bubblegum

<figure><img src="../../../.gitbook/assets/BubblegumThemeScreenshot.png" alt="Bubblegum theme preview"><figcaption><p>The Bubblegum theme applied to a sample settings panel.</p></figcaption></figure>

Pastel pink casual-game look with rounded pill buttons and soft drop shadows. Bundled font: Nunito.

```csharp
// Initialize
using Gum.Themes.Bubblegum;

BubblegumTheme.Apply(GraphicsDevice);
```

NuGet packages: `Gum.Themes.Bubblegum.MonoGame`, `Gum.Themes.Bubblegum.Kni`

### Forest Glade

<figure><img src="../../../.gitbook/assets/ForestGladeThemeScreenshot.png" alt="Forest Glade theme preview"><figcaption><p>The Forest Glade theme applied to a sample settings panel.</p></figcaption></figure>

Lush green nature-themed look with gradient leaf-shaped buttons (sharp top-left / bottom-right corners, rounded top-right / bottom-left), a deep canopy background, and soft text drop shadows. Bundled font: Nunito.

```csharp
// Initialize
using Gum.Themes.ForestGlade;

ForestGladeTheme.Apply(GraphicsDevice);
```

{% hint style="info" %}
For the intended look, clear the back buffer to `ForestGladeColors.CanopyDeep`.
{% endhint %}

NuGet packages: `Gum.Themes.ForestGlade.MonoGame`, `Gum.Themes.ForestGlade.Kni`

### Neon

<figure><img src="../../../.gitbook/assets/NeonThemeScreenshot.png" alt="Neon theme preview"><figcaption><p>The Neon theme applied to a sample settings panel.</p></figcaption></figure>

Cyberpunk / neon-glow dark theme with a saturated cyan accent and a Gaussian glow on focus. Bundled fonts: Share Tech Mono (body), Orbitron (titles), and DejaVu Sans Mono (icons).

```csharp
// Initialize
using Gum.Themes.Neon;

NeonTheme.Apply(GraphicsDevice);
```

{% hint style="info" %}
For the intended look, clear the back buffer to `NeonColors.Background` (`#060612`).
{% endhint %}

NuGet packages: `Gum.Themes.Neon.MonoGame`, `Gum.Themes.Neon.Kni`

### Retro95

<figure><img src="../../../.gitbook/assets/Retro95ThemeScreenshot.png" alt="Retro95 theme preview"><figcaption><p>The Retro95 theme applied to a sample settings panel.</p></figcaption></figure>

Windows 95 "Classic" battleship-gray chrome with raised/sunken bevels and navy selection. Bundled fonts: Nunito (MS Sans Serif stand-in) and DejaVu Sans Mono (icons).

```csharp
// Initialize
using Gum.Themes.Retro95;

Retro95Theme.Apply(GraphicsDevice);
```

NuGet packages: `Gum.Themes.Retro95.MonoGame`, `Gum.Themes.Retro95.Kni`

## Fonts and licensing

All bundled fonts are SIL Open Font License or the Bitstream Vera / DejaVu license — both permit redistribution. License files ship inside each NuGet.

## Supported backends

Themes are currently published only for **MonoGame** and **KNI**. If you'd like to see a theme published for **FNA**, **Raylib**, or **Skia**, please open an issue or start a discussion on the [Gum GitHub repo](https://github.com/vchelaru/Gum).

## Requirements

* .NET 8.0+
* MonoGame 3.8+ (for the MonoGame packages) or KNI (for the KNI packages)
* [Gum.MonoGame](https://www.nuget.org/packages/Gum.MonoGame) or [Gum.Kni](https://www.nuget.org/packages/Gum.Kni)
* [KernSmith.MonoGameGum](https://www.nuget.org/packages/KernSmith.MonoGameGum) — optional, only needed when using runtime in-memory font generation
