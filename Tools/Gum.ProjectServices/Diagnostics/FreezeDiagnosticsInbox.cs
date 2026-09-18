using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Gum.Diagnostics;

/// <inheritdoc cref="IFreezeDiagnosticsInbox"/>
public sealed class FreezeDiagnosticsInbox : IFreezeDiagnosticsInbox
{
    private const string SentinelFileName = "session-running";
    private const string SuppressFileName = "do-not-prompt";
    private const string ReportedFolderName = "Reported";

    private readonly string _sentinelPath;
    private readonly string _suppressPath;

    public FreezeDiagnosticsInbox(string directory)
    {
        DirectoryPath = directory;
        _sentinelPath = Path.Combine(directory, SentinelFileName);
        _suppressPath = Path.Combine(directory, SuppressFileName);
    }

    /// <inheritdoc/>
    public string DirectoryPath { get; }

    /// <inheritdoc/>
    public bool ArePromptsSuppressed => File.Exists(_suppressPath);

    /// <inheritdoc/>
    public bool BeginSession()
    {
        bool wasDirty = File.Exists(_sentinelPath);
        Directory.CreateDirectory(DirectoryPath);
        File.WriteAllText(_sentinelPath, "");
        return wasDirty;
    }

    /// <inheritdoc/>
    public void EndSessionCleanly() => File.Delete(_sentinelPath);

    /// <inheritdoc/>
    public IReadOnlyList<string> GetUnreportedFiles()
    {
        if (!Directory.Exists(DirectoryPath))
        {
            return new List<string>();
        }

        return Directory.GetFiles(DirectoryPath)
            .Where(file => file != _sentinelPath && file != _suppressPath)
            .ToList();
    }

    /// <inheritdoc/>
    public void MarkReported(IEnumerable<string> files)
    {
        string reportedFolder = Path.Combine(DirectoryPath, ReportedFolderName);
        Directory.CreateDirectory(reportedFolder);
        foreach (string file in files)
        {
            File.Move(file, Path.Combine(reportedFolder, Path.GetFileName(file)), overwrite: true);
        }
    }

    /// <inheritdoc/>
    public void SuppressPrompts()
    {
        Directory.CreateDirectory(DirectoryPath);
        File.WriteAllText(_suppressPath, "Delete this file to be prompted about new freeze diagnostics again.");
    }
}
