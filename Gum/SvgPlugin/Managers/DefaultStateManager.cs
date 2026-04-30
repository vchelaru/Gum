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
    // Canvas/Svg/LottieAnimation default-state metadata moved to GumCommon's
    // StandardElementsManager so headless consumers (Gum.ProjectServices, gumcli) can
    // resolve them without loading SkiaGum. These forwarders are kept so existing
    // callers (StandardAdder, MainSkiaPlugin, Skia SystemManagers) continue to compile.
    public static StateSave GetSvgState() => StandardElementsManager.GetSvgState();
    public static StateSave GetCanvasState() => StandardElementsManager.GetCanvasState();
    public static StateSave GetLottieAnimationState() => StandardElementsManager.GetLottieAnimationState();


    #region Property Grid Utilities

    internal static void HandleVariableSet(ElementSave owner, InstanceSave instance, string variableName, object oldValue)
    {
        var rootName = VariableSave.GetRootName(variableName);

        var shouldRefresh = rootName == "UseGradient" ||
            rootName == "GradientType" ||
            rootName == "HasDropshadow" ||
            rootName == "IsFilled";

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

        #region Stroke and Fill

        if (rootName == "StrokeWidth" || rootName == "StrokeDashLength" || rootName == "StrokeGapLength")
        {
            var isFilled = recursiveVariableFinder.GetValue(prefix + "IsFilled");
            if (isFilled is true)
            {
                return true;
            }
        }

        #endregion

        return false;
    }

#if GUM
    internal static void UpdateDisplayersForStandards()
    {
        Gum.Plugins.InternalPlugins.VariableGrid.StandardElementsManagerGumTool.SetPreferredDisplayers(StandardElementsManager.GetArcState());
        Gum.Plugins.InternalPlugins.VariableGrid.StandardElementsManagerGumTool.SetPreferredDisplayers(StandardElementsManager.GetColoredCircleState());
        Gum.Plugins.InternalPlugins.VariableGrid.StandardElementsManagerGumTool.SetPreferredDisplayers(StandardElementsManager.GetLineState());
        Gum.Plugins.InternalPlugins.VariableGrid.StandardElementsManagerGumTool.SetPreferredDisplayers(StandardElementsManager.GetRoundedRectangleState());

        Gum.Plugins.InternalPlugins.VariableGrid.StandardElementsManagerGumTool.SetPreferredDisplayers(GetCanvasState());
        Gum.Plugins.InternalPlugins.VariableGrid.StandardElementsManagerGumTool.SetPreferredDisplayers(GetSvgState());
        Gum.Plugins.InternalPlugins.VariableGrid.StandardElementsManagerGumTool.SetPreferredDisplayers(GetLottieAnimationState());

    }
#endif
    #endregion

}
