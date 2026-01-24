# Shapes (Apos.Shapes)

## Introduction

GumUI supports rendering vector shapes as visuals. The following shapes are supported.

* ArcRuntime
* ColoredCircleRuntime
* RoundedRectangleRuntime

## Adding NuGet packages

{% tabs %}
{% tab title="MonoGame" %}
Gum.Shapes.MonoGame uses Apos.Shapes for shape drawing. Add the following two NuGet packages:

[https://www.nuget.org/packages/Gum.Shapes.MonoGame](https://www.nuget.org/packages/Gum.Shapes.MonoGame)

[https://www.nuget.org/packages/Apos.Shapes/](https://www.nuget.org/packages/Apos.Shapes/)

Modify csproj:

```xml
<PackageReference Include="Gum.Shapes.MonoGame" Version="*" />
<PackageReference Include="Apos.Shapes" Version="*" />
```

Or add through command line:

```bash
dotnet add package Gum.Shapes.MonoGame
dotnet add package Apos.Shapes
```

Future versions of Gum may not require explicitly adding Apos.Shapes.
{% endtab %}

{% tab title="KNI" %}
The Apos.Shapes library is needed to render shapes in MonoGame projects. Add the Gum.Shapes.KNI NuGet package ([https://www.nuget.org/packages/Gum.Shapes.KNI](https://www.nuget.org/packages/Gum.Shapes.KNI)):

Modify csproj:

```xml
<PackageReference Include="Gum.Shapes.KNI" Version="*" />
```

Or add through command line:

```bash
dotnet add package Gum.Shapes.KNI
```
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
GumUI.Initialize(...);
// Initialize ShapeRenderer after GumUI:
ShapeRenderer.Self.Initialize(GraphicsDevice, Content);
```

If using January 2026 or later:

```csharp
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
        GumUI.Initialize(this, Gum.Forms.DefaultVisualsVersion.V3);
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
