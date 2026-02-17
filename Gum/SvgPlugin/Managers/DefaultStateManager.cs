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
namespace SkiaPlugin.Managers;

public static class DefaultStateManager
{
    #region Fields/Properties

    static StateSave? canvasState;
    static StateSave? svgState;

    static StateSave? lottieAnimationState;

    #endregion

    #region Svg State
    public static StateSave GetSvgState()
    {
        if (svgState == null)
        {
            svgState = new StateSave();
            svgState.Name = "Default";
            StandardElementsManager.AddVisibleVariable(svgState);
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

            svgState.Variables.Add(new VariableSave { SetsValue = true, Type = "string", Value = "", Name = "SourceFile", IsFile = true, Category="Source" });

            StandardElementsManager.AddBlendVariable(svgState);

            svgState.Variables.Add(new VariableSave { Type = "float", Value = 0.0f, Category = "Flip and Rotation", Name = "Rotation", SetsValue = true });

            StandardElementsManager.AddVariableReferenceList(svgState);

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

            StandardElementsManager.AddVisibleVariable(canvasState);

            StandardElementsManager.AddClipsChildren(canvasState);

            StandardElementsManager.AddPositioningVariables(canvasState);

            StandardElementsManager.AddDimensionsVariables(canvasState, 64, 64,
                StandardElementsManager.DimensionVariableAction.ExcludeFileOptions);

            StandardElementsManager.AddVariableReferenceList(canvasState);

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
            StandardElementsManager.AddVisibleVariable(lottieAnimationState);
            StandardElementsManager.AddPositioningVariables(lottieAnimationState);
            StandardElementsManager.AddDimensionsVariables(lottieAnimationState, 100, 100,
                Gum.Managers.StandardElementsManager.DimensionVariableAction.AllowFileOptions);

            // Do we support colors?
            //StandardElementsManager.AddColorVariables(lottieAnimationState);

            lottieAnimationState.Variables.Add(new VariableSave { SetsValue = true, Type = "string", Value = "", Name = "SourceFile", IsFile = true, Category = "Source" });

            StandardElementsManager.AddBlendVariable(lottieAnimationState);


            StandardElementsManager.AddVariableReferenceList(lottieAnimationState);

            StandardElementsManager.AddEventVariables(lottieAnimationState);
        }
        return lottieAnimationState;
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
            Locator.GetRequiredService<IGuiCommands>().RefreshVariables(force: true);
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

#if GUM
    internal static void UpdateDisplayersForStandards()
    {
        Gum.Plugins.InternalPlugins.VariableGrid.StandardElementsManagerGumTool.SetPreferredDisplayers(StandardElementsManager.GetArcState());
        Gum.Plugins.InternalPlugins.VariableGrid.StandardElementsManagerGumTool.SetPreferredDisplayers(StandardElementsManager.GetColoredCircleState());
        Gum.Plugins.InternalPlugins.VariableGrid.StandardElementsManagerGumTool.SetPreferredDisplayers(StandardElementsManager.GetRoundedRectangleState());

        Gum.Plugins.InternalPlugins.VariableGrid.StandardElementsManagerGumTool.SetPreferredDisplayers(GetCanvasState());
        Gum.Plugins.InternalPlugins.VariableGrid.StandardElementsManagerGumTool.SetPreferredDisplayers(GetSvgState());
        Gum.Plugins.InternalPlugins.VariableGrid.StandardElementsManagerGumTool.SetPreferredDisplayers(GetLottieAnimationState());

    }
#endif
    #endregion

}
