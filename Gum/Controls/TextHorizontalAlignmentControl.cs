using System;
using System.Windows.Media.Imaging;
using WpfDataUi;
using WpfDataUi.Controls;
using static WpfDataUi.Controls.ToggleButtonOptionDisplay;

namespace Gum.Controls
{
    public class TextHorizontalAlignmentControl : ToggleButtonOptionContainer
    {
        static Option[] cachedOptions;

        protected override Option[] GetOptions()
        {
            if(cachedOptions == null)
            {
                BitmapImage centerAlignBitmap = CreateBitmapFromFile("Content/Icons/Alignment/CenterAlign.png");
                BitmapImage leftAlignBitmap = CreateBitmapFromFile("Content/Icons/Alignment/LeftAlign.png");
                BitmapImage rightAlignBitmap = CreateBitmapFromFile("Content/Icons/Alignment/RightAlign.png");

                cachedOptions = new Option[]
                {
                    new Option
                    {
                        Name = "Left",
                        Value = global::RenderingLibrary.Graphics.HorizontalAlignment.Left,
                        Image = leftAlignBitmap

                    },
                    new Option
                    {
                        Name = "Center",
                        Value = global::RenderingLibrary.Graphics.HorizontalAlignment.Center,
                        Image = centerAlignBitmap
                    },
                    new Option
                    {
                        Name = "Right",
                        Value = global::RenderingLibrary.Graphics.HorizontalAlignment.Right,
                        Image = rightAlignBitmap
                    },
                };
            }

            return cachedOptions;
        }
        

    }
}
