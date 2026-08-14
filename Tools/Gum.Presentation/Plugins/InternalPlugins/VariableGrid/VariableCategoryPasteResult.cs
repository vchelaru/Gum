using System.Collections.Generic;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// What a category paste actually did. Paste is deliberately lenient - a variable the target does not
/// have, cannot take, or would shadow a reference on is skipped rather than failing the whole paste -
/// so the caller needs this to report the outcome.
/// </summary>
/// <param name="AppliedVariableNames">Variables whose values were written to the target.</param>
/// <param name="AlreadyMatchedVariableNames">Variables the target already showed with the copied value, left untouched.</param>
/// <param name="SkippedVariableNames">Copied variables that were neither written nor matched.</param>
public record VariableCategoryPasteResult(
    IReadOnlyList<string> AppliedVariableNames,
    IReadOnlyList<string> AlreadyMatchedVariableNames,
    IReadOnlyList<string> SkippedVariableNames);
