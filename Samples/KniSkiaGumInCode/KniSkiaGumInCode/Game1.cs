using Gum;
using GumSamples;
using GumSamples.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SilkNetGum.Screens;

namespace KniSkiaGumInCode;

/// <summary>
/// The KNI Skia version of MonoGameGumInCode: a nav strip of feature screens built in code, drawn by SkiaGum.
/// The screens are the SKIA builds of the shared screen files that SilkNetGum also shows.
/// </summary>
public class Game1 : Game
{
    GumService GumUI => GumService.Default;

    private readonly GraphicsDeviceManager _graphics;

#if DEBUG
    // Plays an NDJSON script when launched with --gum-automation <script>; null otherwise.
    // Automation/smoke.ndjson has the launch command; GumAutomationHost documents the flags.
    private readonly Gum.Automation.GumAutomationHost? _automation;
#endif

    private readonly GumGallery gallery = new GumGallery();

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1400,
            PreferredBackBufferHeight = 900,
        };
        IsMouseVisible = true;
        Window.AllowUserResizing = true;

#if DEBUG
        _automation = Gum.Automation.GumAutomationHost.FromArgs(Environment.GetCommandLineArgs(), this);
#endif
    }

    protected override void Initialize()
    {
        // The screens load their files (Content/..., resources/...) relative to the working directory.
        // Visual Studio starts the sample in its output folder, but `dotnet run` keeps the folder it
        // was run from.
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);

        GumUI.Initialize(this);

        GumUI.UseKeyboardDefaults();

        GumUI.UseGamepadDefaults();

        // The shared screens load their textures relative to the Gum project folder, as in SilkNetGum.
        ToolsUtilities.FileManager.RelativeDirectory = "Content/GumProject/";

        // Gum core ships no shader loader, so the app registers a resolver that turns a
        // ContainerRuntime.SourceShaderFile (.sksl path) into a compiled SKRuntimeEffect.
        SkiaGum.CustomSetPropertyOnRenderable.RenderTargetEffectResolver =
            RenderTargetShaderScreen.CompileEffectFromFile;

        // Adds .fx and .slang files to SourceShaderFile (ShadowDusk converts them to SkSL); the .sksl
        // resolver above keeps handling .sksl files.
        GumUI.UseShadowDusk();

        // Demo the auto-fit helpers. Flip via the Zoom/Expand buttons in the nav strip.
        GumUI.EnableZoomToWindow();

        gallery.Build("Forms");

        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        bool isAutomated = false;
#if DEBUG
        _automation?.BeforeUpdate();
        // An automation run reads no OS input; the script's scripted cursor and keyboard drive Gum.
        isAutomated = _automation != null;
#endif
        if (!isAutomated && (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape)))
        {
            Exit();
        }

        GumUI.Update(gameTime);

        gallery.Tick(gameTime.ElapsedGameTime.TotalSeconds);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        GumUI.Draw();
        base.Draw(gameTime);
    }
}
