using System;
using System.Collections.Generic;
using System.Linq;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.GumCommon;

/// <summary>
/// Unit enums are persisted as raw ints, so a hand-edited or corrupt file can hold a value the enum
/// does not define, and the runtime reaches these conversions while building an element from file
/// data. They must degrade to a usable default instead of throwing.
/// </summary>
public class UnitTypeFallbackTests
{
    const DimensionUnitType UndefinedDimensionUnit = (DimensionUnitType)14;
    const PositionUnitType UndefinedPositionUnit = (PositionUnitType)14;

    [Fact]
    public void GetDependencyType_ShouldReturnNoDependency_WhenValueIsNotDefined()
    {
        HierarchyDependencyType dependencyType = UndefinedDimensionUnit.GetDependencyType();

        dependencyType.ShouldBe(HierarchyDependencyType.NoDependency);
    }

    [Fact]
    public void ConvertToGeneralUnit_ShouldReturnPixelsFromSmall_WhenDimensionUnitIsNotDefined()
    {
        GeneralUnitType generalUnitType = UnitConverter.ConvertToGeneralUnit(UndefinedDimensionUnit);

        generalUnitType.ShouldBe(GeneralUnitType.PixelsFromSmall);
    }

    [Fact]
    public void ConvertToGeneralUnit_ShouldReturnPixelsFromSmall_WhenPositionUnitIsNotDefined()
    {
        GeneralUnitType generalUnitType = UnitConverter.ConvertToGeneralUnit(UndefinedPositionUnit);

        generalUnitType.ShouldBe(GeneralUnitType.PixelsFromSmall);
    }

    /// <summary>
    /// The fallback above returns the same value a legitimately-independent unit returns, so a newly
    /// added member nobody wired into the switch would look handled. Pinning every defined member
    /// keeps that visible.
    /// </summary>
    [Fact]
    public void GetDependencyType_ShouldMapEveryDefinedDimensionUnitType()
    {
        Dictionary<DimensionUnitType, HierarchyDependencyType> expected = new()
        {
            [DimensionUnitType.Absolute] = HierarchyDependencyType.NoDependency,
            [DimensionUnitType.PercentageOfSourceFile] = HierarchyDependencyType.NoDependency,
            [DimensionUnitType.PercentageOfOtherDimension] = HierarchyDependencyType.NoDependency,
            [DimensionUnitType.MaintainFileAspectRatio] = HierarchyDependencyType.NoDependency,
            [DimensionUnitType.AbsoluteMultipliedByFontScale] = HierarchyDependencyType.NoDependency,
            [DimensionUnitType.ScreenPixel] = HierarchyDependencyType.NoDependency,
            [DimensionUnitType.PercentageOfParent] = HierarchyDependencyType.DependsOnParent,
            [DimensionUnitType.RelativeToParent] = HierarchyDependencyType.DependsOnParent,
            [DimensionUnitType.RelativeToMaxParentOrChildren] = HierarchyDependencyType.DependsOnParent,
            [DimensionUnitType.RelativeToChildren] = HierarchyDependencyType.DependsOnChildren,
            [DimensionUnitType.Ratio] = HierarchyDependencyType.DependsOnSiblings,
        };

        List<DimensionUnitType> defined = Enum.GetValues<DimensionUnitType>().Distinct().ToList();
        defined.ShouldBe(expected.Keys, ignoreOrder: true);

        foreach (KeyValuePair<DimensionUnitType, HierarchyDependencyType> pair in expected)
        {
            pair.Key.GetDependencyType().ShouldBe(pair.Value);
        }
    }

    [Fact]
    public void TryConvertToGeneralUnit_ShouldReturnFalse_WhenDimensionUnitIsNotDefined()
    {
        bool converted = UnitConverter.TryConvertToGeneralUnit(UndefinedDimensionUnit, out GeneralUnitType result);

        converted.ShouldBeFalse();
    }

    [Fact]
    public void TryConvertToGeneralUnit_ShouldReturnTrue_WhenDimensionUnitIsDefined()
    {
        bool converted = UnitConverter.TryConvertToGeneralUnit(DimensionUnitType.PercentageOfParent, out GeneralUnitType result);

        converted.ShouldBeTrue();
        result.ShouldBe(GeneralUnitType.Percentage);
    }
}
