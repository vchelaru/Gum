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
            var absoluteBitmap =
                                CreateBitmapFromFile("Content/Icons/WidthUnits/AbsoluteWidth.png");

            var percentageOfHeightBitmap =
                CreateBitmapFromFile("Content/Icons/WidthUnits/PercentageOfOtherHeight.png");

            var percentOfParentBitmap =
                CreateBitmapFromFile("Content/Icons/WidthUnits/PercentOfParent.png");

            var ratioBitmap =
                CreateBitmapFromFile("Content/Icons/WidthUnits/Ratio.png");

            var relativeToChildrenBitmap =
                CreateBitmapFromFile("Content/Icons/WidthUnits/RelativeToChildren.png");

            var relativeToParentBitmap =
                CreateBitmapFromFile("Content/Icons/WidthUnits/RelativeToParent.png");

            var percentageOfFileWidth =
                CreateBitmapFromFile("Content/Icons/WidthUnits/PercentageOfFileWidth.png");

            var maintainFileAspectRatio =
                CreateBitmapFromFile("Content/Icons/WidthUnits/MaintainFileAspectRatioWidth.png");

            var absoluteMultipliedByFontScale =
                CreateBitmapFromFile("Content/Icons/WidthUnits/AbsoluteWidthMulitpliedByFontScale.png");

            cachedOptions = new Option[]
            {
                    new Option
                    {
                        Name = "Absolute",
                        Value = DimensionUnitType.Absolute,
                        Image = absoluteBitmap,
                        GumIconName = "WidthUnitsAbsolute"

                    },
                    new Option
                    {
                        Name = "Relative to Parent",
                        Value = DimensionUnitType.RelativeToParent,
                        Image = relativeToParentBitmap,
                        GumIconName = "WidthUnitsRelativeToParent"
                    },
                    new Option
                    {
                        Name = "Percentage of Parent",
                        Value = DimensionUnitType.PercentageOfParent,
                        Image = percentOfParentBitmap,
                        GumIconName = "WidthUnitsPercentageOfParent"
                    },
                    new Option
                    {
                        Name = "Ratio of Parent",
                        Value = DimensionUnitType.Ratio,
                        Image = ratioBitmap,
                        GumIconName = "WidthUnitsRatioOfParent"
                    },
                    new Option
                    {
                        Name = "Relative to Children",
                        Value = DimensionUnitType.RelativeToChildren,
                        Image = relativeToChildrenBitmap,
                        GumIconName = "WidthUnitsRelativeToChildren"
                    },
                    new Option
                    {
                        Name = "Percentage of Height",
                        Value = DimensionUnitType.PercentageOfOtherDimension,
                        Image = percentageOfHeightBitmap,
                        GumIconName = "WidthUnitsPercentageOfHeight"
                    },
                    new Option
                    {
                        Name = "Percentage of File Width",
                        Value = DimensionUnitType.PercentageOfSourceFile,
                        Image = percentageOfFileWidth,
                        GumIconName = "WidthUnitsPercentageOfFileWidth"
                    },
                    new Option
                    {
                        Name = "Maintain File Aspect Ratio Width",
                        Value = DimensionUnitType.MaintainFileAspectRatio,
                        Image = maintainFileAspectRatio,
                        GumIconName = "WidthUnitsMaintainFileAspectRatio"
                    },
                    new Option
                    {
                        Name = "Absolute Multiplied by Font Scale",
                        Value = DimensionUnitType.AbsoluteMultipliedByFontScale,
                        Image = absoluteMultipliedByFontScale,
                        GumIconName = "WidthUnitsAbsoluteMultipliedByFontScale"
                    }

            };
        }
    }
}
