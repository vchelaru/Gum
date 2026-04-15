using Microsoft.Win32;
using System.Diagnostics;

namespace WpfDataUi.Controls;

/// <summary>
/// Shared file-picker plumbing for IDataUi controls that open an OpenFileDialog or
/// FolderBrowserDialog and optionally reveal the selected file in Windows Explorer.
/// Composed by controls such as FileSelectionDisplay and MultiFileDisplay, similar to
/// how TextBoxDisplayLogic is composed by text-based controls.
/// </summary>
public class FilePickingLogic
{
    /// <summary>
    /// OpenFileDialog filter string (e.g. "Localization Files (*.csv;*.resx)|*.csv;*.resx").
    /// Ignored when <see cref="IsFolderDialog"/> is true.
    /// </summary>
    public string Filter { get; set; } = string.Empty;

    /// <summary>
    /// When true, <see cref="ShowOpenDialog"/> uses a FolderBrowserDialog instead of an
    /// OpenFileDialog.
    /// </summary>
    public bool IsFolderDialog { get; set; }

    /// <summary>
    /// Optional base directory used when resolving relative paths for
    /// <see cref="ShowInExplorer"/>. Static so all pickers in the process share it.
    /// </summary>
    public static string FolderRelativeTo { get; set; } = string.Empty;

    /// <summary>
    /// Shows the open-file (or folder) dialog and returns the selected path, or null
    /// if the user cancelled.
    /// </summary>
    public string? ShowOpenDialog()
    {
        if (IsFolderDialog)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = fbd.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                return fbd.SelectedPath;
            }
            return null;
        }
        else
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = Filter;

            bool? shouldOpen = fileDialog.ShowDialog();
            if (shouldOpen.HasValue && shouldOpen.Value)
            {
                return fileDialog.FileName;
            }
            return null;
        }
    }

    /// <summary>
    /// Opens Windows Explorer and selects the given file. If the path is relative and
    /// <see cref="FolderRelativeTo"/> is set, the path is resolved against it first.
    /// No-op if the path is empty or the resolved file does not exist.
    /// </summary>
    public void ShowInExplorer(string fileToOpen)
    {
        if (string.IsNullOrEmpty(fileToOpen))
        {
            return;
        }

        if (!string.IsNullOrEmpty(FolderRelativeTo))
        {
            fileToOpen = RemoveDotDotSlash(FolderRelativeTo + fileToOpen).Replace("/", "\\");
        }

        if (System.IO.File.Exists(fileToOpen))
        {
            Process.Start("explorer.exe", "/select," + fileToOpen);
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
