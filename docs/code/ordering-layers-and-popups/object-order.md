# Object Order

## Introduction

This document discusses how to control the visual order of objects which overlap. This section also discusses click priority which is based on visual order.

## Order and Hierarchy

By default Gum draws and performs click checks by traversing the scene. When drawing is performed, the root object iterates through its children. Each child draws itself and all of its own children recursively before siblings are drawn. In other words, siblings at the end of the Children list are drawn last (on top).

The following code shows the draw order by creating Button instances in a loop. The buttons added at the end draw on top of the first buttons.

```csharp
for(int i = 0; i < 10; i++)
{
    Button button = new();
    button.AddToRoot();
    button.X = i * 30;
    button.Y = i * 5;
}
```

The later buttons also have click priority since they are drawn on top.

<figure><img src="../../.gitbook/assets/07_20 26 09.gif" alt=""><figcaption></figcaption></figure>

The order of items can be adjusted by reordering the Children property on the item's parent. The code above adds all buttons to the root, so the button visuals can be reordered in the root.

The following code brings the clicked button to the front of all children:

<pre class="language-csharp"><code class="lang-csharp">for(int i = 0; i &#x3C; 10; i++)
{
    Button button = new();
    button.AddToRoot();
    button.X = i * 30;
    button.Y = i * 5;

<strong>    button.Click += (_, _) =>
</strong><strong>    {
</strong><strong>        var buttonIndex = GumUi.Root.Children.IndexOf(button.Visual);
</strong><strong>        GumUi.Root.Children.Move(buttonIndex, GumUi.Root.Children.Count - 1);
</strong><strong>    };
</strong>}
</code></pre>

<figure><img src="../../.gitbook/assets/07_20 36 30.gif" alt=""><figcaption></figcaption></figure>

## Manual Draw and Updates

{% hint style="info" %}
The code presented below requires version 2026.1.8 or newer.
{% endhint %}

A typical Gum project includes the following draw and update calls:

<pre class="language-csharp"><code class="lang-csharp">protected override void Update(GameTime gameTime)
{
<strong>    GumUi.Update(gameTime);
</strong>
    base.Update(gameTime);
}

protected override void Draw(GameTime gameTime)
{
    GraphicsDevice.Clear(Color.CornflowerBlue);

<strong>    GumUi.Draw();
</strong>
    base.Draw(gameTime);
}
</code></pre>

These draw calls Update and Draw the root object. If all of our items should be drawn in one call, then this simple approach will work. However, you may want to mix drawing code with Gum, such as by drawing a sprite in between Gum objects.

The following code shows how to draw two Gum windows - one is below and one is above a SpriteBatch draw call.

```csharp
public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    SpriteBatch _spriteBatch;
    GumBatch _gumBatch;

    GumService GumUi => GumService.Default;

    ContainerRuntime _belowEverythingContainer;
    ContainerRuntime _aboveEverythingContainer;

    Texture2D _gumLogoTexture;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        GumService.Default.Initialize(this, Gum.Forms.DefaultVisualsVersion.V3);
        _spriteBatch = new SpriteBatch(this.GraphicsDevice);
        _gumLogoTexture = Texture2D.FromFile(GraphicsDevice, "Content/GumLogo.png");
        _gumBatch  = new GumBatch();


        _belowEverythingContainer = new();
        _belowEverythingContainer.Dock(Dock.Fill);

        _aboveEverythingContainer = new();
        _aboveEverythingContainer.Dock(Dock.Fill);

        Window belowEverythingWindow = new ();
        _belowEverythingContainer.AddChild(belowEverythingWindow);

        Window aboveEverythingWindow = new();
        _aboveEverythingContainer.AddChild(aboveEverythingWindow);

        base.Initialize();
    }

    List<GraphicalUiElement> itemsToUpdate = new List<GraphicalUiElement>();
    protected override void Update(GameTime gameTime)
    {
        itemsToUpdate.Clear();

        itemsToUpdate.Add(_belowEverythingContainer);
        itemsToUpdate.Add(GumUi.Root);
        itemsToUpdate.Add(_aboveEverythingContainer);

        GumUi.Update(gameTime, itemsToUpdate);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _gumBatch.Begin();
        _gumBatch.Draw(_belowEverythingContainer);
        _gumBatch.End();

        _spriteBatch.Begin();
        _spriteBatch.Draw(_gumLogoTexture, Vector2.Zero, Color.White);
        _spriteBatch.End();

        GumUi.Draw();

        _gumBatch.Begin();
        _gumBatch.Draw(_aboveEverythingContainer);
        _gumBatch.End();

        base.Draw(gameTime);
    }
}

```

<figure><img src="../../.gitbook/assets/08_05 40 56.gif" alt=""><figcaption><p>Two windows, one drawn below and one drawn above a SpriteBatch Draw</p></figcaption></figure>
