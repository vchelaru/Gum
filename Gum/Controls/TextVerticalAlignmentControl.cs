using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                        Image = topAlignBitmap

                    },
                    new Option
                    {
                        Name = "Center",
                        Value = global::RenderingLibrary.Graphics.VerticalAlignment.Center,
                        Image = verticalCenterAlignBitmap
                    },
                    new Option
                    {
                        Name = "Bottom",
                        Value = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom,
                        Image = bottomAlignBitmap
                    },
                };
            }

            return cachedOptions;
        }
    }
}
