using System.Drawing;
using Gum.ToolStates;

namespace Gum.Services;

public class StateEditingIndicatorService : IStateEditingIndicatorService
{
    private readonly ISelectedState _selectedState;

    public StateEditingIndicatorService(ISelectedState selectedState)
    {
        _selectedState = selectedState;
    }

    public StateEditingIndicatorInfo GetInfo()
    {
        var element = _selectedState.SelectedElement;
        var state = _selectedState.SelectedStateSave;

        if (element == null || state == null || state == element.DefaultState)
        {
            return new StateEditingIndicatorInfo(false, null, Color.Empty);
        }

        if (_selectedState.CustomCurrentStateSave != null)
        {
            return new StateEditingIndicatorInfo(true, "Displaying custom (animated) state", Color.Pink);
        }

        var category = _selectedState.SelectedStateCategorySave;
        var stateName = category != null ? $"{category.Name}/{state.Name}" : state.Name;

        return new StateEditingIndicatorInfo(true, $"Editing state {stateName}", Color.Yellow);
    }
}
