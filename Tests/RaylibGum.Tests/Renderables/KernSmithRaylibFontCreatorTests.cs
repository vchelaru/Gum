using KernSmith.Gum;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;

namespace RaylibGum.Tests.Renderables;

public class KernSmithRaylibFontCreatorTests : BaseTestClass
{
    // End-to-end regression for the multi-page atlas problem (#3164 / kaltinril/KernSmith#115): a
    // large, outlined font must rasterize onto a SINGLE page and yield a usable Raylib_cs.Font rather
    // than spilling across pages (which raylib's single-texture Font can't represent, so the wrapper
    // would return null and the text would fall back to the default-size system font). This exercises
    // the whole pipeline — KernSmith generation, the single-page atlas ceiling, the outline channel
    // layout (which required KernSmith 0.15.1), and the raw-pixel -> Texture2D upload. It would fail
    // on KernSmith 0.15.0, where this glyph set spilled to multiple pages.
    [Fact]
    public void TryCreateFont_LargeBoldItalicOutlinedFont_ProducesUsableFont()
    {
        KernSmithRaylibFontCreator creator = new KernSmithRaylibFontCreator();

        BmfcSave bmfcSave = new BmfcSave
        {
            FontName = "Arial",
            FontSize = 96,
            IsBold = true,
            IsItalic = true,
            OutlineThickness = 8,
            UseSmoothing = true,
            Ranges = BmfcSave.GetEffectiveDefaultRanges(),
            SpacingHorizontal = 1,
            SpacingVertical = 1,
        };

        Raylib_cs.Font? font = creator.TryCreateFont(bmfcSave);

        font.ShouldNotBeNull();
        font!.Value.BaseSize.ShouldBe(96);
        font.Value.GlyphCount.ShouldBeGreaterThan(0);
        font.Value.Texture.Id.ShouldBeGreaterThan(0u);
    }

    [Fact]
    public void TryCreateFont_WithDropshadow_ProducesUsableFont()
    {
        KernSmithRaylibFontCreator creator = new KernSmithRaylibFontCreator();

        BmfcSave bmfcSave = new BmfcSave
        {
            FontName = "Arial",
            FontSize = 32,
            UseSmoothing = true,
            Ranges = "65",
            HasDropshadow = true,
            DropshadowOffsetX = 2f,
            DropshadowOffsetY = 2f,
            DropshadowBlur = 2f,
            DropshadowAlpha = 255,
        };

        Raylib_cs.Font? font = creator.TryCreateFont(bmfcSave);

        font.ShouldNotBeNull();
        font!.Value.BaseSize.ShouldBe(32);
        font.Value.GlyphCount.ShouldBeGreaterThan(0);
        font.Value.Texture.Id.ShouldBeGreaterThan(0u);
    }
}
