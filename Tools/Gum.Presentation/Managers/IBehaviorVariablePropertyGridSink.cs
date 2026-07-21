using Gum.DataTypes.Variables;

namespace Gum.Managers;

/// <summary>
/// Narrow seam so headless selection logic (<see cref="Gum.ToolStates.SelectedState"/>) can push
/// the newly-selected behavior variable into the property grid's view model without depending on
/// the concrete, WPF-coupled <c>PropertyGridManager</c>. Implemented by <c>PropertyGridManager</c>.
/// </summary>
public interface IBehaviorVariablePropertyGridSink
{
    VariableSave SelectedBehaviorVariable { set; }
}
