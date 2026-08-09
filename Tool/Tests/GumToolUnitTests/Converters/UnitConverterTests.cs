using Gum.Converters;
using Gum.DataTypes;
using Shouldly;

namespace GumToolUnitTests.Converters;

/// <summary>
/// Pins <see cref="UnitConverter.ConvertToGeneralUnit(DimensionUnitType)"/> against every
/// <see cref="DimensionUnitType"/> value, including ones added after the switch was last updated
/// (#4395 - resizing an object using one of these threw NotImplementedException).
/// </summary>
public class UnitConverterTests
{
    // Covers every DimensionUnitType selectable as a Width/Height unit in the tool (see
    // WidthUnitsControl/HeightUnitsControl's option lists) - PixelsFromSmall/PixelsFromLarge both
    // mean "resize 1:1 with the cursor" (ElementCommands.ConvertAmountToPixelAccordingToUnitType
    // returns the raw pixel delta unchanged for both buckets).
    [Theory]
    [InlineData(DimensionUnitType.Absolute, GeneralUnitType.PixelsFromSmall)]
    [InlineData(DimensionUnitType.RelativeToParent, GeneralUnitType.PixelsFromLarge)]
    [InlineData(DimensionUnitType.PercentageOfParent, GeneralUnitType.Percentage)]
    [InlineData(DimensionUnitType.Ratio, GeneralUnitType.PixelsFromSmall)]
    [InlineData(DimensionUnitType.RelativeToChildren, GeneralUnitType.PixelsFromSmall)]
    [InlineData(DimensionUnitType.PercentageOfOtherDimension, GeneralUnitType.PercentageOfOtherDimension)]
    [InlineData(DimensionUnitType.PercentageOfSourceFile, GeneralUnitType.PercentageOfFile)]
    [InlineData(DimensionUnitType.MaintainFileAspectRatio, GeneralUnitType.MaintainFileAspectRatio)]
    [InlineData(DimensionUnitType.AbsoluteMultipliedByFontScale, GeneralUnitType.PixelsFromSmall)]
    [InlineData(DimensionUnitType.RelativeToMaxParentOrChildren, GeneralUnitType.PixelsFromLarge)]
    [InlineData(DimensionUnitType.ScreenPixel, GeneralUnitType.PixelsFromSmall)]
    public void ConvertToGeneralUnit_ShouldMapEveryDimensionUnitTypeWithoutThrowing(DimensionUnitType unitType, GeneralUnitType expected)
    {
        GeneralUnitType result = UnitConverter.ConvertToGeneralUnit(unitType);

        result.ShouldBe(expected);
    }

    [Fact]
    public void ConvertToUnitTypeCoordinates_WithPercentageOfParentX_ShouldScaleByParentWidth()
    {
        float parentWidth = 200f;
        float pixelDeltaX = 10f;

        UnitConverter.Self.ConvertToUnitTypeCoordinates(pixelDeltaX, 0f,
            GeneralUnitType.Percentage, GeneralUnitType.Percentage,
            ownerWidthInPixels: 0f, ownerHeightInPixels: 0f,
            parentWidth: parentWidth, parentHeight: 0f,
            fileWidth: 0f, fileHeight: 0f,
            out float relativeX, out float relativeY);

        relativeX.ShouldBe(5f, tolerance: 0.0001f);
    }

    [Fact]
    public void ConvertToUnitTypeCoordinates_WithPercentageOfSourceFileX_ShouldScaleByFileWidth()
    {
        // Covers the EntireTexture case only; ElementCommands.ConvertAmountToPixelAccordingToUnitType
        // applies an additional texture-visible-ratio adjustment for non-EntireTexture sprites.
        float fileWidth = 300f;
        float pixelDeltaX = 10f;

        UnitConverter.Self.ConvertToUnitTypeCoordinates(pixelDeltaX, 0f,
            GeneralUnitType.PercentageOfFile, GeneralUnitType.PercentageOfFile,
            ownerWidthInPixels: 0f, ownerHeightInPixels: 0f,
            parentWidth: 0f, parentHeight: 0f,
            fileWidth: fileWidth, fileHeight: 0f,
            out float relativeX, out float relativeY);

        relativeX.ShouldBe(10f / 3f, tolerance: 0.0001f);
    }

    [Fact]
    public void ConvertToUnitTypeCoordinates_WithPercentageOfOtherDimensionX_ShouldScaleByOwnerHeight()
    {
        float ownerHeightInPixels = 40f;
        float pixelDeltaX = 10f;

        UnitConverter.Self.ConvertToUnitTypeCoordinates(pixelDeltaX, 0f,
            GeneralUnitType.PercentageOfOtherDimension, GeneralUnitType.PercentageOfOtherDimension,
            ownerWidthInPixels: 0f, ownerHeightInPixels: ownerHeightInPixels,
            parentWidth: 0f, parentHeight: 0f,
            fileWidth: 0f, fileHeight: 0f,
            out float relativeX, out float relativeY);

        relativeX.ShouldBe(25f, tolerance: 0.0001f);
    }

    [Fact]
    public void ConvertToUnitTypeCoordinates_WithMaintainFileAspectRatioX_ShouldScaleByFileAndOwnerSize()
    {
        // Sprite where pixelWidth = ownerHeightInPixels * (fileWidth/fileHeight) * (mWidth/100):
        // 40 * (300/100) * (100/100) = 120. Growing by 12px should scale mWidth by 10 (not 12) -
        // #4395 follow-up: this branch was a stub that returned the raw pixel delta unchanged.
        float ownerHeightInPixels = 40f;
        float fileWidth = 300f;
        float fileHeight = 100f;
        float pixelDeltaX = 12f;

        UnitConverter.Self.ConvertToUnitTypeCoordinates(pixelDeltaX, 0f,
            GeneralUnitType.MaintainFileAspectRatio, GeneralUnitType.MaintainFileAspectRatio,
            ownerWidthInPixels: 0f, ownerHeightInPixels: ownerHeightInPixels,
            parentWidth: 0f, parentHeight: 0f,
            fileWidth: fileWidth, fileHeight: fileHeight,
            out float relativeX, out float relativeY);

        relativeX.ShouldBe(10f, tolerance: 0.0001f);
    }

    [Fact]
    public void ConvertToUnitTypeCoordinates_WithMaintainFileAspectRatioY_ShouldScaleByFileAndOwnerSize()
    {
        // pixelHeight = ownerWidthInPixels * (fileHeight/fileWidth) * (mHeight/100):
        // 40 * (100/300) * (100/100) = 13.333. Growing by 4px should scale mHeight by 30 (not 4).
        float ownerWidthInPixels = 40f;
        float fileWidth = 300f;
        float fileHeight = 100f;
        float pixelDeltaY = 4f;

        UnitConverter.Self.ConvertToUnitTypeCoordinates(0f, pixelDeltaY,
            GeneralUnitType.MaintainFileAspectRatio, GeneralUnitType.MaintainFileAspectRatio,
            ownerWidthInPixels: ownerWidthInPixels, ownerHeightInPixels: 0f,
            parentWidth: 0f, parentHeight: 0f,
            fileWidth: fileWidth, fileHeight: fileHeight,
            out float relativeX, out float relativeY);

        relativeY.ShouldBe(30f, tolerance: 0.0001f);
    }
}
