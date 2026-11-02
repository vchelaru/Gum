using System;
using System.IO;
using Gum.Diagnostics;
using ToolsUtilities;

namespace Gum.Managers;

/// <summary>
/// Keeps the <see cref="ILastProjectLoadMarker"/> record as a small file in the per-user settings
/// folder. The file exists only while a last-project load is in progress; a crash leaves it behind.
/// </summary>
public class LastProjectLoadMarker : ILastProjectLoadMarker
{
    private const string MarkerFileName = "LoadingLastProject.txt";

    private readonly Func<string> _folder;

    /// <summary>Uses the per-user settings folder, read at each call so a <c>--user-data</c> override applies.</summary>
    public LastProjectLoadMarker() : this(() => FileManager.UserApplicationDataForThisApplication)
    {
    }

    /// <summary>Uses the folder <paramref name="folder"/> returns.</summary>
    public LastProjectLoadMarker(Func<string> folder)
    {
        _folder = folder;
    }

    private string MarkerPath => Path.Combine(_folder(), MarkerFileName);

    /// <inheritdoc/>
    public string? InterruptedProject
    {
        get
        {
            string path = MarkerPath;
            string? project = null;
            // Best effort: a marker another running Gum holds open reads as no interrupted load.
            BestEffortFile.TryRun(() => project = File.Exists(path) ? File.ReadAllText(path) : null);
            return project;
        }
    }

    /// <inheritdoc/>
    public void MarkStarted(string projectPath)
    {
        string path = MarkerPath;
        BestEffortFile.TryRun(() =>
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, projectPath);
        });
    }

    /// <inheritdoc/>
    public void Clear()
    {
        string path = MarkerPath;
        BestEffortFile.TryRun(() => File.Delete(path));
    }
}
