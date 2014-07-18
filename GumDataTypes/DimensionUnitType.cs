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
        PercentageOfSourceFile
    }

    public static class DimensionUnitTypeExtensions
    {
        public static bool GetIsPixelBased(this DimensionUnitType unitType)
        {
            return unitType == DimensionUnitType.Absolute || unitType == DimensionUnitType.RelativeToContainer;
        }
    }
}
