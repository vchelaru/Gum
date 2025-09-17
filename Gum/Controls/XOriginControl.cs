using System.Windows.Media.Imaging;
using static WpfDataUi.Controls.ToggleButtonOptionDisplay;

namespace Gum.Controls
{
    class XOriginControl : ToggleButtonOptionContainer
    {
        static Option[] cachedOptions;


        protected override Option[] GetOptions()
        {
            if (cachedOptions == null)
            {
                BitmapImage leftBitmap =
                    CreateBitmapFromFile("Content/Icons/Origins/LeftOrigin.png");

                BitmapImage centerBitmap =
                    CreateBitmapFromFile("Content/Icons/Origins/CenterOrigin.png");

                BitmapImage rightBitmap =
                    CreateBitmapFromFile("Content/Icons/Origins/RightOrigin.png");


                cachedOptions = new Option[]
                {
                    new Option
                    {
                        Name = "Left",
                        Value = global::RenderingLibrary.Graphics.HorizontalAlignment.Left,
                        Image = leftBitmap,
                        IconName = "AlignLeft"

                    },
                    new Option
                    {
                        Name = "Center",
                        Value = global::RenderingLibrary.Graphics.HorizontalAlignment.Center,
                        Image = centerBitmap,
                        IconName = "AlignCenterVertical"
                    },
                    new Option
                    {
                        Name = "Right",
                        Value = global::RenderingLibrary.Graphics.HorizontalAlignment.Right,
                        Image = rightBitmap,
                        IconName = "AlignRight"
                    }
                };
            }

            return cachedOptions;
        }
    }
}
