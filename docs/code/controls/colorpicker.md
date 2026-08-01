# ColorPicker

## Introduction

The ColorPicker control lets the user pick a color by dragging in a saturation/value square and a hue bar. The chosen color is available through `SelectedColor`, and changes raise `SelectedColorChanged`.

The saturation/value square and hue bar are drawn with textures generated at runtime, on every backend the control ships on (the MonoGame family, raylib, and SkiaSharp).

## Code Example: Creating a ColorPicker

The following code creates a ColorPicker and shows the selected color as a hex string in a Label whenever it changes.

```csharp
// Initialize
var label = new Label();
label.AddToRoot();
label.X = 50;
label.Y = 24;

var colorPicker = new ColorPicker();
colorPicker.AddToRoot();
colorPicker.X = 50;
colorPicker.Y = 50;
colorPicker.SelectedColor = System.Drawing.Color.CornflowerBlue;
colorPicker.SelectedColorChanged += (_, _) =>
{
    var color = colorPicker.SelectedColor;
    label.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
};
```

{% hint style="warning" %}
Screenshot pending.
{% endhint %}

## SelectedColor

`SelectedColor` is a `System.Drawing.Color` representing the currently selected color. It is a backend-neutral type (a plain struct with no dependency on any runtime), so assigning it to a runtime visual requires converting to that runtime's own color type:

```csharp
// Initialize
var color = colorPicker.SelectedColor;
// MonoGame example: convert to an XNA color for a rectangle's FillColor
var xnaColor = new Microsoft.Xna.Framework.Color(color.R, color.G, color.B);
```

Setting `SelectedColor` updates the hue/saturation/value state, repositions the indicators, and raises `SelectedColorChanged`.

## Hue, Saturation, and Value

`Hue`, `Saturation`, and `Value` are `float` properties that expose the HSV representation of the selected color, kept in sync with `SelectedColor`:

* `Hue` — 0 to 360
* `Saturation` — 0 to 100
* `Value` — 0 to 100

Setting any of them updates `SelectedColor` and raises `SelectedColorChanged`. Values outside the valid range are clamped.

```csharp
// Initialize
colorPicker.Hue = 120;        // green
colorPicker.Saturation = 100;
colorPicker.Value = 100;
```

## Customizing the Visual

Like all Forms controls, ColorPicker is lookless — its `Visual` can be any element tree, whether the built-in `ColorPickerVisual`, a component authored in the Gum tool, or a custom `InteractiveGue`. The control locates the pieces it drives **by name**, so a custom visual only needs to contain elements with the following names:

| Named child | Expected type | Purpose | Needed? |
|---|---|---|---|
| `SaturationValueContainer` | `InteractiveGue` with events enabled | Drag surface; horizontal position sets saturation, vertical sets value | Required to pick saturation/value |
| `SaturationValueDisplay` | Sprite (e.g. `SpriteRuntime`) | Shows the saturation/value gradient; the control generates its texture | Required to see the square |
| `HueContainer` | `InteractiveGue` with events enabled | Drag surface; vertical position sets hue | Required to pick hue |
| `HueDisplay` | Sprite (e.g. `SpriteRuntime`) | Shows the hue gradient; the control generates its texture | Required to see the hue bar |
| `SaturationValueIndicator` | Any element | Marker moved to the current saturation/value point | Optional |
| `HueIndicator` | Any element | Marker moved to the current hue | Optional |

Every lookup is null-safe, so a missing element never throws — the corresponding feature is simply inactive. Two type requirements matter: the container elements must have events enabled to receive drag input, and the display elements must be sprites for the generated textures to appear.
