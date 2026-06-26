# Shapes

## Introduction

GumUI supports rendering vector shapes as visuals. The two primary shape runtimes are:

* `CircleRuntime` — a circle sized to fit within its `Width` × `Height` bounds.
* `RectangleRuntime` — a rectangle with an optional uniform or per-corner `CornerRadius`.

Each shape has a **fill** and an **outline (stroke)**. The fill is controlled by `FillColor` (and `IsFilled`); the outline is controlled by `StrokeColor`, `StrokeWidth`, and `StrokeWidthUnits`. On top of fill and outline, shapes can render a gradient, a drop shadow, and a dashed outline.

A third shape, `ArcRuntime`, draws a stroked circular arc. It has no direct `CircleRuntime` / `RectangleRuntime` equivalent and remains a current, supported shape.

{% hint style="info" %}
The fill + outline `CircleRuntime` / `RectangleRuntime` surface described on this page ships in the May 2026 release. Earlier releases used separate `ColoredCircleRuntime` and `RoundedRectangleRuntime` types (documented further down), which are now obsolete shims.
{% endhint %}

## Adding Shape Support (Recommended)

On MonoGame, KNI, and FNA, an outlined shape (`StrokeColor`, `StrokeWidth`, `StrokeWidthUnits`) and the geometry properties (`Width`, `Height`, `Radius`, `CornerRadius`) render out of the box with no extra package. The fill and the richer effects require the shape support package for your platform (the `Gum.Shapes.*` package, which uses Apos.Shapes under the hood). We recommend installing it for most projects so that fill, gradient, drop shadow, dashed stroke, and anti-aliasing all draw. Without it, `FillColor` (and the fill channels), gradient (`UseGradient`), drop shadow (`HasDropshadow`), dashed stroke (`StrokeDashLength` / `StrokeGapLength`), anti-aliasing (`IsAntialiased`), and `Blend` are stored and round-trip but silently do not draw — nothing throws.

{% tabs %}
{% tab title="MonoGame" %}
The Gum.Shapes.MonoGame NuGet package adds fill and effect support for shapes. Add the following NuGet package:

[https://www.nuget.org/packages/Gum.Shapes.MonoGame](https://www.nuget.org/packages/Gum.Shapes.MonoGame)

Modify csproj:

```xml
<PackageReference Include="Gum.Shapes.MonoGame" Version="*" />
```

Or add through command line:

```bash
dotnet add package Gum.Shapes.MonoGame
```
{% endtab %}

{% tab title="KNI" %}
The Gum.Shapes.KNI NuGet package adds fill and effect support for shapes. Add the following NuGet package:

[https://www.nuget.org/packages/Gum.Shapes.KNI](https://www.nuget.org/packages/Gum.Shapes.KNI)

Modify csproj:

```xml
<PackageReference Include="Gum.Shapes.KNI" Version="*" />
```

Or add through command line:

```bash
dotnet add package Gum.Shapes.KNI
```

{% hint style="warning" %}
Apos.Shapes compiles a shader at compile time which is used by the library to draw shapes. As of January 2026 shader compilation is not supported on Linux for KNI. Therefore, libraries must be compiled on Windows.

For more information, see this issue: [https://github.com/vchelaru/Gum/issues/2034](https://github.com/vchelaru/Gum/issues/2034)
{% endhint %}
{% endtab %}

{% tab title="FNA" %}
There is no shape support NuGet package for FNA. An outlined `Circle` or `Rectangle` renders without any package (`StrokeColor`, `StrokeWidth`, and geometry all work), but fill and the richer effects are currently available on MonoGame and KNI only.
{% endtab %}

{% tab title=".NET MAUI" %}
No additional setup is required to use shapes in .NET MAUI. The full fill, outline, gradient, drop shadow, dashed-stroke, and corner-radius surface is supported natively.
{% endtab %}

{% tab title="raylib" %}
No additional setup is required to use shapes on raylib. The full surface — fill, outline (stroke), gradient, drop shadow, dashed stroke, and corner-radius — is built into `CircleRuntime` and `RectangleRuntime` natively; no extra NuGet package or initialization is needed.
{% endtab %}

{% tab title="Silk.NET" %}
No additional setup is required to use shapes in Silk.NET.
{% endtab %}
{% endtabs %}

## Setup in Code

{% tabs %}
{% tab title="MonoGame / KNI / FNA" %}
Whether you are using code-only or the Gum tool, add the following line of code in your Initialize method, after `GumUI.Initialize(...)`:

```csharp
// Initialize
GumUI.Initialize(...);
ShapeRenderer.Self.Initialize();
```

{% hint style="info" %}
**December 2025 and earlier** used a different signature that took the graphics device and content manager: `ShapeRenderer.Self.Initialize(GraphicsDevice, Content);`. From January 2026 on, the parameterless `Initialize()` is the form to use.
{% endhint %}
{% endtab %}

{% tab title=".NET MAUI" %}
No additional setup is needed if you have already added SkiaSharp and Gum to your project. For more information see the [.NET Maui Initializing Gum](../getting-started/setup/adding-initializing-gum/.net-maui.md) page.
{% endtab %}

{% tab title="raylib" %}
No additional setup is required. The full surface — fill, outline (stroke), gradient, drop shadow, dashed stroke, and corner-radius — is built into `CircleRuntime` and `RectangleRuntime` natively — see those pages for their full property surface.
{% endtab %}

{% tab title="Silk.NET" %}
No additional setup is needed if you have already added Gum to your project. For more information see the [Silk.NET Initializing Gum](../getting-started/setup/adding-initializing-gum/silk.net.md) page.
{% endtab %}
{% endtabs %}

### Code Example: Rendering Shapes in Code

The following code shows how to add shapes to a MonoGame project:

```csharp
public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    GumService GumUI => GumService.Default;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.GraphicsProfile = GraphicsProfile.HiDef;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        GumUI.Initialize(this);
        // Initialize shape renderer after GumUI:
        ShapeRenderer.Self.Initialize();

        var circle = new CircleRuntime();
        circle.AddToRoot();
        circle.Width = 100;
        circle.Height = 100;
        circle.IsFilled = true;
        circle.FillColor = Color.Red;

        var rectangle = new RectangleRuntime();
        rectangle.AddToRoot();
        rectangle.X = 100;
        rectangle.CornerRadius = 15;
        rectangle.IsFilled = true; // FillColor is the gradient start, so show the fill
        rectangle.FillColor = Color.Blue;
        rectangle.UseGradient = true;
        rectangle.Color2 = Color.Green;

        var arc = new ArcRuntime();
        arc.AddToRoot();
        arc.X = 200;
        arc.Color = Color.Purple;
        arc.Thickness = 20;
        arc.StartAngle = 0;
        arc.SweepAngle = 270;

        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        GumUI.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        GumUI.Draw();
        base.Draw(gameTime);
    }
}
```

<figure><img src="../../.gitbook/assets/06_05 57 11.png" alt=""><figcaption><p>Shapes rendered in an otherwise empty project</p></figcaption></figure>

## CircleRuntime and RectangleRuntime

`CircleRuntime` and `RectangleRuntime` are the current shape runtimes. Each has a **fill** and an **outline (stroke)**, plus optional gradient, drop shadow, dashed-outline, and anti-aliasing effects. `RectangleRuntime` adds rounded-corner support.

A freshly-constructed shape renders as a **stroke-only outline**: `IsFilled` defaults to `false`, so the fill is gated off even though `FillColor` defaults to opaque white. `StrokeColor` defaults to white and `StrokeWidth` defaults to `1`. Set `IsFilled = true` to show the fill (assigning `FillColor` alone does not show it), or set `StrokeWidth` to `0` to hide the outline. A `CircleRuntime` is 32 × 32 by default; a `RectangleRuntime` is 50 × 50.

{% hint style="info" %}
On MonoGame, KNI, and FNA the outline and geometry render without the shapes package, but the fill and effects (gradient, drop shadow, dashed stroke, anti-aliasing, `Blend`) only draw once the `Gum.Shapes.<platform>` package is added — otherwise the values round-trip but silently do not draw. Skia and .NET MAUI support the full surface natively; raylib supports a near-full subset (see [Shape Support Across Platforms](../../gum-tool/gum-elements/skia-standard-elements/shapes-platform-support.md)).
{% endhint %}

The property tables in this section describe both runtimes. Properties marked **Rectangle only** do not exist on `CircleRuntime`.

### Geometry

| Property | Type | Description |
| --- | --- | --- |
| `Width`, `Height` | `float` | Size of the shape. A `CircleRuntime` is drawn to fit within these bounds. (`Width` / `Height` use the standard `WidthUnits` / `HeightUnits` from [GraphicalUiElement](../gum-code-reference/graphicaluielement/README.md), like any other visual.) |
| `CornerRadius` | `float` | **Rectangle only.** Uniform corner radius, in pixels. `0` produces square corners. |
| `CornerRadiusUnits` | `DimensionUnitType` | **Rectangle only.** How `CornerRadius` (and the per-corner overrides) are interpreted. `Absolute` is in pixels; `ScreenPixel` divides by the camera zoom each frame so corners hold a constant on-screen size. |
| `CustomRadiusTopLeft`, `CustomRadiusTopRight`, `CustomRadiusBottomLeft`, `CustomRadiusBottomRight` | `float?` | **Rectangle only.** Per-corner radius overrides. When non-null, that corner uses the override; when `null` (the default), the corner falls back to `CornerRadius`. Lets a single rectangle render asymmetric corners. |

{% hint style="info" %}
A `CircleRuntime` also exposes a `Radius` property, but sizing through `Width` / `Height` is recommended — it keeps shapes consistent with every other visual and participates in the layout system. Setting `Radius` simply sets `Width` and `Height` to `Radius × 2`.
{% endhint %}

### Fill

| Property | Type | Description |
| --- | --- | --- |
| `FillColor` | `Color` | The fill color. Defaults to opaque white, but the fill is gated off because `IsFilled` defaults to `false` — so a freshly-constructed shape renders as an outline only. Set `IsFilled = true` to render the fill. |
| `FillRed`, `FillGreen`, `FillBlue`, `FillAlpha` | `int` | Individual fill channels (0–255). |
| `IsFilled` | `bool` | The canonical fill gate. Defaults to `false`, so a freshly-constructed shape is outline-only; set it to `true` to render the fill. Assigning `FillColor` alone does not show the fill. |

{% hint style="info" %}
**Why `IsFilled` defaults to `false`:** `CircleRuntime` and `RectangleRuntime` are historically outline-only, so a freshly-constructed shape stays a stroke-only outline for visual back-compat. The default pairs `IsFilled = false` with an **opaque white** `FillColor` — so flipping `IsFilled = true` alone yields a visible white fill with no need to also assign a color. (The earlier pairing of `IsFilled = true` with a *transparent* `FillColor` was a footgun: setting `IsFilled = true` left the shape invisible until you also assigned a `FillColor`.)
{% endhint %}

### Outline (stroke)

| Property | Type | Description |
| --- | --- | --- |
| `StrokeColor` | `Color` | The outline color. Defaults to white. |
| `StrokeRed`, `StrokeGreen`, `StrokeBlue`, `StrokeAlpha` | `int` | Individual stroke channels (0–255). |
| `StrokeWidth` | `float` | Outline thickness. Defaults to `1`. `0` hides the outline. |
| `StrokeWidthUnits` | `DimensionUnitType` | How `StrokeWidth` is interpreted. `Absolute` is in world pixels; `ScreenPixel` divides by the camera zoom each frame so the outline holds a constant on-screen size regardless of zoom. |

### Gradient

When `UseGradient` is `true`, the shape is filled with a gradient between the shape's fill/stroke color and `Color2` instead of a solid color. The gradient **start** color is the shape's active body color — `FillColor` when `IsFilled` is `true`, or `StrokeColor` when the shape is outline-only — and `Color2` is the **end** color. The endpoints (`GradientX1`/`GradientY1` and `GradientX2`/`GradientY2`) define the gradient vector for `Linear` gradients; for `Radial` gradients only the start point and the radius properties are used.

| Property | Type | Description |
| --- | --- | --- |
| `UseGradient` | `bool` | If `true`, the gradient properties drive the fill. If `false`, the solid `FillColor` is used. |
| `GradientType` | `GradientType` | `Linear` (the default) or `Radial`. |
| `Color2` | `Color` | The second (end) gradient color. The start color is the shape's `FillColor` (or `StrokeColor` when outline-only). Shortcut for setting `Red2`, `Green2`, `Blue2`, and `Alpha2`. |
| `Red2`, `Green2`, `Blue2`, `Alpha2` | `int` | Channels for the second gradient color (0–255). |
| `GradientX1`, `GradientY1` | `float` | Coordinates of the gradient start point. Interpretation depends on the matching `Units` value. |
| `GradientX1Units`, `GradientY1Units` | `GeneralUnitType` | Coordinate system used to interpret `GradientX1` / `GradientY1`. |
| `GradientX2`, `GradientY2` | `float` | Coordinates of the gradient end point (`Linear` gradients only). |
| `GradientX2Units`, `GradientY2Units` | `GeneralUnitType` | Coordinate system used to interpret `GradientX2` / `GradientY2`. |
| `GradientInnerRadius` | `float` | Inner radius (`Radial` gradients only). Inside this radius the shape is filled with the start color. |
| `GradientInnerRadiusUnits` | `DimensionUnitType` | Unit type for `GradientInnerRadius`. `Absolute` (pixels), `PercentageOfParent` (percentage of the shape's `Width`, so `100` = `Width`), or `RelativeToParent` (pixel offset from the shape's `Width`, so `0` = `Width` and `-10` = `Width − 10`). The shape's natural inscribed radius is `Width / 2` — to fit a circle inside the shape, use `50` (`PercentageOfParent`) or `-Width/2` (`RelativeToParent`). |
| `GradientOuterRadius` | `float` | Outer radius at which the gradient has fully blended to `Color2` (`Radial` gradients only). |
| `GradientOuterRadiusUnits` | `DimensionUnitType` | Unit type for `GradientOuterRadius`. Same options as `GradientInnerRadiusUnits`. |

### Drop shadow

A shape can render a drop shadow behind itself. The shadow is only drawn when `HasDropshadow` is `true`. `DropshadowOffsetX` / `DropshadowOffsetY` position the shadow relative to the shape, and `DropshadowBlur` controls how soft the edges are.

| Property | Type | Description |
| --- | --- | --- |
| `HasDropshadow` | `bool` | Whether the drop shadow is rendered. |
| `DropshadowColor` | `Color` | The drop shadow color. Shortcut for the four channel properties below. |
| `DropshadowRed`, `DropshadowGreen`, `DropshadowBlue`, `DropshadowAlpha` | `int` | Drop shadow color channels (0–255). |
| `DropshadowOffsetX`, `DropshadowOffsetY` | `float` | Horizontal / vertical offset of the shadow, in pixels. |
| `DropshadowBlur` | `float` | Blur radius, in pixels. `0` is a sharp shadow; larger values soften the edges. This is a single scalar (matching CSS `box-shadow` blur), not a per-axis pair. |

### Dashed stroke

Setting both `StrokeDashLength` and `StrokeGapLength` to non-zero values renders the outline as a dashed line instead of a solid one.

| Property | Type | Description |
| --- | --- | --- |
| `StrokeDashLength` | `float` | Length of each dash segment, in `StrokeWidthUnits`. `0` (the default) produces a solid stroke. |
| `StrokeGapLength` | `float` | Length of each gap between dashes, in `StrokeWidthUnits`. Ignored when `StrokeDashLength` is `0`. |

{% hint style="info" %}
Both `StrokeDashLength` and `StrokeGapLength` must be greater than `0` for dashed rendering to engage. The dash perimeter walk assumes uniform corners — dashed strokes are not aware of the per-corner `CustomRadius*` overrides.
{% endhint %}

### Anti-aliasing and blend

| Property | Type | Description |
| --- | --- | --- |
| `IsAntialiased` | `bool` | When `true` (the default) the shape's edge is anti-aliased. Set it to `false` for crisp, hard-edged rasterization — useful for pixel-art / retro themes and hairline borders. |
| `Blend` | `Gum.RenderingLibrary.Blend` | Blend mode used when drawing the shape. Defaults to `Normal`. Other values: `Additive`, `Replace`, `SubtractAlpha`, `ReplaceAlpha`, `MinAlpha`. |

### Examples

A filled circle with a soft drop shadow:

```csharp
// Initialize
var circle = new CircleRuntime();
circle.Width = 100;
circle.Height = 100;
circle.IsFilled = true;
circle.FillColor = Color.Red;
circle.HasDropshadow = true;
circle.DropshadowColor = Color.Black;
circle.DropshadowOffsetX = 4;
circle.DropshadowOffsetY = 4;
circle.DropshadowBlur = 6;
container.Children.Add(circle);
```

A rounded rectangle filled with a linear gradient:

```csharp
// Initialize
var rectangle = new RectangleRuntime();
rectangle.Width = 200;
rectangle.Height = 80;
rectangle.CornerRadius = 12;
rectangle.IsFilled = true; // FillColor is the gradient start, so show the fill
rectangle.FillColor = Color.Blue;
rectangle.UseGradient = true;
rectangle.Color2 = Color.Green;
container.Children.Add(rectangle);
```

A dashed, stroke-only rectangle with asymmetric corners:

```csharp
// Initialize
var rectangle = new RectangleRuntime();
rectangle.Width = 160;
rectangle.Height = 48;
rectangle.IsFilled = false;          // outline only
rectangle.StrokeColor = Color.White;
rectangle.StrokeWidth = 2;
rectangle.StrokeDashLength = 8;
rectangle.StrokeGapLength = 4;
rectangle.CornerRadius = 4;
rectangle.CustomRadiusTopRight = 16; // override a single corner
container.Children.Add(rectangle);
```

## Obsolete shape runtimes

{% hint style="info" %}
**`ColoredCircleRuntime`, `RoundedRectangleRuntime`, `ColoredRectangleRuntime`, and `SolidRectangleRuntime` are obsolete shims being phased out.** Use `CircleRuntime` (for `ColoredCircleRuntime`) and `RectangleRuntime` (for the rectangle variants, with `CornerRadius` covering `RoundedRectangleRuntime`) instead. For the property mapping and the automated code fix, see [Migrating to 2026 May](../../gum-tool/upgrading/migrating-to-2026-may.md). `ArcRuntime` is **not** obsolete — it has no `CircleRuntime` / `RectangleRuntime` equivalent and remains the way to draw a circular arc.
{% endhint %}

The property tables below document the shared surface of the obsolete shims and `ArcRuntime`, which derive from `AposShapeRuntime`. They use older names that differ from the current shapes — for example `Color` (the solid fill) instead of `FillColor`, and per-axis `DropshadowBlurX` / `DropshadowBlurY` instead of the scalar `DropshadowBlur`. For the current `CircleRuntime` / `RectangleRuntime` surface, use the [CircleRuntime and RectangleRuntime](#circleruntime-and-rectangleruntime) tables above.

### Common Properties

These properties are available on every shape that derives from `AposShapeRuntime` (the obsolete shims and `ArcRuntime`).

#### Solid Color

The solid color is used when `UseGradient` is `false`. Setting `Color` is a shortcut for setting `Red`, `Green`, `Blue`, and `Alpha` together.

| Property | Type    | Description                                                                                |
| -------- | ------- | ------------------------------------------------------------------------------------------ |
| `Color`  | `Color` | The fill color used when `UseGradient` is `false`.                                         |
| `Red`    | `int`   | Red channel (0–255). Ignored when `UseGradient` is `true`.                                 |
| `Green`  | `int`   | Green channel (0–255). Ignored when `UseGradient` is `true`.                               |
| `Blue`   | `int`   | Blue channel (0–255). Ignored when `UseGradient` is `true`.                                |
| `Alpha`  | `int`   | Alpha/opacity (0–255). Ignored when `UseGradient` is `true`.                               |

#### Gradient

When `UseGradient` is `true`, the shape is filled with a gradient between `Color1` and `Color2` instead of the solid `Color`. The gradient endpoints (`GradientX1/Y1` and `GradientX2/Y2`) define the gradient vector for `Linear` gradients; for `Radial` gradients only the start point and the radius properties are used.

| Property                                    | Type                | Description                                                                                                       |
| ------------------------------------------- | ------------------- | ----------------------------------------------------------------------------------------------------------------- |
| `UseGradient`                               | `bool`              | If `true`, the gradient color properties are used. If `false`, the solid color is used.                           |
| `GradientType`                              | `GradientType`      | The type of gradient. Defaults to `Linear`. Other option: `Radial`.                                               |
| `Color1`                                    | `Color`             | The first gradient color. Shortcut for setting `Red1`, `Green1`, `Blue1`, and `Alpha1`.                           |
| `Red1`, `Green1`, `Blue1`, `Alpha1`         | `int`               | Channels for the first gradient color (0–255).                                                                    |
| `Color2`                                    | `Color`             | The second gradient color. Shortcut for setting `Red2`, `Green2`, `Blue2`, and `Alpha2`.                          |
| `Red2`, `Green2`, `Blue2`, `Alpha2`         | `int`               | Channels for the second gradient color (0–255).                                                                   |
| `GradientX1`, `GradientY1`                  | `float`             | Coordinates of the gradient start point. Interpretation depends on the matching `Units` value.                    |
| `GradientX1Units`, `GradientY1Units`        | `GeneralUnitType`   | Coordinate system used to interpret `GradientX1` / `GradientY1`.                                                  |
| `GradientX2`, `GradientY2`                  | `float`             | Coordinates of the gradient end point (Linear gradients only).                                                    |
| `GradientX2Units`, `GradientY2Units`        | `GeneralUnitType`   | Coordinate system used to interpret `GradientX2` / `GradientY2`.                                                  |
| `GradientInnerRadius`                       | `float`             | Inner radius (Radial gradients only). Inside this radius the shape is filled with `Color1`.                       |
| `GradientInnerRadiusUnits`                  | `DimensionUnitType` | Unit type for `GradientInnerRadius`. Supported values: `Absolute` (pixels), `PercentageOfParent` (percentage of the shape's `Width`, so `100` = `Width`), and `RelativeToParent` (pixels offset from the shape's `Width`, so `0` = `Width` and `-10` = `Width − 10`). Note that the shape's natural inscribed radius is `Width / 2` — to fit a circle inside the shape, use `50` (`PercentageOfParent`) or `-Width/2` (`RelativeToParent`). |
| `GradientOuterRadius`                       | `float`             | Outer radius at which the gradient has fully blended to `Color2` (Radial gradients only).                         |
| `GradientOuterRadiusUnits`                  | `DimensionUnitType` | Unit type for `GradientOuterRadius`. Supported values: `Absolute` (pixels), `PercentageOfParent` (percentage of the shape's `Width`, so `100` = `Width`), and `RelativeToParent` (pixels offset from the shape's `Width`, so `0` = `Width` and `-10` = `Width − 10`). Note that the shape's natural inscribed radius is `Width / 2` — to fit a circle inside the shape, use `50` (`PercentageOfParent`) or `-Width/2` (`RelativeToParent`). |

#### Drop Shadow

A shape can render a drop shadow behind itself. The shadow is only drawn when `HasDropshadow` is `true`. `DropshadowOffsetX/Y` controls the position of the shadow relative to the shape, and `DropshadowBlurX/Y` controls how soft the shadow edges are (0 = sharp).

| Property                                                                          | Type    | Description                                                                                |
| --------------------------------------------------------------------------------- | ------- | ------------------------------------------------------------------------------------------ |
| `HasDropshadow`                                                                   | `bool`  | Whether the drop shadow is rendered.                                                       |
| `DropshadowColor`                                                                 | `Color` | Drop shadow color. Shortcut for setting the four `Dropshadow*` channel properties.         |
| `DropshadowRed`, `DropshadowGreen`, `DropshadowBlue`, `DropshadowAlpha`           | `int`   | Drop shadow color channels (0–255).                                                        |
| `DropshadowOffsetX`, `DropshadowOffsetY`                                          | `float` | Horizontal/vertical offset of the shadow, in pixels.                                       |
| `DropshadowBlurX`, `DropshadowBlurY`                                              | `float` | Amount of horizontal/vertical blur applied to the shadow. `0` means a sharp shadow.        |

#### Fill and Stroke

By default shapes are filled. Setting `IsFilled` to `false` produces an outline whose thickness is controlled by `StrokeWidth`. `StrokeWidthUnits` lets the outline either keep a constant size in world units (`Absolute`) or stay a constant size on screen regardless of camera zoom (`ScreenPixel`).

| Property            | Type                | Description                                                                                                       |
| ------------------- | ------------------- | ----------------------------------------------------------------------------------------------------------------- |
| `IsFilled`          | `bool`              | If `true`, the shape is filled. If `false`, only the outline is drawn using `StrokeWidth`.                        |
| `StrokeWidth`       | `float`             | Outline thickness. Only used when `IsFilled` is `false`.                                                          |
| `StrokeWidthUnits`  | `DimensionUnitType` | How `StrokeWidth` is interpreted. `Absolute` is in world units; `ScreenPixel` keeps the outline constant on screen across camera zoom. |

### ArcRuntime

`ArcRuntime` draws a circular arc inscribed in the shape's `Width` × `Height` bounds. The arc starts at `StartAngle` and sweeps counter-clockwise by `SweepAngle` degrees. Unlike a filled circle, an arc is always stroked — `Thickness` controls how thick the arc is, and `IsEndRounded` controls whether its ends are rounded or flat.

Defaults: `Width` = `Height` = 100, `StartAngle` = 0, `SweepAngle` = 90, `IsEndRounded` = `true`, `Color` = `White`.

| Property        | Type    | Description                                                                                              |
| --------------- | ------- | -------------------------------------------------------------------------------------------------------- |
| `Thickness`     | `float` | Thickness of the arc, in pixels.                                                                         |
| `StartAngle`    | `float` | Angle, in degrees, at which the arc begins. `0` points to the right.                                     |
| `SweepAngle`    | `float` | How far the arc sweeps from `StartAngle`, in degrees. `360` produces a full ring.                        |
| `IsEndRounded`  | `bool`  | If `true`, the ends of the arc are rounded. If `false`, the ends are flat.                               |

### ColoredCircleRuntime

`ColoredCircleRuntime` draws a circle sized to fit within its `Width` and `Height` bounds. It does not add any properties beyond the common set — its size is controlled by `Width`/`Height`, and its appearance is controlled by the common color, gradient, drop shadow, and fill/stroke properties.

Defaults: `Width` = `Height` = 100, `IsFilled` = `true`, `StrokeWidth` = 1, `Color` = `White`.

### RoundedRectangleRuntime

`RoundedRectangleRuntime` draws a rectangle with rounded corners. Its size is controlled by `Width` and `Height`. It adds the following properties to the common set:

| Property                                                                                       | Type     | Description                                                                                              |
| ---------------------------------------------------------------------------------------------- | -------- | -------------------------------------------------------------------------------------------------------- |
| `CornerRadius`                                                                                 | `float`  | Radius, in pixels, used for any corner that does not have a per-corner override. `0` produces a sharp-cornered rectangle. |
| `CustomRadiusTopLeft`, `CustomRadiusTopRight`, `CustomRadiusBottomRight`, `CustomRadiusBottomLeft` | `float?` | Optional per-corner overrides. When non-null, that corner uses the override; when null, the corner falls back to `CornerRadius`. |

Defaults: `Width` = `Height` = 100, `CornerRadius` = 5, all `CustomRadius*` = `null`, solid color = `White`.

#### Per-corner radii

Setting any of the four `CustomRadius*` properties opts the corresponding corner out of the uniform `CornerRadius` value. This lets a single `RoundedRectangleRuntime` render asymmetric shapes — for example, a tab with rounded top corners only, or the "leaf" silhouette below where two opposing corners stay sharp.

```csharp
var leaf = new RoundedRectangleRuntime();
parent.Children.Add(leaf);
leaf.Width = 120;
leaf.Height = 32;
leaf.Color = Microsoft.Xna.Framework.Color.Green;

// Sharp on TL/BR, rounded on TR/BL — the "leaf" silhouette.
leaf.CustomRadiusTopLeft     = 2f;
leaf.CustomRadiusTopRight    = 12f;
leaf.CustomRadiusBottomRight = 2f;
leaf.CustomRadiusBottomLeft  = 12f;

// To undo a per-corner override and fall back to CornerRadius, set it to null:
// leaf.CustomRadiusTopLeft = null;
```

Per-corner radii require Apos.Shapes 0.6.9 or later (which `Gum.Shapes.MonoGame` / `Gum.Shapes.KNI` depend on as of this release). The Skia backend has supported the same properties for longer.

The Gum tool's variable grid does not yet expose the four `CustomRadius*` variables — only `CornerRadius`. To use per-corner radii today, set the properties in code on the runtime instance after the visual is created. Tool-side parity is tracked in [issue #2720](https://github.com/vchelaru/Gum/issues/2720).

Dashed strokes (`StrokeDashLength` / `StrokeGapLength`) are not aware of per-corner radii — the dash perimeter walk currently assumes uniform corners. If you need a dashed stroke on a per-corner-radii rectangle, render the body and the dashed outline as two separate `RoundedRectangleRuntime` instances (one filled with per-corner radii, one stroked with uniform `CornerRadius` set to whichever radius reads best).

## Setup in Gum Tool

Shapes can be used in the Gum tool. To add shapes:

1. Launch the Gum tool
2. Select Plugins ⇒ Add Skia Standard Elements
3. Add instances of Arc, ColoredCircle, or RoundedRectangleRuntime to your Screens or Components

For information on using these shapes in the Gum tool, see the [Arc](../../gum-tool/gum-elements/skia-standard-elements/arc/), [ColoredCircle](../../gum-tool/gum-elements/skia-standard-elements/coloredcircle.md), and [RoundedRectangle](../../gum-tool/gum-elements/skia-standard-elements/roundedrectangle/) pages. These shapes all share common values for fill, gradients, dropshadows. For information on these general properties, see the [Shape Properties](../../gum-tool/gum-elements/shape-properties/README.md) page.

{% hint style="warning" %}
The MonoGame and KNI runtimes only supports the shapes listed above. Adding other Skia instances, such as SVG or Lottie, will result in compile time or runtime errors.
{% endhint %}

Screens and components containing shapes mentioned above can be loaded with no code gen, by reference code gen, or full code gen (no .gumx loaded at runtime).

<figure><img src="../../.gitbook/assets/06_07 20 36.png" alt=""><figcaption><p>Shapes in the Gum tool</p></figcaption></figure>
