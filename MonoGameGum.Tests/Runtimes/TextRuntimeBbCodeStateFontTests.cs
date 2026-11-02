using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// #7204: FontReferenceCollector bakes the fonts a [state=Name] tag produces, so it must predict exactly
// the fonts the renderer asks for. Gum.Bundle.Tests pins the same list for the collector
// (FontReferenceCollectorBbCodeStateTests.BigStateKeys): the base font, then each font the stack passes
// through as the state's Font and FontSize open and close one at a time.
public class TextRuntimeBbCodeStateFontTests : BaseTestClass
{
    private const string FontData =
@"info face=""Arial"" size=-18 bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight=20 base=20 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""x.png""
chars count=2
char id=32 x=0 y=0 width=10 height=13 xoffset=0 yoffset=4 xadvance=10 page=0 chnl=15
char id=97 x=0 y=0 width=10 height=13 xoffset=0 yoffset=4 xadvance=10 page=0 chnl=15
";

    private class RecordingFontCreator : IInMemoryFontCreator
    {
        public List<string> RequestedFiles { get; } = new List<string>();

        public BitmapFont? TryCreateFont(BmfcSave bmfcSave)
        {
            RequestedFiles.Add(bmfcSave.FontCacheFileName);
            BitmapFont font = new BitmapFont((Texture2D)null!, FontData);
            font.SetFontPattern(256, 256);
            return font;
        }
    }

    [Fact]
    public void Text_WithStateTag_RequestsTheFontsTheCollectorBakes()
    {
        IInMemoryFontCreator? previousCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        RecordingFontCreator creator = new RecordingFontCreator();
        CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;
        try
        {
            TextRuntime textRuntime = new();
            StateSave big = new() { Name = "Big" };
            big.Variables.Add(new VariableSave { Name = "Font", Type = "string", Value = "StateFaceB", SetsValue = true });
            big.Variables.Add(new VariableSave { Name = "FontSize", Type = "int", Value = 40, SetsValue = true });
            textRuntime.AddStates(new List<StateSave> { big });
            textRuntime.Font = "StateFaceA";
            textRuntime.FontSize = 18;
            // The base font (18, StateFaceA) was loaded when it was set; the runs are what the text asks for.
            creator.RequestedFiles.Clear();

            textRuntime.Text = "[state=Big]a[/state]";

            creator.RequestedFiles.ShouldBe(
                new[]
                {
                    CacheFileName(18, "StateFaceB"),
                    CacheFileName(40, "StateFaceB"),
                    CacheFileName(40, "StateFaceA"),
                    // closing the last tag returns to the already-loaded base: CacheFileName(18, "StateFaceA")
                },
                ignoreOrder: true);
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = previousCreator;
        }
    }

    private static string CacheFileName(int size, string name) =>
        BmfcSave.GetFontCacheFileNameFor(
            fontSize: size, fontName: name, outline: 0, useFontSmoothing: true,
            isItalic: false, isBold: false, fontFilePath: null);
}
