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
                        Name = "Pixels From Center Y Inverted",
                        Value = PositionUnitType.PixelsFromCenterYInverted,
                        Image = pixelsFromCenterYInvertedBitmap
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
                    },
                    new Option
                    {
                        Name = "Pixels From Baseline",
                        Value = PositionUnitType.PixelsFromBaseline,
                        Image = pixelsFromBaseline
                    }
                };
            }

            return cachedOptions;
        }
    }
}
