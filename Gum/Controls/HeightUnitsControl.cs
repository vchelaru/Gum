using Gum.DataTypes;
using Gum.Managers;
using System.Collections.Generic;
using System.Linq;
using static WpfDataUi.Controls.ToggleButtonOptionDisplay;

namespace Gum.Controls
{
    class HeightUnitsControl : ToggleButtonOptionContainer
    {
        static Option[] cachedOptions;

        public HeightUnitsControl() : base()
        {
            this.RefreshButtonsOnSelection = true;
        }

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
                var variable = state.Variables.FirstOrDefault(item => item.Name == "HeightUnits");

                if (variable?.ExcludedValuesForEnum?.Any() == true)
                {
                    foreach (var toExclude in variable.ExcludedValuesForEnum)
                    {
                        var matchingOption = toReturn.FirstOrDefault(item => (DimensionUnitType)item.Value == (DimensionUnitType)toExclude);

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
                        Name = "Absolute",
                        Value = DimensionUnitType.Absolute,
                        GumIconName = "HeightUnitsAbsolute"
                    },
                    new Option
                    {
                        Name = "Relative to Parent",
                        Value = DimensionUnitType.RelativeToParent,
                        GumIconName = "HeightUnitsRelativeToParent"
                    },
                    new Option
                    {
                        Name = "Percentage of Parent",
                        Value = DimensionUnitType.PercentageOfParent,
                        GumIconName = "HeightUnitsPercentageOfParent"
                    },
                    new Option
                    {
                        Name = "Ratio of Parent",
                        Value = DimensionUnitType.Ratio,
                        GumIconName = "HeightUnitsRatioOfParent"
                    },
                    new Option
                    {
                        Name = "Relative to Children",
                        Value = DimensionUnitType.RelativeToChildren,
                        GumIconName = "HeightUnitsRelativeToChildren"
                    },
                    new Option
                    {
                        Name = "Percentage of Width",
                        Value = DimensionUnitType.PercentageOfOtherDimension,
                        GumIconName = "HeightUnitsPercentageOfWidth"
                    },
                    new Option
                    {
                        Name = "Percentage of File Height",
                        Value = DimensionUnitType.PercentageOfSourceFile,
                        GumIconName = "HeightUnitsPercentageOfFileHeight"
                    },
                    new Option
                    {
                        Name = "Maintain File Aspect Ratio Height",
                        Value = DimensionUnitType.MaintainFileAspectRatio,
                        GumIconName = "HeightUnitsMaintainFileAspectRatio"
                    },
                    new Option
                    {
                        Name = "Absolute Multiplied by Font Scale",
                        Value = DimensionUnitType.AbsoluteMultipliedByFontScale,
                        GumIconName = "HeightUnitsAbsoluteMultipliedByFontScale"
                    }
            };
        }
    }
}
