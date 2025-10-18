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
                var ellipsisBitmap = CreateBitmapFromFile("Content/Icons/TextOverflow/Ellipsis.png");
                var truncateWordBitmap = CreateBitmapFromFile("Content/Icons/TextOverflow/TruncateWord.png");

                cachedOptions = new Option[]
                {
                    new Option
                    {
                        Name = "Truncate Word",
                        Value = TextOverflowHorizontalMode.TruncateWord,
                        Image = truncateWordBitmap,
                        GumIconName = "TextOverflowHorizontalTruncateWord"
                    },
                    new Option
                    {
                        Name = "Ellipsis Letter",
                        Value = TextOverflowHorizontalMode.EllipsisLetter,
                        Image = ellipsisBitmap,
                        GumIconName = "TextOverflowHorizontalEllipsisLetter"
                    }
                };
            }

            return cachedOptions;
        }
    }
}
