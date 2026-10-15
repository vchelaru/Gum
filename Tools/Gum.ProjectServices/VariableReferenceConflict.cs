using Gum.DataTypes;
using Gum.DataTypes.Variables;

namespace Gum.ProjectServices;

/// <summary>
/// The data behind a GUM0002 result: a variable whose stored value disagrees with what the
/// VariableReference that owns it evaluates to. A tool uses it to offer replacing the stored value.
/// </summary>
public class VariableReferenceConflict
{
    /// <summary>The element the conflicting state belongs to.</summary>
    public ElementSave Element { get; }

    /// <summary>The state holding the stale value.</summary>
    public StateSave State { get; }

    /// <summary>The variable's name in <see cref="State"/>, qualified by its instance when it has one.</summary>
    public string VariableName { get; }

    /// <summary>The instance the variable belongs to, or null for a variable of the element itself.</summary>
    public string? InstanceName { get; }

    /// <summary>The value stored in <see cref="State"/>.</summary>
    public object? OldValue { get; }

    /// <summary>The value the reference evaluates to.</summary>
    public object? NewValue { get; }

    /// <summary>
    /// The reference line (e.g. <c>X = Other.X</c>, left side unqualified) when the reference sits on
    /// <see cref="State"/> itself. Null when it sits on a categorized state of the instance's base type,
    /// whose references the instance's own state never re-applies.
    /// </summary>
    public string? ReferenceLine { get; }

    public VariableReferenceConflict(ElementSave element, StateSave state, string variableName, string? instanceName,
        object? oldValue, object? newValue, string? referenceLine)
    {
        Element = element;
        State = state;
        VariableName = variableName;
        InstanceName = instanceName;
        OldValue = oldValue;
        NewValue = newValue;
        ReferenceLine = referenceLine;
    }
}
