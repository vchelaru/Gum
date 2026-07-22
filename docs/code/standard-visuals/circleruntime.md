# CircleRuntime

## Introduction

`CircleRuntime` draws a circle with a **fill** and an **outline (stroke)**. The circle is sized to fit within its `Width` × `Height` bounds. The fill is set by `FillColor`; the outline is set by `StrokeColor` and `StrokeWidth`. On top of fill and outline, a `CircleRuntime` can also render a gradient, a drop shadow, and a dashed outline.

A freshly-constructed `CircleRuntime` is 32 × 32 and renders as a **stroke-only outline** — `IsFilled` defaults to `false`, so the fill is gated off even though `FillColor` defaults to opaque white. `StrokeColor` defaults to white and `StrokeWidth` defaults to `1`. Set `IsFilled = true` to show the fill (assigning `FillColor` alone does not show it), or set `StrokeWidth` to `0` to hide the outline.

`IsFilled` defaults to `false` because `CircleRuntime` is historically outline-only. It pairs with an opaque white `FillColor`, so setting `IsFilled = true` alone yields a visible white fill — see [Shapes](shapes-apos.shapes.md#fill) for the full rationale.

For the full property surface — fill, outline, gradient, drop shadow, dashed stroke, and the platform/package requirements — see the [Shapes](shapes-apos.shapes.md#circleruntime-and-rectangleruntime) page. The examples below cover the common cases.

{% hint style="info" %}
A `CircleRuntime` also exposes a `Radius` property, but sizing through `Width` / `Height` is recommended — it keeps the circle consistent with every other visual and participates in the layout system. Setting `Radius` simply sets `Width` and `Height` to `Radius × 2`.
{% endhint %}

{% hint style="info" %}
On MonoGame, KNI, and FNA the outline and geometry render out of the box, but the **fill** and the richer effects (gradient, drop shadow, dashed stroke, anti-aliasing) only draw once the `Gum.Shapes.<platform>` package is added — otherwise they are stored and round-trip but silently do not draw. See the [Shapes](shapes-apos.shapes.md) page for setup.

Skia, .NET MAUI, raylib, and Silk.NET support the full surface natively — no additional package needed.
{% endhint %}

### Code Example

The following code creates an outlined `CircleRuntime`:

```csharp
// Initialize
var circle = new CircleRuntime();
circle.Width = 128;
circle.Height = 128;
circle.StrokeColor = Color.Green; // This is a Microsoft.Xna.Framework.Color
container.Children.Add(circle);
```

<figure><img src="../../.gitbook/assets/07_06 14 58.png" alt=""><figcaption><p>Green outlined circle</p></figcaption></figure>

To fill the circle, set `IsFilled = true` and assign a `FillColor`. On MonoGame, KNI, and FNA the fill requires the shape support package (see the [Shapes](shapes-apos.shapes.md) page):

```csharp
// Initialize
var circle = new CircleRuntime();
circle.Width = 128;
circle.Height = 128;
circle.IsFilled = true;
circle.FillColor = Color.Green; // show the fill
container.Children.Add(circle);
```

## Legacy color members

{% hint style="warning" %}
The `Color`, `Red`, `Green`, `Blue`, and `Alpha` members are obsolete and are being phased out. They write the **stroke** color (`CircleRuntime` was historically outline-only) and are kept only for backward compatibility. Use `StrokeColor` for the outline and `FillColor` for the fill instead. For the property mapping and the automated code fix, see [Migrating to 2026 May](../../gum-tool/upgrading/migrating-to-2026-may.md).
{% endhint %}
