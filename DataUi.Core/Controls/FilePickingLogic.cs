using System.IO;

namespace WpfDataUi.Controls;

/// <summary>
/// Shared file-picker plumbing for IDataUi controls that open a file picker and optionally reveal
/// the selected file in the OS file manager. Composed by controls such as FileSelectionDisplay and
/// MultiFileDisplay, similar to how TextBoxDisplayLogic is composed by text-based controls. The
/// dialogs themselves go through <see cref="FilePicker"/>.
/// </summary>
public class FilePickingLogic
{
    /// <summary>
    /// File-dialog filter string (e.g. "Localization Files (*.csv;*.resx)|*.csv;*.resx").
    /// </summary>
    public string Filter { get; set; } = string.Empty;

    /// <summary>
    /// Optional base directory used when resolving relative paths for
    /// <see cref="ShowInExplorer"/>. Static so all pickers in the process share it.
    /// </summary>
    public static string FolderRelativeTo { get; set; } = string.Empty;

    /// <summary>
    /// Opens the dialogs for every picker in the process. Assigned once at tool startup; with none
    /// assigned the pickers do nothing.
    /// </summary>
    public static IDataUiFilePicker? FilePicker { get; set; }

    /// <summary>
    /// Shows the open-file dialog and returns the selected path, or null if the user cancelled.
    /// </summary>
    public string? ShowOpenDialog()
    {
        return FilePicker?.PickFile(Filter);
    }

    /// <summary>
    /// Opens the OS file manager and selects the given file. If <see cref="FolderRelativeTo"/> is
    /// set, the path is resolved against it first. No-op if the path is empty or the resolved file
    /// does not exist.
    /// </summary>
    public void ShowInExplorer(string fileToOpen)
    {
        if (string.IsNullOrEmpty(fileToOpen))
        {
            return;
        }

        if (!string.IsNullOrEmpty(FolderRelativeTo))
        {
            fileToOpen = RemoveDotDotSlash(FolderRelativeTo + fileToOpen);
        }

        if (File.Exists(fileToOpen))
        {
            FilePicker?.RevealFile(fileToOpen);
        }
    }

    /// <summary>
    /// Normalizes a path by collapsing any "../" segments against the preceding
    /// directory. Pure helper, no state.
    /// </summary>
    public static string RemoveDotDotSlash(string fileNameToFix)
    {
        if (fileNameToFix.Contains(".."))
        {
            fileNameToFix = fileNameToFix.Replace("\\", "/");

            // First let's get rid of any ..'s that are in the middle
            // for example:
            //
            // "content/zones/area1/../../background/outdoorsanim/outdoorsanim.achx"
            //
            // would become
            //
            // "content/background/outdoorsanim/outdoorsanim.achx"

            int indexOfNextDotDotSlash = fileNameToFix.IndexOf("../");

            bool shouldLoop = indexOfNextDotDotSlash > 0;

            while (shouldLoop)
            {
                int indexOfPreviousDirectory = fileNameToFix.LastIndexOf('/', indexOfNextDotDotSlash - 2, indexOfNextDotDotSlash - 2);

                fileNameToFix = fileNameToFix.Remove(indexOfPreviousDirectory + 1, indexOfNextDotDotSlash - indexOfPreviousDirectory + 2);

                indexOfNextDotDotSlash = fileNameToFix.IndexOf("../");

                shouldLoop = indexOfNextDotDotSlash > 0;
            }
        }

        return fileNameToFix.Replace("\\", "/");
    }
}
