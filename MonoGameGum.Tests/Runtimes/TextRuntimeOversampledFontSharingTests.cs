using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// Issue #6810: TextRuntimes asking for the same oversampled font (same font identity and raster size)
// share one generated font instead of each running a full generation on its first draw.
public class TextRuntimeOversampledFontSharingTests : BaseTestClass
{
    private readonly bool _savedUseFontOversampling = TextRuntime.UseFontOversampling;
    private readonly bool _savedUseAutomaticFontGrowth = TextRuntime.UseAutomaticFontGrowth;
    private readonly IInMemoryFontCreator? _savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;

    public TextRuntimeOversampledFontSharingTests()
    {
        TextRuntime.UseFontOversampling = true;
    }

    public override void Dispose()
    {
        TextRuntime.UseFontOversampling = _savedUseFontOversampling;
        TextRuntime.UseAutomaticFontGrowth = _savedUseAutomaticFontGrowth;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = _savedCreator;
        base.Dispose();
    }

    [Fact]
    public void RegenerateOversampledFont_IdenticalRequests_GenerateOnceAndShareTheFont()
    {
        CountingFontCreator creator = new();
        CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;
        TextRuntime first = CreateText(fontSize: 20);
        TextRuntime second = CreateText(fontSize: 20);

        first.RegenerateOversampledFont(2f).ShouldBeTrue();
        second.RegenerateOversampledFont(2f).ShouldBeTrue();

        creator.CountRequestsAt(40f).ShouldBe(1);
        GetText(second).BitmapFont.ShouldBeSameAs(GetText(first).BitmapFont);
    }

    [Fact]
    public void RegenerateOversampledFont_DifferentFontSize_GeneratesSeparateFonts()
    {
        CountingFontCreator creator = new();
        CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;
        TextRuntime first = CreateText(fontSize: 20);
        TextRuntime second = CreateText(fontSize: 21);

        first.RegenerateOversampledFont(2f);
        second.RegenerateOversampledFont(2f);

        creator.CountRequestsAt(40f).ShouldBe(1);
        creator.CountRequestsAt(42f).ShouldBe(1);
        GetText(second).BitmapFont.ShouldNotBeSameAs(GetText(first).BitmapFont);
    }

    [Fact]
    public void RegenerateOversampledFont_SharedFont_IsKeptWhileAnyTextUsesItAndRegeneratedAfterTheLastLeaves()
    {
        CountingFontCreator creator = new();
        CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;
        TextRuntime first = CreateText(fontSize: 20);
        TextRuntime second = CreateText(fontSize: 20);
        first.RegenerateOversampledFont(2f);
        second.RegenerateOversampledFont(2f);

        first.RegenerateOversampledFont(3f);
        TextRuntime third = CreateText(fontSize: 20);
        third.RegenerateOversampledFont(2f);

        creator.CountRequestsAt(40f).ShouldBe(1, "because the second text still uses the font");

        second.RegenerateOversampledFont(3f);
        third.RegenerateOversampledFont(3f);
        TextRuntime fourth = CreateText(fontSize: 20);
        fourth.RegenerateOversampledFont(2f);

        creator.CountRequestsAt(40f).ShouldBe(2, "because no text used the font any more, so it was released");
    }

    [Fact]
    public void FontPropertyChange_ReleasesTheSharedOversampledFont()
    {
        CountingFontCreator creator = new();
        CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;
        TextRuntime first = CreateText(fontSize: 20);
        first.RegenerateOversampledFont(2f);

        // Re-resolving the font puts the text back on its native font.
        first.FontSize = 22;
        TextRuntime second = CreateText(fontSize: 20);
        second.RegenerateOversampledFont(2f);

        creator.CountRequestsAt(40f).ShouldBe(2);
    }

    [Fact]
    public void RegenerateOversampledFont_AfterCreatorChanges_DoesNotReuseTheOldCreatorsFont()
    {
        CountingFontCreator oldCreator = new();
        CustomSetPropertyOnRenderable.InMemoryFontCreator = oldCreator;
        TextRuntime first = CreateText(fontSize: 20);
        first.RegenerateOversampledFont(2f);

        CountingFontCreator newCreator = new();
        CustomSetPropertyOnRenderable.InMemoryFontCreator = newCreator;
        TextRuntime second = CreateText(fontSize: 20);
        second.RegenerateOversampledFont(2f);

        newCreator.CountRequestsAt(40f).ShouldBe(1);
        GetText(second).BitmapFont.ShouldNotBeSameAs(GetText(first).BitmapFont);
    }

    [Fact]
    public void AutomaticFontGrowth_OnSharedOversampledFont_IsVisibleToEveryTextSharingIt()
    {
        TextRuntime.UseAutomaticFontGrowth = true;
        CountingFontCreator creator = new();
        CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;
        TextRuntime first = CreateText(fontSize: 20);
        TextRuntime second = CreateText(fontSize: 20);
        first.RegenerateOversampledFont(2f);
        second.RegenerateOversampledFont(2f);

        first.Text = "C";

        Text secondText = GetText(second);
        secondText.BitmapFont!.HasCharacter('C').ShouldBeTrue();
        creator.CountRequestsAt(40f).ShouldBe(1);
    }

    private static Text GetText(TextRuntime textRuntime) => (Text)textRuntime.RenderableComponent!;

    private static TextRuntime CreateText(float fontSize)
    {
        TextRuntime textRuntime = new();
        textRuntime.Font = "StubFont";
        textRuntime.FontSize = fontSize;
        return textRuntime;
    }

    private const string StubFontData =
@"info face=""Arial"" size=-18 bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight=21 base=17 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""StubFont_0.png""
chars count=3
char id=32   x=0   y=0   width=3     height=1     xoffset=-1    yoffset=20    xadvance=5     page=0  chnl=15
char id=65   x=0   y=0   width=10    height=10    xoffset=0     yoffset=0     xadvance=10    page=0  chnl=15
char id=66   x=10  y=0   width=10    height=10    xoffset=0     yoffset=0     xadvance=10    page=0  chnl=15
";

    // Returns a fresh font on every call and records each requested raster size, so a test can count
    // how many generations a given size cost. Grows by adding a 10x10 glyph for any missing character.
    private sealed class CountingFontCreator : IInMemoryFontCreator, IGrowableFontCreator
    {
        private readonly List<float> _requestedFontSizes = new();

        public int CountRequestsAt(float fontSize) => _requestedFontSizes.Count(size => size == fontSize);

        public BitmapFont? TryCreateFont(BmfcSave bmfcSave)
        {
            _requestedFontSizes.Add(bmfcSave.FontSize);
            BitmapFont font = new BitmapFont((Texture2D)null!, StubFontData);
            font.SetFontPattern(256, 256);
            return font;
        }

        public IReadOnlyList<char>? TryAddGlyphs(BitmapFont font, BmfcSave bmfcSave, string characters)
        {
            foreach (char c in characters.Where(c => !font.HasCharacter(c)))
            {
                font.AddOrUpdateCharacter(
                    new FontFileCharLine { Id = c, Width = 10, Height = 10, XAdvance = 10 }, 256, 256);
            }
            return new List<char>();
        }
    }
}
