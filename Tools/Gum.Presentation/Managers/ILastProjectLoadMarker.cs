namespace Gum.Managers;

/// <summary>
/// Remembers that the tool started reopening the last project, so the next launch can tell when
/// that load never finished (the tool crashed or was killed) and skip reopening it.
/// </summary>
public interface ILastProjectLoadMarker
{
    /// <summary>
    /// The project whose load started but never finished, or null if the last load finished
    /// (or none was started).
    /// </summary>
    string? InterruptedProject { get; }

    /// <summary>Records that loading <paramref name="projectPath"/> has started.</summary>
    void MarkStarted(string projectPath);

    /// <summary>Clears the record, so the next launch reopens the last project normally.</summary>
    void Clear();
}
