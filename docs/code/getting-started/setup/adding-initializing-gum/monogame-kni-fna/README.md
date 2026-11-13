# MonoGame/KNI/FNA

## Introduction

This page assumes you have an existing MonoGame project. This can be an empty project or an existing game.

MonoGame Gum works on a variety of platforms including DesktopGL, DirectX, and mobile. It's fully functional with all flavors of XNA-like libraries including MonoGame, Kni (including on web), and FNA. It can be used alongside other libraries such as MonoGameExtended and Nez. If your particular platform is not supported please contact us on Discord and we will do our best to add support.

## Starting From a New Project

If you plan to use a Gum project, the easiest start is a new game that Gum creates for you. In the Gum tool, select **File** > **New Project**, check **Create C# project**, and choose MonoGame or KNI in **Platform**. Or run [`gumcli new MyGame --platform monogame`](../../../../../cli/new.md) (`--platform kni` for KNI). The game already references Gum, loads the Gum project and copies it to the output folder, so you can skip the rest of this page. See [Creating a Project](../../../../../gum-tool/setup/creating-a-project.md).

The rest of this page adds Gum to a game you already have.

## Adding Gum NuGet Packages

The easiest way to add Gum to your project is to use NuGet. Open your project in your preferred IDE, or add packages through the command line. Each Gum NuGet package works on any platform. For example, MonoGame Desktop and Android project types use the same Gum NuGet package.

{% tabs %}
{% tab title="MonoGame" %}
Add the Gum.MonoGame NuGet package ([https://www.nuget.org/packages/Gum.MonoGame](https://www.nuget.org/packages/Gum.MonoGame))

Modify csproj:

```xml
<PackageReference Include="Gum.MonoGame" Version="*" />
```

Or add through command line:

```bash
dotnet add package Gum.MonoGame
```
{% endtab %}

{% tab title="KNI" %}
Add the Gum.KNI NuGet package ([https://www.nuget.org/packages/Gum.KNI](https://www.nuget.org/packages/Gum.KNI))

Modify csproj:

```xml
<PackageReference Include="Gum.KNI" Version="*" />
```

Or add through command line:

```bash
dotnet add package Gum.KNI
```
{% endtab %}

{% tab title="FNA" %}
Add the Gum.FNA NuGet package ([https://www.nuget.org/packages/Gum.FNA](https://www.nuget.org/packages/Gum.FNA))

Modify csproj:

```xml
<PackageReference Include="Gum.FNA" Version="*" />
```

Or add through command line:

```bash
dotnet add package Gum.FNA
```
{% endtab %}
{% endtabs %}

The `Game1` class in [Adding Gum to Game](#adding-gum-to-game) needs only this package. Extra features come as separate packages, listed in [Optional Extras](#optional-extras).

{% hint style="warning" %}
Don't name your project `MonoGameGum` (or `KniGum` / `FnaGum` for the KNI/FNA tabs), that's the assembly name inside the matching runtime package. A same-named project produces a same-named output DLL that silently overwrites the runtime's copy in your `bin` folder, causing a `TypeLoadException` at runtime with no build warning. Gum's build now catches this for you: if your `AssemblyName` collides, the build fails with an error telling you to change it.
{% endhint %}

## Adding Source (Optional)

You can directly link your project to source instead of a NuGet package for improved debuggability, access to fixes and features before NuGet packages are published, or if you are interested in contributing.

To add source, first clone the Gum repository: [https://github.com/vchelaru/Gum](https://github.com/vchelaru/Gum)

If you have already added the Gum NuGet package to your project, remove it.

{% tabs %}
{% tab title="MonoGame" %}
Add the following projects to your solution:

* \<Gum Root>/MonoGameGum/MonoGameGum.csproj
* \<Gum Root>/GumCommon/GumCommon.csproj

Next, add project references in your game project. Your project might look like this depending on the location of the Gum repository relative to your game project:

```xml
<ProjectReference Include="..\Gum\MonoGameGum\MonoGameGum.csproj" />
<ProjectReference Include="..\Gum\GumCommon\GumCommon.csproj" />
```

To link an extra from [Optional Extras](#optional-extras), add its project instead of its package:

* Shapes: \<Gum Root>/Runtimes/GumShapes/MonoGameGumShapes.csproj
* KernSmith: \<Gum Root>/Integrations/KernSmith/KernSmith.MonoGameGum/KernSmith.MonoGameGum.csproj (also add \<Gum Root>/Integrations/KernSmith/KernSmith.GumCommon/KernSmith.GumCommon.csproj to the solution)
* ShadowDusk: \<Gum Root>/Runtimes/GumShadowDusk/MonoGameGum.ShadowDusk.csproj
* Expressions: \<Gum Root>/Runtimes/GumExpressions/GumExpressions.csproj
{% endtab %}

{% tab title="KNI" %}
Add the following projects to your solution:

* \<Gum Root>/MonoGameGum/KniGum/KniGum.csproj
* \<Gum Root>/GumCommon/GumCommon.csproj

Next, add project references in your game project. Your project might look like this depending on the location of the Gum repository relative to your game project:

```xml
<ProjectReference Include="..\Gum\MonoGameGum\KniGum\KniGum.csproj" />
<ProjectReference Include="..\Gum\GumCommon\GumCommon.csproj" />
```

To link an extra from [Optional Extras](#optional-extras), add its project instead of its package:

* Shapes: \<Gum Root>/Runtimes/GumShapes/KniGumShapes.csproj
* KernSmith: \<Gum Root>/Integrations/KernSmith/KernSmith.KniGum/KernSmith.KniGum.csproj (also add \<Gum Root>/Integrations/KernSmith/KernSmith.GumCommon/KernSmith.GumCommon.csproj to the solution)
* ShadowDusk: \<Gum Root>/Runtimes/GumShadowDusk/KniGum.ShadowDusk.csproj
* Expressions: \<Gum Root>/Runtimes/GumExpressions/GumExpressions.csproj
{% endtab %}

{% tab title="FNA" %}
Add the following projects to your solution:

* \<Gum Root>/MonoGameGum/FnaGum/FnaGum.csproj
* \<Gum Root>/GumCommon/GumCommon.csproj

Next, add project references in your game project. Your project might look like this depending on the location of the Gum repository relative to your game project:

```xml
<ProjectReference Include="..\Gum\MonoGameGum\FnaGum\FnaGum.csproj" />
<ProjectReference Include="..\Gum\GumCommon\GumCommon.csproj" />
```

To link an extra from [Optional Extras](#optional-extras), add its project instead of its package:

* ShadowDusk: \<Gum Root>/Runtimes/GumShadowDusk/FnaGum.ShadowDusk.csproj
* Expressions: \<Gum Root>/Runtimes/GumExpressions/GumExpressions.csproj
{% endtab %}
{% endtabs %}

If using Visual Studio Code, see the [Visual Studio Code and Linking Source](visual-studio-code-and-linking-source.md) page.

## Nez (Core)

If your game derives from Nez's `Core` instead of `Game`, use this class instead of the `Game1` in the next section.

```csharp
using Gum;
using Gum.Forms;

public class Game1 : Core
{
    GumService GumUI => GumService.Default;

    protected override void Initialize()
    {
        base.Initialize();

        GumUI.Initialize(Core.GraphicsDevice);
        GumUI.UseKeyboardDefaults();
        GumUI.UseGamepadDefaults();

        Scene = new BasicScene();
    }

    protected override void Update(GameTime gameTime)
    {
        GumUI.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);
        // Add GumUI.Draw after base.Draw or else graphics won't show up
        GumUI.Draw();
    }
}
```

## Adding Gum to Game

The following is a complete `Game1` class. It needs only the base package and builds its UI in code, so it runs without a Gum project.

{% hint style="info" %}
Available in preview NuGet packages (use a `-preview` version), or now if building Gum from source.
{% endhint %}

{% hint style="info" %}
This example uses retained mode rendering. For immediate mode rendering, see the [Setup for GumBatch](../../setup-for-gumbatch.md) page.
{% endhint %}

```csharp
namespace MonoGameGum1;

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

| Extra | MonoGame package | KNI package | FNA package | Line after `Initialize` |
| --- | --- | --- | --- | --- |
| [Shapes](../../../../standard-visuals/shapes-apos.shapes.md) (**Recommended**): circle fill, rounded corners, gradient, drop shadow | `Gum.Shapes.MonoGame` | `Gum.Shapes.KNI` | none | `GumUI.UseShapes();` |
| [KernSmith](../../../../files-and-fonts/font-strategies.md#dynamic-kernsmith-generation) (**Recommended**): fonts built at runtime, no .fnt files | `KernSmith.MonoGameGum` | `KernSmith.KniGum` | none | `GumUI.UseKernSmith();` |
| [ShadowDusk](../../../../standard-visuals/containerruntime.md#loading-shader-files-with-sourceshaderfile): `.fx` and `.slang` shader files | `Gum.MonoGame.ShadowDusk` | `Gum.KNI.ShadowDusk` | `Gum.FNA.ShadowDusk` | `GumUI.UseShadowDusk();` |
| [Expressions](../../../../styling/runtime-variable-references.md): arithmetic in variable references | `Gum.Expressions` | `Gum.Expressions` | `Gum.Expressions` | `GumUI.UseExpressions();` |

Shapes also needs the HiDef graphics profile. Add this line to the `Game1` constructor, right after `_graphics` is created:

```csharp
// Initialize
_graphics.GraphicsProfile = Microsoft.Xna.Framework.Graphics.GraphicsProfile.HiDef;
```

{% hint style="warning" %}
**KNI on web (BlazorGL):** KernSmith builds fonts with FreeType by default, which is native code and cannot run in a browser. Also add the `KernSmith.Rasterizers.StbTrueType` package, register it once in `Program.cs` before the host runs, and pass its backend to `UseKernSmith`. Otherwise Gum falls back to its built in 18 pixel font with no error.

```csharp
using System.Runtime.CompilerServices;
using KernSmith.Rasterizers.StbTrueType;

RuntimeHelpers.RunClassConstructor(typeof(StbTrueTypeRasterizer).TypeHandle);
```

```csharp
// Initialize
GumUI.UseKernSmith(KernSmith.RasterizerBackend.StbTrueType);
```
{% endhint %}

## Loading Your Gum Project

To load a Gum project, pass its `.gumj` path to `Initialize`. The path is relative to the `Content` folder:

```csharp
// Initialize
GumUI.Initialize(this, "GumProject/GumProject.gumj");
```

Then copy the Gum project to the output folder by adding this to your `.csproj`:

```xml
<ItemGroup>
    <None Update="Content\GumProject\**\*.*">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
</ItemGroup>
```

To create the Gum project, load it on Android or web, and show its screens, see [Loading a Gum Project](../../loading-a-gum-project-.gumx.md).

## Troubleshooting

{% tabs %}
{% tab title="Nez" %}
Could not load file or assembly 'MonoGame.Framework, Version=3.8.1.303

If you add the Gum code to your project, you may experience this exception internally from Nez:

<figure><img src="../../../../../.gitbook/assets/image (71).png" alt=""><figcaption></figcaption></figure>

The reason this is happening is because currently (as of July 2024) Nez links MonoGame 3.8.0 instead of 3.8.1 (the latest).

To solve this problem, your project must explicitly link MonoGame 3.8.1 or else you will have this exception.

To do this:

1. Open your project in Visual Studio
2. Expand the Dependencies item
3.  Right-click on Packages and select Manage NuGet Packages\\

    <figure><img src="../../../../../.gitbook/assets/image (73).png" alt=""><figcaption><p>Right-click Manage NuGet Packages... option</p></figcaption></figure>
4. Click on the Browse tab
5. Search for MonoGame.Framework
6.  Select the MonoGame.Framework NuGet package for your particular project type. This is most likely MonoGame.Framework.DesktopGL, but it may be different if you are targeting another platform.\\

    <figure><img src="../../../../../.gitbook/assets/image (74).png" alt=""><figcaption><p>MonoGame.Framework NuGet packages</p></figcaption></figure>
7. Click the Install button to add the NuGet package

After adding MonoGame, your NuGet packages should similar to the following image:
{% endtab %}
{% endtabs %}
