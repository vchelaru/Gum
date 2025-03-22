using System;

namespace Gum.DataTypes
{
    public enum DimensionUnitType
    {
        /// <summary>
        /// Width and Height values are measured in absolute pixel values
        /// </summary>
        Absolute = 0,

        [Obsolete("Use PercentageOfParent")]
        Percentage = 1,

        /// <summary>
        /// Width and Height values are measured in percentage of parent, where 100 is 100% of the parent's width
        /// </summary>
        PercentageOfParent = 1,
        
        [Obsolete("Use RelativeToParent")]
        RelativeToContainer = 2,

        /// <summary>
        /// Width and Height values are measured in pixels relative to the parent, where a value of 0
        /// equals the size of the parent. Positive values are larger than the parent. Negative values are
        /// smaller than the parent.
        /// </summary>
        RelativeToParent = 2,

        /// <summary>
        /// Width and Height values are measured in percentage of the source file , where 100 is 100% of the source file's width
        /// or height. Width and Height values consider texture coordinates, so if custom coordinates are used, then
        /// the Width and Height values are multplied by the visible portion of the source file.
        /// </summary>
        PercentageOfSourceFile = 3,

        /// <summary>
        /// Width and Height values are measured in pixels relative to the necessary size to contain children. A larger
        /// value adds additional padding.
        /// </summary>
        RelativeToChildren = 4,

        /// <summary>
        /// The selected dimension is measured in percentage of the other dimension. For example, if WidthUnits 
        /// is set to PercentageOfOtherDimension and Width is set to 50, then the Width is 50% of the Height.
        /// Only one of the two dimensions should use this unit type.
        /// </summary>
        PercentageOfOtherDimension = 5,

        /// <summary>
        /// The selected dimension is a percentage of the necessary value for maintaining the aspect ratio of the file.
        /// For example, if WidthUnits is set to MaintainFileAspectRatio and Width is set to 100, then the
        /// effective width value is set to match the aspect ratio of the file. This considers texture coordinates.
        /// </summary>
        MaintainFileAspectRatio = 6,

        /// <summary>
        /// The Width or Height of the parent is distributed among all siblings using Ratio after
        /// subtracting the Width and Height values of siblings using Absolute values.
        /// </summary>
        Ratio = 7,

        /// <summary>
        /// Width and Height values are measured in absolute pixels multiplied by the device's font scale.
        /// </summary>
        /// <remarks>
        /// Not all platforms support this value. If this value is not supported, absolute pixel values are used
        /// </remarks>
        AbsoluteMultipliedByFontScale = 8,

        /// <summary>
        /// Width and Height values are measured in screen pixels. If the Camera is zoomed 100% then 
        /// values are the same as Absolute. Zooming the camera affects absolute size.
        /// </summary>
        ScreenPixel = 9
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
                unitType == DimensionUnitType.RelativeToParent ||
                unitType == DimensionUnitType.RelativeToChildren ||
                unitType == DimensionUnitType.AbsoluteMultipliedByFontScale ||
                unitType == DimensionUnitType.ScreenPixel

                ;
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
                case DimensionUnitType.ScreenPixel:
                    return HierarchyDependencyType.NoDependency;
                case DimensionUnitType.PercentageOfParent:
                case DimensionUnitType.RelativeToParent:
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
