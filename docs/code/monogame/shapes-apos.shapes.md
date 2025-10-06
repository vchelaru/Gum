# Shapes (Apos.Shapes)

## Introduction

The Apos.Shapes library can be used with Gum to add support for the following additional standard types:

* ArcRuntime
* ColoredCircleRuntime
* RoundedRectangleRuntime

The interface for working with these types is the same as working with these types in Skia.

## Adding Shapes to a MonoGame Gum Project

Currently this library is not distributed as a NuGet package, so games must link to source. To do this:

1. Clone the Gum repository locally
2. Add the following csproj to your game's .sln: \<Gum Root>/Runtimes/MonoGameGumShapes/MonoGameGumShapes.csproj
3. Link this project in your main game's csproj as a dependency

Your game now has access to the shape runtimes mentioned above.

## Setup in Code

Whether you are using code-only or the Gum tool, you must add the following line of code in your Initialize method:

```csharp
ShapeRenderer.Self.Initialize(GraphicsDevice, Content);
// initialize Gum now:
```

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
        GumUI.Initialize(this, Gum.Forms.DefaultVisualsVersion.V2);
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

{% hint style="warning" %}
Shapes only supports the shapes listed above. Adding other Skia instances, such as SVG or Lottie, will result in compile time or runtime errors.
{% endhint %}

Screens and components containing shapes mentioned above can be loaded with no code gen, by reference code gen, or full code gen (no .gumx loaded at runtime).

