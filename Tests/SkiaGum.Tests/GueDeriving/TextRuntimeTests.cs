using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum.GueDeriving;

namespace SkiaGum.Tests.GueDeriving;

public class TextRuntimeTests
{
    #region FontFamily

    [Fact]
    public void Font_ShouldDelegateToFontFamily()
    {
        TextRuntime sut = new();
        sut.FontFamily = "Comic Sans MS";
        sut.Font.ShouldBe("Comic Sans MS");
    }

    [Fact]
    public void FontFamily_ShouldSetAndGetFont()
    {
        TextRuntime sut = new();
        sut.FontFamily = "Impact";
        sut.FontFamily.ShouldBe("Impact");
    }

    #endregion

    #region FontScale

    [Fact]
    public void FontScale_ShouldNotThrow_WhenSetToSameValue()
    {
        TextRuntime sut = new();
        sut.FontScale = 2.0f;
        Should.NotThrow(() => sut.FontScale = 2.0f);
    }

    [Fact]
    public void FontScale_ShouldUpdateValue()
    {
        TextRuntime sut = new();
        sut.FontScale = 3.0f;
        sut.FontScale.ShouldBe(3.0f);
    }

    #endregion

    #region HeightUnits

    [Fact]
    public void HeightUnits_ShouldDefaultToRelativeToChildren()
    {
        TextRuntime sut = new();
        sut.HeightUnits.ShouldBe(Gum.DataTypes.DimensionUnitType.RelativeToChildren);
    }

    #endregion

    #region LineHeightMultiplier

    [Fact]
    public void LineHeightMultiplier_ShouldNotThrow_WhenSetToSameValue()
    {
        TextRuntime sut = new();
        sut.LineHeightMultiplier = 1.5f;
        Should.NotThrow(() => sut.LineHeightMultiplier = 1.5f);
    }

    [Fact]
    public void LineHeightMultiplier_ShouldUpdateValue()
    {
        TextRuntime sut = new();
        sut.LineHeightMultiplier = 1.5f;
        sut.LineHeightMultiplier.ShouldBe(1.5f);
    }

    #endregion

    #region TextOverflowHorizontalMode

    [Fact]
    public void TextOverflowHorizontalMode_Default_ShouldBeTruncateWord()
    {
        TextRuntime sut = new();
        sut.TextOverflowHorizontalMode.ShouldBe(TextOverflowHorizontalMode.TruncateWord);
    }

    [Fact]
    public void TextOverflowHorizontalMode_WhenSetToEllipsis_ShouldReadBackAsEllipsis()
    {
        TextRuntime sut = new();
        sut.TextOverflowHorizontalMode = TextOverflowHorizontalMode.EllipsisLetter;
        sut.TextOverflowHorizontalMode.ShouldBe(TextOverflowHorizontalMode.EllipsisLetter);
    }

    [Fact]
    public void TextOverflowHorizontalMode_WhenSetBackToTruncate_ShouldReadBackAsTruncate()
    {
        TextRuntime sut = new();
        sut.TextOverflowHorizontalMode = TextOverflowHorizontalMode.EllipsisLetter;
        sut.TextOverflowHorizontalMode = TextOverflowHorizontalMode.TruncateWord;
        sut.TextOverflowHorizontalMode.ShouldBe(TextOverflowHorizontalMode.TruncateWord);
    }

    #endregion

    #region WidthUnits

    [Fact]
    public void WidthUnits_ShouldDefaultToRelativeToChildren()
    {
        TextRuntime sut = new();
        sut.WidthUnits.ShouldBe(Gum.DataTypes.DimensionUnitType.RelativeToChildren);
    }

    #endregion
}
