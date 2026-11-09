using Gum;
using Gum.Forms.Controls;
using KniSkiaGumCodeGeneration.Screens;
using Microsoft.Xna.Framework;

namespace KniSkiaGumCodeGeneration;

/// <summary>
/// The KNI Skia version of MonoGameGumCodeGeneration: shows the generated MainMenuFullGeneration screen,
/// drawn by SkiaGum.
/// </summary>
public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;

#if DEBUG
    // Plays an NDJSON script when launched with --gum-automation <script>; null otherwise.
    // Automation/smoke.ndjson has the launch command; GumAutomationHost documents the flags.
    private readonly Gum.Automation.GumAutomationHost? _automation;
#endif
    private MainMenuFullGeneration? _mainMenu;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1280,
            PreferredBackBufferHeight = 720
        };
        IsMouseVisible = true;

#if DEBUG
        _automation = Gum.Automation.GumAutomationHost.FromArgs(Environment.GetCommandLineArgs(), this);
#endif
    }

    protected override void Initialize()
    {
        // The graphics device exists once the base Initialize has run.
        base.Initialize();

        // Skia reads the project from disk. The csproj copies Content/GumProject to the output.
        GumService.Default.Initialize(this, "Content/GumProject/GumProject.gumx");

        _mainMenu = new MainMenuFullGeneration
        {
            Name = "MainMenu"
        };
        _mainMenu.AddToRoot();
    }

    protected override void Update(GameTime gameTime)
    {
#if DEBUG
        _automation?.BeforeUpdate();
#endif
        GumService.Default.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        GumService.Default.Draw();
        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        GumService.Default.Root.Children.Clear();
        _mainMenu = null;
        base.UnloadContent();
    }
}
