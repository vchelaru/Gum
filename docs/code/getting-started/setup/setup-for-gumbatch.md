# Setup for GumBatch (Optional)

### Introduction

Gum provides a GumBatch object which works similar to SpriteBatch. It can be used for _immediate mode_ rendering, which allows for calling `Begin`, `Draw`, and `End` just like SpriteBatch. This is useful if your project requires mixing Gum and MonoGame rendering, or if you are more comfortable using a SpriteBatch-like interface.

This page assumes you have an existing MonoGame project. This can be an empty project or an existing game.

Usage of GumBatch is completely optional, and it is only needed if you want to draw Gum objects at a particular point in your drawing code. If you are using Gum to load .gumx projects, or if you would like Gum to handle all UI or HUD rendering, then you do not need to use GumBatch.

### GumBatch Quick Start

GumBatch takes the place of GumService, so if your project includes GumService you should remove its usage from your Game code.

To initialize a GumBatch, you must:

* Declare a GumBatch at class scope
* Initialize the Gum SystemManagers
* Initialize the GumBatch
* (optional) load a .fnt file
* Draw with GumBatch in your Draw

The following shows a simple Game1.cs file which renders Gum Text:

<pre class="language-csharp"><code class="lang-csharp">public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
<strong>    RenderingLibrary.Graphics.BitmapFont font;
</strong><strong>    GumBatch gumBatch;
</strong>
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
<strong>        MonoGameGum.GumService.Default.Initialize(this);
</strong>        
<strong>        gumBatch = new GumBatch();
</strong><strong>        font = new RenderingLibrary.Graphics.BitmapFont(
</strong><strong>            "Fonts/Font18Caladea.fnt", 
</strong><strong>            SystemManagers.Default);
</strong>
        base.Initialize();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

<strong>        gumBatch.Begin();
</strong>        
<strong>        gumBatch.DrawString(
</strong><strong>            font, 
</strong><strong>            "This is using Gum Batch", 
</strong><strong>            new Vector2(0, 150), 
</strong><strong>            Color.White);
</strong>            
<strong>        gumBatch.End();
</strong>
        base.Draw(gameTime);
    }
}

</code></pre>

This code produces the following image:

<figure><img src="../../../.gitbook/assets/image (61).png" alt=""><figcaption><p>GumBatch rendering text</p></figcaption></figure>

Note that this code assumes a font .fnt file (and matching .png) are in the Content/Fonts/ folder. All content is loaded relative to the Content folder, just like normal content in MonoGame. Also note that this content does not use the content pipeline, but must be set to Copy to Output.

<figure><img src="../../../.gitbook/assets/image (62).png" alt=""><figcaption><p>.fnt file copied to output folder</p></figcaption></figure>

For more information on loading FNT files, see the [File Loading](../../monogame/file-loading.md) documentation.

For a more detailed discussion of using GumBatch, see the [GumBatch](../../monogame/gumbatch.md) page.
