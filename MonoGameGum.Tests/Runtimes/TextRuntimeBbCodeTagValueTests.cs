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

// What a tag's value does to the run it wraps: a value that parses styles the run, and a value that does
// not parse leaves the run as if the tag were absent (the base style, or the style of an enclosing tag of
// the same name). The SkiaGum and raylib classes of the same name run the same rows.
public class TextRuntimeBbCodeTagValueTests : BaseTestClass
{
    private const string FontData =
@"info face=""Arial"" size=-18 bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight=20 base=20 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""x.png""
chars count=4
char id=32 x=0 y=0 width=10 height=13 xoffset=0 yoffset=4 xadvance=10 page=0 chnl=15
char id=97 x=0 y=0 width=10 height=13 xoffset=0 yoffset=4 xadvance=10 page=0 chnl=15
char id=98 x=0 y=0 width=10 height=13 xoffset=0 yoffset=4 xadvance=10 page=0 chnl=15
char id=99 x=0 y=0 width=10 height=13 xoffset=0 yoffset=4 xadvance=10 page=0 chnl=15
";

    private class RecordingFontCreator : IInMemoryFontCreator
    {
        public readonly Dictionary<BitmapFont, string> Descriptions = new();

        public BitmapFont? TryCreateFont(BmfcSave bmfcSave)
        {
            BitmapFont font = new BitmapFont((Texture2D)null!, FontData);
            font.SetFontPattern(256, 256);
            Descriptions[font] = $"{bmfcSave.FontName}/{bmfcSave.FontSize}/{(bmfcSave.IsBold ? "B" : "-")}/{(bmfcSave.IsItalic ? "I" : "-")}/{bmfcSave.OutlineThickness}";
            return font;
        }
    }

    private static Text MakeText(string markup, out RecordingFontCreator creator)
    {
        creator = new RecordingFontCreator();
        IInMemoryFontCreator? previousCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;
        try
        {
            TextRuntime textRuntime = new();
            // A name no other test uses, so no font cached by another test answers for this one.
            textRuntime.Font = "TagValueTestsFont";
            textRuntime.FontSize = 18;
            textRuntime.Text = markup;
            return (Text)textRuntime.RenderableComponent!;
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = previousCreator;
        }
    }

    // The font of the last per-run font variable covering the character, or "base".
    private static string FontAt(Text text, RecordingFontCreator creator, int index)
    {
        InlineVariable? covering = text.InlineVariables.LastOrDefault(v =>
            v.Value is BitmapFont && index >= v.StartIndex && index < v.StartIndex + v.CharacterCount);
        if (covering == null)
        {
            return "base";
        }
        return creator.Descriptions.TryGetValue((BitmapFont)covering.Value, out string? description)
            ? description
            : "unknown font";
    }

    [Theory]
    [InlineData("[Color=red]a[/Color]", 255, 255, 0, 0)]
    [InlineData("[Color=CornflowerBlue]a[/Color]", 255, 100, 149, 237)]
    [InlineData("[Color=0xFF00FF00]a[/Color]", 255, 0, 255, 0)]
    [InlineData("[Color=0x8000FF00]a[/Color]", 128, 0, 255, 0)]
    public void Text_ColorTagWithValidValue_AppliesColor(string markup, int a, int r, int g, int b)
    {
        Text text = MakeText(markup, out _);

        InlineVariable color = text.InlineVariables.Single(v => v.VariableName == "Color");
        System.Drawing.Color value = (System.Drawing.Color)color.Value;
        (value.A, value.R, value.G, value.B).ShouldBe(((byte)a, (byte)r, (byte)g, (byte)b));
    }

    [Theory]
    [InlineData("[Red=300]a[/Red]")]
    [InlineData("[Green=-1]a[/Green]")]
    [InlineData("[Blue=1.5]a[/Blue]")]
    [InlineData("[FontScale=abc]a[/FontScale]")]
    [InlineData("[FontScale=]a[/FontScale]")]
    public void Text_ColorOrScaleTagWithUnparseableValue_AddsNoRun(string markup)
    {
        Text text = MakeText(markup, out _);

        text.RawText.ShouldBe("a");
        text.InlineVariables.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("[FontScale=1.5]a[/FontScale]", 1.5f)]
    [InlineData("[FontScale=.5]a[/FontScale]", 0.5f)]
    public void Text_FontScaleTagWithDecimalValue_AppliesScale(string markup, float expected)
    {
        Text text = MakeText(markup, out _);

        text.InlineVariables.Single(v => v.VariableName == "FontScale").Value.ShouldBe(expected);
    }

    [Theory]
    [InlineData("a[FontSize=abc]b[/FontSize]c")]
    [InlineData("a[IsBold=maybe]b[/IsBold]c")]
    public void Text_FontTagWithUnparseableValue_KeepsBaseFont(string markup)
    {
        Text text = MakeText(markup, out RecordingFontCreator creator);

        text.RawText.ShouldBe("abc");
        // The run may get no font variable or one resolved to the base font; both draw the base font.
        FontAt(text, creator, 1).ShouldBeOneOf("base", "TagValueTestsFont/18/-/-/0");
    }

    [Theory]
    [InlineData("[Alpha=128]a[/Alpha]")]
    [InlineData("[LineHeightMultiplier=2]a[/LineHeightMultiplier]")]
    [InlineData("[UseFontSmoothing=false]a[/UseFontSmoothing]")]
    [InlineData("[HasDropshadow=false]a[/HasDropshadow]")]
    [InlineData("[Link=shop]a[/Link]")]
    public void Text_KnownTagWithoutInlineStyle_IsRemovedFromDisplayedText(string markup)
    {
        Text text = MakeText(markup, out _);

        text.RawText.ShouldBe("a", "Because every runtime recognizes the same tag names, so a known tag never draws as literal text");
    }

    [Fact]
    public void Text_CustomTagAfterEmoji_ReceivesUtf16IndexWithTagsRemoved()
    {
        Text.Customizations["TagValueIndex"] = (index, block) => new LetterCustomization();
        try
        {
            Text text = MakeText("\U0001F600[Color=Red]a[/Color][Custom=TagValueIndex]bc[/Custom]", out _);

            List<ParameterizedLetterCustomizationCall> calls = text.InlineVariables
                .Select(v => v.Value).OfType<ParameterizedLetterCustomizationCall>().ToList();
            calls.Select(c => c.CharacterIndex).ShouldBe(new[] { 3, 4 });
            calls.ShouldAllBe(c => c.TextBlock == "bc");
        }
        finally
        {
            Text.Customizations.Remove("TagValueIndex");
        }
    }
}
