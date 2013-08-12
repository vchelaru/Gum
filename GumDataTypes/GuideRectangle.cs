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
        RelativeToContainer
    }
    
    public class GuideRectangle : NamedRectangle
    {
        public DimensionUnitType WidthUnitType;
        public DimensionUnitType HeightUnitType;
        public DimensionUnitType XUnitType;
        public DimensionUnitType YUnitType;

    }
}
