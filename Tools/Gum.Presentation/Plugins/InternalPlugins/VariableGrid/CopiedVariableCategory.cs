using System.Collections.Generic;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// The values captured by a Variables tab category copy, held in memory by
/// <see cref="VariableCategoryCopyPasteService"/> rather than on the system clipboard (mirroring
/// <c>CopyPasteLogic.CopiedData</c>).
/// </summary>
/// <param name="CategoryName">The name of the category the values came from, reported back when pasting.</param>
/// <param name="Values">The captured values, in the order they appeared in the source category.</param>
/// <param name="IndeterminateVariableNames">
/// Variables that could not be captured because the source was a multi-selection whose instances
/// disagree on the value. Reported so the user knows the copy is incomplete.
/// </param>
public record CopiedVariableCategory(
    string CategoryName,
    IReadOnlyList<CopiedVariableValue> Values,
    IReadOnlyList<string> IndeterminateVariableNames);

/// <summary>A single variable name/value pair captured by a category copy.</summary>
/// <param name="RootVariableName">The unqualified variable name, used to match a row on the paste target.</param>
/// <param name="Value">The effective value at copy time.</param>
public record CopiedVariableValue(string RootVariableName, object Value);
