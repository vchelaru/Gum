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


    // Gradient/dropshadow/stroke exclusion (GetIfVariableIsExcluded) and the corresponding
    // VariableSet grid-refresh trigger (HandleVariableSet) moved out of this plugin in
    // #2931 — they live in Gum.Plugins.InternalPlugins.VariableGrid.ExclusionsPlugin and
    // SetVariableLogic.VariablesRequiringRefresh now, since those variables are no longer
    // Skia-only.

#if GUM
    internal static void UpdateDisplayersForStandards()
    {
        // SvgPlugin is a separate plugin assembly outside the main DI container, so it resolves
        // the (now instance) StandardElementsManagerGumTool through the service Locator.
        Gum.Plugins.InternalPlugins.VariableGrid.IStandardElementsManagerGumTool standardElementsManagerGumTool =
            Locator.GetRequiredService<Gum.Plugins.InternalPlugins.VariableGrid.IStandardElementsManagerGumTool>();

        standardElementsManagerGumTool.SetPreferredDisplayers(StandardElementsManager.GetArcState());
        standardElementsManagerGumTool.SetPreferredDisplayers(StandardElementsManager.GetColoredCircleState());
        standardElementsManagerGumTool.SetPreferredDisplayers(StandardElementsManager.GetLineState());
        standardElementsManagerGumTool.SetPreferredDisplayers(StandardElementsManager.GetRoundedRectangleState());

        standardElementsManagerGumTool.SetPreferredDisplayers(GetCanvasState());
        standardElementsManagerGumTool.SetPreferredDisplayers(GetSvgState());
        standardElementsManagerGumTool.SetPreferredDisplayers(GetLottieAnimationState());
    }
#endif

}
