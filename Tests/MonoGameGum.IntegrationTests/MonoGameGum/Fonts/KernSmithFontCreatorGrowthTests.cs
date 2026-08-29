using KernSmith.Gum;
using KernSmith.Output;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Fonts;

/// <summary>
/// Issue #4535 Phase 2 -- <see cref="KernSmithFontCreator.TryAddGlyphs"/> grows a font this creator
/// already built via <see cref="KernSmithFontCreator.TryCreateFont"/>, blitting new glyph pixels into
/// the live <see cref="Texture2D"/> instead of a full regenerate. Uses a real <see cref="GraphicsDevice"/>
/// (mirrors <see cref="KernSmithFontCreatorTests"/>) since this exercises actual texture uploads/reads.
/// </summary>
public class KernSmithFontCreatorGrowthTests : BaseTestClass
{
    [Fact]
    public void TryAddGlyphs_AddsANewCharacter_UpdatesMetricsAndBlitsPixels()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        KernSmithFontCreator creator = new(game.GraphicsDevice);

        BmfcSave bmfcSave = BuildBmfcSave(ranges: "65"); // just 'A'
        BitmapFont? font = creator.TryCreateFont(bmfcSave);
        font.ShouldNotBeNull();

        GlyphAdditionResult? result = creator.TryAddGlyphs(font!, bmfcSave, "B");

        result.ShouldNotBeNull();
        result!.Added.Select(g => g.Codepoint).ShouldContain((int)'B');

        BitmapCharacterInfo bInfo = font!.GetCharacterInfo('B');
        bInfo.ShouldNotBeNull();
        (bInfo.PixelRight - bInfo.PixelLeft).ShouldBeGreaterThan(0, "because 'B' must have real, non-fallback glyph dimensions after growth");

        Texture2D page = font.Textures[0];
        Color[] pixels = new Color[page.Width * page.Height];
        page.GetData(pixels);
        bool anyOpaquePixelInGlyphRegion = false;
        for (int y = bInfo.PixelTop; y < bInfo.PixelBottom && !anyOpaquePixelInGlyphRegion; y++)
        {
            for (int x = bInfo.PixelLeft; x < bInfo.PixelRight; x++)
            {
                if (pixels[y * page.Width + x].A > 0)
                {
                    anyOpaquePixelInGlyphRegion = true;
                    break;
                }
            }
        }
        anyOpaquePixelInGlyphRegion.ShouldBeTrue("because the new glyph's pixel bytes must actually be blitted into the live texture");
    }

    [Fact]
    public void TryAddGlyphs_WhenFontWasNotCreatedByThisCreator_ReturnsNull()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        KernSmithFontCreator creator = new(game.GraphicsDevice);

        BmfcSave bmfcSave = BuildBmfcSave(ranges: "65");
        BitmapFont untrackedFont = new BitmapFont((Texture2D)null!, MinimalFontData);
        untrackedFont.SetFontPattern(256, 256);

        GlyphAdditionResult? result = creator.TryAddGlyphs(untrackedFont, bmfcSave, "B");

        result.ShouldBeNull();
    }

    [Fact]
    public void TryAddGlyphs_WhenAtlasGrows_ReallocatesTextureAndKeepsExistingGlyphPixelPositions()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        KernSmithFontCreator creator = new(game.GraphicsDevice);

        // A generous font size against a tiny max atlas ceiling forces a Grow on the second add.
        BmfcSave bmfcSave = BuildBmfcSave(ranges: "65", fontSize: 48);
        bmfcSave.OutputWidth = 64;
        bmfcSave.OutputHeight = 64;
        BitmapFont? font = creator.TryCreateFont(bmfcSave);
        font.ShouldNotBeNull();

        BitmapCharacterInfo originalAInfo = font!.GetCharacterInfo('A');
        int originalPixelLeft = originalAInfo.PixelLeft;
        int originalPixelTop = originalAInfo.PixelTop;
        int originalTextureWidth = font.Textures[0].Width;

        bmfcSave.OutputWidth = 4096;
        bmfcSave.OutputHeight = 4096;
        GlyphAdditionResult? result = creator.TryAddGlyphs(font, bmfcSave, "WWWWWWWWWWWWWWWW");

        result.ShouldNotBeNull();

        BitmapCharacterInfo rescaledAInfo = font.GetCharacterInfo('A');
        rescaledAInfo.PixelLeft.ShouldBe(originalPixelLeft, "because a grow must never move an already-placed glyph's pixel position");
        rescaledAInfo.PixelTop.ShouldBe(originalPixelTop);

        int newTextureWidth = font.Textures[0].Width;
        newTextureWidth.ShouldBeGreaterThan(originalTextureWidth, "because the atlas must actually have grown to fit the new batch");
        rescaledAInfo.TULeft.ShouldBe(originalPixelLeft / (float)newTextureWidth, tolerance: 0.0001f,
            "because 'A's UV must be rescaled against the NEW texture width, not left stale from the old one");
    }

    // Issue #4542: TextRuntime's automatic growth trigger can't reference KernSmithFontCreator
    // directly (that would make RenderingLibrary/MonoGameGum depend on the optional KernSmith
    // package), so it goes through the IGrowableFontCreator interface instead -- this explicit
    // implementation adapts the richer GlyphAdditionResult down to the interface's plain
    // IReadOnlyList<char>? of failed characters.
    [Fact]
    public void IGrowableFontCreator_TryAddGlyphs_WhenFontWasNotCreatedByThisCreator_ReturnsNull()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        KernSmithFontCreator creator = new(game.GraphicsDevice);
        IGrowableFontCreator growable = creator;

        BitmapFont untrackedFont = new BitmapFont((Texture2D)null!, MinimalFontData);
        untrackedFont.SetFontPattern(256, 256);

        IReadOnlyList<char>? result = growable.TryAddGlyphs(untrackedFont, BuildBmfcSave(ranges: "65"), "B");

        result.ShouldBeNull();
    }

    [Fact]
    public void IGrowableFontCreator_TryAddGlyphs_WhenAllCharactersSucceed_ReturnsEmptyList_AndGrowsTheFont()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        KernSmithFontCreator creator = new(game.GraphicsDevice);
        IGrowableFontCreator growable = creator;

        BmfcSave bmfcSave = BuildBmfcSave(ranges: "65"); // just 'A'
        BitmapFont? font = creator.TryCreateFont(bmfcSave);
        font.ShouldNotBeNull();
        font!.HasCharacter('B').ShouldBeFalse();

        IReadOnlyList<char>? failed = growable.TryAddGlyphs(font, bmfcSave, "B");

        failed.ShouldNotBeNull();
        failed.ShouldBeEmpty();
        font.HasCharacter('B').ShouldBeTrue();
    }

    [Fact]
    public void IGrowableFontCreator_TryAddGlyphs_WhenACharacterHasNoGlyphInTheFontFile_ListsItAsFailed()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        KernSmithFontCreator creator = new(game.GraphicsDevice);
        IGrowableFontCreator growable = creator;

        // U+2192 ('->') is confirmed absent from Orbitron-Black.ttf's cmap (unlike U+20AC/U+2026,
        // which it does contain) -- verified via KernSmith.BmFont.ReadFontInfo(...).AvailableCodepoints
        // against the fixture, not guessed.
        BmfcSave bmfcSave = BuildBmfcSave(ranges: "65");
        BitmapFont? font = creator.TryCreateFont(bmfcSave);
        font.ShouldNotBeNull();

        IReadOnlyList<char>? failed = growable.TryAddGlyphs(font!, bmfcSave, "B→");

        failed.ShouldNotBeNull();
        failed.ShouldBe(new[] { '→' });
        font!.HasCharacter('B').ShouldBeTrue("because a failure for one requested character must not block the others in the same batch");
        font.HasCharacter('→').ShouldBeFalse();
    }

    // Incremental sessions require font file bytes -- KernSmith's BeginIncremental/ResumeIncremental
    // have no system-font overload, so growth needs a real .ttf, not a FontName-only system font.
    private static string FixtureFontPath =>
        Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", "Orbitron-Black.ttf");

    private static BmfcSave BuildBmfcSave(string ranges, float fontSize = 24) => new BmfcSave
    {
        FontName = "Orbitron-Black",
        FontFile = FixtureFontPath,
        FontSize = fontSize,
        UseSmoothing = true,
        Ranges = ranges,
    };

    private const string MinimalFontData =
@"info face=""Arial"" size=-18 bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight=21 base=17 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""Font18Arial_0.png""
chars count=1
char id=65   x=0   y=0   width=10     height=10    xoffset=0    yoffset=0    xadvance=10     page=0  chnl=15
";

    private class MinimalGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;

        public MinimalGame()
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this);
        }

        protected override void Update(GameTime gameTime) { }
        protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(Color.CornflowerBlue);

        protected override void Dispose(bool disposing)
        {
            LoaderManager.Self?.DisposeAndClear();
            base.Dispose(disposing);
        }
    }
}
