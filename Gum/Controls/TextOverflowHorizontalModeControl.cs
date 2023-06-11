using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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
                        Image = truncateWordBitmap
                    },
                    new Option
                    {
                        Name = "Ellipsis Letter",
                        Value = TextOverflowHorizontalMode.EllipsisLetter,
                        Image = ellipsisBitmap
                    }
                };
            }

            return cachedOptions;
        }
    }
}
