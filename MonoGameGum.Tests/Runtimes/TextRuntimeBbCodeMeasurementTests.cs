using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// Measurement, wrapping, alignment and hit testing of BBCode-styled text compared with the same plain text.
// The expected values come from measuring plain or styled strings on their own (RelativeToChildren width),
// never from the code path under test.
public class TextRuntimeBbCodeMeasurementTests : BaseTestClass
{
    // Every measurement goes through TextRuntime only, so the same test body runs on MonoGame, Skia and raylib.
    private const float Tolerance = 1.5f;

    private static TextRuntime MakeRelative(string text)
    {
        TextRuntime textRuntime = new();
        textRuntime.WidthUnits = DimensionUnitType.RelativeToChildren;
        textRuntime.Width = 0;
        textRuntime.HeightUnits = DimensionUnitType.RelativeToChildren;
        textRuntime.Height = 0;
        textRuntime.Text = text;
        return textRuntime;
    }

    private static float WidthOf(string text) => MakeRelative(text).GetAbsoluteWidth();

    private static string[] WrappedLines(string text, float width)
    {
        TextRuntime textRuntime = new();
        textRuntime.WidthUnits = DimensionUnitType.Absolute;
        textRuntime.Width = width;
        textRuntime.Text = text;
        return textRuntime.WrappedText.Select(line => line.TrimEnd('\n', '\r')).ToArray();
    }

    [Fact]
    public void Width_RelativeToChildren_ColorRun_EqualsPlainWidth()
    {
        WidthOf("ab [Color=Red]mm[/Color] cd").ShouldBe(WidthOf("ab mm cd"), Tolerance);
    }

    [Fact]
    public void Width_RelativeToChildren_FontScaleRunOverWholeText_IsScaledPlainWidth()
    {
        float plain = WidthOf("mmmm");

        WidthOf("[FontScale=2]mmmm[/FontScale]").ShouldBe(plain * 2, Tolerance * 2);
    }

    [Fact]
    public void Width_RelativeToChildren_FontScaleRunOverPartOfText_AddsOnlyTheRunsExtraWidth()
    {
        float plainAll = WidthOf("aamm");
        float plainRun = WidthOf("mm");

        WidthOf("aa[FontScale=2]mm[/FontScale]").ShouldBe(plainAll + plainRun, Tolerance * 2);
    }

    [Fact]
    public void Height_RelativeToChildren_FontScaleRun_IsTallerThanPlain()
    {
        float plain = MakeRelative("mm").GetAbsoluteHeight();

        MakeRelative("m[FontScale=2]m[/FontScale]").GetAbsoluteHeight().ShouldBeGreaterThan(plain + 1);
    }

    [Fact]
    public void Text_TaggedThenPlain_LeavesNoStaleRunOnWrappedText()
    {
        TextRuntime textRuntime = MakeRelative("[Color=Red]ab[/Color] [FontScale=2]cd[/FontScale]");

        textRuntime.Text = "plain";

        string.Concat(textRuntime.WrappedText).ShouldBe("plain");
        ((global::RenderingLibrary.Graphics.Text)textRuntime.RenderableComponent!).InlineVariables.ShouldBeEmpty();
        textRuntime.GetAbsoluteWidth().ShouldBe(WidthOf("plain"), Tolerance);
    }

    [Fact]
    public void FontScaleProperty_ChangedWhileColorTagsPresent_ScalesLikePlainText()
    {
        TextRuntime plain = MakeRelative("mmmm");
        plain.FontScale = 2;
        TextRuntime tagged = MakeRelative("[Color=Red]mm[/Color]mm");

        tagged.FontScale = 2;

        tagged.GetAbsoluteWidth().ShouldBe(plain.GetAbsoluteWidth(), Tolerance);
    }

    [Fact]
    public void FontScaleFromState_AppliedWhileColorTagsPresent_ScalesLikePlainText()
    {
        TextRuntime plain = MakeRelative("mmmm");
        plain.FontScale = 2;
        TextRuntime tagged = MakeRelative("[Color=Red]mm[/Color]mm");
        Gum.DataTypes.Variables.StateSave big = new() { Name = "Big" };
        big.Variables.Add(new Gum.DataTypes.Variables.VariableSave { Name = "FontScale", Value = 2f, SetsValue = true });

        tagged.ApplyState(big);

        tagged.GetAbsoluteWidth().ShouldBe(plain.GetAbsoluteWidth(), Tolerance);
    }

    [Fact]
    public void WrappedText_FontScaleRunPushesWordToNextLine_WhenPlainTextFits()
    {
        float plainWidth = WidthOf("AA BB");
        float extra = WidthOf("BB");
        // Wide enough for plain "AA BB", narrower than "AA " + BB at 2x.
        float width = plainWidth + extra / 2;

        WrappedLines("AA BB", width).ShouldBe(new[] { "AA BB" });
        WrappedLines("AA [FontScale=2]BB[/FontScale]", width).Length.ShouldBe(2);
    }

    [Fact]
    public void WrappedText_ColorRunAcrossWrapPoint_WrapsLikePlainText()
    {
        float width = WidthOf("AA BB") + 2;

        WrappedLines("AA [Color=Red]BB CC[/Color] DD", width)
            .ShouldBe(WrappedLines("AA BB CC DD", width));
    }

    [Fact]
    public void GetTagAtIndex_TwoLinksAcrossWrap_EachFoundOnItsOwnLine()
    {
        float width = WidthOf("AA BB") + 2;
        TextRuntime textRuntime = new();
        textRuntime.WidthUnits = DimensionUnitType.Absolute;
        textRuntime.Width = width;
        textRuntime.Text = "[Link=a]AA BB[/Link] [Link=b]CC[/Link]";

        string.Concat(textRuntime.WrappedText).ShouldBe("AA BB CC");
        textRuntime.GetTagAtIndex("Link", 4)!.Value.Argument.ShouldBe("a");
        textRuntime.GetTagAtIndex("Link", 6)!.Value.Argument.ShouldBe("b");
    }
}
