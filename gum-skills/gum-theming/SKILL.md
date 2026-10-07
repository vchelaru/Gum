---
name: gum-theming
description: Gum Themes — one call restyles every Forms control (DarkPro, Bubblegum, Neon, Retro95, ...). Triggers: Gum.Themes.* NuGet, DarkProTheme.Apply, XyzStyling.ActiveStyle, picking a UI look, Add Forms theme dropdown, making a new theme.
---

# Gum Themes

A Theme replaces the look of every Forms control at once. Use one when you want a complete,
consistent style. To adjust individual colors or one control, see **gum-styling**. Docs, with
screenshots of every theme: <https://docs.flatredball.com/gum/code/styling/themes>.

## Two ways to get a Theme

| Your project | How |
|---|---|
| Code only (no Gum project) | Install the theme's NuGet package and call `Apply()` |
| Gum project with Forms components | In the Gum tool, **Add Forms** and pick the theme from the dropdown. The look is copied into your `.gumx`, so no theme package is needed. |

Use one path or the other. A project's Forms components replace the package's visuals for the
same control types (see **gum-styling**, "where do your controls' visuals come from").

## Code only: install and apply

Each theme ships one package per backend:
`Gum.Themes.<Name>.MonoGame`, `.Kni`, `.Raylib`, or `.SilkNet`. The theme list is in the docs page
linked above (Editor is meant for tool/editor UI and is not offered by Add Forms).

```csharp
using Gum.Themes.DarkPro;

GumService.Default.Initialize(this, DefaultVisualsVersion.Newest);
DarkProTheme.Apply();      // after Initialize, before creating any control

var button = new Button();
```

**Landmines:**

- **Order matters.** Controls created before `Apply()` keep the default look.
- **Apply one theme per app.** A second `Apply()` mixes leftover state from both.
- On MonoGame, KNI and raylib, the packages bring in KernSmith (font generation), and every theme
  except Editor also brings in Apos.Shapes on MonoGame/KNI. `Apply()` sets both up for you.
- Several themes expect a specific clear color for the area behind the UI (for example
  `NeonStyling.ActiveStyle.Colors.Background`). The docs list it per theme.

## Recoloring a theme

Each theme has its own `<Name>Styling.ActiveStyle` with `Colors` and `Text`. Change them **before**
`Apply()`, the same ordering rule as `Styling.ActiveStyle` in **gum-styling**:

```csharp
DarkProStyling.ActiveStyle.Colors.Accent = new Color(214, 64, 214);
DarkProStyling.ActiveStyle.Text.FontSize = 20;
DarkProTheme.Apply();
```

**Landmine: one color rarely moves the whole look.** Each theme has its own color names, and
controls read different ones (Bubblegum's TextBox uses `Surface1`/`Border`, not `Accent`). Every
theme's `Colors` has `TextPrimary`, `TextMuted`, `Primary` and `Accent`, but on some themes these
are read-only aliases for the theme's own names (Forest Glade's `Accent` returns `LeafBright`).
Copy the theme's "How to customize" block from the docs, which lists the colors that actually
need to change together.

`Text.FontFamily` only selects a font that already exists: the theme's bundled one
(`<Name>Theme.BundledFontFamily`) or a font installed on the machine. It does not load a new font
file.

## Making a new Theme

Clone the Gum repository (<https://github.com/vchelaru/Gum>) and start from
`Themes/Gum.Themes.Template.MonoGame`. The template is a complete, buildable theme meant to be
copied, and the repo contains the shipped themes as examples plus the authoring guide for agents.
