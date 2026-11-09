using Gum;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Graphics;

namespace KniSkiaGumFontLoading;

/// <summary>
/// The KNI Skia version of MonoGameGumFontLoading: texts that use a custom font file, plus one text drawn in
/// immediate mode. Skia renders .ttf/.otf files, so the fonts are TrueType rather than bmfont .fnt files.
/// </summary>
public class Game1 : Game
{
    GumService GumUI => GumService.Default;

    private readonly GraphicsDeviceManager _graphics;
    private readonly List<TextRuntime> _texts = new List<TextRuntime>();

#if DEBUG
    // Plays an NDJSON script when launched with --gum-automation <script>; null otherwise.
    // Automation/smoke.ndjson has the launch command; GumAutomationHost documents the flags.
    private readonly Gum.Automation.GumAutomationHost? _automation;
#endif
    private TextRuntime _immediateText = null!;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        IsMouseVisible = true;

#if DEBUG
        // The texts are added to the managers, not the root, so the script finds them through _texts.
        _automation = Gum.Automation.GumAutomationHost.FromArgs(Environment.GetCommandLineArgs(), this, _texts);
#endif
    }

    protected override void Initialize()
    {
        GumUI.Initialize(this);

        GumUI.UseKeyboardDefaults();

        GumUI.UseGamepadDefaults();

        AddText("CustomFontText", "Content/Fonts/CustomFont.ttf", "I use CustomFont", 0);
        AddText("SuperMorningText", "Content/Fonts/Super Morning.ttf", "I use Super Morning", 30);
        AddText("WinterSelfieText", "Content/Fonts/Winter Selfie.ttf", "I use Winter Selfie", 60);

        _immediateText = new TextRuntime
        {
            UseCustomFont = true,
            CustomFontFile = "Content/Fonts/CustomFont.ttf",
            Text = "I am an immediate mode TextRuntime",
            X = 110,
            Y = 120
        };

        // Drawn on top of Gum's own draw, inside the same Skia pass.
        GumUI.AfterDraw += _ =>
        {
            Renderer renderer = SystemManagers.Default.Renderer;
            renderer.Begin();
            renderer.Draw(_immediateText);
            renderer.End();
        };

        base.Initialize();
    }

    private void AddText(string name, string fontFile, string text, float yPosition)
    {
        TextRuntime textRuntime = new TextRuntime
        {
            Name = name,
            UseCustomFont = true,
            CustomFontFile = fontFile,
            Text = text,
            Y = yPosition
        };
        textRuntime.AddToManagers(SystemManagers.Default, null);
        _texts.Add(textRuntime);
    }

    protected override void Update(GameTime gameTime)
    {
#if DEBUG
        _automation?.BeforeUpdate();
#endif
        GumUI.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        GumUI.Draw();
        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        foreach (TextRuntime text in _texts)
        {
            text.RemoveFromManagers();
        }
        _texts.Clear();
        base.UnloadContent();
    }
}
