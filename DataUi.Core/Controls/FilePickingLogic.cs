using System;
using System.Collections.Generic;
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
    /// When true, <see cref="ShowOpenDialog"/> shows a folder-browse dialog instead of an
    /// open-file dialog.
    /// </summary>
    public bool IsFolderDialog { get; set; }

    /// <summary>
    /// Optional base directory used when resolving relative paths for
    /// <see cref="ShowInExplorer"/>. Static so all pickers in the process share it.
    /// </summary>
    public static string FolderRelativeTo { get; set; } = string.Empty;

    /// <summary>
    /// This picker's own base folder for resolving relative paths in <see cref="ShowInExplorer"/>,
    /// read on every call. When it is unset or returns null, <see cref="FolderRelativeTo"/> is used.
    /// </summary>
    public Func<string?>? RelativeToProvider { get; set; }

    /// <summary>
    /// Opens the dialogs for every picker in the process. Assigned once at tool startup; with none
    /// assigned the pickers do nothing.
    /// </summary>
    public static IDataUiFilePicker? FilePicker { get; set; }

    /// <summary>
    /// Shows the open-file (or folder-browse, if <see cref="IsFolderDialog"/>) dialog and returns
    /// the selected path, or null if the user cancelled.
    /// </summary>
    public string? ShowOpenDialog()
    {
        return IsFolderDialog ? FilePicker?.PickFolder() : FilePicker?.PickFile(Filter);
    }

    /// <summary>
    /// Opens the OS file manager and selects the given file or folder. A relative path is resolved
    /// against <see cref="RelativeToProvider"/>, or else <see cref="FolderRelativeTo"/>, first. No-op if the path is empty or nothing exists
    /// at the resolved path.
    /// </summary>
    public void ShowInExplorer(string fileToOpen)
    {
        if (string.IsNullOrEmpty(fileToOpen))
        {
            return;
        }

        string relativeTo = RelativeToProvider?.Invoke() ?? FolderRelativeTo;
        if (!string.IsNullOrEmpty(relativeTo) && !Path.IsPathRooted(fileToOpen))
        {
            fileToOpen = RemoveDotDotSlash(relativeTo + fileToOpen);
        }

        if (File.Exists(fileToOpen))
        {
            FilePicker?.RevealFile(fileToOpen);
        }
        else if (Directory.Exists(fileToOpen))
        {
            // A trailing separator makes the file manager open the folder instead of selecting it.
            FilePicker?.RevealFile(fileToOpen.TrimEnd('/', '\\'));
        }
    }

    /// <summary>
    /// Normalizes a path to "/" separators and collapses ".", ".." and empty segments. Same rules as
    /// ToolsUtilities.FileManager.RemoveDotDotSlash (a test ties the two together): leading separators are kept,
    /// a ".." above a root is dropped, a ".." a relative path cannot resolve stays, a leading "./" stays, and a
    /// URL is returned unchanged. This is a copy because DataUi.Core cannot reference ToolsUtilities: GumCommon
    /// compiles the same sources, so a consumer of both would see every type twice.
    /// </summary>
    public static string RemoveDotDotSlash(string fileNameToFix)
    {
        fileNameToFix = fileNameToFix.Replace('\\', '/');

        if (fileNameToFix.StartsWith("http:", StringComparison.Ordinal) ||
            fileNameToFix.StartsWith("https:", StringComparison.Ordinal))
        {
            return fileNameToFix;
        }

        int prefixLength = 0;
        while (prefixLength < fileNameToFix.Length && fileNameToFix[prefixLength] == '/')
        {
            prefixLength++;
        }
        bool isRooted = prefixLength > 0;
        string[] segments = fileNameToFix.Substring(prefixLength).Split('/');

        List<string> kept = new List<string>(segments.Length);
        // The leading "./" marker or a drive ("C:") is never removed by a "..".
        int unremovableCount = 0;
        bool endsInDirectory = false;
        for (int i = 0; i < segments.Length; i++)
        {
            string segment = segments[i];
            endsInDirectory = false;
            if (segment.Length == 0)
            {
                endsInDirectory = true;
            }
            else if (segment == ".")
            {
                if (i == 0 && !isRooted)
                {
                    kept.Add(segment);
                    unremovableCount = 1;
                }
                else
                {
                    endsInDirectory = true;
                }
            }
            else if (segment == "..")
            {
                if (kept.Count > unremovableCount && kept[kept.Count - 1] != "..")
                {
                    kept.RemoveAt(kept.Count - 1);
                    endsInDirectory = true;
                }
                else if (isRooted)
                {
                    endsInDirectory = true;
                }
                else
                {
                    kept.Add(segment);
                }
            }
            else
            {
                if (i == 0 && !isRooted && segment.Length == 2 && segment[1] == ':')
                {
                    isRooted = true;
                    unremovableCount = 1;
                }
                kept.Add(segment);
            }
        }

        string result = fileNameToFix.Substring(0, prefixLength) + string.Join("/", kept);
        if (endsInDirectory && kept.Count > 0)
        {
            result += "/";
        }
        return result;
    }
}
