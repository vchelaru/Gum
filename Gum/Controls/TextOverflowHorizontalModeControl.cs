using RenderingLibrary.Graphics;
using static WpfDataUi.Controls.ToggleButtonOptionDisplay;

namespace Gum.Controls
{
    class TextOverflowHorizontalModeControl : ToggleButtonOptionContainer
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
                        Name = "Truncate Word",
                        Value = TextOverflowHorizontalMode.TruncateWord,
                        GumIconName = "TextOverflowHorizontalTruncateWord"
                    },
                    new Option
                    {
                        Name = "Ellipsis Letter",
                        Value = TextOverflowHorizontalMode.EllipsisLetter,
                        GumIconName = "TextOverflowHorizontalEllipsisLetter"
                    }
                };
            }

            return cachedOptions;
        }
    }
}
