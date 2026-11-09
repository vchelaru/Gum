# KNI with Skia

## Introduction

This page assumes you have an existing KNI DesktopGL project. This can be an empty project or an existing game.

`Gum.KniSkia` is a KNI host that draws Gum through SkiaSharp instead of SpriteBatch. Your `Game` class looks the same as it does for [MonoGame/KNI/FNA](monogame-kni-fna/README.md): Gum initializes from the game, and each frame is one `Update` and one `Draw` call. Gum owns the Skia render target and composites it onto the back buffer, and mouse, keyboard, and gamepad reach Gum through KNI.

{% hint style="info" %}
Available in preview NuGet packages (use a `-preview` version), or now if building Gum from source.
{% endhint %}

## Adding Gum NuGet package

Add the Gum.KniSkia NuGet package ([https://www.nuget.org/packages/Gum.KniSkia](https://www.nuget.org/packages/Gum.KniSkia)) along with the KNI DesktopGL packages your project already uses.

The block below includes the base package and two add-ons marked **optional**: shader file support and arithmetic expression support. If you don't need an add-on, skip its line here and delete the matching line from the initialization code in [Initializing Gum](#initializing-gum) below.

```xml
<PackageReference Include="Gum.KniSkia" Version="*-*" />
<PackageReference Include="Gum.SkiaSharp.ShadowDusk" Version="*" /> <!-- Optional: .fx and .slang shader files -->
<PackageReference Include="Gum.Expressions" Version="*" /> <!-- Optional: arithmetic expressions in variable references -->
```

{% hint style="warning" %}
Don't name your project `KniSkiaGum`, that's the assembly name inside the `Gum.KniSkia` package. A same-named project produces a same-named output DLL that silently overwrites the runtime's copy in your `bin` folder, causing a `TypeLoadException` at runtime with no build warning.
{% endhint %}

## Adding Source (Optional)

You can directly link your project to source instead of a NuGet package for improved debuggability, access to fixes and features before NuGet packages are published, or if you are interested in contributing.

To add source, first clone the Gum repository: [https://github.com/vchelaru/Gum](https://github.com/vchelaru/Gum)

If you have already added the Gum NuGet package to your project, remove it. Add the following projects to your solution:

* \<Gum Root>/Runtimes/KniSkiaGum/KniSkiaGum.csproj
* \<Gum Root>/Runtimes/SkiaGum.ShadowDusk/SkiaGum.ShadowDusk.csproj, **Optional:** .fx and .slang shader files
* \<Gum Root>/Runtimes/GumExpressions/GumExpressions.csproj, **Optional:** arithmetic expressions in variable references

Then add project references in your game project. Your project might look like this depending on the location of the Gum repository relative to your game project:

```xml
<ProjectReference Include="..\Gum\Runtimes\KniSkiaGum\KniSkiaGum.csproj" />
<ProjectReference Include="..\Gum\Runtimes\SkiaGum.ShadowDusk\SkiaGum.ShadowDusk.csproj" />
<ProjectReference Include="..\Gum\Runtimes\GumExpressions\GumExpressions.csproj" />
```

To create a project that already references Gum source, run [`gumcli new MyGame -p kniskia --source-linked`](../../../../cli/new.md#linking-to-gum-source).

## Initializing Gum

The following is a complete `Game1` class that also shows a button. Lines marked **Optional** match the optional packages above, so delete the lines for any package you skipped. `Window.AllowUserResizing` is optional too: the render target follows the window when it is resized.

```csharp
namespace MyGame;

// The usings are inside the namespace so a project named like one of these types (Game, Button) still finds them.
using Gum;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Microsoft.Xna.Framework;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;

    GumService GumUI => GumService.Default;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        Window.AllowUserResizing = true; // Optional: the Gum canvas follows the window size
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        GumUI.Initialize(this, "Content/GumProject/GumProject.gumj");
        GumUI.UseShadowDusk(); // Optional: .fx and .slang shader files
        Gum.Expressions.GumExpressionService.Initialize(); // Optional: arithmetic expressions in variable references
        GumUI.UseKeyboardDefaults();
        GumUI.UseGamepadDefaults();

        var button = new Button();
        button.AddToRoot();
        button.Width = 200;
        button.Anchor(Anchor.Center);
        button.Click += (_, _) => button.Text = $"Clicked\n{System.DateTime.Now}";

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

Skia reads the Gum project from disk instead of through the content pipeline, so the path starts at `Content` and the project files must be copied to the output folder. To get the same class with a Gum project already set up, run [`gumcli new MyGame -p kniskia --source-linked`](../../../../cli/new.md).

For a working project, see the Gum KNI with Skia sample:

{% embed url="https://github.com/vchelaru/Gum/tree/main/Samples/KniSkiaGum" %}
