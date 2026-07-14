using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum;
using SkiaGum.GueDeriving;
using SkiaSharp;

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

    #region Blend

    [Fact]
    public void Blend_SetInCode_ShouldPushToContainedText()
    {
        // TextRuntime.Blend was #if !SKIA — it silently didn't exist on Skia, so callers had to reach
        // through to the renderable. Unified interface: the property now forwards to SkiaGum.Text.Blend
        // (the XNA BlendState translation stays #if XNALIKE). No SystemManagers wiring needed — the
        // setter forwards straight to the contained renderable, not through the font-update delegate.
        TextRuntime sut = new();
        sut.Blend = Gum.RenderingLibrary.Blend.Additive;

        Text containedText = (Text)sut.RenderableComponent;
        containedText.Blend.ShouldBe(Gum.RenderingLibrary.Blend.Additive);
    }

    #endregion

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

    #region Clone

    [Fact]
    public void Clone_ShouldCreateClonedText()
    {
        TextRuntime sut = new();
        TextRuntime clone = (TextRuntime)sut.Clone();
        clone.ShouldNotBeNull();
        clone.RenderableComponent.ShouldNotBeSameAs(sut.RenderableComponent);
    }

    #endregion

    #region Color

    [Fact]
    public void Color_ShouldDefaultToWhite_MatchingMonoGameAndRaylib()
    {
        // MonoGame (RenderingLibrary.Graphics.Text: mRed/mGreen/mBlue/mAlpha = 255) and raylib
        // (Renderables.Text: Color = Color.White) both default text to white; the SkiaGum renderable
        // drifted to black. Parity: the outlier is corrected to white.
        TextRuntime sut = new();

        Text containedText = (Text)sut.RenderableComponent;
        containedText.Color.ShouldBe(SKColors.White);
    }

    #endregion

    #region Dropshadow

    [Fact]
    public void HasDropshadow_SetInCode_ShouldDriveRenderableImageFilterShadow()
    {
        // Cross-backend baked-shadow API (TextRuntime.HasDropshadow + params) maps onto the SkiaGum.Text
        // renderable's standalone ImageFilter shadow via the code-property path (SystemManagers.UpdateFonts).
        // Without it, HasDropshadow silently no-ops on Skia (renders plain text, no shadow).
        new RenderingLibrary.SystemManagers().Initialize();

        TextRuntime sut = new();
        sut.HasDropshadow = true;
        sut.DropshadowColor = new SKColor(220, 40, 160, 220);
        sut.DropshadowOffsetX = 2;
        sut.DropshadowOffsetY = 4;
        sut.DropshadowBlur = 6;

        Text contained = (Text)sut.RenderableComponent;
        contained.HasDropshadow.ShouldBeTrue();
        contained.DropshadowColor.ShouldBe(new SKColor(220, 40, 160, 220));
        contained.DropshadowOffsetX.ShouldBe(2);
        contained.DropshadowOffsetY.ShouldBe(4);
        contained.DropshadowBlurX.ShouldBe(6);
        contained.DropshadowBlurY.ShouldBe(6);
        contained.GetRenderPaint().ShouldNotBeNull();
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

    #region IsTruncatingWithEllipsisOnLastLine

    [Fact]
    public void IsTruncatingWithEllipsisOnLastLine_SetInCode_ShouldPushToContainedText()
    {
        // Unified onto TextRuntime (#3677 overflow demo): was renderable-only, so both samples had to
        // cast to the renderable to reach it. Now forwards straight to the contained renderable.
        TextRuntime sut = new();
        sut.IsTruncatingWithEllipsisOnLastLine = true;

        Text containedText = (Text)sut.RenderableComponent;
        containedText.IsTruncatingWithEllipsisOnLastLine.ShouldBeTrue();
    }

    #endregion

    #region HasEvents

    [Fact]
    public void HasEvents_ShouldDefaultToFalse()
    {
        TextRuntime sut = new();
        sut.HasEvents.ShouldBeFalse();
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

    #region MaxLettersToShow

    [Fact]
    public void MaxLettersToShow_SetProperty_ShouldPushToContainedText()
    {
        TextRuntime sut = new();

        // Routes through the newly-wired SKIA dispatch arm (issue #3678), which was previously
        // gated #if MONOGAME || XNA4 and dead on Skia.
        sut.SetProperty("MaxLettersToShow", (int?)4);

        Text containedText = (Text)sut.RenderableComponent;
        containedText.MaxLettersToShow.ShouldBe(4);
    }

    #endregion

    #region OutlineThickness

    [Fact]
    public void OutlineThickness_SetInCode_ShouldPushToContainedText()
    {
        // The code-property path (TextRuntime.OutlineThickness setter -> UpdateToFontValues ->
        // UpdateFontFromProperties delegate) is wired by SystemManagers.Initialize. #3675 only wired
        // the string/SetProperty path, so setting OutlineThickness in code drew no halo until #3684.
        new RenderingLibrary.SystemManagers().Initialize();

        TextRuntime sut = new();
        sut.OutlineThickness = 7;

        Text containedText = (Text)sut.RenderableComponent;
        containedText.OutlineThickness.ShouldBe(7);
    }

    #endregion

    #region PropertyChanged

    [Fact]
    public void PropertyChanged_ShouldRaise_WhenTextChanges()
    {
        TextRuntime sut = new();
        string? changedPropertyName = null;
        sut.PropertyChanged += (_, args) => changedPropertyName = args.PropertyName;

        sut.Text = "Hello 1234";

        changedPropertyName.ShouldBe(nameof(TextRuntime.Text));
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

    #region TextOverflowVerticalMode

    [Fact]
    public void TextOverflowVerticalMode_SetInCode_ShouldPushToContainedText()
    {
        // Unified onto TextRuntime (#3677 overflow demo): was renderable-only. Now forwards straight
        // to the contained renderable so the overflow sample doesn't have to cast.
        TextRuntime sut = new();
        sut.TextOverflowVerticalMode = TextOverflowVerticalMode.TruncateLine;

        Text containedText = (Text)sut.RenderableComponent;
        containedText.TextOverflowVerticalMode.ShouldBe(TextOverflowVerticalMode.TruncateLine);
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
