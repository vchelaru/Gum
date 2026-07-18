using Gum.DataTypes;
using Gum.DataTypes.Variables;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Deletes a custom variable from an element or behavior. See
/// <see cref="Gum.Plugins.InternalPlugins.VariableGrid.DeleteVariableService"/> for the concrete
/// implementation (tool project).
/// </summary>
public interface IDeleteVariableService
{
    bool CanDeleteVariable(VariableSave variable);
    void DeleteVariable(VariableSave variable, IStateContainer stateContainer);
}
