using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum;
using SkiaGum.GueDeriving;

namespace SkiaGum.Tests.GueDeriving;

public class TextRuntimeTests
{
    public TextRuntimeTests()
    {
        // Wire up the SkiaGum custom property setter so SetProperty routes correctly.
        // Normally done by SystemManagers.Initialize(), but we don't need the full
        // rendering pipeline for these unit tests.
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    #region BoldWeight

    [Fact]
    public void BoldWeight_ShouldDefaultToOne()
    {
        TextRuntime sut = new();
        sut.BoldWeight.ShouldBe(1f);
    }

    [Fact]
    public void BoldWeight_ShouldRoundTrip()
    {
        TextRuntime sut = new();
        sut.BoldWeight = 2.0f;
        sut.BoldWeight.ShouldBe(2.0f);
    }

    [Fact]
    public void BoldWeight_ShouldPushToContainedText()
    {
        TextRuntime sut = new();
        sut.BoldWeight = 1.5f;
        Text containedText = (Text)sut.RenderableComponent;
        containedText.BoldWeight.ShouldBe(1.5f);
    }

    #endregion

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

    #region FontSize

    [Fact]
    public void FontSize_ShouldDefaultTo18()
    {
        TextRuntime sut = new();
        sut.FontSize.ShouldBe(18);
    }

    [Fact]
    public void FontSize_ShouldRoundTrip()
    {
        TextRuntime sut = new();
        sut.FontSize = 24;
        sut.FontSize.ShouldBe(24);
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

    #region IsBold

    [Fact]
    public void IsBold_ShouldDefaultToFalse()
    {
        TextRuntime sut = new();
        sut.IsBold.ShouldBeFalse();
    }

    [Fact]
    public void IsBold_WhenSetTrue_ShouldSetBoldWeightAboveOne()
    {
        TextRuntime sut = new();
        sut.IsBold = true;
        sut.BoldWeight.ShouldBeGreaterThan(1f);
    }

    [Fact]
    public void IsBold_WhenSetFalse_ShouldSetBoldWeightToOne()
    {
        TextRuntime sut = new();
        sut.IsBold = true;
        sut.IsBold = false;
        sut.BoldWeight.ShouldBe(1f);
    }

    [Fact]
    public void IsBold_ShouldReflectBoldWeight()
    {
        TextRuntime sut = new();
        sut.BoldWeight = 2.0f;
        sut.IsBold.ShouldBeTrue();

        sut.BoldWeight = 1.0f;
        sut.IsBold.ShouldBeFalse();
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

    #region SetTextNoTranslate

    [Fact]
    public void SetTextNoTranslate_ShouldUpdateTextProperty()
    {
        TextRuntime sut = new();
        sut.SetTextNoTranslate("Translated Text");
        sut.Text.ShouldBe("Translated Text");
    }

    [Fact]
    public void SetTextNoTranslate_WhenNull_ShouldSetTextToNull()
    {
        TextRuntime sut = new();
        sut.Text = "Some text";
        sut.SetTextNoTranslate(null);
        sut.Text.ShouldBeNull();
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

    #region UseFontSmoothing

    [Fact]
    public void UseFontSmoothing_ShouldDefaultToTrue()
    {
        TextRuntime sut = new();
        sut.UseFontSmoothing.ShouldBeTrue();
    }

    [Fact]
    public void UseFontSmoothing_ShouldRoundTrip()
    {
        TextRuntime sut = new();
        sut.UseFontSmoothing = false;
        sut.UseFontSmoothing.ShouldBeFalse();
    }

    #endregion

    #region Defaults

    [Fact]
    public void DefaultFont_ShouldBeArial()
    {
        TextRuntime.DefaultFont.ShouldBe("Arial");
    }

    [Fact]
    public void DefaultFontSize_ShouldBe18()
    {
        TextRuntime.DefaultFontSize.ShouldBe(18);
    }

    [Fact]
    public void CustomFontFile_ShouldBeNullByDefault()
    {
        TextRuntime sut = new();
        sut.CustomFontFile.ShouldBeNull();
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
