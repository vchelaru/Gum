using System.Collections.Generic;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// What a category paste actually did. Paste is deliberately lenient - a variable the target does not
/// have, cannot take, or would shadow a reference on is skipped rather than failing the whole paste -
/// so the caller needs this to report the outcome.
/// </summary>
public class VariableCategoryPasteResult
{
    /// <summary>Names of the variables whose values were written to the target.</summary>
    public IReadOnlyList<string> AppliedVariableNames { get; }

    /// <summary>Names of the copied variables that were not written.</summary>
    public IReadOnlyList<string> SkippedVariableNames { get; }

    public VariableCategoryPasteResult(IReadOnlyList<string> appliedVariableNames, IReadOnlyList<string> skippedVariableNames)
    {
        AppliedVariableNames = appliedVariableNames;
        SkippedVariableNames = skippedVariableNames;
    }
}
