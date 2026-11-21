using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Gum.Managers;
using Gum.Plugins;
using Gum.ToolStates;
using ToolsUtilities;

namespace Gum.Diagnostics;

/// <inheritdoc cref="IDiagnosticsContentSource"/>
public class DiagnosticsContentSource : IDiagnosticsContentSource
{
    private const int MaxToolLogs = 3;
    private const int MaxCrashLogs = 5;
    private const long MaxSettingsFileBytes = 512 * 1024;

    private readonly IProjectState _projectState;
    private readonly ISelectedState _selectedState;
    private readonly IErrorChecker _errorChecker;
    private readonly IPluginManager _pluginManager;
    private readonly ICrashReporter? _crashReporter;

    /// <summary>Creates the source over the tool's live state.</summary>
    public DiagnosticsContentSource(
        IProjectState projectState,
        ISelectedState selectedState,
        IErrorChecker errorChecker,
        IPluginManager pluginManager,
        ICrashReporter? crashReporter = null)
    {
        _projectState = projectState;
        _selectedState = selectedState;
        _errorChecker = errorChecker;
        _pluginManager = pluginManager;
        _crashReporter = crashReporter;
    }

    /// <inheritdoc/>
    public string? GumProjectFile
    {
        get
        {
            string? file = _projectState.GumProjectSave?.FullFileName;
            return string.IsNullOrEmpty(file) ? null : file;
        }
    }

    /// <inheritdoc/>
    public string? CSharpProjectFolder => CSharpProjectLocator.FindFolder(GumProjectFile);

    /// <inheritdoc/>
    public DiagnosticsContent Collect()
    {
        List<string> logs = new List<string>();
        logs.AddRange(Newest(ToolLog.LogFolder, "tool-*.log", MaxToolLogs));
        logs.AddRange(Newest(Path.Combine(Path.GetTempPath(), "GumPreview"), "preview*.log", 2));
        logs.AddRange(Newest(_crashReporter?.DirectoryPath ?? string.Empty, "*.txt", MaxCrashLogs));

        return new DiagnosticsContent(
            logs,
            CollectErrors(),
            CollectPlugins(),
            CollectSettingsFiles().ToList(),
            GumProjectFile,
            CSharpProjectFolder);
    }

    private string CollectErrors()
    {
        StringBuilder text = new StringBuilder();
        try
        {
            List<ErrorViewModel> errors = new List<ErrorViewModel>();
            if (_projectState.GumProjectSave is { } project)
            {
                errors.AddRange(_errorChecker.GetErrorsFor(_selectedState.SelectedElement, project));
                errors.AddRange(_errorChecker.GetProjectErrors(project));
            }
            errors.AddRange(_errorChecker.GetPluginErrors());

            text.AppendLine(errors.Count == 0 ? "No errors." : $"{errors.Count} error(s):");
            foreach (ErrorViewModel error in errors)
            {
                text.AppendLine(error.ClipboardText);
            }
        }
        catch (Exception exception)
        {
            text.AppendLine("The errors could not be collected: " + exception.Message);
        }
        return text.ToString();
    }

    private string CollectPlugins()
    {
        StringBuilder text = new StringBuilder();
        foreach (PluginSummary plugin in _pluginManager.GetAllPluginSummaries())
        {
            text.Append(plugin.DisplayText);
            text.Append(plugin.IsEnabled ? " (enabled)" : " (disabled)");
            if (!string.IsNullOrEmpty(plugin.SourceFile))
            {
                text.Append(" - ").Append(plugin.SourceFile);
            }
            text.AppendLine();
        }
        return text.ToString();
    }

    private static IEnumerable<string> CollectSettingsFiles()
    {
        string folder = FileManager.UserApplicationDataForThisApplication;
        if (!Directory.Exists(folder))
        {
            return Array.Empty<string>();
        }
        return Directory.GetFiles(folder)
            .Where(file => new FileInfo(file).Length <= MaxSettingsFileBytes && LooksLikeText(file))
            .ToList();
    }

    private static bool LooksLikeText(string file)
    {
        try
        {
            using FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            byte[] buffer = new byte[4096];
            int read = stream.Read(buffer, 0, buffer.Length);
            return Array.IndexOf(buffer, (byte)0, 0, read) < 0;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static IEnumerable<string> Newest(string folder, string pattern, int count)
    {
        if (!Directory.Exists(folder))
        {
            return Array.Empty<string>();
        }
        return Directory.GetFiles(folder, pattern)
            .OrderByDescending(file => File.GetLastWriteTimeUtc(file))
            .Take(count)
            .ToList();
    }
}
