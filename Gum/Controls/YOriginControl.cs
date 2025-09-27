using Gum.DataTypes;
using Gum.Managers;
using RenderingLibrary.Graphics;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using static WpfDataUi.Controls.ToggleButtonOptionDisplay;

namespace Gum.Controls
{
    class YOriginControl : ToggleButtonOptionContainer
    {
        static Option[] cachedOptions;

        protected override Option[] GetOptions()
        {
            if (cachedOptions == null)
            {
                CreateCachedOptions();
            }

            List<Option> toReturn = cachedOptions.ToList();

            StandardElementSave rootElement = GetRootElement();

            var state = StandardElementsManager.Self.GetDefaultStateFor(rootElement?.Name);

            if (state != null)
            {
                var variable = state.Variables.FirstOrDefault(item => item.Name == "YOrigin");

                if(variable != null)
                {
                    foreach (var toExclude in variable.ExcludedValuesForEnum)
                    {
                        var matchingOption = toReturn.FirstOrDefault(item => (VerticalAlignment)item.Value == (VerticalAlignment)toExclude);

                        if (matchingOption != null)
                        {
                            toReturn.Remove(matchingOption);
                        }
                    }
                }
            }

            return toReturn.ToArray();
        }

        private static void CreateCachedOptions()
        {
            BitmapImage topBitmap =
                CreateBitmapFromFile("Content/Icons/Origins/TopOrigin.png");

            BitmapImage centerBitmap =
                CreateBitmapFromFile("Content/Icons/Origins/CenterOrigin.png");

            BitmapImage bottomBitmap =
                CreateBitmapFromFile("Content/Icons/Origins/BottomOrigin.png");

            BitmapImage baselineBitmap =
                CreateBitmapFromFile("Content/Icons/Origins/Baseline.png");

            cachedOptions = new Option[]
            {
                    new Option
                    {
                        Name = "Top",
                        Value = global::RenderingLibrary.Graphics.VerticalAlignment.Top,
                        Image = topBitmap,
                        IconName = "AlignTop"

                    },
                    new Option
                    {
                        Name = "Center",
                        Value = global::RenderingLibrary.Graphics.VerticalAlignment.Center,
                        Image = centerBitmap,
                        IconName = "AlignCenterHorizontal"
                    },
                    new Option
                    {
                        Name = "Bottom",
                        Value = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom,
                        Image = bottomBitmap,
                        IconName = "AlignBottom"
                    },
                    new Option
                    {
                        Name = "Baseline",
                        Value = global::RenderingLibrary.Graphics.VerticalAlignment.TextBaseline,
                        Image = baselineBitmap,
                        IconName = "TextboxAlignBottom"
                    }

            };
        }
    }
}
