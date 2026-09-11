namespace WpfDataUi.Controls;

/// <summary>
/// Opens file pickers and reveals files for the file-selection displayers. The tool implements it
/// over its dialog service, so both heads use their own native dialogs.
/// </summary>
public interface IDataUiFilePicker
{
    /// <summary>
    /// Shows an open-file dialog and returns the chosen path, or null if cancelled.
    /// <paramref name="filter"/> uses the "Description|*.ext" format.
    /// </summary>
    string? PickFile(string filter);

    /// <summary>Opens the OS file manager with <paramref name="filePath"/> selected.</summary>
    void RevealFile(string filePath);
}
