using Gum.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Wireframe
{
    public partial class GraphicalUiElement
    {
        #region Enums/Internal Classes

        enum ChildType
        {
            Absolute = 1,
            Relative = 1 << 1,
            BothAbsoluteAndRelative = Absolute | Relative,
            StackedWrapped = 1 << 2,
            All = Absolute | Relative | StackedWrapped
        }

        class DirtyState
        {
            public ParentUpdateType ParentUpdateType;
            public int ChildrenUpdateDepth;
            public XOrY? XOrY;
        }

        public enum ParentUpdateType
        {
            None = 0,
            IfParentStacks = 1,
            IfParentWidthHeightDependOnChildren = 2,
            IfParentIsAutoGrid = 4,
            IfParentHasRatioSizedChildren = 8,
            All = 16

        }

        #endregion
    }
}