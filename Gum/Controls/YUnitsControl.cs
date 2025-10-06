using Gum.Managers;
using System.Windows.Media.Imaging;
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

                BitmapImage pixelsFromCenterYInvertedBitmap =
                    CreateBitmapFromFile("Content/Icons/Units/PixelsFromCenterYInverted.png");

                BitmapImage pixelsFromBottomBitmap =
                    CreateBitmapFromFile("Content/Icons/Units/PixelsFromBottom.png");

                BitmapImage pixelsFromBaseline =
                    CreateBitmapFromFile("Content/Icons/Units/PixelsFromBaseline.png");


                cachedOptions = new Option[]
                {
                    new Option
                    {
                        Name = "Pixels From Top",
                        Value = PositionUnitType.PixelsFromTop,
                        Image = pixelsFromTopBitmap,
                        GumIconName = "YUnitsTop"

                    },
                    new Option
                    {
                        Name = "Pixels From Center",
                        Value = PositionUnitType.PixelsFromCenterY,
                        Image = pixelsFromCenterBitmap,
                        GumIconName = "YUnitsCenter"
                    },
                    // November 7, 2024
                    // even though this exists in the underlying layout engine, we never use it in Gum so let's remove it.
                    //new Option
                    //{
                    //    Name = "Pixels From Center Y Inverted",
                    //    Value = PositionUnitType.PixelsFromCenterYInverted,
                    //    Image = pixelsFromCenterYInvertedBitmap
                    //},
                    new Option
                    {
                        Name = "Pixels From Bottom",
                        Value = PositionUnitType.PixelsFromBottom,
                        Image = pixelsFromBottomBitmap,
                        GumIconName = "YUnitsBottom"
                    },
                    new Option
                    {
                        Name = "Percentage Parent Height",
                        Value = PositionUnitType.PercentageHeight,
                        Image = percentageBitmap,
                        GumIconName = "YUnitsPercentageParent"
                    },
                    new Option
                    {
                        Name = "Pixels From Baseline",
                        Value = PositionUnitType.PixelsFromBaseline,
                        Image = pixelsFromBaseline,
                        GumIconName = "YUnitsBaseline"
                    }
                };
            }

            return cachedOptions;
        }
    }
}
