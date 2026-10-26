using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// Issue #6818: a character outside the font's ranges used to draw as blank space with no report.
// Reports are deduplicated per font and character across every Text, so each test uses its own font
// name to stay independent of what other tests already reported.
public class TextRuntimeMissingCharacterReportTests : BaseTestClass
{
    [Fact]
    public void Text_WhenFontLacksCharacter_ReportsTextNameFontSizeAndCodePoint()
    {
        RunWithStubCreator(growth: false, messages =>
        {
            TextRuntime textRuntime = NewText("ReportFontA");
            textRuntime.Name = "ScoreLabel";
            textRuntime.FontSize = 22;
            messages.Clear();

            textRuntime.Text = "AÉ";

            messages.ShouldHaveSingleItem();
            messages[0].ShouldContain("ScoreLabel");
            messages[0].ShouldContain("ReportFontA");
            messages[0].ShouldContain("22");
            messages[0].ShouldContain("U+00C9");
            messages[0].ShouldContain("201");
            messages[0].ShouldContain("FontRanges");
        });
    }

    [Fact]
    public void Text_WhenSameCharacterIsMissingAgain_ReportsOncePerFont()
    {
        RunWithStubCreator(growth: false, messages =>
        {
            TextRuntime first = NewText("ReportFontB");
            TextRuntime second = NewText("ReportFontB");
            messages.Clear();

            first.Text = "É";
            first.Text = "AÉ";
            second.Text = "É";

            messages.ShouldHaveSingleItem();

            second.Text = "Ñ";
            messages.Count.ShouldBe(2, "because a different missing character is a new report");

            // Changing the size re-wraps the current text, so give it one the font has first.
            second.Text = "A";
            second.FontSize = 30;
            second.Text = "É";
            messages.Count.ShouldBe(3, "because the same character missing from a different font size is a new report");
        });
    }

    [Fact]
    public void Text_WhenBehaviorIsConsumeSilently_ReportsNothing()
    {
        MissingCharacterBehavior saved = TextRuntime.MissingCharacterBehavior;
        try
        {
            TextRuntime.MissingCharacterBehavior = MissingCharacterBehavior.ConsumeSilently;
            RunWithStubCreator(growth: false, messages =>
            {
                TextRuntime textRuntime = NewText("ReportFontC");
                messages.Clear();

                textRuntime.Text = "É";

                messages.ShouldBeEmpty();
            });
        }
        finally
        {
            TextRuntime.MissingCharacterBehavior = saved;
        }
    }

    [Fact]
    public void Text_WhenBehaviorIsThrowException_ThrowsEveryTimeInsteadOfReporting()
    {
        MissingCharacterBehavior saved = TextRuntime.MissingCharacterBehavior;
        try
        {
            RunWithStubCreator(growth: false, messages =>
            {
                TextRuntime textRuntime = NewText("ReportFontF");
                textRuntime.Text = "A";
                textRuntime.Name = "ScoreLabel";
                TextRuntime.MissingCharacterBehavior = MissingCharacterBehavior.ThrowException;
                messages.Clear();

                InvalidOperationException first = Should.Throw<InvalidOperationException>(() => textRuntime.Text = "É");
                first.Message.ShouldContain("ScoreLabel");
                first.Message.ShouldContain("U+00C9");
                Should.Throw<InvalidOperationException>(() => textRuntime.Text = "AÉ",
                    "because throwing is not deduplicated the way reports are");
                Should.NotThrow(() => textRuntime.Text = "AB");
                messages.ShouldBeEmpty();
            });
        }
        finally
        {
            TextRuntime.MissingCharacterBehavior = saved;
        }
    }

    [Fact]
    public void Text_WhenGrowthAddsTheCharacter_ReportsNothing()
    {
        RunWithStubCreator(growth: true, messages =>
        {
            TextRuntime textRuntime = NewText("ReportFontD");
            messages.Clear();

            textRuntime.Text = "É";

            messages.ShouldBeEmpty();
        });
    }

    [Fact]
    public void Text_WithCarriageReturn_DoesNotReportControlCharacters()
    {
        RunWithStubCreator(growth: false, messages =>
        {
            TextRuntime textRuntime = NewText("ReportFontE");
            messages.Clear();

            textRuntime.Text = "A\r\nB\tA";

            messages.ShouldBeEmpty();
        });
    }

    private static TextRuntime NewText(string fontName)
    {
        TextRuntime textRuntime = new();
        textRuntime.Font = fontName;
        return textRuntime;
    }

    private static void RunWithStubCreator(bool growth, System.Action<List<string>> body)
    {
        bool savedGrowth = TextRuntime.UseAutomaticFontGrowth;
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        List<string> messages = new();
        System.Action<string> handler = messages.Add;
        try
        {
            TextRuntime.UseAutomaticFontGrowth = growth;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new StubFontCreator();
            CustomSetPropertyOnRenderable.PropertyAssignmentError += handler;
            body(messages);
        }
        finally
        {
            CustomSetPropertyOnRenderable.PropertyAssignmentError -= handler;
            TextRuntime.UseAutomaticFontGrowth = savedGrowth;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    private const string BaseFontData =
@"info face=""Arial"" size=-18 bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight=21 base=17 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""StubFont_0.png""
chars count=3
char id=32   x=0   y=0   width=3     height=1     xoffset=-1    yoffset=20    xadvance=5     page=0  chnl=15
char id=65   x=0   y=0   width=10    height=10    xoffset=0     yoffset=0     xadvance=10    page=0  chnl=15
char id=66   x=10  y=0   width=10    height=10    xoffset=0     yoffset=0     xadvance=10    page=0  chnl=15
";

    // Knows only space, 'A' and 'B'; growth adds whatever is asked for.
    private sealed class StubFontCreator : IInMemoryFontCreator, IGrowableFontCreator
    {
        public BitmapFont? TryCreateFont(BmfcSave bmfcSave)
        {
            BitmapFont font = new BitmapFont((Texture2D)null!, BaseFontData);
            font.SetFontPattern(256, 256);
            return font;
        }

        public IReadOnlyList<char>? TryAddGlyphs(BitmapFont font, BmfcSave bmfcSave, string characters)
        {
            foreach (char c in characters)
            {
                font.AddOrUpdateCharacter(
                    new FontFileCharLine { Id = c, Width = 10, Height = 10, XAdvance = 10 }, 256, 256);
            }
            return new List<char>();
        }
    }
}
