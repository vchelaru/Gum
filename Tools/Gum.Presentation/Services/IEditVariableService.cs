using Gum.DataTypes;
using Gum.DataTypes.Variables;

namespace Gum.Services;

/// <summary>
/// Determines how a variable can be edited and opens the corresponding editing UI. See
/// <see cref="Gum.Services.EditVariableService"/> for the concrete implementation (tool project)
/// and <c>IEditVariableMenuService</c> for the WpfDataUi-coupled counterpart that wires this into a
/// variable row's context menu.
/// </summary>
public interface IEditVariableService
{
    VariableEditMode GetAvailableEditModeFor(VariableSave variableSave, IStateContainer stateCategoryListContainer);

    /// <summary>
    /// Returns the context-menu label for editing the given variable, or null when the variable
    /// offers no edit action.
    /// </summary>
    string? GetEditVariableMenuLabel(VariableSave variableSave, IStateContainer stateListCategoryContainer);

    void ShowEditVariableWindow(VariableSave variable, IStateContainer container);
}

public enum VariableEditMode
{
    None,
    ExposedName,
    FullEdit
}
