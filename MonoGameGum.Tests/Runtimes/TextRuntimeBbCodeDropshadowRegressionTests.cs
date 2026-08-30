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

// #3625: a Text with a base HasDropshadow=true and an inline BBCode font-family run (e.g.
// [IsBold=true]) generated the per-run font WITHOUT the dropshadow fields, so the tagged run
// rendered without the shadow while the surrounding base text kept it. GetAndCreateFontIfNecessary
// (Gum/Wireframe/CustomSetPropertyOnRenderable.cs) must copy the base TextRuntime's dropshadow
// fields onto every per-run BmfcSave, matching TextRuntime.CopyFontGenerationFieldsTo (the base-font
// path). Deferred from #3624.
public class TextRuntimeBbCodeDropshadowRegressionTests : BaseTestClass
{
    [Fact]
    public void Text_WithBbCodeBoldRun_WhenBaseHasDropshadow_PropagatesDropshadowToInlineRunFont()
    {
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            var recordingCreator = new RecordingInMemoryFontCreator();
            CustomSetPropertyOnRenderable.InMemoryFontCreator = recordingCreator;

            TextRuntime textRuntime = new();
            // A font/size NOT in the test harness's stubbed embedded resources (which only cover
            // Arial-18 and its Bold/Italic/Bold_Italic variants), so resolution actually reaches the
            // in-memory creator instead of being satisfied by an embedded font.
            textRuntime.Font = "Garet";
            textRuntime.FontSize = 12;
            textRuntime.HasDropshadow = true;
            textRuntime.DropshadowOffsetX = 2f;
            textRuntime.DropshadowOffsetY = 3f;
            textRuntime.DropshadowBlur = 1.5f;
            textRuntime.DropshadowRed = 10;
            textRuntime.DropshadowGreen = 20;
            textRuntime.DropshadowBlue = 30;
            textRuntime.DropshadowAlpha = 200;

            textRuntime.Text = "normal [IsBold=true]bold[/IsBold] normal";

            // The base-font resolution and the inline-bold-run resolution both call the creator; only
            // the bold request exercises the per-run path this issue is about.
            var boldRequest = recordingCreator.Requests.Single(r => r.IsBold);
            boldRequest.HasDropshadow.ShouldBeTrue();
            boldRequest.DropshadowOffsetX.ShouldBe(2f);
            boldRequest.DropshadowOffsetY.ShouldBe(3f);
            boldRequest.DropshadowBlur.ShouldBe(1.5f);
            boldRequest.DropshadowRed.ShouldBe((byte)10);
            boldRequest.DropshadowGreen.ShouldBe((byte)20);
            boldRequest.DropshadowBlue.ShouldBe((byte)30);
            boldRequest.DropshadowAlpha.ShouldBe((byte)200);
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // Records every BmfcSave a per-request font resolution asks for, and returns a minimal valid
    // BitmapFont (space + A/B/C/D/L/N/O/R/M/space glyphs aren't needed - measurement isn't asserted).
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
