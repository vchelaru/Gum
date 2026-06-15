using Gum;
using Gum.Wireframe;
using KernSmith.Gum;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace FontPlayground.MonoGame;

/// <summary>
/// Thin MonoGame host for the dynamic-font playground. All UI and live-update logic lives in the
/// platform-neutral <see cref="FontPlaygroundScreen"/> (shared via a linked source file). This host
/// only bootstraps Gum, registers KernSmith for in-memory font generation, and pumps Update/Draw.
///
/// Fonts are generated in memory by KernSmith — there are no .fnt files shipped with this sample.
/// </summary>
public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 1024;
        _graphics.PreferredBackBufferHeight = 768;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        GumService.Default.Initialize(this);

        // Wire up KernSmith so any TextRuntime can get a font for any (family, size, style)
        // without a .fnt file on disk. This is what makes the live preview re-render work.
        CustomSetPropertyOnRenderable.InMemoryFontCreator =
            new KernSmithFontCreator(GraphicsDevice);

        FontPlaygroundScreen.Build(GumService.Default.Root);

        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        GumService.Default.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(30, 30, 46));

        GumService.Default.Draw();

        base.Draw(gameTime);
    }
}
