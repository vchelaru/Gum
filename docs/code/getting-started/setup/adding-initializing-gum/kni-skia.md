# KNI with Skia

## Introduction

This page assumes you have an existing KNI DesktopGL project. This can be an empty project or an existing game.

`Gum.KniSkia` is a KNI host that draws Gum through SkiaSharp instead of SpriteBatch. Your `Game` class looks the same as it does for [MonoGame/KNI/FNA](monogame-kni-fna/README.md): Gum initializes from the game, and each frame is one `Update` and one `Draw` call. Gum owns the Skia render target and composites it onto the back buffer, and mouse, keyboard, and gamepad reach Gum through KNI.

{% hint style="info" %}
Available in preview NuGet packages (use a `-preview` version), or now if building Gum from source.
{% endhint %}

## Starting From a New Project

If you plan to use a Gum project, the easiest start is a new game that Gum creates for you. In the Gum tool, select **File** > **New Project**, check **Create C# project**, and choose KNI with Skia in **Platform**. Or run [`gumcli new MyGame --platform kniskia --preview`](../../../../cli/new.md). The game already references Gum, loads the Gum project and copies it to the output folder, so you can skip the rest of this page. See [Creating a Project](../../../../gum-tool/setup/creating-a-project.md).

The rest of this page adds Gum to a game you already have.

## Adding Gum NuGet package

Add the Gum.KniSkia NuGet package ([https://www.nuget.org/packages/Gum.KniSkia](https://www.nuget.org/packages/Gum.KniSkia)) along with the KNI DesktopGL packages your project already uses.

```xml
<PackageReference Include="Gum.KniSkia" Version="*-*" />
```

The `Game1` class in [Initializing Gum](#initializing-gum) needs only this package. Extra features come as separate packages, listed in [Optional Extras](#optional-extras).

{% hint style="warning" %}
Don't name your project `KniSkiaGum`, that's the assembly name inside the `Gum.KniSkia` package. A same-named project produces a same-named output DLL that silently overwrites the runtime's copy in your `bin` folder, causing a `TypeLoadException` at runtime with no build warning.
{% endhint %}

## Adding Source (Optional)

You can directly link your project to source instead of a NuGet package for improved debuggability, access to fixes and features before NuGet packages are published, or if you are interested in contributing.

To add source, first clone the Gum repository: [https://github.com/vchelaru/Gum](https://github.com/vchelaru/Gum)

If you have already added the Gum NuGet package to your project, remove it. Add the following project to your solution:

* \<Gum Root>/Runtimes/KniSkiaGum/KniSkiaGum.csproj

Then add a project reference in your game project. Your project might look like this depending on the location of the Gum repository relative to your game project:

```xml
<ProjectReference Include="..\Gum\Runtimes\KniSkiaGum\KniSkiaGum.csproj" />
```

To link an extra from [Optional Extras](#optional-extras), add its project instead of its package:

* ShadowDusk: \<Gum Root>/Runtimes/SkiaGum.ShadowDusk/SkiaGum.ShadowDusk.csproj
* Expressions: \<Gum Root>/Runtimes/GumExpressions/GumExpressions.csproj

To create a project that already references Gum source, run [`gumcli new MyGame -p kniskia --source-linked`](../../../../cli/new.md#linking-to-gum-source).

## Initializing Gum

The following is a complete `Game1` class that also shows a button. It needs only the base package and builds its UI in code, so it runs without a Gum project. `Window.AllowUserResizing` is optional: the render target follows the window when it is resized.

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
        GumUI.Initialize(this);
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

The button in the center of the window confirms Gum works. Delete it once you start building your game.

## Optional Extras

Each extra is a separate package. Add the package, then add its line right after `GumUI.Initialize`.

| Extra | Package | Line after `Initialize` |
| --- | --- | --- |
| [ShadowDusk](../../../standard-visuals/containerruntime.md#render-target-shaders-on-skiasharp): `.fx` and `.slang` shader files | `Gum.SkiaSharp.ShadowDusk` | `GumUI.UseShadowDusk();` |
| [Expressions](../../../styling/runtime-variable-references.md): arithmetic in variable references | `Gum.Expressions` | `GumUI.UseExpressions();` |

## Loading Your Gum Project

To load a Gum project, pass its `.gumj` path to `Initialize`. Skia reads the Gum project from disk instead of through the content pipeline, so the path starts at `Content`:

```csharp
// Initialize
GumUI.Initialize(this, "Content/GumProject/GumProject.gumj");
```

Then copy the Gum project to the output folder by adding this to your `.csproj`:

```xml
<ItemGroup>
    <None Update="Content\GumProject\**\*.*">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
</ItemGroup>
```

To create the Gum project and show its screens, see [Loading a Gum Project](../loading-a-gum-project-.gumx.md).

## Sample Project

For a working project, see the Gum KNI with Skia sample:

{% embed url="https://github.com/vchelaru/Gum/tree/main/Samples/KniSkiaGum" %}
