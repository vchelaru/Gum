# Setup for GumBatch (Optional)

### Introduction

Gum provides a GumBatch object which works similar to SpriteBatch. It can be used for _immediate mode_ rendering, which allows for calling `Begin`, `Draw`, and `End` just like SpriteBatch. This is useful if your project requires mixing Gum and MonoGame rendering, or if you are more comfortable using a SpriteBatch-like interface.

This page assumes you have an existing MonoGame project. This can be an empty project or an existing game.

Usage of GumBatch is completely optional, and it is only needed if you want to draw Gum objects at a partciular point in your drawing code. If you are using Gum to load .gumx projects, or if you would like Gum to handle all UI or HUD rendering, then you do not need to use GumBatch.

### Adding Gum NuGet Packages

1. Open your MonoGame project in your preferred IDE.
2. Add the `Gum.MonoGame` NuGet package

<figure><img src="../../.gitbook/assets/NugetMonoGameGumSetup1.png" alt=""><figcaption><p>Add Gum.MonoGame NuGet Package to your project</p></figcaption></figure>

### GumBatch Quick Start

To initialize a GumBatch, you must:

* Declare a GumBatch at class scope
* Initialize the Gum SystemManagers - this is needed whether you use GumBatch or the full Gum _retained mode_ rendering system
* Initialize the GumBatch
* (optional) load a .fnt file
* Draw with gumBatch in your Draw

The following shows a simple Game1.cs file which renders Gum Text:

```csharp
public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    RenderingLibrary.Graphics.BitmapFont font;
    GumBatch gumBatch;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        MonoGameGum.GumService.Default.Initialize(this);
        
        gumBatch = new GumBatch();
        font = new RenderingLibrary.Graphics.BitmapFont(
            "Fonts/Font18Caladea.fnt", 
            SystemManagers.Default);

        base.Initialize();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        gumBatch.Begin();
        
        gumBatch.DrawString(
            font, 
            "This is using Gum Batch", 
            new Vector2(0, 150), 
            Color.White);
            
        gumBatch.End();

        base.Draw(gameTime);
    }
}

```

This code produces the following image:

<figure><img src="../../.gitbook/assets/image (61).png" alt=""><figcaption><p>GumBatch rendering text</p></figcaption></figure>

Note that this code assumes a font .fnt file (and matching .png) are in the Content/Fonts/ folder. All content is loaded relative to the Content folder, just like normal content in MonoGame. Also note that this content does not use the content pipeline, but must be set to Copy to Output.

<figure><img src="../../.gitbook/assets/image (62).png" alt=""><figcaption><p>.fnt file copied to output folder</p></figcaption></figure>

For more information on loading FNT files, see the [File Loading](file-loading.md) documentation.

For a more detailed discussion of using GumBatch, see the [GumBatch](gumbatch.md) page.
