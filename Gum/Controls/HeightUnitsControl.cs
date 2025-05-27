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
            var absoluteBitmap =
               CreateBitmapFromFile("Content/Icons/HeightUnits/AbsoluteHeight.png");

            var percentageOfHeightBitmap =
                CreateBitmapFromFile("Content/Icons/HeightUnits/PercentageOfOtherWidth.png");

            var percentOfParentBitmap =
                CreateBitmapFromFile("Content/Icons/HeightUnits/PercentOfParent.png");

            var ratioBitmap =
                CreateBitmapFromFile("Content/Icons/HeightUnits/Ratio.png");

            var relativeToChildrenBitmap =
                CreateBitmapFromFile("Content/Icons/HeightUnits/RelativeToChildren.png");

            var relativeToParentBitmap =
                CreateBitmapFromFile("Content/Icons/HeightUnits/RelativeToParent.png");

            var percentageOfFileHeightBitmap =
                CreateBitmapFromFile("Content/Icons/HeightUnits/PercentageOfFileHeight.png");

            var maintainFileAspectRatio =
                CreateBitmapFromFile("Content/Icons/HeightUnits/MaintainFileAspectRatioHeight.png");

            var absoluteMultipliedByFontScale =
                CreateBitmapFromFile("Content/Icons/HeightUnits/AbsoluteHeightMulitpliedByFontScale.png");

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
                        Name = "Relative to Parent",
                        Value = DimensionUnitType.RelativeToParent,
                        Image = relativeToParentBitmap
                    },
                    new Option
                    {
                        Name = "Percentage of Parent",
                        Value = DimensionUnitType.PercentageOfParent,
                        Image = percentOfParentBitmap
                    },
                    new Option
                    {
                        Name = "Ratio of Parent",
                        Value = DimensionUnitType.Ratio,
                        Image = ratioBitmap
                    },
                    new Option
                    {
                        Name = "Relative to Children",
                        Value = DimensionUnitType.RelativeToChildren,
                        Image = relativeToChildrenBitmap
                    },
                    new Option
                    {
                        Name = "Percentage of Width",
                        Value = DimensionUnitType.PercentageOfOtherDimension,
                        Image = percentageOfHeightBitmap
                    },
                    new Option
                    {
                        Name = "Percentage of File Height",
                        Value = DimensionUnitType.PercentageOfSourceFile,
                        Image = percentageOfFileHeightBitmap
                    },
                    new Option
                    {
                        Name = "Maintain File Aspect Ratio Height",
                        Value = DimensionUnitType.MaintainFileAspectRatio,
                        Image = maintainFileAspectRatio
                    },
                    new Option
                    {
                        Name = "Absolute Multiplied by Font Scale",
                        Value = DimensionUnitType.AbsoluteMultipliedByFontScale,
                        Image = absoluteMultipliedByFontScale
                    }
            };
        }
    }
}
