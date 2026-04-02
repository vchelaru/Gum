using Gum.Managers;
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
                cachedOptions = new Option[]
                {
                    new Option
                    {
                        Name = "Pixels From Top",
                        Value = PositionUnitType.PixelsFromTop,
                        GumIconName = "YUnitsTop"
                    },
                    new Option
                    {
                        Name = "Pixels From Center",
                        Value = PositionUnitType.PixelsFromCenterY,
                        GumIconName = "YUnitsCenter"
                    },
                    new Option
                    {
                        Name = "Pixels From Bottom",
                        Value = PositionUnitType.PixelsFromBottom,
                        GumIconName = "YUnitsBottom"
                    },
                    new Option
                    {
                        Name = "Percentage Parent Height",
                        Value = PositionUnitType.PercentageHeight,
                        GumIconName = "YUnitsPercentageParent"
                    },
                    new Option
                    {
                        Name = "Pixels From Baseline",
                        Value = PositionUnitType.PixelsFromBaseline,
                        GumIconName = "YUnitsBaseline"
                    }
                };
            }

            return cachedOptions;
        }
    }
}
