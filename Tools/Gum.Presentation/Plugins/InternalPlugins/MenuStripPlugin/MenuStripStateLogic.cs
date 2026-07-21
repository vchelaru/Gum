using Gum.DataTypes;
using Gum.ToolStates;

namespace Gum.Managers;

/// <summary>
/// The result of <see cref="MenuStripStateLogic.GetRefreshState"/> — the header text and
/// enabled/checked state for <see cref="MenuStripManager"/>'s selection-dependent menu items.
/// </summary>
public record MenuStripRefreshState(
    bool StandardsPaletteChecked,
    string RemoveStateHeader,
    bool RemoveStateEnabled,
    string RemoveElementHeader,
    bool RemoveElementEnabled,
    string RemoveVariableHeader,
    bool RemoveVariableEnabled);

/// <summary>
/// Headless decision logic behind <see cref="MenuStripManager.RefreshUI"/> (ADR-0005): which
/// header text and enabled/checked state the menu strip's selection-dependent items should show
/// for the current selection. <see cref="MenuStripManager"/> stays responsible only for pushing
/// the returned <see cref="MenuStripRefreshState"/> onto its live WPF <c>MenuItem</c>s.
/// </summary>
public class MenuStripStateLogic
{
    private readonly ISelectedState _selectedState;
    private readonly IProjectManager _projectManager;

    public MenuStripStateLogic(ISelectedState selectedState, IProjectManager projectManager)
    {
        _selectedState = selectedState;
        _projectManager = projectManager;
    }

    /// <summary>
    /// Computes the header text and enabled/checked state for the menu strip's
    /// selection-dependent items based on the current selection and project settings.
    /// </summary>
    public MenuStripRefreshState GetRefreshState()
    {
        string removeStateHeader;
        bool removeStateEnabled;
        if (_selectedState.SelectedStateSave != null && _selectedState.SelectedStateSave.Name != "Default")
        {
            removeStateHeader = "State " + _selectedState.SelectedStateSave.Name;
            removeStateEnabled = true;
        }
        else if (_selectedState.SelectedStateCategorySave != null)
        {
            removeStateHeader = "Category " + _selectedState.SelectedStateCategorySave.Name;
            removeStateEnabled = true;
        }
        else
        {
            removeStateHeader = "<no state selected>";
            removeStateEnabled = false;
        }

        string removeElementHeader;
        bool removeElementEnabled;
        if (_selectedState.SelectedElement != null && !(_selectedState.SelectedElement is StandardElementSave))
        {
            removeElementHeader = _selectedState.SelectedElement.Name;
            removeElementEnabled = true;
        }
        else
        {
            removeElementHeader = "<no element selected>";
            removeElementEnabled = false;
        }

        string removeVariableHeader;
        bool removeVariableEnabled;
        if (_selectedState.SelectedBehaviorVariable != null)
        {
            removeVariableHeader = _selectedState.SelectedBehaviorVariable.ToString();
            removeVariableEnabled = true;
        }
        else
        {
            removeVariableHeader = "<no behavior variable selected>";
            removeVariableEnabled = false;
        }

        return new MenuStripRefreshState(
            _projectManager.EffectiveUseStandardsPalette,
            removeStateHeader, removeStateEnabled,
            removeElementHeader, removeElementEnabled,
            removeVariableHeader, removeVariableEnabled);
    }
}
