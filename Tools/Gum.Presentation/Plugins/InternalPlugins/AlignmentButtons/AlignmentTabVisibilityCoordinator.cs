using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.ToolStates;

namespace Gum.Plugins.AlignmentButtons;

internal class AlignmentTabVisibilityCoordinator
{
    private readonly ISelectedState _selectedState;
    private readonly IPluginTab _tab;
    private readonly IProjectRestrictions _projectRestrictions;

    public AlignmentTabVisibilityCoordinator(ISelectedState selectedState, IPluginTab tab, IProjectRestrictions projectRestrictions)
    {
        _projectRestrictions = projectRestrictions;
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

        // Alignment writes the units and origins, so it is useless when the rules hide any of them.
        if (shouldShow)
        {
            ElementSave? element = _selectedState.SelectedElement;
            InstanceSave? instance = _selectedState.SelectedInstance;
            foreach (string name in new[] { "XUnits", "YUnits", "XOrigin", "YOrigin" })
            {
                bool hidden = instance != null
                    ? _projectRestrictions.IsVariableHidden(instance, name)
                    : _projectRestrictions.IsVariableHidden(element!, name);
                if (hidden)
                {
                    return false;
                }
            }
        }

        return shouldShow;
    }
}
