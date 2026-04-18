using Gum.GueDeriving;
using Gum.Renderables;
using RenderingLibrary.Graphics;
using Shouldly;

namespace SokolGum.Tests.Runtimes;

public class TextRuntimeTests : BaseTestClass
{
    [Fact]
    public void ContainedRenderable_ShouldBeText()
    {
        var sut = new TextRuntime();
        sut.RenderableComponent.ShouldBeOfType<Text>();
    }

    [Fact]
    public void RawText_ShouldBeNullByDefault()
    {
        var sut = new TextRuntime();
        sut.RawText.ShouldBeNull();
    }

    [Fact]
    public void RawText_ShouldRoundTrip()
    {
        var sut = new TextRuntime { RawText = "hello" };
        sut.RawText.ShouldBe("hello");
    }

    [Fact]
    public void FontSize_ShouldDefaultTo16()
    {
        var sut = new TextRuntime();
        sut.FontSize.ShouldBe(16f);
    }

    [Fact]
    public void FontSize_ShouldRoundTrip()
    {
        var sut = new TextRuntime { FontSize = 24f };
        sut.FontSize.ShouldBe(24f);
    }

    [Fact]
    public void Font_ShouldBeNullByDefault()
    {
        // Font is null until explicitly assigned (no "default Arial" behaviour
        // as in RaylibGum — fontstash-based text has no built-in fallback).
        var sut = new TextRuntime();
        sut.Font.ShouldBeNull();
    }

    [Fact]
    public void HorizontalAlignment_ShouldDefaultToLeft()
    {
        var sut = new TextRuntime();
        sut.HorizontalAlignment.ShouldBe(HorizontalAlignment.Left);
    }

    [Fact]
    public void HorizontalAlignment_ShouldRoundTrip()
    {
        var sut = new TextRuntime { HorizontalAlignment = HorizontalAlignment.Center };
        sut.HorizontalAlignment.ShouldBe(HorizontalAlignment.Center);
    }

    [Fact]
    public void VerticalAlignment_ShouldDefaultToTop()
    {
        var sut = new TextRuntime();
        sut.VerticalAlignment.ShouldBe(VerticalAlignment.Top);
    }

    [Fact]
    public void VerticalAlignment_ShouldRoundTrip()
    {
        var sut = new TextRuntime { VerticalAlignment = VerticalAlignment.Bottom };
        sut.VerticalAlignment.ShouldBe(VerticalAlignment.Bottom);
    }

    [Fact]
    public void TextOverflowHorizontalMode_ShouldDefaultToTruncateWord()
    {
        var sut = new TextRuntime();
        sut.TextOverflowHorizontalMode.ShouldBe(TextOverflowHorizontalMode.TruncateWord);
    }

    [Fact]
    public void TextOverflowHorizontalMode_ShouldRoundTrip()
    {
        var sut = new TextRuntime { TextOverflowHorizontalMode = TextOverflowHorizontalMode.EllipsisLetter };
        sut.TextOverflowHorizontalMode.ShouldBe(TextOverflowHorizontalMode.EllipsisLetter);
    }

    [Fact]
    public void TextOverflowVerticalMode_ShouldDefaultToSpillOver()
    {
        var sut = new TextRuntime();
        sut.TextOverflowVerticalMode.ShouldBe(TextOverflowVerticalMode.SpillOver);
    }

    [Fact]
    public void OutlineThickness_ShouldDefaultToZero()
    {
        var sut = new TextRuntime();
        sut.OutlineThickness.ShouldBe(0);
    }

    [Fact]
    public void OutlineThickness_ShouldRoundTrip()
    {
        var sut = new TextRuntime { OutlineThickness = 2 };
        sut.OutlineThickness.ShouldBe(2);
    }

    [Fact]
    public void WrapTextInsideBlock_ShouldDefaultToTrue()
    {
        var sut = new TextRuntime();
        sut.WrapTextInsideBlock.ShouldBeTrue();
    }

    [Fact]
    public void LineHeightMultiplier_ShouldDefaultToOne()
    {
        var sut = new TextRuntime();
        sut.LineHeightMultiplier.ShouldBe(1f);
    }

    [Fact]
    public void LineHeightMultiplier_ShouldRoundTrip()
    {
        var sut = new TextRuntime { LineHeightMultiplier = 1.5f };
        sut.LineHeightMultiplier.ShouldBe(1.5f);
    }

    [Fact]
    public void MaxLettersToShow_ShouldBeNullByDefault()
    {
        var sut = new TextRuntime();
        sut.MaxLettersToShow.ShouldBeNull();
    }

    [Fact]
    public void MaxLettersToShow_ShouldRoundTrip()
    {
        var sut = new TextRuntime { MaxLettersToShow = 10 };
        sut.MaxLettersToShow.ShouldBe(10);
    }
}
