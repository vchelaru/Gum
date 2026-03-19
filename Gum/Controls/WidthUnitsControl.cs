using Gum.DataTypes;
using Gum.Managers;
using Gum.Services;
using Gum.ToolStates;
using System.Collections.Generic;
using System.Linq;
using static WpfDataUi.Controls.ToggleButtonOptionDisplay;

namespace Gum.Controls
{
    class WidthUnitsControl : ToggleButtonOptionContainer
    {
        static Option[] cachedOptions;

        public WidthUnitsControl()
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
                var variable = state.Variables.FirstOrDefault(item => item.Name == "WidthUnits");

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

        private static StandardElementSave GetRootElement()
        {
            ISelectedState selectedState = Locator.GetRequiredService<ISelectedState>();
            
            StandardElementSave rootElement = null;

            if (selectedState.SelectedInstance != null)
            {
                rootElement =
                    ObjectFinder.Self.GetRootStandardElementSave(selectedState.SelectedInstance);
            }
            else if (selectedState.SelectedElement != null)
            {
                rootElement =
                    ObjectFinder.Self.GetRootStandardElementSave(selectedState.SelectedElement);
            }

            return rootElement;
        }

        private static void CreateCachedOptions()
        {
            cachedOptions = new Option[]
            {
                    new Option
                    {
                        Name = "Absolute",
                        Value = DimensionUnitType.Absolute,
                        GumIconName = "WidthUnitsAbsolute"
                    },
                    new Option
                    {
                        Name = "Relative to Parent",
                        Value = DimensionUnitType.RelativeToParent,
                        GumIconName = "WidthUnitsRelativeToParent"
                    },
                    new Option
                    {
                        Name = "Percentage of Parent",
                        Value = DimensionUnitType.PercentageOfParent,
                        GumIconName = "WidthUnitsPercentageOfParent"
                    },
                    new Option
                    {
                        Name = "Ratio of Parent",
                        Value = DimensionUnitType.Ratio,
                        GumIconName = "WidthUnitsRatioOfParent"
                    },
                    new Option
                    {
                        Name = "Relative to Children",
                        Value = DimensionUnitType.RelativeToChildren,
                        GumIconName = "WidthUnitsRelativeToChildren"
                    },
                    new Option
                    {
                        Name = "Percentage of Height",
                        Value = DimensionUnitType.PercentageOfOtherDimension,
                        GumIconName = "WidthUnitsPercentageOfHeight"
                    },
                    new Option
                    {
                        Name = "Percentage of File Width",
                        Value = DimensionUnitType.PercentageOfSourceFile,
                        GumIconName = "WidthUnitsPercentageOfFileWidth"
                    },
                    new Option
                    {
                        Name = "Maintain File Aspect Ratio Width",
                        Value = DimensionUnitType.MaintainFileAspectRatio,
                        GumIconName = "WidthUnitsMaintainFileAspectRatio"
                    },
                    new Option
                    {
                        Name = "Absolute Multiplied by Font Scale",
                        Value = DimensionUnitType.AbsoluteMultipliedByFontScale,
                        GumIconName = "WidthUnitsAbsoluteMultipliedByFontScale"
                    },
                    new Option
                    {
                        Name = "Relative to Max of Children or Parent",
                        Value  = DimensionUnitType.RelativeToMaxParentOrChildren,
                        GumIconName = "WidthUnitsAbsoluteMultipliedByFontScale"
                    }
            };
        }
    }
}
