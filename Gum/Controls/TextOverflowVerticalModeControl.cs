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
                cachedOptions = new Option[]
                {
                    new Option
                    {
                        Name = "Spill",
                        Value = TextOverflowVerticalMode.SpillOver,
                        GumIconName = "TextOverflowVerticalSpill"
                    },
                    new Option
                    {
                        Name = "Truncate Line",
                        Value = TextOverflowVerticalMode.TruncateLine,
                        GumIconName = "TextOverflowVerticalTruncateLine"
                    }
                };
            }

            return cachedOptions;
        }
    }
}
