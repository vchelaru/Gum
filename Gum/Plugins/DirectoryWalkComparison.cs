using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ToolsUtilities;

namespace Gum.Plugins;

/// <summary>
/// Compares <see cref="FileManager.GetAllFilesInDirectory(string, string)"/> against the framework's
/// own recursive search over the same folder, and reports where they disagree.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="FileManager"/>'s hand-rolled walk has been seen returning nothing for a folder holding
/// dozens of matching files, while <see cref="Directory.EnumerateFiles(string, string, SearchOption)"/>
/// found them all on the same machine moments later. The cause is not understood and does not
/// reproduce on a developer machine, so this narrows it down where it actually happens.
/// </para>
/// <para>
/// This matters well beyond plugin loading: <see cref="FileManager"/> is compiled into every Gum
/// runtime through <c>GumCoreShared.projitems</c>, and its <c>FindAndAddExtension</c> and
/// extension-resolving <c>FileExists</c> both rely on this walk. A game on an affected machine
/// would fail to find content files that exist.
/// </para>
/// </remarks>
internal class DirectoryWalkComparison
{
    /// <summary>
    /// Returns a description of how the two walks differ over <paramref name="folder"/>, or null
    /// when they agree — which is the normal case and worth no words at all.
    /// </summary>
    public string? Compare(string folder, string extension)
    {
        List<string> frameworkFiles;
        List<string> fileManagerFiles;

        try
        {
            frameworkFiles = Directory
                .EnumerateFiles(folder, "*." + extension, SearchOption.AllDirectories)
                .Where(x => x.EndsWith("." + extension, StringComparison.OrdinalIgnoreCase))
                .ToList();
            fileManagerFiles = FileManager.GetAllFilesInDirectory(folder, extension);
        }
        catch (Exception exception)
        {
            return $"Could not compare directory walks for '{folder}': {exception.Message}";
        }

        if (frameworkFiles.Count == fileManagerFiles.Count)
        {
            return null;
        }

        StringBuilder text = new();
        text.AppendLine($"FileManager.GetAllFilesInDirectory found {fileManagerFiles.Count} " +
            $".{extension} files where Directory.EnumerateFiles found {frameworkFiles.Count}. " +
            "This is a bug in Gum, not in your setup. The detail below is for a bug report:");

        AppendFolderProbe(text, folder, "    top level");

        // The walk recurses one folder at a time, so the first subfolder is where it either kept
        // going or quietly stopped.
        string? firstSubfolder = TryFirst(() => Directory.EnumerateDirectories(folder));
        if (firstSubfolder is null)
        {
            text.AppendLine("    no subfolders were returned at the top level");
        }
        else
        {
            AppendFolderProbe(text, firstSubfolder, "    first subfolder");
        }

        // The per-file match is a string comparison against FileManager's own extension parser, so
        // what it makes of a real path matters as much as whether the file was reached.
        string? firstFile = frameworkFiles.FirstOrDefault();
        if (firstFile is not null)
        {
            text.AppendLine($"    a file the framework found: {firstFile}");
            text.AppendLine($"    FileManager.GetExtension of it: " +
                $"'{TryGet(() => FileManager.GetExtension(firstFile))}' (expected '{extension}')");
        }

        return text.ToString();
    }

    private static void AppendFolderProbe(StringBuilder text, string folder, string label)
    {
        text.AppendLine($"{label}: {folder}");
        text.AppendLine($"{label} exists: {TryGet(() => Directory.Exists(folder).ToString())}");
        text.AppendLine($"{label} GetFiles: {TryGet(() => Directory.GetFiles(folder).Length.ToString())}");
        text.AppendLine($"{label} GetDirectories: " +
            $"{TryGet(() => Directory.GetDirectories(folder).Length.ToString())}");
    }

    private static string TryGet(Func<string> get)
    {
        try
        {
            return get();
        }
        catch (Exception exception)
        {
            return $"threw {exception.GetType().Name}: {exception.Message}";
        }
    }

    private static string? TryFirst(Func<IEnumerable<string>> get)
    {
        try
        {
            return get().FirstOrDefault();
        }
        catch (Exception)
        {
            return null;
        }
    }
}
