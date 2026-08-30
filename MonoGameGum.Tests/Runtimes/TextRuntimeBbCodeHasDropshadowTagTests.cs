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

// #3528: a [HasDropshadow] BBCode tag lets a run toggle the shadow independently of the base
// TextRuntime.HasDropshadow, using the same push/pop stack model as [IsBold]/[FontSize]/etc.
// (Gum/Wireframe/CustomSetPropertyOnRenderable.cs). Distinct from #3625 (which only propagated the
// base shadow onto per-run font swaps) - this is the tag actually changing the per-run value.
public class TextRuntimeBbCodeHasDropshadowTagTests : BaseTestClass
{
    [Fact]
    public void Text_WithHasDropshadowFalseTag_WhenBaseHasDropshadow_ResolvesRunFontWithoutDropshadow()
    {
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            var recordingCreator = new RecordingInMemoryFontCreator();
            CustomSetPropertyOnRenderable.InMemoryFontCreator = recordingCreator;

            TextRuntime textRuntime = new();
            textRuntime.Font = "Garet";
            textRuntime.FontSize = 12;
            textRuntime.HasDropshadow = true;

            textRuntime.Text = "normal [IsBold=true][HasDropshadow=false]off[/HasDropshadow][/IsBold] normal";

            // The [IsBold] open (still base HasDropshadow=true), the [HasDropshadow=false] open, and the
            // matching close each re-resolve a bold font - only the middle one has shadow turned off.
            recordingCreator.Requests.ShouldContain(r => r.IsBold && !r.HasDropshadow);
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    [Fact]
    public void Text_WithHasDropshadowTrueTag_WhenBaseHasNoDropshadow_ResolvesRunFontWithDropshadow()
    {
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            var recordingCreator = new RecordingInMemoryFontCreator();
            CustomSetPropertyOnRenderable.InMemoryFontCreator = recordingCreator;

            TextRuntime textRuntime = new();
            textRuntime.Font = "Garet";
            textRuntime.FontSize = 12;
            textRuntime.HasDropshadow = false;

            textRuntime.Text = "normal [IsBold=true][HasDropshadow=true]on[/HasDropshadow][/IsBold] normal";

            recordingCreator.Requests.ShouldContain(r => r.IsBold && r.HasDropshadow);
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // Records every BmfcSave a per-request font resolution asks for, and returns a minimal valid
    // BitmapFont (mirrors TextRuntimeBbCodeDropshadowRegressionTests.RecordingInMemoryFontCreator).
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
