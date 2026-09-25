using Gum.DataTypes;
using Gum.DataTypes.Variables;

namespace Gum.Undo;

/// <summary>
/// One variable change made on an element other than the one that owns the undo entry it's attached
/// to - for example, deleting a state removes MyButton.VisibilityState wherever another element set
/// it, and renaming the state rewrites that value. Captured when the change is made so the owning
/// element's undo entry can reverse it on undo and re-apply it on redo, without the other element
/// needing its own undo entry. See ADR 0016.
/// </summary>
public class CrossElementVariableChange
{
    public ElementSave Container { get; set; } = null!;

    /// <summary>
    /// The instance the variable assigns, or null for a variable the container sets on itself (an
    /// element deriving from the one whose state changed).
    /// </summary>
    public InstanceSave? Instance { get; set; }

    /// <summary>
    /// The state the variable lives in - not necessarily <see cref="Container"/>'s default state, since
    /// a variable can be assigned inside any category state.
    /// </summary>
    public StateSave State { get; set; } = null!;

    /// <summary>
    /// A copy of the variable before the change, or null when the change added it.
    /// </summary>
    public VariableSave? Before { get; set; }

    /// <summary>
    /// A copy of the variable after the change, or null when the change removed it.
    /// </summary>
    public VariableSave? After { get; set; }

    /// <summary>
    /// Copies <paramref name="variable"/> as <see cref="Before"/>. Call before the change; a removal
    /// needs nothing more, a modification calls <see cref="CaptureAfter"/> once it is done.
    /// </summary>
    public static CrossElementVariableChange CaptureBefore(ElementSave container, StateSave state, VariableSave variable)
    {
        return new CrossElementVariableChange
        {
            Container = container,
            Instance = string.IsNullOrEmpty(variable.SourceObject) ? null : container.GetInstance(variable.SourceObject),
            State = state,
            Before = variable.Clone(),
        };
    }

    /// <summary>
    /// Records the modified variable as <see cref="After"/>.
    /// </summary>
    public void CaptureAfter(VariableSave variable)
    {
        After = variable.Clone();
    }
}
