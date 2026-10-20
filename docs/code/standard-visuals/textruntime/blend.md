# Blend

## Introduction

`Blend` controls how a TextRuntime combines its colors with whatever is drawn below it. The alpha-only values `ReplaceAlpha` and `MinAlpha` also use the whole bounds of the text as a mask. For what each value does, see the [Blend](../../../gum-tool/gum-elements/general-properties/blend.md) page.

## Example - Masking With Text

Alpha-only blends modify what is below the text, so the text belongs in a container with `IsRenderTarget` set to `true`, drawn after the content it masks:

```csharp
// Initialize
var container = new ContainerRuntime();
container.IsRenderTarget = true;
container.Width = 200;
container.Height = 60;
container.AddToRoot();

var rectangle = new ColoredRectangleRuntime();
rectangle.Width = 200;
rectangle.Height = 60;
container.AddChild(rectangle);

var text = new TextRuntime();
text.Text = "Gum";
text.Blend = Gum.RenderingLibrary.Blend.MinAlpha;
container.AddChild(text);
```

The glyphs keep the alpha of the rectangle, and the rest of the text bounds becomes transparent. The bounds are the measured text block plus a 2 pixel margin, and the drop shadow offset when the text has a drop shadow. Nothing outside the bounds changes.

On MonoGame, KNI, and FNA, text masks its bounds only while it draws into a render target container, and only when text renders character by character, which is the default `TextRenderingMode`.

{% hint style="info" %}
Masking the whole text bounds is available in November 2026, or now if building Gum from source.
{% endhint %}

{% hint style="warning" %}
On MonoGame, KNI, and FNA, glyph boxes that overlap (tight kerning, italics, large outlines) can overwrite each other's alpha under these blends.
{% endhint %}
