using System;
using System.Threading.Tasks;
using Gum.CommandLine;
using Gum.DataTypes;
using ToolsUtilities;

namespace Gum.Startup;

/// <summary>
/// Starts the serializer warm-up for the format of the project startup is about to open, on a pool
/// thread, so the setup overlaps plugin start-up instead of running inside the load. A project in
/// another format, or no project, starts nothing. A failing warm-up is logged, never thrown: the
/// load then does the setup itself.
/// </summary>
public class ProjectSerializerWarmupLauncher
{
    private readonly Action _warmJson;
    private readonly Action _warmXml;
    private readonly Action<Exception> _logFailure;
    private readonly Func<Action, Task> _runInBackground;

    /// <summary>Creates the launcher over the two warm-ups, the failure log and the way it runs work off the UI thread.</summary>
    public ProjectSerializerWarmupLauncher(Action warmJson, Action warmXml, Action<Exception> logFailure, Func<Action, Task> runInBackground)
    {
        _warmJson = warmJson;
        _warmXml = warmXml;
        _logFailure = logFailure;
        _runInBackground = runInBackground;
    }

    /// <summary>
    /// Starts the warm-up for the project startup will most likely open: the one the command line
    /// names, else the last project. A wrong guess (an OS open request, Shift held) only costs the
    /// pool thread's time. Returns the started work, or a completed task when nothing matched.
    /// </summary>
    public Task Start(string[] commandLineArgs, string? lastProject)
    {
        string? projectPath = lastProject;
        foreach (string arg in commandLineArgs)
        {
            if (!string.IsNullOrEmpty(arg) && CommandLineManager.TryResolveStartupFile(arg, out string? project, out _))
            {
                projectPath = project;
            }
        }

        string? extension = projectPath == null ? null : FileManager.GetExtension(projectPath);
        Action? warmUp = extension == GumProjectSave.ProjectJsonExtension ? _warmJson
            : extension == GumProjectSave.ProjectExtension ? _warmXml
            : null;
        if (warmUp == null)
        {
            return Task.CompletedTask;
        }

        return _runInBackground(() =>
        {
            try
            {
                warmUp();
            }
            catch (Exception exception)
            {
                _logFailure(exception);
            }
        });
    }
}
