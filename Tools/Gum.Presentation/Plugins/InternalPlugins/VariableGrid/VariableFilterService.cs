using System;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <inheritdoc cref="IVariableFilterService"/>
/// <remarks>
/// Substring rather than fuzzy matching: the Variables tab is tens of rows, not a command palette, so
/// fuzzy ranking would buy nothing and make results feel unpredictable.
/// </remarks>
public class VariableFilterService : IVariableFilterService
{
    /// <inheritdoc/>
    public bool HasFilter(string? filterText) => !string.IsNullOrWhiteSpace(filterText);

    /// <inheritdoc/>
    public bool IsMatch(string? filterText, string variableName, string? displayName)
    {
        if (!HasFilter(filterText))
        {
            return true;
        }

        // Typing leaves a trailing space often enough that not trimming reads as the filter breaking.
        string trimmed = filterText!.Trim();

        // An instance's rows are named "Instance.Variable"; the instance part would match every row.
        string unqualifiedName = variableName.Substring(variableName.LastIndexOf('.') + 1);

        if (unqualifiedName.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return displayName?.Contains(trimmed, StringComparison.OrdinalIgnoreCase) == true;
    }
}
