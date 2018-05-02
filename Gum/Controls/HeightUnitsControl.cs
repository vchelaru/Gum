using Gum.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using static WpfDataUi.Controls.ToggleButtonOptionDisplay;

namespace Gum.Controls
{
    class HeightUnitsControl : ToggleButtonOptionContainer
    {
        static Option[] cachedOptions;

        protected override Option[] GetOptions()
        {
            if (cachedOptions == null)
            {
                BitmapImage absoluteBitmap =
                    CreateBitmapFromFile("Content/Icons/HeightUnits/AbsoluteHeight.png");

                BitmapImage percentageOfHeightBitmap =
                    CreateBitmapFromFile("Content/Icons/HeightUnits/PercentageOfOtherWidth.png");

                BitmapImage percentOfParentBitmap =
                    CreateBitmapFromFile("Content/Icons/HeightUnits/PercentOfParent.png");

                BitmapImage relativeToChildrenBitmap =
                    CreateBitmapFromFile("Content/Icons/HeightUnits/RelativeToChildren.png");

                BitmapImage relativeToParentBitmap =
                    CreateBitmapFromFile("Content/Icons/HeightUnits/RelativeToParent.png");


                cachedOptions = new Option[]
                {
                    new Option
                    {
                        Name = "Absolute",
                        Value = DimensionUnitType.Absolute,
                        Image = absoluteBitmap

                    },
                    new Option
                    {
                        Name = "Relative to Container",
                        Value = DimensionUnitType.RelativeToContainer,
                        Image = relativeToParentBitmap
                    },
                    new Option
                    {
                        Name = "Percentage of Container",
                        Value = DimensionUnitType.Percentage,
                        Image = percentOfParentBitmap
                    },
                    new Option
                    {
                        Name = "Relative to Children",
                        Value = DimensionUnitType.RelativeToChildren,
                        Image = relativeToChildrenBitmap
                    },
                    new Option
                    {
                        Name = "Percentage of Height",
                        Value = DimensionUnitType.PercentageOfOtherDimension,
                        Image = percentageOfHeightBitmap
                    }
                };
            }

            return cachedOptions;
        }


    }
}
