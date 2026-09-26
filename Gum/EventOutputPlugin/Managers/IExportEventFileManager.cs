namespace EventOutputPlugin.Managers;

/// <summary>
/// Records element, instance and state events to the open project's <c>EventExport/gum_events.json</c>.
/// </summary>
public interface IExportEventFileManager
{
    /// <summary>
    /// Appends an event for the current user and saves the file. Does nothing when no project is open.
    /// </summary>
    void ExportEvent(string? newName, string? oldName, GumEventTypes eventType, string? elementType);

    /// <summary>
    /// Removes events older than the retention window and saves the file.
    /// </summary>
    void DeleteOldEventFiles();
}
