using System.Collections.Generic;
using WpfDataUi.DataTypes;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Adapts the Variables tab's WPF rows to <see cref="IVariableCategoryRow"/> so the headless
/// <see cref="IVariableCategoryCopyPasteService"/> can read and write them without referencing WPF.
/// </summary>
public interface IVariableCategoryRowAdapter
{
    /// <summary>Wraps each member of a category as a copy/paste row.</summary>
    List<IVariableCategoryRow> CreateRows(IEnumerable<InstanceMember> members);
}
