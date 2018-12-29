using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using static WpfDataUi.Controls.ToggleButtonOptionDisplay;

namespace Gum.Controls
{
    class ChildrenLayoutControl : ToggleButtonOptionContainer
    {
        static Option[] cachedOptions;

        protected override Option[] GetOptions()
        {
            if (cachedOptions == null)
            {
                BitmapImage regularBitmap = CreateBitmapFromFile("Content/Icons/ChildrenLayout/Regular.png");
                BitmapImage topToBottomBitmap = CreateBitmapFromFile("Content/Icons/ChildrenLayout/TopToBottom.png");
                BitmapImage leftToRightBitmap = CreateBitmapFromFile("Content/Icons/ChildrenLayout/LeftToRight.png");

                cachedOptions = new Option[]
                {
                    new Option
                    {
                        Name = "Regular",
                        Value = Gum.Managers.ChildrenLayout.Regular,
                        Image = regularBitmap

                    },
                    new Option
                    {
                        Name = "Top to Bottom Stack",
                        Value = Gum.Managers.ChildrenLayout.TopToBottomStack,
                        Image = topToBottomBitmap
                    },
                    new Option
                    {
                        Name = "Left to Right Stack",
                        Value = Gum.Managers.ChildrenLayout.LeftToRightStack,
                        Image = leftToRightBitmap
                    },
                };
            }

            return cachedOptions;
        }


    }
}
