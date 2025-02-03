# Setup

This page assumes you have an existing MonoGame project. This can be an empty project or an existing game.

MonoGame Gum works on a variety of platforms including DesktopGL, DirectX, and mobile. It's fully functional with all flavors of XNA-like libraries including MonoGame, Kni (including on web), and FNA. It can be used alongside other libraries such as MonoGameExtended and Nez. If your particular platform is not supported please contact us on Discord and we will do our best to add support.

### Adding Gum NuGet Packages

1. Open your MonoGame project in your preferred IDE.
2.  Add the `Gum.MonoGame` NuGet package\\

    <figure><img src="../../../.gitbook/assets/NugetMonoGameGumSetup1.png" alt=""><figcaption><p>Add Gum.MonoGame NuGet Package to your project</p></figcaption></figure>

### Initializing Gum

To initialize Gum, modify your Game project (such as Game1.cs) so that it includes the following calls:

```csharp
protected override void Initialize()
{
    MonoGameGum.GumService.Default.Initialize(this);
    base.Initialize();
}

protected override void Update(GameTime gameTime)
{
    MonoGameGum.GumService.Default.Update(this, gameTime);
    base.Update(gameTime);
}

protected override void Draw(GameTime gameTime)
{
    GraphicsDevice.Clear(Color.CornflowerBlue);
    MonoGameGum.GumService.Default.Draw();
    base.Draw(gameTime);
}
```

### Testing Gum

To test that you have successfully added Gum to the project, modify your Initialize method to add a rectangle:

```csharp
protected override void Initialize()
{
    MonoGameGum.GumService.Default.Initialize(this);

    var rectangle = new ColoredRectangleRuntime();
    rectangle.Width = 100;
    rectangle.Height = 100;
    rectangle.Color = Color.White;
    rectangle.AddToManagers(SystemManagers.Default, null);

    base.Initialize();
}
```

<figure><img src="../../../.gitbook/assets/image (25).png" alt=""><figcaption><p>White ColoredRectangleRuntime in game</p></figcaption></figure>

If everything is initialized correctly, you should see a white rectangle at the top-left of the screen.

### Loading Gum Projects

This page shows the minimum code needed to get Gum up and running in your project. If you want to load a Gum project created in the Gum tool, see the [Loading a Gum Project](../loading-.gumx-gum-project.md) page.

### Downloading the Gum Tool

The Gum tool can be used to create Gum projects which can be loaded into your MonoGame project. To download and run the Gum tool, see the [Setup page](../../../gum-tool/setup/).
