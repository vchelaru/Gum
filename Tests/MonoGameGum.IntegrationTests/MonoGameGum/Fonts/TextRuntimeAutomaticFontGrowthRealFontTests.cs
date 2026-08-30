using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using KernSmith.Gum;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Fonts;

/// <summary>
/// Issue #4542 end-to-end: TextRuntime.UseAutomaticFontGrowth through the real KernSmithFontCreator
/// (not a fake) -- proves the production IGrowableFontCreator wiring actually grows a live font's
/// texture, warns on a genuinely unrenderable glyph, and replays growth into a freshly-oversampled
/// font. Mirrors TextRuntimeFontOversamplingRealFontTests.cs's MinimalGame setup.
/// </summary>
public class TextRuntimeAutomaticFontGrowthRealFontTests : BaseTestClass
{
    private static readonly string OrbitronPath = System.IO.Path.Combine(
        AppContext.BaseDirectory, "Content", "Fonts", "Orbitron-Black.ttf");

    // U+20AC ('€') is confirmed present in Orbitron-Black.ttf's cmap but outside BmfcSave's default
    // Ranges (32-126,160-255), so a freshly-resolved font never bakes it in -- a genuine growth
    // candidate. U+2192 ('→') is confirmed absent from the font entirely (verified via KernSmith's
    // BmFont.ReadFontInfo(...).AvailableCodepoints against the fixture, not guessed).
    private const char GrowableChar = '€';
    private const char UnrenderableChar = '→';

    [Fact]
    public void AutomaticFontGrowth_RealFont_MissingCharacter_GrowsFontWithARealNonFallbackGlyph()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        bool savedFlag = TextRuntime.UseAutomaticFontGrowth;
        try
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithFontCreator(game.GraphicsDevice);
            TextRuntime.UseAutomaticFontGrowth = true;

            TextRuntime textRuntime = new();
            textRuntime.UseCustomFont = true;
            textRuntime.CustomFontFile = OrbitronPath;
            textRuntime.FontSize = 24;
            game.GumService.Root.Children.Add(textRuntime);

            Text text = (Text)textRuntime.RenderableComponent;
            text.BitmapFont.HasCharacter(GrowableChar).ShouldBeFalse(
                "because the default Ranges must not already include this character, or growth has nothing to prove");

            textRuntime.Text = $"Price: {GrowableChar}10";

            text.BitmapFont.HasCharacter(GrowableChar).ShouldBeTrue(
                "because the missing character must be grown synchronously, in the same Text assignment that discovered it");
            BitmapCharacterInfo info = text.BitmapFont.GetCharacterInfo(GrowableChar);
            (info.PixelRight - info.PixelLeft).ShouldBeGreaterThan(0,
                "because the grown glyph must have real, non-fallback dimensions, not the space glyph's");
        }
        finally
        {
            TextRuntime.UseAutomaticFontGrowth = savedFlag;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    [Fact]
    public void AutomaticFontGrowth_RealFont_CharacterWithNoGlyphInTheFontFile_RaisesPropertyAssignmentError()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        bool savedFlag = TextRuntime.UseAutomaticFontGrowth;
        List<string> reportedMessages = new();
        System.Action<string> handler = reportedMessages.Add;
        try
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithFontCreator(game.GraphicsDevice);
            TextRuntime.UseAutomaticFontGrowth = true;

            TextRuntime textRuntime = new();
            textRuntime.UseCustomFont = true;
            textRuntime.CustomFontFile = OrbitronPath;
            textRuntime.FontSize = 24;
            game.GumService.Root.Children.Add(textRuntime);

            CustomSetPropertyOnRenderable.PropertyAssignmentError += handler;
            reportedMessages.Clear();
            textRuntime.Text = $"Go{UnrenderableChar}";

            Text text = (Text)textRuntime.RenderableComponent;
            text.BitmapFont.HasCharacter(UnrenderableChar).ShouldBeFalse();
            reportedMessages.ShouldHaveSingleItem();
            reportedMessages[0].ShouldContain(UnrenderableChar.ToString());
        }
        finally
        {
            CustomSetPropertyOnRenderable.PropertyAssignmentError -= handler;
            TextRuntime.UseAutomaticFontGrowth = savedFlag;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // Issue #4542 design decision #3: a full RegenerateOversampledFont builds a brand-new BitmapFont
    // from BmfcSave.Ranges alone -- through the real KernSmith incremental-session machinery (not a
    // fake), previously-grown characters must still be replayed into it.
    [Fact]
    public void AutomaticFontGrowth_RealFont_OversamplingActive_RegenerateReplaysGrownCharacterIntoTheNewFont()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        bool savedGrowth = TextRuntime.UseAutomaticFontGrowth;
        bool savedOversampling = TextRuntime.UseFontOversampling;
        try
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithFontCreator(game.GraphicsDevice);
            TextRuntime.UseAutomaticFontGrowth = true;
            TextRuntime.UseFontOversampling = true;

            TextRuntime textRuntime = new();
            textRuntime.UseCustomFont = true;
            textRuntime.CustomFontFile = OrbitronPath;
            textRuntime.FontSize = 24;
            game.GumService.Root.Children.Add(textRuntime);

            textRuntime.Text = $"Price: {GrowableChar}10";
            Text text = (Text)textRuntime.RenderableComponent;
            text.BitmapFont.HasCharacter(GrowableChar).ShouldBeTrue("because the native font must have grown first");

            bool result = textRuntime.RegenerateOversampledFont(2.5f);

            result.ShouldBeTrue();
            text.BitmapFont.HasCharacter(GrowableChar).ShouldBeTrue(
                "because previously-grown characters must be replayed into the freshly-regenerated oversampled font");
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedOversampling;
            TextRuntime.UseAutomaticFontGrowth = savedGrowth;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    private class MinimalGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        public Gum.GumService GumService { get; }

        public MinimalGame()
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this);
            GumService = new Gum.GumService();
        }

        protected override void Initialize()
        {
            base.Initialize();
            GumService.Initialize(this, Gum.Forms.DefaultVisualsVersion.V3);
        }

        protected override void Update(GameTime gameTime) { }
        protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(Color.CornflowerBlue);

        protected override void Dispose(bool disposing)
        {
            if (GumService.IsInitialized)
            {
                GumService.Uninitialize();
            }
            LoaderManager.Self?.DisposeAndClear();
            base.Dispose(disposing);
        }
    }
}
