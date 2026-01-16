using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Wireframe
{
    public partial class GraphicalUiElement
    {
        #region Get Child Layout Type

        ChildType GetChildLayoutType(GraphicalUiElement parent)
        {
            var doesParentWrapStack = parent.WrapsChildren && (parent.ChildrenLayout == ChildrenLayout.LeftToRightStack || parent.ChildrenLayout == ChildrenLayout.TopToBottomStack);

            var parentWidthDependencyType = parent.WidthUnits.GetDependencyType();
            var parentHeightDependencyType = parent.HeightUnits.GetDependencyType();

            var isParentWidthNoDependencyOrOnParent = parentWidthDependencyType == HierarchyDependencyType.NoDependency || parentWidthDependencyType == HierarchyDependencyType.DependsOnParent;
            var isParentHeightNoDependencyOrOnParent = parentHeightDependencyType == HierarchyDependencyType.NoDependency || parentHeightDependencyType == HierarchyDependencyType.DependsOnParent;

            var isAbsolute = (mWidthUnit.GetDependencyType() != HierarchyDependencyType.DependsOnParent || isParentWidthNoDependencyOrOnParent) &&
                            (mHeightUnit.GetDependencyType() != HierarchyDependencyType.DependsOnParent || isParentHeightNoDependencyOrOnParent) &&
                            (mWidthUnit.GetDependencyType() != HierarchyDependencyType.DependsOnSiblings) &&
                            (mHeightUnit.GetDependencyType() != HierarchyDependencyType.DependsOnSiblings) &&

                (mXUnits == GeneralUnitType.PixelsFromSmall ||
                 (mXUnits == GeneralUnitType.PixelsFromMiddle && isParentWidthNoDependencyOrOnParent) ||
                 (mXUnits == GeneralUnitType.PixelsFromLarge && isParentWidthNoDependencyOrOnParent) ||
                 (mXUnits == GeneralUnitType.PixelsFromMiddleInverted && isParentWidthNoDependencyOrOnParent)) &&

                (mYUnits == GeneralUnitType.PixelsFromSmall ||
                 (mYUnits == GeneralUnitType.PixelsFromMiddle && isParentHeightNoDependencyOrOnParent) ||
                 (mYUnits == GeneralUnitType.PixelsFromLarge && isParentHeightNoDependencyOrOnParent) ||
                 (mYUnits == GeneralUnitType.PixelsFromMiddleInverted && isParentHeightNoDependencyOrOnParent) ||
                 mYUnits == GeneralUnitType.PixelsFromBaseline);

            if (doesParentWrapStack)
            {
                return isAbsolute ? ChildType.StackedWrapped : ChildType.Relative;
            }
            else
            {
                return isAbsolute ? ChildType.Absolute : ChildType.Relative;
            }
        }

        ChildType GetChildLayoutType(XOrY xOrY, GraphicalUiElement parent)
        {
            bool isAbsolute;
            var doesParentWrapStack = parent.WrapsChildren && (parent.ChildrenLayout == ChildrenLayout.LeftToRightStack || parent.ChildrenLayout == ChildrenLayout.TopToBottomStack);

            if (xOrY == XOrY.X)
            {
                var widthUnitDependencyType = mWidthUnit.GetDependencyType();
                isAbsolute = (widthUnitDependencyType != HierarchyDependencyType.DependsOnParent || this.WidthUnits.GetDependencyType() == HierarchyDependencyType.NoDependency) &&
                    (mXUnits == GeneralUnitType.PixelsFromLarge || mXUnits == GeneralUnitType.PixelsFromMiddle ||
                        mXUnits == GeneralUnitType.PixelsFromSmall || mXUnits == GeneralUnitType.PixelsFromMiddleInverted);

            }
            else // Y
            {
                isAbsolute = (mHeightUnit.GetDependencyType() != HierarchyDependencyType.DependsOnParent || this.HeightUnits.GetDependencyType() == HierarchyDependencyType.NoDependency) &&
                    (mYUnits == GeneralUnitType.PixelsFromLarge || mYUnits == GeneralUnitType.PixelsFromMiddle ||
                        mYUnits == GeneralUnitType.PixelsFromSmall || mYUnits == GeneralUnitType.PixelsFromMiddleInverted &&
                        mYUnits == GeneralUnitType.PixelsFromBaseline);

            }

            if (doesParentWrapStack)
            {
                return isAbsolute ? ChildType.StackedWrapped : ChildType.Relative;
            }
            else
            {
                return isAbsolute ? ChildType.Absolute : ChildType.Relative;
            }
        }

        #endregion
    }
}
