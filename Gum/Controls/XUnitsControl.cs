using Gum.Managers;
using System.Windows.Media.Imaging;
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
                        Image = pixelsFromLeftBitmap,
                        GumIconName = "XUnitsLeft"

                    },
                    new Option
                    {
                        Name = "Pixels From Center",
                        Value = PositionUnitType.PixelsFromCenterX,
                        Image = pixelsFromCenterBitmap,
                        GumIconName = "XUnitsCenter"
                    },
                    new Option
                    {
                        Name = "Pixels From Right",
                        Value = PositionUnitType.PixelsFromRight,
                        Image = pixelsFromRightBitmap,
                        GumIconName = "XUnitsRight"
                    },
                    new Option
                    {
                        Name = "Percentage Parent Width",
                        Value = PositionUnitType.PercentageWidth,
                        Image = percentageBitmap,
                        GumIconName = "XUnitsPercentageParent"
                    }
                };

            }

            return cachedOptions;
        }
    }
}
