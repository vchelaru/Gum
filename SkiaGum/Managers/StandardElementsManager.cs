using System;
using System.Collections.Generic;
using System.Text;

namespace Gum.Managers
{
    public enum TextureAddress
    {
        EntireTexture,
        Custom,
        DimensionsBased
    }

    public enum ChildrenLayout
    {
        Regular,
        TopToBottomStack,
        LeftToRightStack

    }

    public class StandardElementsManager
    {
        #region Enums

        public enum DimensionVariableAction
        {
            ExcludeFileOptions,
            AllowFileOptions,
            DefaultToPercentageOfFile
        }

        #endregion


    }
}
