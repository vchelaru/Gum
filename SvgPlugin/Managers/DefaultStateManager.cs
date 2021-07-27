using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using SkiaPlugin.Renderables;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkiaPlugin.Managers
{
    public static class DefaultStateManager
    {
        #region Fields/Properties

        static StateSave svgState;
        static StateSave filledCircleState;
        static StateSave roundedRectangleState;
        static StateSave arcState;

        #endregion

        public static StateSave GetSvgState()
        {
            if(svgState == null)
            {
                svgState = new StateSave();
                svgState.Name = "Default";
                AddVisibleVariable(svgState);
                StandardElementsManager.AddPositioningVariables(svgState);
                StandardElementsManager.AddDimensionsVariables(svgState, 100, 100,
                    Gum.Managers.StandardElementsManager.DimensionVariableAction.AllowFileOptions);

                foreach (var variableSave in svgState.Variables.Where(item => item.Type == typeof(DimensionUnitType).Name))
                {
                    variableSave.Value = DimensionUnitType.Absolute;
                    variableSave.ExcludedValuesForEnum.Add(DimensionUnitType.PercentageOfSourceFile);
                    //variableSave.ExcludedValuesForEnum.Add(DimensionUnitType.MaintainFileAspectRatio);

                }

                StandardElementsManager.AddColorVariables(svgState);
                svgState.Variables.Add(new VariableSave { SetsValue = true, Type = "string", Value = "", Name = "SourceFile", IsFile = true });
                svgState.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = false, Name = "UseColorTextureAlpha" });

                svgState.Variables.Add(new VariableSave { Type = "float", Value = 0.0f, Category = "Flip and Rotation", Name = "Rotation" });

            }
            return svgState;
        }

        internal static void HandleVariableSet(ElementSave owner, InstanceSave instance, string variableName, object oldValue)
        {
            var rootName = VariableSave.GetRootName(variableName);

            var shouldRefresh = rootName == "UseGradient";

            if(shouldRefresh)
            {
                GumCommands.Self.GuiCommands.RefreshPropertyGrid(force: true);
            }
        }

        public static StateSave GetColoredCircleState()
        {
            if(filledCircleState == null)
            {
                filledCircleState = new StateSave();
                filledCircleState.Name = "Default";
                AddVisibleVariable(filledCircleState);

                StandardElementsManager.AddPositioningVariables(filledCircleState);
                StandardElementsManager.AddDimensionsVariables(filledCircleState, 64, 64, 
                    StandardElementsManager.DimensionVariableAction.ExcludeFileOptions);
                StandardElementsManager.AddColorVariables(filledCircleState);
            }

            return filledCircleState;
        }

        public static StateSave GetRoundedRectangleState()
        {
            if (roundedRectangleState == null)
            {
                roundedRectangleState = new StateSave();
                roundedRectangleState.Name = "Default";
                AddVisibleVariable(roundedRectangleState);

                roundedRectangleState.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 5, Name = "CornerRadius", Category="Dimensions" });
                roundedRectangleState.Variables.Add(new VariableSave { Type = "float", Value = 0.0f, Category = "Flip and Rotation", Name = "Rotation" });

                StandardElementsManager.AddPositioningVariables(roundedRectangleState);
                StandardElementsManager.AddDimensionsVariables(roundedRectangleState, 64, 64,
                    StandardElementsManager.DimensionVariableAction.ExcludeFileOptions);
                StandardElementsManager.AddColorVariables(roundedRectangleState);


                AddDropshadowVariables(roundedRectangleState);

                AddStrokeAndFilledVariables(roundedRectangleState);
            }

            return roundedRectangleState;
        }

        public static StateSave GetArcState()
        {
            if(arcState == null)
            {
                arcState = new StateSave();
                arcState.Name = "Default";
                arcState.Variables.Add(new VariableSave { Type = "float", Value = 10, Category = "Arc", Name = "Thickness" });
                arcState.Variables.Add(new VariableSave { Type = "float", Value = 0, Category = "Arc", Name = "StartAngle" });
                arcState.Variables.Add(new VariableSave { Type = "float", Value = 90, Category = "Arc", Name = "SweepAngle" });
                arcState.Variables.Add(new VariableSave { Type = "bool", Value = false, Category = "Arc", Name = "IsEndRounded" });

                AddVisibleVariable(arcState);

                StandardElementsManager.AddPositioningVariables(arcState);
                StandardElementsManager.AddDimensionsVariables(arcState, 64, 64,
                    StandardElementsManager.DimensionVariableAction.ExcludeFileOptions);
                StandardElementsManager.AddColorVariables(arcState);

                AddGradientVariables(arcState);
            }

            return arcState;
        }



        private static void AddGradientVariables(StateSave state)
        {
            state.Variables.Add(new VariableSave { Type = "bool", Value = false, Category = "Rendering", Name = "UseGradient" });

            state.Variables.Add(new VariableSave { Type = typeof(GradientType).Name, Value = GradientType.Linear, Name = "GradientType", Category = "Rendering", 
                CustomTypeConverter = new EnumConverter(typeof(GradientType))});


            state.Variables.Add(new VariableSave { Type = "float", Value = 0, Category = "Rendering", Name = "GradientX1" });
            state.Variables.Add(new VariableSave { Type = "float", Value = 0, Category = "Rendering", Name = "GradientY1" });

            state.Variables.Add(new VariableSave { Type = "int", Value = 255, Name = "Red1", Category = "Rendering" });
            state.Variables.Add(new VariableSave { Type = "int", Value = 255, Name = "Green1", Category = "Rendering" });
            state.Variables.Add(new VariableSave { Type = "int", Value = 255, Name = "Blue1", Category = "Rendering" });

            state.Variables.Add(new VariableSave { Type = "float", Value = 100, Category = "Rendering", Name = "GradientX2" });
            state.Variables.Add(new VariableSave { Type = "float", Value = 100, Category = "Rendering", Name = "GradientY2" });

            state.Variables.Add(new VariableSave { Type = "int", Value = 255, Name = "Red2", Category = "Rendering" });
            state.Variables.Add(new VariableSave { Type = "int", Value = 255, Name = "Green2", Category = "Rendering" });
            state.Variables.Add(new VariableSave { Type = "int", Value = 0, Name = "Blue2", Category = "Rendering" });

        }

        private static void AddVisibleVariable(StateSave state)
        {
            state.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = true, Name = "Visible" });
        }

        static void AddDropshadowVariables(StateSave stateSave)
        {
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = false, Name = "HasDropshadow", Category = "Dropshadow" });

            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 0, Name = "DropshadowOffsetX", Category = "Dropshadow" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 3, Name = "DropshadowOffsetY", Category = "Dropshadow" });

            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 0, Name = "DropshadowBlurX", Category = "Dropshadow" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 3, Name = "DropshadowBlurY", Category = "Dropshadow" });

            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 255, Name = "DropshadowAlpha", Category = "Dropshadow" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 0, Name = "DropshadowRed", Category = "Dropshadow" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 0, Name = "DropshadowGreen", Category = "Dropshadow" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 0, Name = "DropshadowBlue", Category = "Dropshadow" });
        }

        private static void AddStrokeAndFilledVariables(StateSave stateSave)
        {
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = true, Name = "IsFilled", Category = "Stroke and Fill" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 2.0f, Name = "StrokeWidth", Category = "Stroke and Fill" });

        }

        internal static bool GetIfVariableIsExcluded(VariableSave variable, RecursiveVariableFinder recursiveVariableFinder)
        {
            var prefix = string.IsNullOrEmpty(variable.SourceObject) ? "" : variable.SourceObject + '.';

            var rootName = variable.GetRootName();

            if (rootName == "Red" || rootName == "Green" || rootName == "Blue")
            {

                var usesGradients = recursiveVariableFinder.GetValue(prefix + "UseGradient");
                if (usesGradients is bool asBool && asBool)
                {
                    return true;
                }

            }
            else if (rootName == "Red1" || rootName == "Green1" || rootName == "Blue1" || rootName == "GradientX1" || rootName == "GradientY1" ||
                rootName == "Red2" || rootName == "Green2" || rootName == "Blue2" || rootName == "GradientX2" || rootName == "GradientY2" ||
                rootName == "GradientType")
            {
                var usesGradients = recursiveVariableFinder.GetValue(prefix + "UseGradient");
                var effectiveUsesGradient = usesGradients is bool asBool && asBool;
                return !effectiveUsesGradient;
            }
            // eventually exclude things here based on gradients
            return false;
        }


    }
}
