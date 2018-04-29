using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WpfDataUi.Controls;
using static WpfDataUi.Controls.ToggleButtonOptionDisplay;

namespace Gum.Controls
{
    class YUnitsControl : ToggleButtonOptionContainer
    {
        static Option[] cachedOptions;


        protected override Option[] GetOptions()
        {
            if (cachedOptions == null)
            {
                BitmapImage pixelsFromTopBitmap =
                    CreateBitmapFromFile("Content/Icons/Units/PixelsFromTop.png");

                BitmapImage percentageBitmap =
                    CreateBitmapFromFile("Content/Icons/Units/PercentageFromTop.png");

                BitmapImage pixelsFromCenterBitmap =
                    CreateBitmapFromFile("Content/Icons/Units/PixelsFromCenterY.png");

                BitmapImage pixelsFromBottomBitmap =
                    CreateBitmapFromFile("Content/Icons/Units/PixelsFromBottom.png");

                cachedOptions = new Option[]
                {
                    new Option
                    {
                        Name = "Pixels From Top",
                        Value = PositionUnitType.PixelsFromTop,
                        Image = pixelsFromTopBitmap

                    },
                    new Option
                    {
                        Name = "Pixels From Center",
                        Value = PositionUnitType.PixelsFromCenterY,
                        Image = pixelsFromCenterBitmap
                    },
                    new Option
                    {
                        Name = "Pixels From Bottom",
                        Value = PositionUnitType.PixelsFromBottom,
                        Image = pixelsFromBottomBitmap
                    },
                    new Option
                    {
                        Name = "Percentage Parent Height",
                        Value = PositionUnitType.PercentageHeight,
                        Image = percentageBitmap
                    }
                };
            }

            return cachedOptions;
        }
    }
}
