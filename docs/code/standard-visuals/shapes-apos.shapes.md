# Shapes (Apos.Shapes)

## Introduction

GumUI supports rendering vector shapes as visuals. The following shapes are supported.

* ArcRuntime
* ColoredCircleRuntime
* RoundedRectangleRuntime

## Adding NuGet packages

{% tabs %}
{% tab title="MonoGame" %}
The Gum.Shapes.MonoGame NuGet package adds support for rendering shapes. Add the following NuGet package:

[https://www.nuget.org/packages/Gum.Shapes.MonoGame](https://www.nuget.org/packages/Gum.Shapes.MonoGame)

Modify csproj:

```xml
<PackageReference Include="Gum.Shapes.MonoGame" Version="*" />
```

Or add through command line:

```bash
dotnet add package Gum.Shapes.MonoGame
```

Future versions of Gum may not require adding this package explicitly.
{% endtab %}

{% tab title="KNI" %}
The Gum.Shapes.KNI NuGet package adds support for rendering shapes. Add the following NuGet package:

[https://www.nuget.org/packages/Gum.Shapes.KNI](https://www.nuget.org/packages/Gum.Shapes.KNI)

Modify csproj:

```xml
<PackageReference Include="Gum.Shapes.KNI" Version="*" />
```

Or add through command line:

```bash
dotnet add package Gum.Shapes.KNI
```

Future versions of Gum may not require adding this package explicitly.

{% hint style="warning" %}
Apos.Shapes compiles a shader at compile time which is used by the library to draw shapes. As of January 2026 shader compilation is not supported on Linux for KNI. Therefore, libraries must be compiled on Windows.

For more information, see this issue: [https://github.com/vchelaru/Gum/issues/2034](https://github.com/vchelaru/Gum/issues/2034)
{% endhint %}
{% endtab %}

{% tab title=".NET MAUI" %}
No additional setup is required to use shapes in .NET MAUI
{% endtab %}

{% tab title="raylib" %}
Shape visuals are not currently supported in raylib. Please create an issue on GitHub or chat with us on Discord to let us know you need this feature.
{% endtab %}

{% tab title="Silk.NET" %}
No additional setup is required to use shapes in Silk.NET.
{% endtab %}
{% endtabs %}

## Setup in Code

{% tabs %}
{% tab title="MonoGame / KNI" %}
Whether you are using code-only or the Gum tool, you must add the following line of code in your Initialize method:

If using December 2025 or earlier:

```csharp
// Initialize
GumUI.Initialize(...);
// Initialize ShapeRenderer after GumUI:
ShapeRenderer.Self.Initialize(GraphicsDevice, Content);
```

If using January 2026 or later:

```csharp
// Initialize
GumUI.Initialize(...);
// Initialize ShapeRenderer after GumUI:
ShapeRenderer.Self.Initialize();
```
{% endtab %}

{% tab title=".NET MAUI" %}
No additional setup is needed if you have already added SkiaSharp and Gum to your project. For more information see the [.NET Maui Initializing Gum](../getting-started/setup/adding-initializing-gum/.net-maui.md) page.
{% endtab %}

{% tab title="raylib" %}
Shape visuals are not currently supported in raylib. Please create an issue on GitHub or chat with us on Discord to let us know you need this feature.
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
        // Initialize shape renderer:
        Renderables.ShapeRenderer.Self.Initialize(GraphicsDevice, Content);

        GumUI.Draw();

        var circle = new ColoredCircleRuntime();
        circle.AddToRoot();
        circle.Color = Color.Red;

        var rectangle = new RoundedRectangleRuntime();
        rectangle.AddToRoot();
        rectangle.X = 100;
        rectangle.CornerRadius = 15;
        rectangle.UseGradient = true;
        rectangle.Color1 = Color.Blue;
        rectangle.Color2 = Color.Green;
        base.Initialize();

        var arc = new ArcRuntime();
        arc.AddToRoot();
        arc.X = 200;
        arc.Color = Color.Purple;
        arc.Thickness = 20;
        arc.StartAngle = 0;
        arc.SweepAngle = 270;
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

## Properties

All three shapes — `ArcRuntime`, `ColoredCircleRuntime`, and `RoundedRectangleRuntime` — derive from `AposShapeRuntime` and therefore share a common set of properties for color, gradient, drop shadow, and fill/stroke. Each shape additionally exposes a small number of properties unique to its geometry.

### Common Properties

These properties are available on every shape.

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

`ColoredCircleRuntime` draws a circle (or ellipse) sized by its `Width` and `Height`. It does not add any properties beyond the common set — its size is controlled by `Width`/`Height`, and its appearance is controlled by the common color, gradient, drop shadow, and fill/stroke properties.

Defaults: `Width` = `Height` = 100, `IsFilled` = `true`, `StrokeWidth` = 1, `Color` = `White`.

### RoundedRectangleRuntime

`RoundedRectangleRuntime` draws a rectangle with rounded corners. Its size is controlled by `Width` and `Height`. It adds a single property to the common set:

| Property        | Type    | Description                                                                                              |
| --------------- | ------- | -------------------------------------------------------------------------------------------------------- |
| `CornerRadius`  | `float` | Radius, in pixels, of each rounded corner. A value of `0` produces a sharp-cornered rectangle.           |

Defaults: `Width` = `Height` = 100, `CornerRadius` = 5, solid color = `White`.

## Setup in Gum Tool

Shapes can be used in the Gum tool. To add shapes:

1. Launch the Gum tool
2. Select Plugins ⇒ Add Skia Standard Elements
3. Add instances of Arc, ColoredCircle, or RoundedRectangleRuntime to your Screens or Components

For information on using these shapes in the Gum tool, see the [Arc](../../gum-tool/gum-elements/skia-standard-elements/arc/), [ColoredCircle](../../gum-tool/gum-elements/skia-standard-elements/coloredcircle.md), and [RoundedRectangle](../../gum-tool/gum-elements/skia-standard-elements/roundedrectangle/) pages. These shapes all share common values for fill, gradients, dropshadows. For information on these general properties, see the [Skia Element General Properties](../../gum-tool/gum-elements/skia-standard-elements/general-properties/) page.

{% hint style="warning" %}
The MonoGame and KNI runtimes only supports the shapes listed above. Adding other Skia instances, such as SVG or Lottie, will result in compile time or runtime errors.
{% endhint %}

Screens and components containing shapes mentioned above can be loaded with no code gen, by reference code gen, or full code gen (no .gumx loaded at runtime).

<figure><img src="../../.gitbook/assets/06_07 20 36.png" alt=""><figcaption><p>Shapes in the Gum tool</p></figcaption></figure>
