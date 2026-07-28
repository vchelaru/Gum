using Gum.Plugins;
using Gum.ToolStates;

namespace Gum.Plugins.AlignmentButtons;

internal class AlignmentTabVisibilityCoordinator
{
    private readonly ISelectedState _selectedState;
    private readonly IPluginTab _tab;

    public AlignmentTabVisibilityCoordinator(ISelectedState selectedState, IPluginTab tab)
    {
        _selectedState = selectedState;
        _tab = tab;
    }

    public void Refresh()
    {
        if (ShouldShowTab())
        {
            _tab.Show();
        }
        else
        {
            _tab.Hide();
        }
    }

    private bool ShouldShowTab()
    {
        var shouldShow = _selectedState.SelectedElement != null &&
            _selectedState.SelectedStateSave != null;

        if (shouldShow && _selectedState.SelectedScreen != null && _selectedState.SelectedInstance == null)
        {
            // screens as a whole can't be aligned
            shouldShow = false;
        }

        return shouldShow;
    }
}
