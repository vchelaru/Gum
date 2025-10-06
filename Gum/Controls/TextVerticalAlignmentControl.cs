using System.Windows.Media.Imaging;
using static WpfDataUi.Controls.ToggleButtonOptionDisplay;

namespace Gum.Controls
{
    class TextVerticalAlignmentControl : ToggleButtonOptionContainer
    {
        static Option[] cachedOptions;

        protected override Option[] GetOptions()
        {
            if (cachedOptions == null)
            {
                BitmapImage topAlignBitmap = CreateBitmapFromFile("Content/Icons/Alignment/TopAlign.png");
                BitmapImage verticalCenterAlignBitmap = CreateBitmapFromFile("Content/Icons/Alignment/VerticalCenterAlign.png");
                BitmapImage bottomAlignBitmap = CreateBitmapFromFile("Content/Icons/Alignment/BottomAlign.png");

                cachedOptions = new Option[]
                {
                    new Option
                    {
                        Name = "Top",
                        Value = global::RenderingLibrary.Graphics.VerticalAlignment.Top,
                        Image = topAlignBitmap,
                        IconName = "TextboxAlignTop"

                    },
                    new Option
                    {
                        Name = "Center",
                        Value = global::RenderingLibrary.Graphics.VerticalAlignment.Center,
                        Image = verticalCenterAlignBitmap,
                        IconName = "TextboxAlignMiddle"
                    },
                    new Option
                    {
                        Name = "Bottom",
                        Value = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom,
                        Image = bottomAlignBitmap,
                        IconName = "TextboxAlignBottom"
                    },
                };
            }

            return cachedOptions;
        }
    }
}
