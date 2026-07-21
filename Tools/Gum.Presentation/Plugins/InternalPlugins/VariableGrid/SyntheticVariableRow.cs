using System;
using System.Collections.Generic;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Headless stand-in for an ad hoc WpfDataUi <c>InstanceMember</c> row that isn't backed by a real
/// state-driven <see cref="VariableGridEntry"/> - e.g. a category's "remove from category" row, or a
/// behavior's synthetic property row. A WPF-side mapper materializes each into a real
/// <c>InstanceMember</c> using the delegates held here.
/// </summary>
public class SyntheticVariableRow
{
    public required string Name { get; init; }

    /// <summary>Reads the row's current display value.</summary>
    public required Func<object?> Get { get; init; }

    /// <summary>Writes a new value, or null when the row is read-only.</summary>
    public Action<object?>? Set { get; init; }

    public Type ValueType { get; init; } = typeof(string);

    public VariableDisplayerKind? PreferredDisplayerKindOverride { get; init; }

    public string? DetailText { get; init; }

    /// <summary>Standard-values list driving a combo-box row (e.g. behavior implementation names).</summary>
    public IList<object>? CustomOptions { get; init; }
}
