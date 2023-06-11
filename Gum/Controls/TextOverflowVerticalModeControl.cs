using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                        Image = spillVertical
                    },
                    new Option
                    {
                        Name = "Truncate Line",
                        Value = TextOverflowVerticalMode.TruncateLine,
                        Image = truncateLine
                    }
                };
            }

            return cachedOptions;
        }
    }
}
