using static WpfDataUi.Controls.ToggleButtonOptionDisplay;

namespace Gum.Controls;

class ChildrenLayoutControl : ToggleButtonOptionContainer
{
    static Option[] cachedOptions;

    protected override Option[] GetOptions()
    {
        if (cachedOptions == null)
        {
            var regularBitmap = CreateBitmapFromFile("Content/Icons/ChildrenLayout/Regular.png");
            var topToBottomBitmap = CreateBitmapFromFile("Content/Icons/ChildrenLayout/TopToBottom.png");
            var leftToRightBitmap = CreateBitmapFromFile("Content/Icons/ChildrenLayout/LeftToRight.png");

            var autoGridHorizontal = CreateBitmapFromFile("Content/Icons/ChildrenLayout/AutoGridHorizontal.png");
            var autoGridVertical = CreateBitmapFromFile("Content/Icons/ChildrenLayout/AutoGridVertical.png");


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
                new Option
                {
                    Name = "Auto Grid Horizontal",
                    Value = Gum.Managers.ChildrenLayout.AutoGridHorizontal,
                    Image = autoGridHorizontal
                },
                new Option
                {
                    Name = "Auto Grid Vertical",
                    Value = Gum.Managers.ChildrenLayout.AutoGridVertical,
                    Image = autoGridVertical
                }
            };
        }

        return cachedOptions;
    }


}
