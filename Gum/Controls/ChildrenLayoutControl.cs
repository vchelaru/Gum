using static WpfDataUi.Controls.ToggleButtonOptionDisplay;

namespace Gum.Controls;

class ChildrenLayoutControl : ToggleButtonOptionContainer
{
    static Option[] cachedOptions;

    protected override Option[] GetOptions()
    {
        if (cachedOptions == null)
        {
            cachedOptions = new Option[]
            {
                new Option
                {
                    Name = "Regular",
                    Value = Gum.Managers.ChildrenLayout.Regular,
                    GumIconName = "ChildrenLayoutRegular"
                },
                new Option
                {
                    Name = "Top to Bottom Stack",
                    Value = Gum.Managers.ChildrenLayout.TopToBottomStack,
                    GumIconName = "ChildrenLayoutTopToBottomStack"
                },
                new Option
                {
                    Name = "Left to Right Stack",
                    Value = Gum.Managers.ChildrenLayout.LeftToRightStack,
                    GumIconName = "ChildrenLayoutLeftToRightStack"
                },
                new Option
                {
                    Name = "Auto Grid Horizontal",
                    Value = Gum.Managers.ChildrenLayout.AutoGridHorizontal,
                    GumIconName = "ChildrenLayoutAutoGridHorizontal"
                },
                new Option
                {
                    Name = "Auto Grid Vertical",
                    Value = Gum.Managers.ChildrenLayout.AutoGridVertical,
                    GumIconName = "ChildrenLayoutAutoGridVertical"
                }
            };
        }

        return cachedOptions;
    }
}
