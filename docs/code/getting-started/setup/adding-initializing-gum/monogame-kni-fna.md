# MonoGame/KNI/FNA

## Introduction

This page assumes you have an existing MonoGame project. This can be an empty project or an existing game.

MonoGame Gum works on a variety of platforms including DesktopGL, DirectX, and mobile. It's fully functional with all flavors of XNA-like libraries including MonoGame, Kni (including on web), and FNA. It can be used alongside other libraries such as MonoGameExtended and Nez. If your particular platform is not supported please contact us on Discord and we will do our best to add support.

## Adding Gum NuGet Package

The easiest way to add Gum to your project is to use the NuGet package. Open your project in your preferred IDE, or add Gum through the command line. Each Gum NuGet package works on any platform. For example, MonoGame Desktop and Android project types use the same Gum NuGet package.

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

## Adding Source (Optional)

You can directly link your project to source instead of a NuGet package for improved debuggability, access to fixes and features before NuGet packages are published, or if you are interested in contributing.

To add source, first clone the Gum repository: [https://github.com/vchelaru/Gum](https://github.com/vchelaru/Gum)

If you have already added the Gum NuGet package to your project, remove it.

{% tabs %}
{% tab title="MonoGame" %}
Add the following projects to your solution:

* \<Gum Root>/MonoGameGum/MonoGameGum.csproj
* \<GumRoot>/GumCommon/GumCommon.csproj

Next, add MonoGameGum as a project reference in your game project. Your project might look like this depending on the location of the Gum repository relative to your game project:

```xml
<ProjectReference Include="..\Gum\MonoGameGum\MonoGameGum.csproj" />
```
{% endtab %}

{% tab title="KNI" %}
Add the following projects to your solution:

* \<Gum Root>/MonoGameGum/KniGum/KniGum.csproj
* \<GumRoot>/GumCommon/GumCommon.csproj

Next, add KniGum as a project reference in your game project. Your project might look like this depending on the location of the Gum repository relative to your game project:

```xml
<ProjectReference Include="..\Gum\MonoGameGum\KniGum\KniGum.csproj" />
```
{% endtab %}

{% tab title="FNA" %}
Add the following projects to your solution:

* \<Gum Root>/MonoGameGum/FnaGum/FnaGum.csproj
* \<GumRoot>/GumCommon/GumCommon.csproj

Next, add FnaGum as a project reference in your game project. Your project might look like this depending on the location of the Gum repository relative to your game project:

```xml
<ProjectReference Include="..\Gum\MonoGameGum\FnaGum\FnaGum.csproj" />
```
{% endtab %}
{% endtabs %}



## Adding Gum to Game

Gum can be added to a Game/Core class with a few lines of code. Projects are encouraged to create a local GumService property called GumUI for convenience.

{% hint style="info" %}
The code in this example assumes that you are using retained mode rendering. If you are interested in immediate mode rendering, see the [Setup for GumBatch](../setup-for-gumbatch.md) page.
{% endhint %}

{% tabs %}
{% tab title="Game Class" %}
Add code to your Game class to Initialize, Update, and Draw Gum as shown in the following code block:

<pre class="language-csharp"><code class="lang-csharp">using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
<strong>using MonoGameGum;
</strong><strong>using Gum.Forms;
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
<strong>        GumUI.Initialize(this, DefaultVisualsVersion.V3);
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

<pre class="language-csharp"><code class="lang-csharp"><strong>using MonoGameGum;
</strong><strong>using Gum.Forms;
</strong>
public class Game1 : Core
{
    GumService GumUI => GumService.Default;    
    protected override void Initialize()
    {
        base.Initialize();

<strong>        GumUI.Initialize(Core.GraphicsDevice, DefaultVisualsVersion.V3);
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

{% hint style="info" %}
The code above initializes Gum using _V3_ (version 3) visuals. Future versions of Gum may introduce new versions of visuals.

Old version will continue to be supported when new versions are released, but you may want to upgrade to new versions to take advantage of new features.
{% endhint %}

## Adding a Button (Testing the Setup)

Gum can be tested by adding a Button after Gum is initialized. To do so, add code to create a `Button` as shown in the following block of code after Gum is initialized:

<pre class="language-csharp"><code class="lang-csharp">protected override void Initialize()
{
    base.Initialize();

    GumUI.Initialize(Core.GraphicsDevice, DefaultVisualsVersion.V3);
    
<strong>    var button = new Button();
</strong><strong>    button.AddToRoot();
</strong><strong>    button.Click += (_,_) =>
</strong><strong>        button.Text = "Clicked at\n" + DateTime.Now;
</strong>    
    // additional code omitted
</code></pre>

<figure><img src="../../../../.gitbook/assets/13_06 56 07.gif" alt=""><figcaption></figcaption></figure>

If everything is initialized correctly, you should see a clickable button at the top-left of the screen. Keep in mind that this is simply a test to make sure Gum is working properly. You may want to delete this button once you begin working on your game.

## Troubleshooting

{% tabs %}
{% tab title="Nez" %}
Could not load file or assembly 'MonoGame.Framework, Version=3.8.1.303

If you add the Gum code to your project, you may experience this exception internally from Nez:

<figure><img src="../../../../.gitbook/assets/image (71).png" alt=""><figcaption></figcaption></figure>

The reason this is happening is because currently (as of July 2024) Nez links MonoGame 3.8.0 instead of 3.8.1 (the latest).

To solve this problem, your project must explicitly link MonoGame 3.8.1 or else you will have this exception.

To do this:

1. Open your project in Visual Studio
2. Expand the Dependencies item
3.  Right-click on Packages and select Manage NuGet Packages\\

    <figure><img src="../../../../.gitbook/assets/image (73).png" alt=""><figcaption><p>Right-click Manage NuGet Packages... option</p></figcaption></figure>
4. Click on the Browse tab
5. Search for MonoGame.Framework
6.  Select the MonoGame.Framework NuGet package for your particular project type. This is most likely MonoGame.Framework.DesktopGL, but it may be different if you are targeting another platform.\\

    <figure><img src="../../../../.gitbook/assets/image (74).png" alt=""><figcaption><p>MonoGame.Framework NuGet packages</p></figcaption></figure>
7. Click the Install button to add the NuGet package

After adding MonoGame, your NuGet packages should similar to the following image:
{% endtab %}
{% endtabs %}
