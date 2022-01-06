using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gum.DataTypes
{
    public enum DimensionUnitType
    {
        Absolute,
        Percentage,
        RelativeToContainer,
        PercentageOfSourceFile,
        RelativeToChildren,
        PercentageOfOtherDimension,
        MaintainFileAspectRatio,
        Ratio,
        AbsoluteMultipliedByFontScale
    }

    public enum HierarchyDependencyType
    {
        NoDependency,
        DependsOnParent,
        DependsOnChildren,
        DependsOnSiblings
    }

    public static class DimensionUnitTypeExtensions
    {
        /// <summary>
        /// Returns whether one unit represents one pixel. 
        /// </summary>
        /// <param name="unitType">The unit type.</param>
        /// <returns>Whether one unit represents one pixel.</returns>
        public static bool GetIsPixelBased(this DimensionUnitType unitType)
        {
            return unitType == DimensionUnitType.Absolute ||
                unitType == DimensionUnitType.RelativeToContainer ||
                unitType == DimensionUnitType.RelativeToChildren ||
                unitType == DimensionUnitType.AbsoluteMultipliedByFontScale;
        }

        public static HierarchyDependencyType GetDependencyType(this DimensionUnitType unitType)
        {
            switch (unitType)
            {
                case DimensionUnitType.Absolute:
                case DimensionUnitType.PercentageOfSourceFile:
                case DimensionUnitType.PercentageOfOtherDimension:
                case DimensionUnitType.MaintainFileAspectRatio:
                case DimensionUnitType.AbsoluteMultipliedByFontScale:
                    return HierarchyDependencyType.NoDependency;
                case DimensionUnitType.Percentage:
                case DimensionUnitType.RelativeToContainer:
                    return HierarchyDependencyType.DependsOnParent;
                case DimensionUnitType.RelativeToChildren:
                    return HierarchyDependencyType.DependsOnChildren;
                case DimensionUnitType.Ratio:
                    return HierarchyDependencyType.DependsOnSiblings;
                default:
                    throw new NotImplementedException($"Need to handle {unitType}");
            }
        }
    }
}
