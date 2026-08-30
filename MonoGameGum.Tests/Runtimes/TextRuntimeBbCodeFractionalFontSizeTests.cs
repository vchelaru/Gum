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

// Issue #4304: the shared inline-BBCode font-size stack (Gum/Wireframe/CustomSetPropertyOnRenderable.cs,
// fontSizeStack) parsed [FontSize=N] with int.TryParse, so a fractional argument like "18.5" failed to
// parse and was silently treated as a closing tag (Pop) instead of pushing the requested size. Widening
// the stack to float alongside TextRuntime.FontSize fixes this: a fractional inline run now resolves via
// float.TryParse and reaches the per-run BmfcSave unrounded.
[Collection(FontStaticsTestCollection.Name)]
public class TextRuntimeBbCodeFractionalFontSizeTests : BaseTestClass
{
    [Fact]
    public void Text_WithFractionalInlineFontSizeRun_PassesUnroundedSizeToInMemoryFontCreator()
    {
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            var recordingCreator = new RecordingInMemoryFontCreator();
            CustomSetPropertyOnRenderable.InMemoryFontCreator = recordingCreator;

            TextRuntime textRuntime = new();
            // A font/size NOT in the test harness's stubbed embedded resources (which only cover
            // Arial-18), so resolution actually reaches the in-memory creator.
            textRuntime.Font = "Garet";
            textRuntime.FontSize = 12;

            textRuntime.Text = "normal [FontSize=18.5]enlarged[/FontSize] normal";

            var fractionalRequest = recordingCreator.Requests.Single(r => r.FontSize == 18.5f);
            fractionalRequest.FontSize.ShouldBe(18.5f);
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    private sealed class RecordingInMemoryFontCreator : IInMemoryFontCreator
    {
        public List<BmfcSave> Requests { get; } = new();

        public BitmapFont? TryCreateFont(BmfcSave bmfcSave)
        {
            Requests.Add(bmfcSave);
            BitmapFont font = new BitmapFont((Texture2D)null!, FontData);
            font.SetFontPattern(256, 256);
            return font;
        }

        private const string FontData =
@"info face=""Arial"" size=-18 bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight=18 base=18 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""x.png""
chars count=1
char id=32 x=0 y=0 width=9 height=13 xoffset=0 yoffset=4 xadvance=9 page=0 chnl=15
";
    }
}
