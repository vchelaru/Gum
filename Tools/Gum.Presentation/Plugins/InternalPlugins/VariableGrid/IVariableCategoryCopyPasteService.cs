using System.Collections.Generic;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Copies the values of a whole Variables tab category (Font, Text, Position, ...) and pastes them onto
/// another object, so matching one object to another does not require retyping each variable.
/// </summary>
public interface IVariableCategoryCopyPasteService
{
    /// <summary>The most recently copied category, or null if nothing has been copied this session.</summary>
    CopiedVariableCategory? CopiedCategory { get; }

    /// <summary>Captures the effective values of the supplied rows, replacing anything previously copied.</summary>
    void Copy(string categoryName, IEnumerable<IVariableCategoryRow> rows);

    /// <summary>
    /// Writes the copied values onto the supplied rows, matching by variable name and recording the whole
    /// group as a single undo. Returns an empty result if nothing has been copied.
    /// </summary>
    VariableCategoryPasteResult Paste(IEnumerable<IVariableCategoryRow> targetRows);
}
