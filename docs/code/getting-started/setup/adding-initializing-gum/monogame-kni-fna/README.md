# MonoGame/KNI/FNA

## Introduction

This page assumes you have an existing MonoGame project. This can be an empty project or an existing game.

MonoGame Gum works on a variety of platforms including DesktopGL, DirectX, and mobile. It's fully functional with all flavors of XNA-like libraries including MonoGame, Kni (including on web), and FNA. It can be used alongside other libraries such as MonoGameExtended and Nez. If your particular platform is not supported please contact us on Discord and we will do our best to add support.

## Adding Gum NuGet Packages

The easiest way to add Gum to your project is to use NuGet. Open your project in your preferred IDE, or add packages through the command line. Each Gum NuGet package works on any platform. For example, MonoGame Desktop and Android project types use the same Gum NuGet package.

The block below includes the base package plus three commonly-used add-ons, each marked **recommended, optional** or **optional**: shape fill/gradient/shadow support, dynamic (KernSmith) fonts, and arithmetic expression support. If you don't need one, skip its line here and delete the matching line from the initialization code in [Adding Gum to Game](./#adding-gum-to-game) below.

{% tabs %}
{% tab title="MonoGame" %}
Add the Gum.MonoGame NuGet package ([https://www.nuget.org/packages/Gum.MonoGame](https://www.nuget.org/packages/Gum.MonoGame))

Modify csproj:

```xml
<PackageReference Include="Gum.MonoGame" Version="*" />
<PackageReference Include="Gum.Shapes.MonoGame" Version="*" /> <!-- Recommended, optional: shape fill/gradient/shadow -->
<PackageReference Include="KernSmith.MonoGameGum" Version="*" /> <!-- Recommended, optional: dynamic fonts -->
<PackageReference Include="Gum.Expressions" Version="*" /> <!-- Optional: arithmetic expressions in variable references -->
```

Or add through command line:

```bash
dotnet add package Gum.MonoGame
dotnet add package Gum.Shapes.MonoGame     # Recommended, optional: shape fill/gradient/shadow
dotnet add package KernSmith.MonoGameGum   # Recommended, optional: dynamic fonts
dotnet add package Gum.Expressions         # Optional: arithmetic expressions in variable references
```
{% endtab %}

{% tab title="KNI" %}
Add the Gum.KNI NuGet package ([https://www.nuget.org/packages/Gum.KNI](https://www.nuget.org/packages/Gum.KNI))

Modify csproj:

```xml
<PackageReference Include="Gum.KNI" Version="*" />
<PackageReference Include="Gum.Shapes.KNI" Version="*" /> <!-- Recommended, optional: shape fill/gradient/shadow -->
<PackageReference Include="KernSmith.KniGum" Version="*" /> <!-- Recommended, optional: dynamic fonts -->
<PackageReference Include="KernSmith.Rasterizers.StbTrueType" Version="*" /> <!-- Only needed if targeting web (BlazorGL) -->
<PackageReference Include="Gum.Expressions" Version="*" /> <!-- Optional: arithmetic expressions in variable references -->
```

Or add through command line:

```bash
dotnet add package Gum.KNI
dotnet add package Gum.Shapes.KNI     # Recommended, optional: shape fill/gradient/shadow
dotnet add package KernSmith.KniGum   # Recommended, optional: dynamic fonts
dotnet add package KernSmith.Rasterizers.StbTrueType   # Only needed if targeting web (BlazorGL)
dotnet add package Gum.Expressions    # Optional: arithmetic expressions in variable references
```
{% endtab %}

{% tab title="FNA" %}
Add the Gum.FNA NuGet package ([https://www.nuget.org/packages/Gum.FNA](https://www.nuget.org/packages/Gum.FNA))

Modify csproj:

```xml
<PackageReference Include="Gum.FNA" Version="*" />
<PackageReference Include="Gum.Expressions" Version="*" /> <!-- Optional: arithmetic expressions in variable references -->
```

Or add through command line:

```bash
dotnet add package Gum.FNA
dotnet add package Gum.Expressions   # Optional: arithmetic expressions in variable references
```

There's no shape support or KernSmith package for FNA yet; skip those lines in [Adding Gum to Game](./#adding-gum-to-game) below. An outlined `Circle`/`Rectangle` still renders without the shapes package (`StrokeColor`, `StrokeWidth`, and geometry all work), and `Rectangle`'s fill renders too, at square corners. `Circle`'s fill and the richer effects (rounded corners, gradient, drop shadow, dashed stroke) are MonoGame/KNI only for now. If you need dynamic fonts on FNA, reach out on Discord.
{% endtab %}
{% endtabs %}

{% hint style="warning" %}
Don't name your project `MonoGameGum` (or `KniGum` / `FnaGum` for the KNI/FNA tabs), that's the assembly name inside the matching runtime package. A same-named project produces a same-named output DLL that silently overwrites the runtime's copy in your `bin` folder, causing a `TypeLoadException` at runtime with no build warning. Gum's build now catches this for you: if your `AssemblyName` collides, the build fails with an error telling you to change it.
{% endhint %}

## Adding Source (Optional)

You can directly link your project to source instead of a NuGet package for improved debuggability, access to fixes and features before NuGet packages are published, or if you are interested in contributing.

To add source, first clone the Gum repository: [https://github.com/vchelaru/Gum](https://github.com/vchelaru/Gum)

If you have already added the Gum NuGet package to your project, remove it.

As with the NuGet packages above, the shape support, KernSmith, and expression projects are marked **recommended, optional** or **optional**; skip a project if you don't need it, and delete its matching line from the initialization code in [Adding Gum to Game](./#adding-gum-to-game).

{% tabs %}
{% tab title="MonoGame" %}
Add the following projects to your solution:

* \<Gum Root>/MonoGameGum/MonoGameGum.csproj
* \<Gum Root>/GumCommon/GumCommon.csproj
* \<Gum Root>/Runtimes/GumShapes/MonoGameGumShapes.csproj, **Recommended, optional:** shape fill/gradient/shadow
* \<Gum Root>/Integrations/KernSmith/KernSmith.GumCommon/KernSmith.GumCommon.csproj, **Recommended, optional:** dynamic fonts
* \<Gum Root>/Integrations/KernSmith/KernSmith.MonoGameGum/KernSmith.MonoGameGum.csproj, **Recommended, optional:** dynamic fonts
* \<Gum Root>/Runtimes/GumExpressions/GumExpressions.csproj, **Optional:** arithmetic expressions in variable references

Next, add project references in your game project for the pieces you use directly (`GumCommon.csproj` and `KernSmith.GumCommon.csproj` are pulled in transitively and don't need a direct reference). Your project might look like this depending on the location of the Gum repository relative to your game project:

```xml
<ProjectReference Include="..\Gum\MonoGameGum\MonoGameGum.csproj" />
<ProjectReference Include="..\Gum\GumCommon\GumCommon.csproj" />
<ProjectReference Include="..\Gum\Runtimes\GumShapes\MonoGameGumShapes.csproj" />
<ProjectReference Include="..\Gum\Integrations\KernSmith\KernSmith.GumCommon\KernSmith.GumCommon.csproj" />
<ProjectReference Include="..\Gum\Integrations\KernSmith\KernSmith.MonoGameGum\KernSmith.MonoGameGum.csproj" />
<ProjectReference Include="..\Gum\Runtimes\GumExpressions\GumExpressions.csproj" />
```
{% endtab %}

{% tab title="KNI" %}
Add the following projects to your solution:

* \<Gum Root>/MonoGameGum/KniGum/KniGum.csproj
* \<Gum Root>/GumCommon/GumCommon.csproj
* \<Gum Root>/Runtimes/GumShapes/KniGumShapes.csproj, **Recommended, optional:** shape fill/gradient/shadow
* \<Gum Root>/Integrations/KernSmith/KernSmith.GumCommon/KernSmith.GumCommon.csproj, **Recommended, optional:** dynamic fonts
* \<Gum Root>/Integrations/KernSmith/KernSmith.KniGum/KernSmith.KniGum.csproj, **Recommended, optional:** dynamic fonts
* \<Gum Root>/Runtimes/GumExpressions/GumExpressions.csproj, **Optional:** arithmetic expressions in variable references

Next, add project references in your game project for the pieces you use directly (`GumCommon.csproj` and `KernSmith.GumCommon.csproj` are pulled in transitively and don't need a direct reference). Your project might look like this depending on the location of the Gum repository relative to your game project:

```xml
<ProjectReference Include="..\Gum\MonoGameGum\KniGum\KniGum.csproj" />
<ProjectReference Include="..\Gum\GumCommon\GumCommon.csproj" />
<ProjectReference Include="..\Gum\Runtimes\GumShapes\KniGumShapes.csproj" />
<ProjectReference Include="..\Gum\Integrations\KernSmith\KernSmith.GumCommon\KernSmith.GumCommon.csproj" />
<ProjectReference Include="..\Gum\Integrations\KernSmith\KernSmith.KniGum\KernSmith.KniGum.csproj" />
<ProjectReference Include="..\Gum\Runtimes\GumExpressions\GumExpressions.csproj" />
```
{% endtab %}

{% tab title="FNA" %}
Add the following projects to your solution:

* \<Gum Root>/MonoGameGum/FnaGum/FnaGum.csproj
* \<Gum Root>/GumCommon/GumCommon.csproj
* \<Gum Root>/Runtimes/GumExpressions/GumExpressions.csproj, **Optional:** arithmetic expressions in variable references

Next, add project references in your game project for the pieces you use directly. Your project might look like this depending on the location of the Gum repository relative to your game project:

```xml
<ProjectReference Include="..\Gum\MonoGameGum\FnaGum\FnaGum.csproj" />
<ProjectReference Include="..\Gum\GumCommon\GumCommon.csproj" />
<ProjectReference Include="..\Gum\Runtimes\GumExpressions\GumExpressions.csproj" />
```

There's no shape support or KernSmith source project wired up for FNA yet; skip those, matching the NuGet tab above.
{% endtab %}
{% endtabs %}

If using Visual Studio Code, see the [Visual Studio Code and Linking Source](visual-studio-code-and-linking-source.md) page.

## Adding Gum to Game

Gum can be added to a Game/Core class with a few lines of code. Projects are encouraged to create a local GumService property called GumUI for convenience.

The code below also wires up shape support, dynamic fonts (KernSmith), and expression support, matching the NuGet packages above. If you skipped a package above, delete its matching line(s) here too.

{% hint style="info" %}
The code in this example assumes that you are using retained mode rendering. If you are interested in immediate mode rendering, see the [Setup for GumBatch](../../setup-for-gumbatch.md) page.
{% endhint %}

{% tabs %}
{% tab title="Game Class" %}
Add code to your Game class to Initialize, Update, and Draw Gum as shown in the following code block:

<pre class="language-csharp"><code class="lang-csharp">using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
<strong>using Gum;
</strong><strong>using Gum.Forms;
</strong><strong>using Gum.Forms.Controls;
</strong>
namespace MonoGameGum1;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

<strong>    GumService GumUI => GumService.Default;
</strong>
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
<strong>        GumUI.Initialize(this);
</strong><strong>        ShapeRenderer.Self.Initialize(); // Recommended, optional: shape fill/gradient/shadow
</strong><strong>        Gum.Wireframe.CustomSetPropertyOnRenderable.InMemoryFontCreator =
</strong><strong>            new KernSmith.Gum.KernSmithFontCreator(GraphicsDevice); // Recommended, optional: dynamic fonts
</strong><strong>        GumExpressionService.Initialize(); // Optional: arithmetic expressions in variable references
</strong>        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
<strong>        GumUI.Update(gameTime);
</strong>        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
<strong>        GumUI.Draw();
</strong>        base.Draw(gameTime);
    }
}
</code></pre>
{% endtab %}

{% tab title="Core (Nez)" %}
Next, add code to your Core-inheriting class to Initialize, Update, and Draw Gum as shown in the following code block:

<pre class="language-csharp"><code class="lang-csharp"><strong>using Gum;
</strong><strong>using Gum.Forms;
</strong>
public class Game1 : Core
{
    GumService GumUI => GumService.Default;    
    protected override void Initialize()
    {
        base.Initialize();

<strong>        GumUI.Initialize(Core.GraphicsDevice);
</strong><strong>        ShapeRenderer.Self.Initialize(); // Recommended, optional: shape fill/gradient/shadow
</strong><strong>        Gum.Wireframe.CustomSetPropertyOnRenderable.InMemoryFontCreator =
</strong><strong>            new KernSmith.Gum.KernSmithFontCreator(Core.GraphicsDevice); // Recommended, optional: dynamic fonts
</strong><strong>        GumExpressionService.Initialize(); // Optional: arithmetic expressions in variable references
</strong>        
        Scene = new BasicScene();
    }

    protected override void Update(GameTime gameTime)
    {
<strong>        GumUI.Activity(gameTime);
</strong>        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);
        // Add GumUI.Draw after base.Draw or else graphics won't show up
<strong>        GumUI.Draw();
</strong>    }
}
</code></pre>
{% endtab %}
{% endtabs %}

### About Shape Support (Recommended)

Gum's `Circle` and `Rectangle` elements have a **fill** and an **outline (stroke)**. On MonoGame, KNI, and FNA, an outlined `Circle` or `Rectangle` renders out of the box: `StrokeColor`, `StrokeWidth`, `StrokeWidthUnits`, and the geometry properties (`Width`, `Height`, `Radius`, `CornerRadius`) all work with no extra package. `Rectangle`'s fill also renders out of the box, at square corners.

Filling a `Circle`, rounding a `Rectangle`'s corners, and the richer effects all need the `Gum.Shapes.*` package added above (it uses Apos.Shapes under the hood). We recommend installing it for most projects so that fill, gradient, drop shadow, dashed stroke, and anti-aliasing all draw everywhere. Without it, the following are stored and round-trip correctly, but silently do not draw: `CircleRuntime.FillColor` (and its fill color channels), `RectangleRuntime.CornerRadius` (the rectangle stays square-cornered), gradient (`UseGradient` and the gradient properties), drop shadow (`HasDropshadow` and the dropshadow properties), dashed stroke (`StrokeDashLength` / `StrokeGapLength`), anti-aliasing (`IsAntialiased`), and `Blend`. Nothing throws; the shape simply renders without that feature.

{% hint style="info" %}
The fill + outline `Circle` and `Rectangle` surface ships in the May 2026 release. Before then, you can use it by building Gum from source.
{% endhint %}

For the full set of fill, outline, gradient, drop shadow, and corner-radius properties, see the [Shapes](../../../../standard-visuals/shapes-apos.shapes.md) page.

### About Dynamic Fonts (Optional)

By default, Gum uses pre-built bitmap font (.fnt) files for text rendering. The KernSmith package added above enables dynamic in-memory font generation, which lets you set `Font`, `FontSize`, `IsBold`, `IsItalic`, `OutlineThickness`, and `UseFontSmoothing` on any `TextRuntime` without needing .fnt/.png files on disk.

{% hint style="info" %}
For shipping games, you should register custom .ttf fonts rather than relying on system fonts. For more information, see the [Fonts](../../../../standard-visuals/textruntime/fonts.md) page.
{% endhint %}

{% hint style="warning" %}
**KNI on web (BlazorGL):** dynamic fonts default to the FreeType rasterizer, which is native code and can't run in the browser. You must select the pure-C# StbTrueType backend instead (the `KernSmith.Rasterizers.StbTrueType` package added above), and register it yourself before Gum uses it — published web builds are trimmed by default, which strips KernSmith's normal automatic backend discovery. Add this once in `Program.cs`, before the host runs:

```csharp
using System.Runtime.CompilerServices;
using KernSmith.Rasterizers.StbTrueType;

RuntimeHelpers.RunClassConstructor(typeof(StbTrueTypeRasterizer).TypeHandle);
```

Then pass the backend explicitly when creating the font creator:

```csharp
new KernSmith.Gum.KernSmithFontCreator(GraphicsDevice, KernSmith.Rasterizer.RasterizerBackend.StbTrueType);
```
{% endhint %}

### About Expression Support (Optional)

If your Gum project uses arithmetic expressions in variable references (such as `Width = OtherInstance.Width + 20`), the `Gum.Expressions` package added above enables full expression evaluation at runtime. Without it, simple variable references like `Width = OtherInstance.Width` still work. It's typically used together with a Gum project that has variable references defined in the tool; see [Loading a Gum Project (.gumx)](../../loading-a-gum-project-.gumx.md).

If linking to source instead of NuGet, add `<Gum Root>/Runtimes/GumExpressions/GumExpressions.csproj` to your solution.

For more information, see the [Runtime Variable References](../../../../styling/runtime-variable-references.md) page.

## Adding a Button (Testing the Setup)

Gum can be tested by adding a Button after Gum is initialized. To do so, add code to create a `Button` as shown in the following block of code after Gum is initialized:

<pre class="language-csharp"><code class="lang-csharp">protected override void Initialize()
{
    base.Initialize();

    GumUI.Initialize(Core.GraphicsDevice);

<strong>    var button = new Button();
</strong><strong>    button.AddToRoot();
</strong><strong>    button.Click += (_,_) =>
</strong><strong>        button.Text = "Clicked at\n" + DateTime.Now;
</strong>    
    // additional code omitted
</code></pre>

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAAA6tW8ix2L81VsiopKk3VUcrMyyzJTMzJrEpVslIqSyxSSCotKcnPU7BVyEstV3ACczQ0rWPyIOJ6jikpIflB-fklyILOOZnJ2QoxpQYGRk62ChrxOvGaCrYgrrErXE1IakUJ0FSwIiOwhtQUhcSSmJg8iBBUu4JLYklqSGZuqp5ffrm1Ui0AnK-bSK8AAAA)

<figure><img src="../../../../../.gitbook/assets/13_06 56 07.gif" alt=""><figcaption></figcaption></figure>

If everything is initialized correctly, you should see a clickable button at the top-left of the screen. Keep in mind that this is simply a test to make sure Gum is working properly. You may want to delete this button once you begin working on your game.

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
