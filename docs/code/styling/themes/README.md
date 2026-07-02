# Themes

A single line of code restyles **every** Gum Forms control. Drop in a built-in theme — or author your own — and the same UI takes on a completely different look without changing any of your control code.

<table><thead><tr><th align="center">DarkPro</th><th align="center">Bubblegum</th><th align="center">Neon</th></tr></thead><tbody><tr><td><img src="../../../.gitbook/assets/DarkProThemeScreenshot.png" alt="DarkPro theme" data-size="original"></td><td><img src="../../../.gitbook/assets/BubblegumThemeScreenshot.png" alt="Bubblegum theme" data-size="original"></td><td><img src="../../../.gitbook/assets/NeonThemeScreenshot.png" alt="Neon theme" data-size="original"></td></tr></tbody></table>

<table><thead><tr><th align="center">Retro 95</th><th align="center">Forest Glade</th><th align="center">Editor</th></tr></thead><tbody><tr><td><img src="../../../.gitbook/assets/Retro95ThemeScreenshot.png" alt="Retro95 theme" data-size="original"></td><td><img src="../../../.gitbook/assets/ForestGladeThemeScreenshot.png" alt="Forest Glade theme" data-size="original"></td><td><img src="../../../.gitbook/assets/EditorThemeScreenshot.png" alt="Editor theme" data-size="original"></td></tr></tbody></table>

The same sample settings panel, rendered by six different themes. The full catalog — with usage for each — is in [Available themes](#available-themes) below.

## Introduction

A **theme** is a per-backend NuGet package that restyles every default Gum Forms control with a single call. After calling `GumService.Default.Initialize(...)`, calling `<Name>Theme.Apply` swaps the default visuals for the theme's visuals — every `Button`, `TextBox`, `CheckBox`, `ComboBox`, etc. created afterward renders in that theme's style.

Themes ship one NuGet per rendering backend (for example `Gum.Themes.DarkPro.MonoGame`, `Gum.Themes.DarkPro.Kni`, and `Gum.Themes.DarkPro.Raylib`) so each package only carries the assets and references it needs.

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

The pattern is the same for every theme — only the `using` namespace, NuGet package, and `Apply` call change. Pick a theme from the sections below, then copy the tab matching your rendering backend for a ready-to-paste install command and initialization snippet.

<!-- MAINTAINER: The MonoGame/KNI tabs and examples show Apply(GraphicsDevice) because the
     stable (May 2026 and earlier) theme packages only expose that overload. From the June 2026
     release on, Apply() is parameterless on every backend. Once June-or-later packages are the
     supported minimum, delete the dated "Apply signature" note in the hint below and simplify
     every Apply(GraphicsDevice) on this page to Apply(). -->

{% hint style="warning" %}
**Raylib theme packages are in preview.** Install them with the `--prerelease` flag, as shown in each theme's **Raylib** tab. The **MonoGame** and **KNI** packages are stable.

**`Apply` signature:** On **Raylib** — and on **MonoGame** / **KNI** packages from **June 2026 and later** — `Apply()` takes no arguments. On **MonoGame** / **KNI** packages from **May 2026 and earlier**, pass the `GraphicsDevice` as the MonoGame and KNI tabs below show: `Apply(GraphicsDevice)`.
{% endhint %}

A few things to keep in mind:

* **Apply one theme per app.** `Apply` mutates the default visuals — calling more than one theme's `Apply` in the same process produces a mix of leftover state.
* **Call `Apply` after `Initialize` and before constructing Forms controls.** Controls capture their visuals at construction time, so any control built before `Apply` will keep the default styling.
* **Themes compose with per-control styling.** For tweaking a single control on top of a theme, see [Code-Only Styling](../code-only-styling/styling-using-activestyles.md) and [Control Customization in Gum Tool](../control-customization-in-gum-tool.md).

### Customizing a theme's colors and fonts

Every theme exposes a mutable `XyzStyling.ActiveStyle` object — its own analog of V3's [`Styling.ActiveStyle`](../code-only-styling/styling-using-activestyles.md), and mutated the same way: set properties on `Colors`/`Text` *before* calling the theme's `Apply()`, not after. Controls created after `Apply()` pick up the change; this is the same "mutate before construct" creation-order rule that page documents for V3's own styling.

```csharp
// Initialize
using Gum.Themes.DarkPro;

DarkProStyling.ActiveStyle.Colors.Accent = Color.Purple;

DarkProTheme.Apply(GraphicsDevice);
```

Every theme's `Colors` exposes the same four guardrail properties — `TextPrimary`, `TextMuted`, `Primary`, and `Accent` — which also flow into V3's own `Styling.ActiveStyle.Colors` for any stock, un-restyled control the theme leaves in place. On some themes these are the theme's real, settable color names directly. On others — where the theme already had its own color vocabulary before the guardrail existed — they're get-only aliases onto a differently named real property; for example Forest Glade's `Colors.Accent` is a read-only alias for `Colors.LeafBright`. Assign the theme's real property in that case; the guardrail alias reflects the change automatically, the same "reactivity is free" behavior as any other derived color. The "How to customize" example under each theme below names the real, settable property to use.

`Text.FontFamily` selects among **already-registered** font families — it doesn't register a new one. Each theme registers its bundled TTFs once, inside `Apply()` / `RegisterBundledFonts()`, under a fixed family name exposed as a `BundledFontFamily` constant (e.g. `DarkProTheme.BundledFontFamily`). Reassigning `Text.FontFamily` only works if the family you name is already registered this way — either one of the theme's own bundled constants, or a font already installed on the host system, such as `"Consolas"` on Windows. KernSmith can resolve an installed system font by name without any explicit registration step, the same as the `Consolas` example on the [Styling Using ActiveStyles](../code-only-styling/styling-using-activestyles.md) page.

## Available themes

### Editor

<figure><img src="../../../.gitbook/assets/EditorThemeScreenshot.png" alt="Editor theme preview"><figcaption><p>The Editor theme applied to a sample settings panel.</p></figcaption></figure>

Flat dark editor/tool chrome — intended for tool and editor interfaces rather than in-game UI. Ships with two extra controls beyond the standard Forms set: `PropertyGridVisual` and `Expander`. See the [Editor Theme](editor-theme.md) page for details on those controls.

{% tabs %}
{% tab title="MonoGame" %}
```bash
dotnet add package Gum.Themes.Editor.MonoGame
```
```csharp
// Initialize
using Gum.Themes.Editor;

EditorTheme.Apply(GraphicsDevice);
```
{% endtab %}
{% tab title="KNI" %}
```bash
dotnet add package Gum.Themes.Editor.Kni
```
```csharp
// Initialize
using Gum.Themes.Editor;

EditorTheme.Apply(GraphicsDevice);
```
{% endtab %}
{% tab title="Raylib" %}
```bash
dotnet add package Gum.Themes.Editor.Raylib --prerelease
```
```csharp
// Initialize
using Gum.Themes.Editor;

EditorTheme.Apply();
```
{% endtab %}
{% endtabs %}

#### How to customize

```csharp
// Initialize
using Gum.Themes.Editor;

EditorStyling.ActiveStyle.Colors.TextPrimary = new Color(210, 225, 255);
EditorStyling.ActiveStyle.Colors.TextMuted = new Color(120, 130, 150);
EditorStyling.ActiveStyle.Colors.Primary = new Color(40, 46, 64);
EditorStyling.ActiveStyle.Colors.Accent = new Color(255, 196, 84);
EditorStyling.ActiveStyle.Colors.Selection = new Color(64, 52, 16);
EditorStyling.ActiveStyle.Text.FontFamily = "Consolas";

EditorTheme.Apply(GraphicsDevice);
```

{% hint style="warning" %}
Screenshot placeholder — replace with an `EditorCustomized` screenshot showing the above customization applied, captured via the Customize checkbox in `MonoGameGumThemesShowcase`.
{% endhint %}

### DarkPro

<figure><img src="../../../.gitbook/assets/DarkProThemeScreenshot.png" alt="DarkPro theme preview"><figcaption><p>The DarkPro theme applied to a sample settings panel.</p></figcaption></figure>

Modern code-editor dark theme with a VS Code / JetBrains feel. Bundled fonts: DM Mono (body) and DejaVu Sans Mono (icons).

{% tabs %}
{% tab title="MonoGame" %}
```bash
dotnet add package Gum.Themes.DarkPro.MonoGame
```
```csharp
// Initialize
using Gum.Themes.DarkPro;

DarkProTheme.Apply(GraphicsDevice);
```
{% endtab %}
{% tab title="KNI" %}
```bash
dotnet add package Gum.Themes.DarkPro.Kni
```
```csharp
// Initialize
using Gum.Themes.DarkPro;

DarkProTheme.Apply(GraphicsDevice);
```
{% endtab %}
{% tab title="Raylib" %}
```bash
dotnet add package Gum.Themes.DarkPro.Raylib --prerelease
```
```csharp
// Initialize
using Gum.Themes.DarkPro;

DarkProTheme.Apply();
```
{% endtab %}
{% endtabs %}

#### How to customize

```csharp
// Initialize
using Gum.Themes.DarkPro;

DarkProStyling.ActiveStyle.Colors.Text = new Color(220, 220, 225);
DarkProStyling.ActiveStyle.Colors.Muted = new Color(150, 150, 160);
DarkProStyling.ActiveStyle.Colors.Surface1 = new Color(30, 34, 42);
DarkProStyling.ActiveStyle.Colors.Accent = new Color(198, 120, 255);
DarkProStyling.ActiveStyle.Colors.AccentDark = new Color(110, 60, 150);
DarkProStyling.ActiveStyle.Text.FontFamily = "Consolas";

DarkProTheme.Apply(GraphicsDevice);
```

{% hint style="warning" %}
Screenshot placeholder — replace with a `DarkProCustomized` screenshot showing the above customization applied, captured via the Customize checkbox in `MonoGameGumThemesShowcase`.
{% endhint %}

### Bubblegum

<figure><img src="../../../.gitbook/assets/BubblegumThemeScreenshot.png" alt="Bubblegum theme preview"><figcaption><p>The Bubblegum theme applied to a sample settings panel.</p></figcaption></figure>

Pastel pink casual-game look with rounded pill buttons and soft drop shadows. Bundled font: Nunito.

{% tabs %}
{% tab title="MonoGame" %}
```bash
dotnet add package Gum.Themes.Bubblegum.MonoGame
```
```csharp
// Initialize
using Gum.Themes.Bubblegum;

BubblegumTheme.Apply(GraphicsDevice);
```
{% endtab %}
{% tab title="KNI" %}
```bash
dotnet add package Gum.Themes.Bubblegum.Kni
```
```csharp
// Initialize
using Gum.Themes.Bubblegum;

BubblegumTheme.Apply(GraphicsDevice);
```
{% endtab %}
{% tab title="Raylib" %}
```bash
dotnet add package Gum.Themes.Bubblegum.Raylib --prerelease
```
```csharp
// Initialize
using Gum.Themes.Bubblegum;

BubblegumTheme.Apply();
```
{% endtab %}
{% endtabs %}

#### How to customize

```csharp
// Initialize
using Gum.Themes.Bubblegum;

BubblegumStyling.ActiveStyle.Colors.Text = new Color(35, 20, 60);
BubblegumStyling.ActiveStyle.Colors.Muted = new Color(150, 120, 190);
BubblegumStyling.ActiveStyle.Colors.Surface1 = new Color(255, 250, 253);
BubblegumStyling.ActiveStyle.Colors.Accent = new Color(120, 200, 255);
BubblegumStyling.ActiveStyle.Colors.AccentLight = new Color(210, 240, 255);
BubblegumStyling.ActiveStyle.Text.FontSize = 16;

BubblegumTheme.Apply(GraphicsDevice);
```

{% hint style="warning" %}
Screenshot placeholder — replace with a `BubblegumCustomized` screenshot showing the above customization applied, captured via the Customize checkbox in `MonoGameGumThemesShowcase`.
{% endhint %}

### Forest Glade

<figure><img src="../../../.gitbook/assets/ForestGladeThemeScreenshot.png" alt="Forest Glade theme preview"><figcaption><p>The Forest Glade theme applied to a sample settings panel.</p></figcaption></figure>

Lush green nature-themed look with gradient leaf-shaped buttons (sharp top-left / bottom-right corners, rounded top-right / bottom-left), a deep canopy background, and soft text drop shadows. Bundled font: Nunito.

{% tabs %}
{% tab title="MonoGame" %}
```bash
dotnet add package Gum.Themes.ForestGlade.MonoGame
```
```csharp
// Initialize
using Gum.Themes.ForestGlade;

ForestGladeTheme.Apply(GraphicsDevice);
```
{% endtab %}
{% tab title="KNI" %}
```bash
dotnet add package Gum.Themes.ForestGlade.Kni
```
```csharp
// Initialize
using Gum.Themes.ForestGlade;

ForestGladeTheme.Apply(GraphicsDevice);
```
{% endtab %}
{% tab title="Raylib" %}
```bash
dotnet add package Gum.Themes.ForestGlade.Raylib --prerelease
```
```csharp
// Initialize
using Gum.Themes.ForestGlade;

ForestGladeTheme.Apply();
```
{% endtab %}
{% endtabs %}

{% hint style="info" %}
For the intended look, clear the back buffer to `ForestGladeStyling.ActiveStyle.Colors.CanopyDeep`.
{% endhint %}

#### How to customize

```csharp
// Initialize
using Gum.Themes.ForestGlade;

ForestGladeStyling.ActiveStyle.Colors.Text = new Color(255, 246, 224);
ForestGladeStyling.ActiveStyle.Colors.Muted = new Color(196, 168, 130);
ForestGladeStyling.ActiveStyle.Colors.CanopyDeep = new Color(48, 24, 10);
ForestGladeStyling.ActiveStyle.Colors.LeafBright = new Color(255, 176, 59);
ForestGladeStyling.ActiveStyle.Colors.ButtonRestFillTop = new Color(230, 140, 40);
ForestGladeStyling.ActiveStyle.Colors.ButtonRestFillBottom = new Color(180, 90, 20);
ForestGladeStyling.ActiveStyle.Text.FontSize = 15;

ForestGladeTheme.Apply(GraphicsDevice);
```

{% hint style="warning" %}
Screenshot placeholder — replace with a `ForestGladeCustomized` screenshot showing the above customization applied, captured via the Customize checkbox in `MonoGameGumThemesShowcase`.
{% endhint %}

### Neon

<figure><img src="../../../.gitbook/assets/NeonThemeScreenshot.png" alt="Neon theme preview"><figcaption><p>The Neon theme applied to a sample settings panel.</p></figcaption></figure>

Cyberpunk / neon-glow dark theme with a saturated cyan accent and a Gaussian glow on focus. Bundled fonts: Share Tech Mono (body), Orbitron (titles), and DejaVu Sans Mono (icons).

{% tabs %}
{% tab title="MonoGame" %}
```bash
dotnet add package Gum.Themes.Neon.MonoGame
```
```csharp
// Initialize
using Gum.Themes.Neon;

NeonTheme.Apply(GraphicsDevice);
```
{% endtab %}
{% tab title="KNI" %}
```bash
dotnet add package Gum.Themes.Neon.Kni
```
```csharp
// Initialize
using Gum.Themes.Neon;

NeonTheme.Apply(GraphicsDevice);
```
{% endtab %}
{% tab title="Raylib" %}
```bash
dotnet add package Gum.Themes.Neon.Raylib --prerelease
```
```csharp
// Initialize
using Gum.Themes.Neon;

NeonTheme.Apply();
```
{% endtab %}
{% endtabs %}

{% hint style="info" %}
For the intended look, clear the back buffer to `NeonStyling.ActiveStyle.Colors.Background` (`#060612`).
{% endhint %}

#### How to customize

```csharp
// Initialize
using Gum.Themes.Neon;

NeonStyling.ActiveStyle.Colors.Text = new Color(255, 224, 250);
NeonStyling.ActiveStyle.Colors.Muted = new Color(128, 80, 128);
NeonStyling.ActiveStyle.Colors.Surface1 = new Color(24, 8, 28);
NeonStyling.ActiveStyle.Colors.Accent = new Color(255, 0, 200);
NeonStyling.ActiveStyle.Colors.Glow = new Color(255, 0, 200);
NeonStyling.ActiveStyle.Text.FontSize = 14;

NeonTheme.Apply(GraphicsDevice);
```

{% hint style="warning" %}
Screenshot placeholder — replace with a `NeonCustomized` screenshot showing the above customization applied, captured via the Customize checkbox in `MonoGameGumThemesShowcase`.
{% endhint %}

### Retro95

<figure><img src="../../../.gitbook/assets/Retro95ThemeScreenshot.png" alt="Retro95 theme preview"><figcaption><p>The Retro95 theme applied to a sample settings panel.</p></figcaption></figure>

Windows 95 "Classic" battleship-gray chrome with raised/sunken bevels and navy selection. Bundled fonts: Nunito (MS Sans Serif stand-in) and DejaVu Sans Mono (icons).

{% tabs %}
{% tab title="MonoGame" %}
```bash
dotnet add package Gum.Themes.Retro95.MonoGame
```
```csharp
// Initialize
using Gum.Themes.Retro95;

Retro95Theme.Apply(GraphicsDevice);
```
{% endtab %}
{% tab title="KNI" %}
```bash
dotnet add package Gum.Themes.Retro95.Kni
```
```csharp
// Initialize
using Gum.Themes.Retro95;

Retro95Theme.Apply(GraphicsDevice);
```
{% endtab %}
{% tab title="Raylib" %}
```bash
dotnet add package Gum.Themes.Retro95.Raylib --prerelease
```
```csharp
// Initialize
using Gum.Themes.Retro95;

Retro95Theme.Apply();
```
{% endtab %}
{% endtabs %}

#### How to customize

```csharp
// Initialize
using Gum.Themes.Retro95;

Retro95Styling.ActiveStyle.Colors.Text = new Color(0, 0, 0);
Retro95Styling.ActiveStyle.Colors.DisabledText = new Color(110, 110, 110);
Retro95Styling.ActiveStyle.Colors.Surface = new Color(0, 128, 128);
Retro95Styling.ActiveStyle.Colors.Selection = new Color(128, 0, 0);
Retro95Styling.ActiveStyle.Colors.HighlightOuter = new Color(200, 255, 255);
Retro95Styling.ActiveStyle.Text.FontSize = 13;

Retro95Theme.Apply(GraphicsDevice);
```

{% hint style="warning" %}
Screenshot placeholder — replace with a `Retro95Customized` screenshot showing the above customization applied, captured via the Customize checkbox in `MonoGameGumThemesShowcase`.
{% endhint %}

### Meadow

<figure><img src="../../../.gitbook/assets/31_19 14 05.png" alt=""><figcaption><p>The Meadow theme applied to a sample settings panel.</p></figcaption></figure>

Cozy cottagecore look with chunky sky-blue pill buttons (flat "stacked card" drop shadow), dashed-outline cream panels, sage selection accents, and coral sliders. Bundled fonts: Baloo 2 (display), Quicksand (body), and DejaVu Sans Mono (icons).

{% tabs %}
{% tab title="MonoGame" %}
```bash
dotnet add package Gum.Themes.Meadow.MonoGame
```
```csharp
// Initialize
using Gum.Themes.Meadow;

MeadowTheme.Apply(GraphicsDevice);
```
{% endtab %}
{% tab title="KNI" %}
```bash
dotnet add package Gum.Themes.Meadow.Kni
```
```csharp
// Initialize
using Gum.Themes.Meadow;

MeadowTheme.Apply(GraphicsDevice);
```
{% endtab %}
{% tab title="Raylib" %}
```bash
dotnet add package Gum.Themes.Meadow.Raylib --prerelease
```
```csharp
// Initialize
using Gum.Themes.Meadow;

MeadowTheme.Apply();
```
{% endtab %}
{% endtabs %}

{% hint style="info" %}
For the intended look, clear the back buffer to `MeadowStyling.ActiveStyle.Colors.Cream` (`#F7EDD6`).
{% endhint %}

#### How to customize

```csharp
// Initialize
using Gum.Themes.Meadow;

MeadowStyling.ActiveStyle.Colors.TealDark = new Color(20, 80, 70);
MeadowStyling.ActiveStyle.Colors.Muted = new Color(150, 130, 110);
MeadowStyling.ActiveStyle.Colors.Cream2 = new Color(255, 248, 235);
MeadowStyling.ActiveStyle.Colors.Blue = new Color(237, 154, 120);
MeadowStyling.ActiveStyle.Colors.Coral = new Color(70, 173, 230);
MeadowStyling.ActiveStyle.Text.FontSize = 16;

MeadowTheme.Apply(GraphicsDevice);
```

{% hint style="warning" %}
Screenshot placeholder — replace with a `MeadowCustomized` screenshot showing the above customization applied, captured via the Customize checkbox in `MonoGameGumThemesShowcase`.
{% endhint %}

### Hazard

<figure><img src="../../../.gitbook/assets/31_19 13 36.png" alt=""><figcaption><p>The Hazard theme applied to a sample settings panel.</p></figcaption></figure>

Industrial space-salvage HUD inspired by Hardspace: Shipbreaker — signature hazard-yellow on warm near-black, muted-gold borders, and square-cornered chrome. Pressing a button flashes the full hazard-yellow accent with black text. Bundled fonts: Saira Condensed (body and labels) and DejaVu Sans Mono (icons).

{% tabs %}
{% tab title="MonoGame" %}
```bash
dotnet add package Gum.Themes.Hazard.MonoGame
```
```csharp
// Initialize
using Gum.Themes.Hazard;

HazardTheme.Apply(GraphicsDevice);
```
{% endtab %}
{% tab title="KNI" %}
```bash
dotnet add package Gum.Themes.Hazard.Kni
```
```csharp
// Initialize
using Gum.Themes.Hazard;

HazardTheme.Apply(GraphicsDevice);
```
{% endtab %}
{% tab title="Raylib" %}
```bash
dotnet add package Gum.Themes.Hazard.Raylib --prerelease
```
```csharp
// Initialize
using Gum.Themes.Hazard;

HazardTheme.Apply();
```
{% endtab %}
{% endtabs %}

{% hint style="info" %}
For the intended look, clear the back buffer to `HazardStyling.ActiveStyle.Colors.Background` (`#0A0A08`).
{% endhint %}

#### How to customize

```csharp
// Initialize
using Gum.Themes.Hazard;

HazardStyling.ActiveStyle.Colors.Text = new Color(227, 100, 40);
HazardStyling.ActiveStyle.Colors.Muted = new Color(120, 70, 38);
HazardStyling.ActiveStyle.Colors.Surface1 = new Color(18, 16, 7);
HazardStyling.ActiveStyle.Colors.Accent = new Color(244, 90, 26);
HazardStyling.ActiveStyle.Colors.TextBright = new Color(255, 140, 59);
HazardStyling.ActiveStyle.Text.FontSize = 16;

HazardTheme.Apply(GraphicsDevice);
```

{% hint style="warning" %}
Screenshot placeholder — replace with a `HazardCustomized` screenshot showing the above customization applied, captured via the Customize checkbox in `MonoGameGumThemesShowcase`.
{% endhint %}

## Fonts and licensing

All bundled fonts are SIL Open Font License or the Bitstream Vera / DejaVu license — both permit redistribution. License files ship inside each NuGet.

## Supported backends

Themes are published for **MonoGame**, **KNI**, and **Raylib** (Raylib in preview — see the note under [Usage](#usage)). If you'd like to see a theme published for **FNA** or **Skia**, please open an issue or start a discussion on the [Gum GitHub repo](https://github.com/vchelaru/Gum).

## Requirements

* .NET 8.0+
* MonoGame 3.8+ (for the MonoGame packages), KNI (for the KNI packages), or Raylib (for the Raylib packages)
* [Gum.MonoGame](https://www.nuget.org/packages/Gum.MonoGame), [Gum.Kni](https://www.nuget.org/packages/Gum.Kni), or [Gum.raylib](https://www.nuget.org/packages/Gum.raylib)
* [KernSmith.MonoGameGum](https://www.nuget.org/packages/KernSmith.MonoGameGum) — optional, only needed when using runtime in-memory font generation
