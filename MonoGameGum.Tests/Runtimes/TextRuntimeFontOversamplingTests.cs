using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// Issue #4302: TextRuntime.RegenerateOversampledFont rasterizes a font at a multiple of FontSize (for
// crisper text under camera zoom) and compensates with Text.FontScale so the on-screen size is
// unchanged. Gated behind the global TextRuntime.UseFontOversampling toggle (off by default, so
// pixel-art projects see no behavior change) and requires an IInMemoryFontCreator (KernSmith) --
// a disk-based font cache only holds a fixed set of pre-baked sizes, so arbitrary-ratio regeneration
// only makes sense with dynamic generation.
public class TextRuntimeFontOversamplingTests : BaseTestClass
{
    [Fact]
    public void RegenerateOversampledFont_WhenEnabledWithCreator_RegeneratesAtOversampledSizeAndCompensatesFontScale()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            var stubFont = new BitmapFont((Texture2D)null!, StubFontData);
            stubFont.SetFontPattern(256, 256);
            var creator = new CapturingInMemoryFontCreator(stubFont);

            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

            TextRuntime textRuntime = new();
            textRuntime.FontSize = 20;

            bool result = textRuntime.RegenerateOversampledFont(2.5f);

            result.ShouldBeTrue();
            creator.CapturedFontSize.ShouldBe(50);
            var text = (Text)textRuntime.RenderableComponent;
            text.BitmapFont.ShouldBeSameAs(stubFont);
            text.FontScale.ShouldBe(20f / 50f);
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    [Fact]
    public void RegenerateOversampledFont_WhenOversamplingDisabled_DoesNotRegenerateFont()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            var creator = new CapturingInMemoryFontCreator(new BitmapFont((Texture2D)null!, StubFontData));

            TextRuntime.UseFontOversampling = false;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

            TextRuntime textRuntime = new();
            textRuntime.FontSize = 20;
            var text = (Text)textRuntime.RenderableComponent;
            var originalFont = text.BitmapFont;
            var originalFontScale = text.FontScale;
            // Assigning FontSize above already resolves this Text's normal (non-oversampled) font
            // through the same global InMemoryFontCreator, so reset the call tracking here to isolate
            // whether RegenerateOversampledFont itself invokes the creator.
            creator.ResetCallTracking();

            bool result = textRuntime.RegenerateOversampledFont(2.5f);

            result.ShouldBeFalse();
            creator.WasCalled.ShouldBeFalse();
            text.BitmapFont.ShouldBeSameAs(originalFont);
            text.FontScale.ShouldBe(originalFontScale);
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    [Fact]
    public void RegenerateOversampledFont_WhenNoInMemoryFontCreatorRegistered_DoesNotRegenerateFont()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = null;

            TextRuntime textRuntime = new();
            textRuntime.FontSize = 20;
            var text = (Text)textRuntime.RenderableComponent;
            var originalFont = text.BitmapFont;
            var originalFontScale = text.FontScale;

            bool result = textRuntime.RegenerateOversampledFont(2.5f);

            result.ShouldBeFalse();
            text.BitmapFont.ShouldBeSameAs(originalFont);
            text.FontScale.ShouldBe(originalFontScale);
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // Reproduction attempt for the wrap-earlier report on #4302: goes through the real TextRuntime
    // property cascade (not a bare Text) with a fixed Width, using a fake creator whose glyph metrics
    // scale perfectly with the requested FontSize -- i.e. the oversampled font is an exact multiple of
    // the base font, same as the "AB AB" bare-Text regression test in TextTests.cs. If this passes,
    // the TextRuntime/GraphicalUiElement plumbing isn't adding its own discrepancy on top of the core
    // BitmapFont/FontScale swap (already proven sound by that other test) -- meaning a real-world
    // wrap shift comes from the actual rasterizer's glyph metrics not scaling perfectly linearly,
    // not from a logic bug in Gum's wrap-width math.
    [Fact]
    public void RegenerateOversampledFont_WithProportionalFont_DoesNotChangeWrappedLineCount()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new ProportionalFontCreator();

            TextRuntime textRuntime = new();
            textRuntime.WidthUnits = DimensionUnitType.Absolute;
            textRuntime.FontSize = 20;
            textRuntime.Width = 5 * 20; // exactly fits "AB AB" (5 chars * FontSize-as-xadvance)
            textRuntime.Text = "AB AB";

            var text = (Text)textRuntime.RenderableComponent;
            text.WrappedText.Count.ShouldBe(1, "because AB AB exactly fits the base font/width before any oversampling");

            bool result = textRuntime.RegenerateOversampledFont(2.5f);

            result.ShouldBeTrue();
            text.WrappedText.Count.ShouldBe(1,
                "because the fake creator's glyph metrics scale exactly with FontSize, so FontScale " +
                "compensates perfectly -- the on-screen size, and therefore the wrap, must not change");
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // The real bug (not the rounding-noise theory above): RelativeToChildren sizes the box to fit its
    // OWN content, so it must never wrap, full stop -- regardless of which font is loaded or how
    // imperfectly that font's glyph metrics scale. RegenerateOversampledFont swaps BitmapFont/FontScale
    // directly on the renderable without calling back into GraphicalUiElement.UpdateLayout(), so a
    // RelativeToChildren box's Width is never re-derived from the new font -- it stays frozen at the
    // natural width measured against the OLD font. That staleness happens to cancel out mathematically
    // when the new font scales perfectly linearly (see the Proportional test above), but any real
    // rasterizer's hinting/rounding makes the oversampled font's glyphs not an exact multiple of the
    // base font's, which the frozen Width has no way to absorb -- unlike a fresh RelativeToChildren
    // measurement, which would just re-size to fit. NearlyProportionalFontCreator simulates that
    // realistic 1px-per-glyph rounding noise at the oversampled size.
    [Fact]
    public void RegenerateOversampledFont_WithRelativeToChildrenWidth_ShouldNeverWrap()
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseFontOversampling = true;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new NearlyProportionalFontCreator(baseFontSize: 20);

            TextRuntime textRuntime = new();
            textRuntime.WidthUnits = DimensionUnitType.RelativeToChildren;
            textRuntime.FontSize = 20;
            textRuntime.Text = "AB AB";

            var text = (Text)textRuntime.RenderableComponent;
            text.WrappedText.Count.ShouldBe(1, "because RelativeToChildren sizes the box to exactly fit this text before any oversampling");

            bool result = textRuntime.RegenerateOversampledFont(2.5f);

            result.ShouldBeTrue();
            text.WrappedText.Count.ShouldBe(1,
                "because RelativeToChildren must re-size to fit whatever font is currently loaded -- it can never " +
                "wrap its own content, even when the oversampled font's real-world glyph metrics aren't a perfect multiple of the base font's");
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // Every glyph's xadvance is 1px wider than a perfect (FontSize/baseFontSize)x scale of the base font
    // whenever the requested size isn't baseFontSize itself -- simulates the rounding/hinting noise a
    // real TrueType rasterizer introduces between two different point sizes of the same font.
    private sealed class NearlyProportionalFontCreator : IInMemoryFontCreator
    {
        private readonly int _baseFontSize;

        public NearlyProportionalFontCreator(int baseFontSize)
        {
            _baseFontSize = baseFontSize;
        }

        public BitmapFont? TryCreateFont(BmfcSave bmfcSave)
        {
            int xadvance = (int)bmfcSave.FontSize;
            if (bmfcSave.FontSize != _baseFontSize)
            {
                xadvance += 1;
            }

            string fontData =
$@"info face=""Arial"" size=-{bmfcSave.FontSize} bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight={bmfcSave.FontSize} base={bmfcSave.FontSize} scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""x.png""
chars count=3
char id=32 x=0 y=0 width={xadvance} height=13 xoffset=0 yoffset=4 xadvance={xadvance} page=0 chnl=15
char id=65 x=0 y=0 width={xadvance} height=13 xoffset=0 yoffset=4 xadvance={xadvance} page=0 chnl=15
char id=66 x=0 y=0 width={xadvance} height=13 xoffset=0 yoffset=4 xadvance={xadvance} page=0 chnl=15
";
            BitmapFont font = new BitmapFont((Texture2D)null!, fontData);
            font.SetFontPattern(256, 256);
            return font;
        }
    }

    // Every glyph's xadvance equals the requested FontSize exactly, so a font generated at size N is
    // a perfect (N/baseSize)x scale of one generated at baseSize -- isolates whether Gum's own
    // wrap-width math introduces error, independent of any real rasterizer's rounding/hinting noise.
    private sealed class ProportionalFontCreator : IInMemoryFontCreator
    {
        public BitmapFont? TryCreateFont(BmfcSave bmfcSave)
        {
            int xadvance = (int)bmfcSave.FontSize;
            string fontData =
$@"info face=""Arial"" size=-{bmfcSave.FontSize} bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight={bmfcSave.FontSize} base={bmfcSave.FontSize} scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""x.png""
chars count=3
char id=32 x=0 y=0 width={xadvance} height=13 xoffset=0 yoffset=4 xadvance={xadvance} page=0 chnl=15
char id=65 x=0 y=0 width={xadvance} height=13 xoffset=0 yoffset=4 xadvance={xadvance} page=0 chnl=15
char id=66 x=0 y=0 width={xadvance} height=13 xoffset=0 yoffset=4 xadvance={xadvance} page=0 chnl=15
";
            BitmapFont font = new BitmapFont((Texture2D)null!, fontData);
            font.SetFontPattern(256, 256);
            return font;
        }
    }

    private const string StubFontData =
@"info face=""Arial"" size=-18 bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight=18 base=18 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""x.png""
chars count=1
char id=32 x=0 y=0 width=9 height=13 xoffset=0 yoffset=4 xadvance=9 page=0 chnl=15
";

    // Captures the FontSize the caller asked for and always returns the stub font supplied at
    // construction, so tests can assert both "what raster size was requested" and "was it wired up".
    private sealed class CapturingInMemoryFontCreator : IInMemoryFontCreator
    {
        private readonly BitmapFont _fontToReturn;

        public CapturingInMemoryFontCreator(BitmapFont fontToReturn)
        {
            _fontToReturn = fontToReturn;
        }

        public bool WasCalled { get; private set; }
        public int CapturedFontSize { get; private set; }

        public void ResetCallTracking() => WasCalled = false;

        public BitmapFont? TryCreateFont(BmfcSave bmfcSave)
        {
            WasCalled = true;
            CapturedFontSize = (int)bmfcSave.FontSize;
            return _fontToReturn;
        }
    }
}
