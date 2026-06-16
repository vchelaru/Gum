# Applying a Theme

## Introduction

The controls in the previous step use Gum's default visuals, which are intentionally minimal so a project starts with the smallest possible dependency footprint. When you want a more polished look, a **theme** restyles every Gum Forms control with a single line of code — no per-control work, and no changes to the code you have already written.

This page is an optional detour. Themes are additive: everything in the rest of this tutorial works the same whether or not a theme is applied.

## One line restyles every control

A theme ships as a per-backend NuGet package (for example `Gum.Themes.DarkPro.MonoGame`). Install the package, then call the theme's `Apply` method after `GumUI.Initialize` and **before** you create any controls:

```csharp
// Initialize
using Gum.Themes.DarkPro;

GumUI.Initialize(this);
DarkProTheme.Apply(GraphicsDevice);

var mainPanel = new StackPanel();
mainPanel.AddToRoot();
```

Every `Button`, `TextBox`, `CheckBox`, `ListBox`, and other control created after `Apply` now renders in the theme's style. Controls created *before* `Apply` keep the default look, so call it early.

{% hint style="warning" %}
**Screenshot placeholder:** the default (unthemed) controls, for before/after comparison. To be added.
{% endhint %}

<figure><img src="../../../../.gitbook/assets/DarkProThemeScreenshot.png" alt="Controls restyled by the DarkPro theme"><figcaption><p>The same controls, restyled by the DarkPro theme.</p></figcaption></figure>

## Same UI, many looks

A theme is a drop-in swap. Change the package and the `Apply` call and the entire UI changes feel — without touching your control code:

<table><thead><tr><th align="center">Bubblegum</th><th align="center">Neon</th><th align="center">Retro 95</th></tr></thead><tbody><tr><td><img src="../../../../.gitbook/assets/BubblegumThemeScreenshot.png" alt="Bubblegum theme" data-size="original"></td><td><img src="../../../../.gitbook/assets/NeonThemeScreenshot.png" alt="Neon theme" data-size="original"></td><td><img src="../../../../.gitbook/assets/Retro95ThemeScreenshot.png" alt="Retro95 theme" data-size="original"></td></tr></tbody></table>

Gum ships several built-in themes — flat editor chrome, pastel casual, neon cyberpunk, Windows 95 retro, and more — and you can author your own. For the full gallery, usage rules, and the complete list, see the [Themes reference](../../../styling/themes/README.md).

{% hint style="info" %}
Themes use [KernSmith](../../../files-and-fonts/font-strategies.md#dynamic-kernsmith-generation) for dynamic fonts, and most use [Apos.Shapes](../../../standard-visuals/shapes-apos.shapes.md) for vector art — both are pulled in automatically by the theme package. This is why the default look stays dependency-light: you opt in to the extra packages only when you apply a theme.
{% endhint %}

## Conclusion

A single `Apply` call restyles your entire UI, and themes compose with the per-control styling covered later in the tutorials. The next page returns to the default visuals and covers the most common Forms controls.
