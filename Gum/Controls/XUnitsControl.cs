using Gum.Managers;
using static WpfDataUi.Controls.ToggleButtonOptionDisplay;

namespace Gum.Controls
{
    class XUnitsControl : ToggleButtonOptionContainer
    {
        static Option[] cachedOptions;

        protected override Option[] GetOptions()
        {
            if(cachedOptions == null)
            {
                cachedOptions = new Option[]
                {
                    new Option
                    {
                        Name = "Pixels From Left",
                        Value = PositionUnitType.PixelsFromLeft,
                        GumIconName = "XUnitsLeft"
                    },
                    new Option
                    {
                        Name = "Pixels From Center",
                        Value = PositionUnitType.PixelsFromCenterX,
                        GumIconName = "XUnitsCenter"
                    },
                    new Option
                    {
                        Name = "Pixels From Right",
                        Value = PositionUnitType.PixelsFromRight,
                        GumIconName = "XUnitsRight"
                    },
                    new Option
                    {
                        Name = "Percentage Parent Width",
                        Value = PositionUnitType.PercentageWidth,
                        GumIconName = "XUnitsPercentageParent"
                    }
                };
            }

            return cachedOptions;
        }
    }
}
