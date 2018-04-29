using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using static WpfDataUi.Controls.ToggleButtonOptionDisplay;

namespace Gum.Controls
{
    class YOriginControl : ToggleButtonOptionContainer
    {
        static Option[] cachedOptions;

        protected override Option[] GetOptions()
        {
            if (cachedOptions == null)
            {
                BitmapImage topBitmap =
                    CreateBitmapFromFile("Content/Icons/Origins/TopOrigin.png");

                BitmapImage centerBitmap =
                    CreateBitmapFromFile("Content/Icons/Origins/CenterOrigin.png");

                BitmapImage bottomBitmap =
                    CreateBitmapFromFile("Content/Icons/Origins/BottomOrigin.png");


                cachedOptions = new Option[]
                {
                    new Option
                    {
                        Name = "Top",
                        Value = global::RenderingLibrary.Graphics.VerticalAlignment.Top,
                        Image = topBitmap

                    },
                    new Option
                    {
                        Name = "Center",
                        Value = global::RenderingLibrary.Graphics.VerticalAlignment.Center,
                        Image = centerBitmap
                    },
                    new Option
                    {
                        Name = "Bottom",
                        Value = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom,
                        Image = bottomBitmap
                    }
                };
            }

            return cachedOptions;
        }

    }
}
