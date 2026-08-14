using System;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// A single variable row as seen by <see cref="VariableCategoryCopyPasteService"/>. The Variables tab
/// rows are WPF <c>InstanceMember</c>s (and multi-select/composite wrappers over them), which cannot be
/// referenced from this headless assembly, so the view side adapts each row to this interface.
/// </summary>
public interface IVariableCategoryRow
{
    /// <summary>The unqualified variable name, used to match a copied value to a row on the paste target.</summary>
    string RootVariableName { get; }

    /// <summary>Whether the row rejects edits (for example a variable on a locked instance).</summary>
    bool IsReadOnly { get; }

    /// <summary>Whether the row's value comes from a <c>VariableReferences</c> assignment, which a paste must not shadow.</summary>
    bool IsAssignedByReference { get; }

    /// <summary>
    /// Whether the row has no single value because a multi-selection's instances disagree. Such a row's
    /// <see cref="Value"/> is null, and a copy must report it rather than silently treating it as unset.
    /// </summary>
    bool IsIndeterminate { get; }

    /// <summary>The row's effective value, whether authored on the selected state or inherited.</summary>
    object? Value { get; }

    /// <summary>
    /// The type the row accepts, or null when it cannot be determined. Used to reject a paste between
    /// same-named variables of different types; the current <see cref="Value"/> is not a reliable stand-in
    /// for this, since a row showing no value at all is exactly where a mistyped paste would land.
    /// </summary>
    Type? ValueType { get; }

    /// <summary>
    /// Writes a value to the row, returning whether the assignment was accepted. Never null: a copy skips
    /// rows that have no value, since writing null would author an explicit null rather than restore
    /// inheritance.
    /// </summary>
    bool TrySetValue(object value);
}
