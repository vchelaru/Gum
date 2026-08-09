using Gum.DataTypes;
using Gum.Wireframe.Editors.Visuals;
using Shouldly;

namespace GumToolUnitTests.Wireframe;

/// <summary>
/// Pins <see cref="DimensionDisplayVisual.GetDimensionSuffix"/> against every DimensionUnitType
/// selectable as a Width/Height unit in the tool. AbsoluteMultipliedByFontScale and
/// RelativeToMaxParentOrChildren were missing, so the resize handle's dimension display showed only
/// the raw absolute pixel size for those two - unlike Ratio/Percentage/etc., which show both the raw
/// stored value and the resulting pixel size (#4395 follow-up).
/// </summary>
public class DimensionDisplayVisualTests
{
    [Theory]
    [InlineData(DimensionUnitType.Absolute, false)]
    [InlineData(DimensionUnitType.RelativeToParent, true)]
    [InlineData(DimensionUnitType.PercentageOfParent, true)]
    [InlineData(DimensionUnitType.Ratio, true)]
    [InlineData(DimensionUnitType.RelativeToChildren, true)]
    [InlineData(DimensionUnitType.PercentageOfOtherDimension, true)]
    [InlineData(DimensionUnitType.PercentageOfSourceFile, true)]
    [InlineData(DimensionUnitType.MaintainFileAspectRatio, true)]
    [InlineData(DimensionUnitType.AbsoluteMultipliedByFontScale, true)]
    [InlineData(DimensionUnitType.RelativeToMaxParentOrChildren, true)]
    public void GetDimensionSuffix_ShouldReturnNonNullSuffix_ForEveryUnitTypeExceptAbsolute(
        DimensionUnitType unitType, bool shouldHaveSuffix)
    {
        string suffix = DimensionDisplayVisual.GetDimensionSuffix(unitType, isWidth: true);

        if (shouldHaveSuffix)
        {
            suffix.ShouldNotBeNull();
        }
        else
        {
            suffix.ShouldBeNull();
        }
    }
}
