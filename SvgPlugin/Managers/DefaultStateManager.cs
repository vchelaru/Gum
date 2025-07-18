using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using RenderingLibrary.Graphics;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if GUM
using Gum.Commands;
using Gum.Services;
using WpfDataUi.Controls;
#endif
namespace SkiaPlugin.Managers
{
    public static class DefaultStateManager
    {
        #region Fields/Properties

        static StateSave arcState;
        static StateSave canvasState;
        static StateSave svgState;
        static StateSave filledCircleState;
        static StateSave roundedRectangleState;
        static StateSave lottieAnimationState;

        #endregion

        #region Svg State
        public static StateSave GetSvgState()
        {
            if (svgState == null)
            {
                svgState = new StateSave();
                svgState.Name = "Default";
                AddVisibleVariable(svgState);
                StandardElementsManager.AddPositioningVariables(svgState);
                StandardElementsManager.AddDimensionsVariables(svgState, 100, 100,
                    Gum.Managers.StandardElementsManager.DimensionVariableAction.AllowFileOptions);
                StandardElementsManager.AddColorVariables(svgState);

                foreach (var variableSave in svgState.Variables.Where(item => item.Type == typeof(DimensionUnitType).Name))
                {
                    variableSave.Value = DimensionUnitType.Absolute;
                    variableSave.ExcludedValuesForEnum.Add(DimensionUnitType.PercentageOfSourceFile);
                    //variableSave.ExcludedValuesForEnum.Add(DimensionUnitType.MaintainFileAspectRatio);

                }

                svgState.Variables.Add(new VariableSave { SetsValue = true, Type = "string", Value = "", Name = "SourceFile", IsFile = true });

                AddBlendVariable(svgState);

                svgState.Variables.Add(new VariableSave { Type = "float", Value = 0.0f, Category = "Flip and Rotation", Name = "Rotation", SetsValue = true });

                AddVariableReferenceList(svgState);

                StandardElementsManager.AddEventVariables(svgState);

            }
            return svgState;
        }
        #endregion

        #region Canvas State

        public static StateSave GetCanvasState()
        {
            if (canvasState == null)
            {
                canvasState = new StateSave();
                canvasState.Name = "Default";

                AddVisibleVariable(canvasState);

                StandardElementsManager.AddClipsChildren(canvasState);

                StandardElementsManager.AddPositioningVariables(canvasState);

                StandardElementsManager.AddDimensionsVariables(canvasState, 64, 64,
                    StandardElementsManager.DimensionVariableAction.ExcludeFileOptions);

                AddVariableReferenceList(canvasState);

                StandardElementsManager.AddEventVariables(canvasState);

            }

            return canvasState;
        }

        #endregion

        #region Lottie Animation State
        public static StateSave GetLottieAnimationState()
        {
            if (lottieAnimationState == null)
            {
                lottieAnimationState = new StateSave();
                lottieAnimationState.Name = "Default";
                AddVisibleVariable(lottieAnimationState);
                StandardElementsManager.AddPositioningVariables(lottieAnimationState);
                StandardElementsManager.AddDimensionsVariables(lottieAnimationState, 100, 100,
                    Gum.Managers.StandardElementsManager.DimensionVariableAction.AllowFileOptions);

                // Do we support colors?
                //StandardElementsManager.AddColorVariables(lottieAnimationState);

                lottieAnimationState.Variables.Add(new VariableSave { SetsValue = true, Type = "string", Value = "", Name = "SourceFile", IsFile = true });

                AddBlendVariable(lottieAnimationState);


                AddVariableReferenceList(lottieAnimationState);

                StandardElementsManager.AddEventVariables(lottieAnimationState);


            }
            return lottieAnimationState;
        }
        #endregion

        #region Colored Circle State
        public static StateSave GetColoredCircleState()
        {
            if (filledCircleState == null)
            {
                filledCircleState = new StateSave();
                filledCircleState.Name = "Default";
                AddVisibleVariable(filledCircleState);

                StandardElementsManager.AddPositioningVariables(filledCircleState);
                StandardElementsManager.AddDimensionsVariables(filledCircleState, 64, 64,
                    StandardElementsManager.DimensionVariableAction.ExcludeFileOptions);
                StandardElementsManager.AddColorVariables(filledCircleState);

                AddGradientVariables(filledCircleState);

                AddDropshadowVariables(filledCircleState);


                AddStrokeAndFilledVariables(filledCircleState);

                AddBlendVariable(filledCircleState);

                filledCircleState.Variables.Add(new VariableSave { Type = "float", Value = 0.0f, Category = "Flip and Rotation", Name = "Rotation", SetsValue = true });

                AddVariableReferenceList(filledCircleState);
                StandardElementsManager.AddEventVariables(filledCircleState);

            }

            return filledCircleState;
        }
        #endregion

        #region Rounded Rectangle State

        public static StateSave GetRoundedRectangleState()
        {
            if (roundedRectangleState == null)
            {
                roundedRectangleState = new StateSave();
                roundedRectangleState.Name = "Default";
                AddVisibleVariable(roundedRectangleState);

                roundedRectangleState.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 5f, Name = "CornerRadius", Category = "Dimensions" });
                roundedRectangleState.Variables.Add(new VariableSave { Type = "float", Value = 0.0f, Category = "Flip and Rotation", Name = "Rotation" });

                StandardElementsManager.AddPositioningVariables(roundedRectangleState);
                StandardElementsManager.AddDimensionsVariables(roundedRectangleState, 64, 64,
                    StandardElementsManager.DimensionVariableAction.ExcludeFileOptions);
                StandardElementsManager.AddColorVariables(roundedRectangleState);

                AddGradientVariables(roundedRectangleState);

                AddDropshadowVariables(roundedRectangleState);

                AddStrokeAndFilledVariables(roundedRectangleState);

                AddBlendVariable(roundedRectangleState);


                AddVariableReferenceList(roundedRectangleState);

                StandardElementsManager.AddClipsChildren(roundedRectangleState);
                StandardElementsManager.AddEventVariables(roundedRectangleState);

            }

            return roundedRectangleState;
        }

        #endregion

        #region Arc State

        public static StateSave GetArcState()
        {
            if (arcState == null)
            {
                arcState = new StateSave();
                arcState.Name = "Default";
                arcState.Variables.Add(new VariableSave { Type = "float", Value = 10f, Category = "Arc", Name = "Thickness", SetsValue = true });

                var startAngle = new VariableSave { Type = "float", Value = 0f, Category = "Arc", Name = "StartAngle", SetsValue = true };
#if GUM
                StandardElementsManagerGumTool.MakeDegreesAngle(startAngle);
#endif
                arcState.Variables.Add(startAngle);

                var sweepAngle = new VariableSave { Type = "float", Value = 90f, Category = "Arc", Name = "SweepAngle", SetsValue = true };
#if GUM
                StandardElementsManagerGumTool.MakeDegreesAngle(sweepAngle);
#endif
                arcState.Variables.Add(sweepAngle);

                arcState.Variables.Add(new VariableSave { Type = "bool", Value = false, Category = "Arc", Name = "IsEndRounded", SetsValue = true });

                AddVisibleVariable(arcState);

                StandardElementsManager.AddPositioningVariables(arcState);
                StandardElementsManager.AddDimensionsVariables(arcState, 64, 64,
                    StandardElementsManager.DimensionVariableAction.ExcludeFileOptions);
                StandardElementsManager.AddColorVariables(arcState);
                StandardElementsManager.AddEventVariables(arcState);
                //StandardElementsManager.


                AddBlendVariable(arcState);


                AddGradientVariables(arcState);
                AddVariableReferenceList(arcState);
            }

            return arcState;
        }

        #endregion


        #region Add Common Variables

        private static void AddGradientVariables(StateSave state)
        {
            List<object> xUnitsExclusions = new List<object>();
            xUnitsExclusions.Add(PositionUnitType.PixelsFromTop);
            xUnitsExclusions.Add(PositionUnitType.PercentageHeight);
            xUnitsExclusions.Add(PositionUnitType.PixelsFromBottom);
            xUnitsExclusions.Add(PositionUnitType.PixelsFromCenterY);
            xUnitsExclusions.Add(PositionUnitType.PixelsFromCenterYInverted);
            xUnitsExclusions.Add(PositionUnitType.PixelsFromBaseline);

            List<object> yUnitsExclusions = new List<object>();
            yUnitsExclusions.Add(PositionUnitType.PixelsFromLeft);
            yUnitsExclusions.Add(PositionUnitType.PixelsFromCenterX);
            yUnitsExclusions.Add(PositionUnitType.PercentageWidth);
            yUnitsExclusions.Add(PositionUnitType.PixelsFromRight);


            state.Variables.Add(new VariableSave { Type = "bool", Value = false, Category = "Rendering", Name = "UseGradient", SetsValue = true });

            state.Variables.Add(new VariableSave
            {
                SetsValue = true,
                Type = typeof(GradientType).Name,
                Value = GradientType.Linear,
                Name = "GradientType",
                Category = "Rendering",
                CustomTypeConverter = new EnumConverter(typeof(GradientType))
            });


            state.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 0f, Category = "Rendering", Name = "GradientX1" });
            state.Variables.Add(new VariableSave { SetsValue = true, Type = typeof(PositionUnitType).Name, Value = PositionUnitType.PixelsFromLeft, Name = "GradientX1Units", Category = "Rendering", ExcludedValuesForEnum = xUnitsExclusions });


            state.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 0f, Category = "Rendering", Name = "GradientY1" });
            state.Variables.Add(new VariableSave { SetsValue = true, Type = typeof(PositionUnitType).Name, Value = PositionUnitType.PixelsFromTop, Name = "GradientY1Units", Category = "Rendering", ExcludedValuesForEnum = yUnitsExclusions });

            state.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 255, Name = "Alpha1", Category = "Rendering" });
            state.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 255, Name = "Red1", Category = "Rendering" });
            state.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 255, Name = "Green1", Category = "Rendering" });
            state.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 255, Name = "Blue1", Category = "Rendering" });

            state.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 100f, Category = "Rendering", Name = "GradientX2" });
            state.Variables.Add(new VariableSave { SetsValue = true, Type = typeof(PositionUnitType).Name, Value = PositionUnitType.PixelsFromLeft, Name = "GradientX2Units", Category = "Rendering", ExcludedValuesForEnum = xUnitsExclusions });

            state.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 100f, Category = "Rendering", Name = "GradientY2" });
            state.Variables.Add(new VariableSave { SetsValue = true, Type = typeof(PositionUnitType).Name, Value = PositionUnitType.PixelsFromTop, Name = "GradientY2Units", Category = "Rendering", ExcludedValuesForEnum = yUnitsExclusions });

            state.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 50f, Category = "Rendering", Name = "GradientInnerRadius" });
            state.Variables.Add(new VariableSave { SetsValue = true, Type = typeof(DimensionUnitType).Name, Value = DimensionUnitType.Absolute, Name = "GradientInnerRadiusUnits", Category = "Rendering" });


            state.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 100f, Category = "Rendering", Name = "GradientOuterRadius" });
            state.Variables.Add(new VariableSave { SetsValue = true, Type = typeof(DimensionUnitType).Name, Value = DimensionUnitType.Absolute, Name = "GradientOuterRadiusUnits", Category = "Rendering" });

            state.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 255, Name = "Alpha2", Category = "Rendering" });
            state.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 255, Name = "Red2", Category = "Rendering" });
            state.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 255, Name = "Green2", Category = "Rendering" });
            state.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 0, Name = "Blue2", Category = "Rendering" });
        }

        private static void AddVisibleVariable(StateSave state)
        {
            state.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = true, Name = "Visible", Category = "States and Visibility" });
        }

        private static void AddVariableReferenceList(StateSave stateSave)
        {
            stateSave.VariableLists.Add(new VariableListSave<string> { Type = "string", Value = new List<string>(), Category = "References", Name = "VariableReferences" });
        }

        static void AddDropshadowVariables(StateSave stateSave)
        {
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = false, Name = "HasDropshadow", Category = "Dropshadow" });

            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 0f, Name = "DropshadowOffsetX", Category = "Dropshadow" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 3f, Name = "DropshadowOffsetY", Category = "Dropshadow" });

            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 0f, Name = "DropshadowBlurX", Category = "Dropshadow" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 3f, Name = "DropshadowBlurY", Category = "Dropshadow" });

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

        private static void AddBlendVariable(StateSave stateSave)
        {
            var blendariable = new VariableSave { SetsValue = true, Type = "Blend", Value = Gum.RenderingLibrary.Blend.Normal, Name = "Blend", Category = "Rendering" };

            stateSave.Variables.Add(blendariable);
        }

        #endregion

        #region Property Grid Utilities

        internal static void HandleVariableSet(ElementSave owner, InstanceSave instance, string variableName, object oldValue)
        {
            var rootName = VariableSave.GetRootName(variableName);

            var shouldRefresh = rootName == "UseGradient" ||
                rootName == "GradientType" ||
                rootName == "HasDropshadow";

            if (shouldRefresh)
            {
#if GUM
// This should probably be handled in a plugin somewhere:
                Locator.GetRequiredService<GuiCommands>().RefreshVariables(force: true);
#endif
            }
        }
        internal static bool GetIfVariableIsExcluded(VariableSave variable, RecursiveVariableFinder recursiveVariableFinder)
        {
            var prefix = string.IsNullOrEmpty(variable.SourceObject) ? "" : variable.SourceObject + '.';

            var rootName = variable.GetRootName();

            #region Gradients and Colors

            if (rootName == "Red" || rootName == "Green" || rootName == "Blue")
            {

                var usesGradients = recursiveVariableFinder.GetValue(prefix + "UseGradient");
                if (usesGradients is bool asBool && asBool)
                {
                    return true;
                }

            }
            else if (rootName == "Red1" || rootName == "Green1" || rootName == "Blue1" || rootName == "Alpha1" ||
                rootName == "GradientX1" || rootName == "GradientY1" ||
                rootName == "GradientX1Units" || rootName == "GradientY1Units" ||
                rootName == "Red2" || rootName == "Green2" || rootName == "Blue2" || rootName == "Alpha2" ||
                rootName == "GradientType")
            {
                var usesGradients = recursiveVariableFinder.GetValue(prefix + "UseGradient");
                var effectiveUsesGradient = usesGradients is bool asBool && asBool;
                return !effectiveUsesGradient;
            }
            else if (rootName == "GradientX2" || rootName == "GradientY2" || rootName == "GradientX2Units" || rootName == "GradientY2Units")
            {
                var usesGradients = recursiveVariableFinder.GetValue(prefix + "UseGradient");
                var effectiveUsesGradient = usesGradients is bool asBool && asBool;

                var gradientTypeAsObject = recursiveVariableFinder.GetValue(prefix + "GradientType");
                GradientType? gradientType = gradientTypeAsObject as GradientType?;

                var hide = effectiveUsesGradient == false || gradientType != GradientType.Linear;

                return hide;
            }
            else if (rootName == "GradientInnerRadius" || rootName == "GradientOuterRadius" ||
                rootName == "GradientInnerRadiusUnits" || rootName == "GradientOuterRadiusUnits")
            {
                var usesGradients = recursiveVariableFinder.GetValue(prefix + "UseGradient");
                var effectiveUsesGradient = usesGradients is bool asBool && asBool;

                var gradientTypeAsObject = recursiveVariableFinder.GetValue(prefix + "GradientType");
                GradientType? gradientType = gradientTypeAsObject as GradientType?;

                var hide = effectiveUsesGradient == false || gradientType != GradientType.Radial;

                return hide;

            }

            #endregion

            #region Dropshadow

            if (rootName == "DropshadowOffsetX" || rootName == "DropshadowOffsetY" || rootName == "DropshadowBlurX" || rootName == "DropshadowBlurY" ||
                rootName == "DropshadowAlpha" || rootName == "DropshadowRed" || rootName == "DropshadowGreen" || rootName == "DropshadowBlue")
            {
                var hasDropshadow = recursiveVariableFinder.GetValue(prefix + "HasDropshadow");
                var effectiveHasDropshadow = hasDropshadow is bool asBool && asBool;
                return !effectiveHasDropshadow;
            }

            #endregion

            return false;
        }
        #endregion


    }
}
