# Setup

This page assumes you have an existing MonoGame project. This can be an empty project or an existing game.

MonoGame Gum works on a variety of platforms including DesktopGL, DirectX, and mobile. It's fully functional with all flavors of XNA-like libraries including MonoGame, Kni (including on web), and FNA. It can be used alongside other libraries such as MonoGameExtended and Nez. If your particular platform is not supported please contact us on Discord and we will do our best to add support.

### Adding Gum NuGet Packages

1. Open your MonoGame project in your preferred IDE.
2.  Add the `Gum.MonoGame` NuGet package

    <figure><img src="../../../.gitbook/assets/NugetMonoGameGumSetup1.png" alt=""><figcaption><p>Add Gum.MonoGame NuGet Package to your project</p></figcaption></figure>

{% hint style="info" %}
The Gum.MonoGame NuGet package is referenced regardless of which platform your game targets.
{% endhint %}

### Initializing Gum

To initialize Gum, modify your Game project (such as Game1.cs) so that it includes the following calls:

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using MonoGameGum.Forms;

public class Game1 : Game
{
    GraphicsDeviceManager _graphics;
    GumService GumUI => GumService.Default;
    
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }    
    
    protected override void Initialize()
    {
        GumUI.Initialize(this, DefaultVisualsVersion.V2);
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

### Testing Gum

To test that you have successfully added Gum to the project, modify your Initialize method to add a rectangle:

{% tabs %}
{% tab title="Full Code" %}
```csharp
protected override void Initialize()
{
    GumUI.Initialize(this, DefaultVisualsVersion.V2);

    var button = new Button();
    button.AddToRoot();
    button.Click += (_,_) =>
        button.Text = "Clicked at\n" + DateTime.Now;


    base.Initialize();
}
```
{% endtab %}

{% tab title="Diff" %}
```diff
protected override void Initialize()
{
    GumUI.Initialize(this, DefaultVisualsVersion.V2);

+   var button = new Button();
+   button.AddToRoot();
+   button.Click += (_,_) =>
+       button.Text = "Clicked at\n" + DateTime.Now;

    base.Initialize();
}
```
{% endtab %}
{% endtabs %}

<figure><img src="../../../.gitbook/assets/13_06 56 07.gif" alt=""><figcaption></figcaption></figure>

If everything is initialized correctly, you should see a clickable button at the top-left of the screen.

### Loading Gum Projects

This page shows the minimum code needed to get Gum up and running in your project. If you want to load a Gum project created in the Gum tool, see the [Loading a Gum Project](../loading-.gumx-gum-project.md) page.

### Downloading the Gum Tool

The Gum tool can be used to create Gum projects which can be loaded into your MonoGame project. To download and run the Gum tool, see the [Setup page](../../../gum-tool/setup/).
