using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Gum.Commands;
using Gum.ToolStates;

namespace Gum.Startup;

/// <inheritdoc cref="IProjectOpenRequestRouter"/>
public class ProjectOpenRequestRouter : IProjectOpenRequestRouter
{
    private readonly ISelectedState _selectedState;
    // Lazy: FileCommands depends on ProjectManager, which reads this router during startup.
    private readonly Lazy<IFileCommands> _fileCommands;
    private string? _pendingProject;
    private bool _isStartupComplete;

    /// <summary>Creates the router.</summary>
    public ProjectOpenRequestRouter(ISelectedState selectedState, Lazy<IFileCommands> fileCommands)
    {
        _selectedState = selectedState;
        _fileCommands = fileCommands;
        _pendingProject = null;
        _isStartupComplete = false;
    }

    /// <inheritdoc/>
    public Task RequestOpenAsync(IEnumerable<string> paths)
    {
        string? project = paths.FirstOrDefault(IsProjectFile);
        if (project == null)
        {
            return Task.CompletedTask;
        }

        if (!_isStartupComplete)
        {
            _pendingProject = project;
            return Task.CompletedTask;
        }

        return OpenAsync(project);
    }

    /// <inheritdoc/>
    public string? TakePendingStartupProject()
    {
        string? project = _pendingProject;
        _pendingProject = null;
        return project;
    }

    /// <inheritdoc/>
    public Task CompleteStartupAsync()
    {
        _isStartupComplete = true;
        return TakePendingStartupProject() is { } project
            ? OpenAsync(project)
            : Task.CompletedTask;
    }

    // Same steps as File > Load Project.
    private Task OpenAsync(string project)
    {
        _selectedState.SelectedInstance = null;
        _selectedState.SelectedElement = null;
        return _fileCommands.Value.LoadProjectAsync(project);
    }

    private static bool IsProjectFile(string path)
    {
        string extension = Path.GetExtension(path);
        return string.Equals(extension, ".gumx", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".gumj", StringComparison.OrdinalIgnoreCase);
    }
}
