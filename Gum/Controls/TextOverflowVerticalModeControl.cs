using RenderingLibrary.Graphics;
using static WpfDataUi.Controls.ToggleButtonOptionDisplay;

namespace Gum.Controls
{
    class TextOverflowVerticalModeControl : ToggleButtonOptionContainer
    {
        static Option[] cachedOptions;

        protected override Option[] GetOptions()
        {
            if (cachedOptions == null)
            {
                var spillVertical = CreateBitmapFromFile("Content/Icons/TextOverflow/SpillVertical.png");
                var truncateLine = CreateBitmapFromFile("Content/Icons/TextOverflow/TruncateLine.png");

                cachedOptions = new Option[]
                {
                    new Option
                    {
                        Name = "Spill",
                        Value = TextOverflowVerticalMode.SpillOver,
                        Image = spillVertical,
                        GumIconName = "TextOverflowVerticalSpill"
                    },
                    new Option
                    {
                        Name = "Truncate Line",
                        Value = TextOverflowVerticalMode.TruncateLine,
                        Image = truncateLine,
                        GumIconName = "TextOverflowVerticalTruncateLine"
                    }
                };
            }

            return cachedOptions;
        }
    }
}
