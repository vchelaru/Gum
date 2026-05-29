# RectangleRuntime

## Introduction

`RectangleRuntime` draws a rectangle with a **fill** and an **outline (stroke)**, plus an optional uniform `CornerRadius` for rounded corners. Its size is controlled by `Width` and `Height`. The fill is set by `FillColor`; the outline is set by `StrokeColor` and `StrokeWidth`. On top of fill and outline, a `RectangleRuntime` can also render a gradient, a drop shadow, and a dashed outline.

A freshly-constructed `RectangleRuntime` renders as a **stroke-only outline** — `FillColor` defaults to transparent, `StrokeColor` defaults to white, and `StrokeWidth` defaults to `1`. Assign a visible `FillColor` to light up the fill, or set `StrokeWidth` to `0` to hide the outline.

For the full property surface — fill, outline, corner radius, gradient, drop shadow, dashed stroke, and the platform/package requirements — see the [Shapes](shapes-apos.shapes.md) page. The examples below cover the common cases.

{% hint style="info" %}
On MonoGame, KNI, and FNA the outline and geometry render out of the box, but the **fill** and the richer effects (gradient, drop shadow, dashed stroke, anti-aliasing) only draw once the `Gum.Shapes.<platform>` package is added — otherwise they are stored and round-trip but silently do not draw. Skia, .NET MAUI, and raylib support the full surface natively. See the [Shapes](shapes-apos.shapes.md) page for setup.
{% endhint %}

### Code Example

The following code creates an outlined `RectangleRuntime`:

```csharp
// Initialize
var rectangle = new RectangleRuntime();
rectangle.Width = 120;
rectangle.Height = 24;
rectangle.StrokeColor = Color.Pink; // This is a Microsoft.Xna.Framework.Color
container.Children.Add(rectangle);
```

<figure><img src="../../.gitbook/assets/WideRectOverBlue.png" alt=""><figcaption><p>Pink outlined rectangle</p></figcaption></figure>

To fill the rectangle and round its corners, assign a visible `FillColor` and a `CornerRadius`. On MonoGame, KNI, and FNA the fill requires the shape support package (see the [Shapes](shapes-apos.shapes.md) page):

```csharp
// Initialize
var rectangle = new RectangleRuntime();
rectangle.Width = 120;
rectangle.Height = 24;
rectangle.CornerRadius = 8;
rectangle.FillColor = Color.Pink; // light up the fill
container.Children.Add(rectangle);
```

## Legacy color members

{% hint style="warning" %}
The `Color`, `Red`, `Green`, `Blue`, and `Alpha` members are obsolete and are being phased out. They write the **stroke** color (`RectangleRuntime` was historically outline-only) and are kept only for backward compatibility. Use `StrokeColor` for the outline and `FillColor` for the fill instead. For the property mapping and the automated code fix, see [Migrating to 2026 June](../../gum-tool/upgrading/migrating-to-2026-june.md).
{% endhint %}
