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
    [Theory]
    [InlineData(DimensionUnitType.Ratio, GeneralUnitType.PixelsFromSmall)]
    [InlineData(DimensionUnitType.ScreenPixel, GeneralUnitType.PixelsFromSmall)]
    [InlineData(DimensionUnitType.RelativeToMaxParentOrChildren, GeneralUnitType.PixelsFromLarge)]
    public void ConvertToGeneralUnit_ShouldMapEveryDimensionUnitTypeWithoutThrowing(DimensionUnitType unitType, GeneralUnitType expected)
    {
        GeneralUnitType result = UnitConverter.ConvertToGeneralUnit(unitType);

        result.ShouldBe(expected);
    }
}
