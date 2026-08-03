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
            CapturedFontSize = bmfcSave.FontSize;
            return _fontToReturn;
        }
    }
}
