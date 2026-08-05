using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using KernSmith.Gum;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Graphics;

namespace TextureFilterDemo.MonoGame;

/// <summary>
/// Point vs. linear texture filtering on Text (#3496, split out from the general TextScreen sample
/// in #4333). Font baked SMALL then magnified via FontScale, so point-filtering's blocky glyph edges
/// are visibly distinct from bilinear's smoothed ones -- a large FontSize at 1x scale doesn't stress
/// the sampler enough to show a difference. Renderer.TextureFilter is a single global sampler state
/// for the whole SpriteBatch pass, so one Text can't be Point and another Linear on the same layer
/// (see docs/code/rendering/texture-filtering.md) -- each side gets its own Layer with
/// Layer.IsLinearFilteringEnabled forcing the mode. Both texts are added directly as top-level Layer
/// members with fixed positions; neither is parented to a layout container, so there's no conflict
/// between layout-child and layer-member duty (see #4333) to fight.
/// </summary>
public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 500;
        _graphics.PreferredBackBufferHeight = 200;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        GumService.Default.Initialize(this);
        CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithFontCreator(GraphicsDevice);

        var pointLayer = SystemManagers.Default.Renderer.AddLayer();
        pointLayer.Name = "Texture Filter - Point";
        pointLayer.IsLinearFilteringEnabled = false;
        var pointText = new TextRuntime();
        pointText.FontSize = 12;
        pointText.FontScale = 4;
        pointText.X = 16;
        pointText.Y = 16;
        pointText.Text = "Point filter (blocky)";
        pointText.AddToManagers(SystemManagers.Default, pointLayer);

        var linearLayer = SystemManagers.Default.Renderer.AddLayer();
        linearLayer.Name = "Texture Filter - Linear";
        linearLayer.IsLinearFilteringEnabled = true;
        var linearText = new TextRuntime();
        linearText.FontSize = 12;
        linearText.FontScale = 4;
        linearText.X = 16;
        linearText.Y = 90;
        linearText.Text = "Linear filter (smoothed)";
        linearText.AddToManagers(SystemManagers.Default, linearLayer);

        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        GumService.Default.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        GumService.Default.Draw();
        base.Draw(gameTime);
    }
}
