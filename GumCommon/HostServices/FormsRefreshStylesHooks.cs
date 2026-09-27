using Gum.Forms.Controls;
using Gum.Wireframe;

// Companion to Gum.GumService / GumServiceSkiaBase (issue #5229): the Forms hooks
// GraphicalUiElement.RefreshStyles calls so live Forms state (typed text, caret, scroll offsets,
// slider values) survives a state re-apply. GumRuntime can't reference FrameworkElement, so each
// host installs these; living here keeps one implementation for both host families.
namespace Gum;

public static class FormsRefreshStylesHooks
{
    /// <summary>
    /// Wires <see cref="GraphicalUiElement.SaveFormsRuntimePropertiesAction"/> and
    /// <see cref="GraphicalUiElement.UpdateFormsStateAction"/> to the <see cref="FrameworkElement"/>
    /// save/restore methods.
    /// </summary>
    public static void Install()
    {
        GraphicalUiElement.SaveFormsRuntimePropertiesAction = formsObject =>
        {
            if (formsObject is FrameworkElement frameworkElement)
            {
                frameworkElement.SaveRuntimeProperties();
            }
        };
        GraphicalUiElement.UpdateFormsStateAction = formsObject =>
        {
            if (formsObject is FrameworkElement frameworkElement)
            {
                frameworkElement.UpdateState();
                frameworkElement.ApplyRuntimeProperties();
            }
        };
    }
}
