namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Decides which Variables tab rows survive the tab's filter box (#4631). The rows themselves are WPF
/// types that cannot be referenced from this headless assembly, so the view side passes each row's names
/// in and applies the answer.
/// </summary>
public interface IVariableFilterService
{
    /// <summary>
    /// Whether <paramref name="filterText"/> is a real filter rather than a blank box. A blank box shows
    /// every row, so callers can skip filtering entirely instead of matching every row against nothing.
    /// </summary>
    bool HasFilter(string? filterText);

    /// <summary>
    /// Whether a row named <paramref name="variableName"/> (shown as <paramref name="displayName"/>, when
    /// that differs) should stay visible under <paramref name="filterText"/>. Matching is a
    /// case-insensitive substring test against either name; a blank filter matches every row.
    /// </summary>
    bool IsMatch(string? filterText, string variableName, string? displayName);
}
