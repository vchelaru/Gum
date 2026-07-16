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

    #region Alpha

    [Fact]
    public void Alpha_SetProperty_ShouldPushToContainedText()
    {
        // Issue #3706/ADR 0010: this dispatch arm was gated #if MONOGAME || XNA4, which is dead
        // inside a method that only compiles under SKIA, so this previously worked only by
        // accident via the reflection fallback. Now redispatched onto TextRuntime.
        TextRuntime sut = new();

        sut.SetProperty("Alpha", 128);

        Text containedText = (Text)sut.RenderableComponent;
        containedText.Alpha.ShouldBe(128);
    }

    #endregion

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

    [Fact]
    public void Blend_SetProperty_ShouldPushToContainedText()
    {
        // Issue #3706/ADR 0010: redispatched onto TextRuntime.Blend (previously wrote
        // text.Blend directly) -- structurally different, same end result.
        TextRuntime sut = new();

        sut.SetProperty("Blend", Gum.RenderingLibrary.Blend.Additive);

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

    [Fact]
    public void Color_SetProperty_ShouldPushToContainedText()
    {
        // Issue #3706/ADR 0010: this dispatch arm was gated #if MONOGAME || XNA4, which is dead
        // inside a method that only compiles under SKIA, so SetProperty("Color", ...) silently
        // fell through to the reflection fallback -- which throws internally converting
        // System.Drawing.Color to SKColor (not IConvertible-compatible) and swallows the failure.
        TextRuntime sut = new();

        sut.SetProperty("Color", System.Drawing.Color.FromArgb(200, 10, 20, 30));

        Text containedText = (Text)sut.RenderableComponent;
        containedText.Color.ShouldBe(new SKColor(10, 20, 30, 200));
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

    [Fact]
    public void DropshadowBlurXY_SetInCode_ShouldOverrideIndependentlyOfDropshadowBlur()
    {
        // Skia-only independent axes (#3709): DropshadowBlur still seeds both axes equally for
        // callers that only set the cross-backend scalar (see the test above), but setting
        // DropshadowBlurX/Y explicitly lets a Skia caller diverge the two -- something the
        // XNALIKE/Raylib baked shadow can't do.
        new RenderingLibrary.SystemManagers().Initialize();

        TextRuntime sut = new();
        sut.DropshadowBlur = 6;
        sut.DropshadowBlurX = 3;
        sut.DropshadowBlurY = 9;

        Text containedText = (Text)sut.RenderableComponent;
        containedText.DropshadowBlurX.ShouldBe(3);
        containedText.DropshadowBlurY.ShouldBe(9);
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

    [Fact]
    public void FontScale_SetProperty_ShouldPushToContainedText()
    {
        // Issue #3706/ADR 0010: redispatched onto TextRuntime.FontScale (previously wrote
        // text.FontScale directly) -- structurally different, same end result.
        TextRuntime sut = new();

        sut.SetProperty("FontScale", 2.5f);

        Text containedText = (Text)sut.RenderableComponent;
        containedText.FontScale.ShouldBe(2.5f);
    }

    #endregion

    #region GetCharacterIndexAtPosition

    // #3708: TextRuntime.GetCharacterIndexAtPosition was #if !SKIA -- SkiaGum's Text renderable had no
    // equivalent at all. Implemented via RichTextKit's own TextBlock.HitTest rather than the per-character
    // measurement loop TextExtensions.GetCharacterIndexAtPosition uses on MonoGame/Raylib, since Skia's
    // renderable has no bitmap-font character-advance table to loop over.
    // An unmanaged Text sits at absolute origin (0,0), so screen (0,0) is its top-left -- mirrors the
    // Raylib parity test (RaylibGum.Tests.Runtimes.TextRuntimeTests).
    [Fact]
    public void GetCharacterIndexAtPosition_AtLeftEdge_ShouldReturnZero()
    {
        TextRuntime sut = new();
        sut.Width = 200;
        sut.Text = "Hello";

        sut.GetCharacterIndexAtPosition(0, 0).ShouldBe(0);
    }

    // A click far past the right edge of a single line clamps to the end of the text (index == length),
    // matching the MonoGame/Raylib renderables' behavior.
    [Fact]
    public void GetCharacterIndexAtPosition_FarRightOfSingleLine_ShouldReturnTextLength()
    {
        TextRuntime sut = new();
        sut.Width = 200;
        sut.Text = "Hello";

        sut.GetCharacterIndexAtPosition(1000, 0).ShouldBe(5);
    }

    // The returned index is defined as an offset into the concatenation of WrappedText (issue body,
    // "index within the WrappedText, so to index in, you need to loop through each line"), not an index
    // local to whichever line was clicked. A click on the second line must therefore account for the
    // first line's length -- clamping to the full text length here proves the cross-line accumulation
    // works, since a line-local implementation would instead clamp to "Line2".Length.
    [Fact]
    public void GetCharacterIndexAtPosition_FarRightOfSecondLine_ShouldReturnFullTextLength()
    {
        TextRuntime sut = new();
        sut.Width = 200;
        string text = "Line1\nLine2";
        sut.Text = text;

        Text containedText = (Text)sut.RenderableComponent;
        var lineHeight = containedText.LineHeightInPixels * containedText.LineHeightMultiplier;

        sut.GetCharacterIndexAtPosition(1000, lineHeight + 1).ShouldBe(text.Length);
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

    #region HorizontalAlignment

    [Fact]
    public void HorizontalAlignment_SetProperty_ShouldPushToContainedText()
    {
        // Issue #3706/ADR 0010: redispatched onto TextRuntime.HorizontalAlignment (previously
        // wrote text.HorizontalAlignment directly) -- structurally different, same end result.
        TextRuntime sut = new();

        sut.SetProperty("HorizontalAlignment", RenderingLibrary.Graphics.HorizontalAlignment.Right);

        Text containedText = (Text)sut.RenderableComponent;
        containedText.HorizontalAlignment.ShouldBe(RenderingLibrary.Graphics.HorizontalAlignment.Right);
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

    [Fact]
    public void LineHeightMultiplier_SetProperty_ShouldPushToContainedText()
    {
        // Issue #3706/ADR 0010: had no dispatch arm at all, so this previously worked only by
        // accident via the reflection fallback. Now redispatched onto TextRuntime.
        TextRuntime sut = new();

        sut.SetProperty("LineHeightMultiplier", 1.75f);

        Text containedText = (Text)sut.RenderableComponent;
        containedText.LineHeightMultiplier.ShouldBe(1.75f);
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

    [Fact]
    public void MaxLettersToShow_SetInCode_ShouldPushToContainedText()
    {
        // The direct code-property setter was #if !SKIA -- the SetProperty dispatch arm above
        // (#3678) already worked on Skia, but callers setting this straight from C# had no property
        // to call. SkiaGum.Text.MaxLettersToShow already implements the capability. (#3709)
        TextRuntime sut = new();

        sut.MaxLettersToShow = 4;

        Text containedText = (Text)sut.RenderableComponent;
        containedText.MaxLettersToShow.ShouldBe(4);
    }

    #endregion

    #region MaxNumberOfLines

    [Fact]
    public void MaxNumberOfLines_SetProperty_ShouldPushToContainedText()
    {
        // Issue #3706/ADR 0010: had no dispatch arm at all, so this previously worked only by
        // accident via the reflection fallback. Now redispatched onto TextRuntime.
        TextRuntime sut = new();

        sut.SetProperty("MaxNumberOfLines", (int?)3);

        Text containedText = (Text)sut.RenderableComponent;
        containedText.MaxNumberOfLines.ShouldBe(3);
    }

    #endregion

    #region Red, Green, Blue

    [Fact]
    public void RedGreenBlue_SetProperty_ShouldPushToContainedText()
    {
        // Issue #3706/ADR 0010: redispatched onto TextRuntime.Red/Green/Blue (previously wrote
        // text.Red/Green/Blue directly) -- structurally different, same end result.
        TextRuntime sut = new();

        sut.SetProperty("Red", 10);
        sut.SetProperty("Green", 20);
        sut.SetProperty("Blue", 30);

        Text containedText = (Text)sut.RenderableComponent;
        containedText.Red.ShouldBe(10);
        containedText.Green.ShouldBe(20);
        containedText.Blue.ShouldBe(30);
    }

    #endregion

    #region OutlineColor

    [Fact]
    public void OutlineColor_SetInCode_ShouldPushToContainedText()
    {
        // Skia-only: MonoGame/Raylib bake the outline into the font atlas and have no runtime
        // outline-color concept. Code-property path -> UpdateToFontValues -> UpdateFonts bridge,
        // mirroring the OutlineThickness fix from bug #3684. (#3709)
        new RenderingLibrary.SystemManagers().Initialize();

        TextRuntime sut = new();
        sut.OutlineColor = new SKColor(10, 20, 30, 255);

        Text containedText = (Text)sut.RenderableComponent;
        containedText.OutlineColor.ShouldBe(new SKColor(10, 20, 30, 255));
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

    [Fact]
    public void OutlineThickness_SetProperty_WithNoFontSet_ShouldStillPushToContainedText()
    {
        // #3684 as filed pointed at CustomSetPropertyOnRenderable.UpdateToFontValues(IText,
        // GraphicalUiElement), which gates its whole body (including OutlineThickness) behind
        // Font being non-empty. That method turned out to have zero callers: both the code-property
        // path and this SetProperty path route through GraphicalUiElement.UpdateToFontValues()'s
        // UpdateFontFromProperties delegate, which SystemManagers.Initialize wires to
        // SystemManagers.UpdateFonts -- an unconditional push, not the gated method. Pinning that
        // wiring here so a future rewire can't silently reintroduce the Font-gated bug.
        new RenderingLibrary.SystemManagers().Initialize();

        TextRuntime sut = new();
        sut.Font = "";

        sut.SetProperty("OutlineThickness", 7);

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

    [Fact]
    public void TextOverflowHorizontalMode_SetProperty_ShouldPushToContainedText()
    {
        // Issue #3706/ADR 0010: this arm never set handled = true, so every assignment redundantly
        // fell through to reflection afterward (the value still applied). Redispatched onto
        // TextRuntime.TextOverflowHorizontalMode, which already implements this enum-to-bool mapping.
        TextRuntime sut = new();

        sut.SetProperty("TextOverflowHorizontalMode", TextOverflowHorizontalMode.EllipsisLetter);

        Text containedText = (Text)sut.RenderableComponent;
        containedText.IsTruncatingWithEllipsisOnLastLine.ShouldBeTrue();
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

    #region TextRenderingPositionMode

    // TextRuntime.TextRenderingPositionMode was #if !SKIA -- Skia's Text renderable had no
    // pixel-snap concept at all (issue #3708). Now forwards straight to the contained renderable,
    // matching the MonoGame/Raylib TextRuntime surface.

    [Fact]
    public void TextRenderingPositionMode_ShouldDefaultToNull()
    {
        TextRuntime sut = new();
        sut.TextRenderingPositionMode.ShouldBeNull();
    }

    [Fact]
    public void TextRenderingPositionMode_ShouldRoundTripThroughRenderable()
    {
        TextRuntime sut = new();
        sut.TextRenderingPositionMode = TextRenderingPositionMode.FreeFloating;

        sut.TextRenderingPositionMode.ShouldBe(TextRenderingPositionMode.FreeFloating);
        ((Text)sut.RenderableComponent).OverrideTextRenderingPositionMode
            .ShouldBe(TextRenderingPositionMode.FreeFloating);
    }

    #endregion

    #region Typeface

    // Typeface (issue #3708): the explicit-font-object escape hatch Skia lacked entirely, unified
    // under the same name as XNALIKE's BitmapFont / Raylib's Typeface. Forwards straight to the
    // contained renderable.

    [Fact]
    public void Typeface_ShouldDefaultToNull()
    {
        TextRuntime sut = new();
        sut.Typeface.ShouldBeNull();
    }

    [Fact]
    public void Typeface_ShouldRoundTripThroughRenderable()
    {
        TextRuntime sut = new();
        SKTypeface typeface = SKTypeface.FromFamilyName("Arial");

        sut.Typeface = typeface;

        sut.Typeface.ShouldBe(typeface);
        ((Text)sut.RenderableComponent).Typeface.ShouldBe(typeface);
    }

    [Fact]
    public void DefaultTypeface_AppliedInConstructor()
    {
        SKTypeface? saved = TextRuntime.DefaultTypeface;
        try
        {
            SKTypeface typeface = SKTypeface.FromFamilyName("Arial");
            TextRuntime.DefaultTypeface = typeface;

            TextRuntime sut = new();

            sut.Typeface.ShouldBe(typeface);
        }
        finally
        {
            TextRuntime.DefaultTypeface = saved;
        }
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

    #region VerticalAlignment

    [Fact]
    public void VerticalAlignment_SetProperty_ShouldPushToContainedText()
    {
        // Issue #3706/ADR 0010: redispatched onto TextRuntime.VerticalAlignment (previously wrote
        // text.VerticalAlignment directly) -- structurally different, same end result.
        TextRuntime sut = new();

        sut.SetProperty("VerticalAlignment", VerticalAlignment.Bottom);

        Text containedText = (Text)sut.RenderableComponent;
        containedText.VerticalAlignment.ShouldBe(VerticalAlignment.Bottom);
    }

    #endregion

    #region WrappedText

    [Fact]
    public void WrappedText_ShouldForwardToContainedText()
    {
        // TextRuntime.WrappedText was #if !SKIA -- SkiaGum.Text.WrappedText already implements this,
        // TextRuntime just never forwarded it. (#3709)
        TextRuntime sut = new();
        sut.Text = "Hello World";

        Text containedText = (Text)sut.RenderableComponent;
        sut.WrappedText.ShouldBe(containedText.WrappedText);
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
