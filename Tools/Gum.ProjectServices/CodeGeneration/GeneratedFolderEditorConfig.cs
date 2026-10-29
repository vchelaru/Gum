using System;
using System.Collections.Generic;
using System.IO;
using ToolsUtilities;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Writes and removes the <c>.editorconfig</c> that marks the generated code folder as generated
/// code (<see cref="CodeOutputProjectSettings.MarkGeneratedCodeFolder"/>, #6776). Gum's file is told
/// apart from a user's by <see cref="HeaderLine"/>; a file without it is never edited, and a file Gum
/// wrote is only removed while it still matches <see cref="Contents"/> exactly. Shared by the tool
/// and <c>gumcli codegen</c>.
/// </summary>
public class GeneratedFolderEditorConfig
{
    /// <summary>The file name written into the generated code folder.</summary>
    public const string FileName = ".editorconfig";

    /// <summary>The first line of Gum's file; how Gum recognizes it later.</summary>
    public const string HeaderLine = "# Written by Gum. Marks the generated files in this folder as generated code.";

    /// <summary>The full text of Gum's file.</summary>
    public const string Contents = HeaderLine + "\r\n[*.cs]\r\ngenerated_code = true\r\n";

    private readonly CodeGenerationFileLocationsService _fileLocations;
    private readonly ICodeGenLogger _logger;
    private readonly ICodeFileWriter _writer;
    private readonly HashSet<string> _reported = new HashSet<string>();

    public GeneratedFolderEditorConfig(CodeGenerationFileLocationsService fileLocations, ICodeGenLogger logger,
        ICodeFileWriter? writer = null)
    {
        _fileLocations = fileLocations;
        _logger = logger;
        _writer = writer ?? new DirectCodeFileWriter(logger);
    }

    /// <summary>
    /// Why the option cannot apply to <paramref name="settings"/>, as one line for the settings row,
    /// or null when the generated folder is its own folder. A file in a shared folder would also mark
    /// the user's custom files.
    /// </summary>
    public static string? GetUnavailableReason(CodeOutputProjectSettings settings, string? projectDirectory)
    {
        string? root = Folder(projectDirectory, settings, string.Empty);
        if (root == null)
        {
            return "Set the Code Project Root first.";
        }
        if (string.IsNullOrEmpty(settings.GeneratedCodeFolder))
        {
            return "Needs a Generated Code Folder.";
        }

        FilePath generated = Folder(projectDirectory, settings, settings.GeneratedCodeFolder)!;
        if (generated == (FilePath)root)
        {
            return "The generated folder is the project root.";
        }

        string customSubfolder = string.IsNullOrEmpty(settings.CustomCodeFolder) ? settings.GeneratedCodeFolder : settings.CustomCodeFolder;
        FilePath custom = Folder(projectDirectory, settings, customSubfolder)!;
        if (custom == generated)
        {
            return "Needs a separate Custom Code Folder.";
        }
        if (generated.IsRootOf(custom))
        {
            return "The custom folder is inside the generated folder.";
        }
        return null;
    }

    private string? GeneratedFolder(CodeOutputProjectSettings settings) =>
        Folder(_fileLocations.ProjectDirectory, settings, settings.GeneratedCodeFolder);

    private static string? Folder(string? projectDirectory, CodeOutputProjectSettings settings, string subfolder)
    {
        string? folder = CodeGenerationFileLocationsService.ResolveFolder(projectDirectory, settings, subfolder);
        // Resolved to a full path first, so "." and ".." segments and doubled separators cannot hide that two
        // folders are the same or nested.
        return folder == null ? null : Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
    }

    /// <summary>
    /// Brings the file in line with <paramref name="current"/>. When <paramref name="previous"/> is given
    /// (a settings edit), the file Gum wrote for it is removed first if the option went off or the folder moved.
    /// </summary>
    public void Sync(CodeOutputProjectSettings current, CodeOutputProjectSettings? previous = null)
    {
        string? currentFolder = IsActive(current) ? GeneratedFolder(current) : null;

        if (previous != null && IsActive(previous))
        {
            string? previousFolder = GeneratedFolder(previous);
            if (previousFolder != null && (currentFolder == null || (FilePath)previousFolder != (FilePath)currentFolder))
            {
                Remove(previousFolder + FileName);
            }
        }

        if (currentFolder != null)
        {
            Write(currentFolder + FileName);
        }
        else if (current.MarkGeneratedCodeFolder)
        {
            Report("unavailable", $"Mark generated folder is on but skipped: {GetUnavailableReason(current, _fileLocations.ProjectDirectory)}");
        }
    }

    private bool IsActive(CodeOutputProjectSettings settings) =>
        settings.MarkGeneratedCodeFolder && GetUnavailableReason(settings, _fileLocations.ProjectDirectory) == null;

    private void Write(string path)
    {
        if (File.Exists(path))
        {
            string existing = TryRead(path) ?? string.Empty;
            if (IsGumFile(existing))
            {
                if (!Matches(existing))
                {
                    Report(path, $"{path} was edited, so Gum left it as it is.");
                }
                return;
            }
            Report(path, $"{path} already exists and was not written by Gum, so Gum left it as it is. " +
                "Add generated_code = true under [*.cs] yourself, or delete the file to let Gum write its own.");
            return;
        }
        if (_writer.TryWrite(path, Contents))
        {
            _logger.PrintOutput($"Wrote {path}");
        }
    }

    private void Remove(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }
        string? existing = TryRead(path);
        if (existing == null || !IsGumFile(existing) || !Matches(existing))
        {
            Report(path, $"{path} is no longer needed, but it was changed after Gum wrote it, so Gum left it as it is.");
            return;
        }
        try
        {
            File.Delete(path);
            _logger.PrintOutput($"Removed {path}");
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
        {
            _logger.PrintError($"Error removing {path}: {e.Message}");
        }
    }

    private static string? TryRead(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static bool IsGumFile(string text) =>
        text.Replace("\r\n", "\n").Split('\n')[0].TrimEnd() == HeaderLine;

    private static bool Matches(string text) =>
        text.Replace("\r\n", "\n") == Contents.Replace("\r\n", "\n");

    // Each message once per service, so a batch of elements or repeated edits do not repeat it.
    private void Report(string key, string message)
    {
        if (_reported.Add(key + message))
        {
            _logger.PrintOutput(message);
        }
    }
}
