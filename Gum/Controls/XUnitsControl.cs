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
    class XUnitsControl : ToggleButtonOptionContainer
    {
        static Option[] cachedOptions;

        protected override Option[] GetOptions()
        {
            if(cachedOptions == null)
            {
                BitmapImage pixelsFromLeftBitmap = 
                    CreateBitmapFromFile("Content/Icons/Units/PixelsFromLeft.png");

                BitmapImage percentageBitmap = 
                    CreateBitmapFromFile("Content/Icons/Units/PercentageFromLeft.png");
                BitmapImage pixelsFromCenterBitmap = 
                    CreateBitmapFromFile("Content/Icons/Units/PixelsFromCenterX.png");
                BitmapImage pixelsFromRightBitmap =
                    CreateBitmapFromFile("Content/Icons/Units/PixelsFromRight.png");

                cachedOptions = new Option[]
                {
                    new Option
                    {
                        Name = "Pixels From Left",
                        Value = PositionUnitType.PixelsFromLeft,
                        Image = pixelsFromLeftBitmap

                    },
                    new Option
                    {
                        Name = "Pixels From Center",
                        Value = PositionUnitType.PixelsFromCenterX,
                        Image = pixelsFromCenterBitmap
                    },
                    new Option
                    {
                        Name = "Pixels From Right",
                        Value = PositionUnitType.PixelsFromRight,
                        Image = pixelsFromRightBitmap
                    },
                    new Option
                    {
                        Name = "Percentage Parent Width",
                        Value = PositionUnitType.PercentageWidth,
                        Image = percentageBitmap
                    }
                };

            }

            return cachedOptions;
        }
    }
}
