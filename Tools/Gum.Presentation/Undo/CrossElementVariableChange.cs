using Gum.DataTypes;
using Gum.DataTypes.Variables;

namespace Gum.Undo;

/// <summary>
/// One instance-level variable assignment removed from an element other than the one that owns the
/// undo entry it's attached to - for example, deleting Component1.Variable1 also removes
/// MyButton.Variable1 wherever some other element has an instance of Component1 with that value
/// assigned. Captured at delete time so the owning element's undo entry can restore it on undo and
/// remove it again on redo, without that other element needing its own undo entry. See ADR 0016.
/// </summary>
public class CrossElementVariableChange
{
    public ElementSave Container { get; set; } = null!;
    public InstanceSave Instance { get; set; } = null!;

    /// <summary>
    /// The state the variable lives in - not necessarily <see cref="Container"/>'s default state, since
    /// an instance-level override can be assigned inside any category state.
    /// </summary>
    public StateSave State { get; set; } = null!;

    public VariableSave Variable { get; set; } = null!;
}
