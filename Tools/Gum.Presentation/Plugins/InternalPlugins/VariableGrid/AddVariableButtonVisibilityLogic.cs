using Gum.ToolStates;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

internal static class AddVariableButtonVisibilityLogic
{
    public static bool ShouldShow(ISelectedState selectedState)
    {
        var shouldShow = selectedState.SelectedBehavior != null ||
            selectedState.SelectedComponent != null ||
            selectedState.SelectedScreen != null;

        if (shouldShow)
        {
            shouldShow = selectedState.SelectedInstance == null;
        }

        return shouldShow;
    }
}
