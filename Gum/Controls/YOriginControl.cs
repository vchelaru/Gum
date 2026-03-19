using Gum.DataTypes;
using Gum.Managers;
using RenderingLibrary.Graphics;
using System.Collections.Generic;
using System.Linq;
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
            cachedOptions = new Option[]
            {
                    new Option
                    {
                        Name = "Top",
                        Value = global::RenderingLibrary.Graphics.VerticalAlignment.Top,
                        GumIconName = "YOriginStart"
                    },
                    new Option
                    {
                        Name = "Center",
                        Value = global::RenderingLibrary.Graphics.VerticalAlignment.Center,
                        GumIconName = "YOriginCenter"
                    },
                    new Option
                    {
                        Name = "Bottom",
                        Value = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom,
                        GumIconName = "YOriginEnd"
                    },
                    new Option
                    {
                        Name = "Baseline",
                        Value = global::RenderingLibrary.Graphics.VerticalAlignment.TextBaseline,
                        GumIconName = "YOriginBaseline"
                    }
            };
        }
    }
}
