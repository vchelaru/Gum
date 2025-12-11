# TransformMatrix

## Introduction

TransformMatrix can be used to adjust the cursor's position. This is useful if your game is scaling its graphics using a matrix or render targets.

## Code Example: Cursor Offsets with TransformMatrix

```csharp
public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;

    GumService GumUI => MonoGameGum.GumService.Default;

    SpriteBatch _spriteBatch;
    RenderTarget2D _renderTarget;

    int spriteBatchXOffset = 100;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        GumUI.Initialize(this, Gum.Forms.DefaultVisualsVersion.V3);

        _renderTarget = new RenderTarget2D(GraphicsDevice, 400, 400);
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        GumUI.CanvasWidth = 400;
        GumUI.CanvasHeight = 400;

        var button = new Button();
        button.AddToRoot();
        button.Text = "Click Me";

        // account for the offset by setting the cursor's TransformMatrix
        // use a negative value to "undo" the translation in the SpriteBatch draw
        GumUI.Cursor.TransformMatrix = Matrix.CreateTranslation(-spriteBatchXOffset, 0, 0);

        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        GumUI.Update(this, gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        GraphicsDevice.SetRenderTarget(_renderTarget);
        GumUI.Draw();

        GraphicsDevice.SetRenderTarget(null);
        _spriteBatch.Begin();
        _spriteBatch.Draw(_renderTarget, new Vector2(spriteBatchXOffset, 0), Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
```

<figure><img src="../../../.gitbook/assets/11_05 57 54.gif" alt=""><figcaption><p>Cursor interacting with a Button drawn on an offset render target</p></figcaption></figure>
