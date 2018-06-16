using Gum.DataTypes;
using Gum.Managers;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
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

            if (rootElement != null && StandardElementsManager.Self.DefaultStates.ContainsKey(rootElement.Name))
            {
                var state = StandardElementsManager.Self.DefaultStates[rootElement.Name];

                var variable = state.Variables.FirstOrDefault(item => item.Name == "Width Units");

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
            StandardElementSave rootElement = null;

            if (SelectedState.Self.SelectedInstance != null)
            {
                rootElement =
                    ObjectFinder.Self.GetRootStandardElementSave(SelectedState.Self.SelectedInstance);
            }
            else if (SelectedState.Self.SelectedElement != null)
            {
                rootElement =
                    ObjectFinder.Self.GetRootStandardElementSave(SelectedState.Self.SelectedElement);
            }

            return rootElement;
        }

        private static void CreateCachedOptions()
        {
            BitmapImage absoluteBitmap =
                                CreateBitmapFromFile("Content/Icons/WidthUnits/AbsoluteWidth.png");

            BitmapImage percentageOfHeightBitmap =
                CreateBitmapFromFile("Content/Icons/WidthUnits/PercentageOfOtherHeight.png");

            BitmapImage percentOfParentBitmap =
                CreateBitmapFromFile("Content/Icons/WidthUnits/PercentOfParent.png");

            BitmapImage relativeToChildrenBitmap =
                CreateBitmapFromFile("Content/Icons/WidthUnits/RelativeToChildren.png");

            BitmapImage relativeToParentBitmap =
                CreateBitmapFromFile("Content/Icons/WidthUnits/RelativeToParent.png");

            BitmapImage percentageOfFileWidth =
                CreateBitmapFromFile("Content/Icons/WidthUnits/PercentageOfFileWidth.png");


            cachedOptions = new Option[]
            {
                    new Option
                    {
                        Name = "Absolute",
                        Value = DimensionUnitType.Absolute,
                        Image = absoluteBitmap

                    },
                    new Option
                    {
                        Name = "Relative to Container",
                        Value = DimensionUnitType.RelativeToContainer,
                        Image = relativeToParentBitmap
                    },
                    new Option
                    {
                        Name = "Percentage of Container",
                        Value = DimensionUnitType.Percentage,
                        Image = percentOfParentBitmap
                    },
                    new Option
                    {
                        Name = "Relative to Children",
                        Value = DimensionUnitType.RelativeToChildren,
                        Image = relativeToChildrenBitmap
                    },
                    new Option
                    {
                        Name = "Percentage of Height",
                        Value = DimensionUnitType.PercentageOfOtherDimension,
                        Image = percentageOfHeightBitmap
                    },
                    new Option
                    {
                        Name = "Percentage of File Width",
                        Value = DimensionUnitType.PercentageOfSourceFile,
                        Image = percentageOfFileWidth
                    }

            };
        }
    }
}
