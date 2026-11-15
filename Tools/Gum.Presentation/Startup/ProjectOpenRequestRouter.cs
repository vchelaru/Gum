using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Startup.SingleInstance;
using Gum.ToolStates;
using ToolsUtilities;

namespace Gum.Startup;

/// <inheritdoc cref="IProjectOpenRequestRouter"/>
public class ProjectOpenRequestRouter : IProjectOpenRequestRouter
{
    private readonly ISelectedState _selectedState;
    // Lazy: FileCommands depends on ProjectManager, which reads this router during startup.
    private readonly Lazy<IFileCommands> _fileCommands;
    private readonly Lazy<IProjectState> _projectState;
    private string? _pendingProject;
    private string? _pendingElement;
    private bool _isStartupComplete;

    /// <inheritdoc/>
    public event Action<string?>? ProjectPathChanged;

    /// <inheritdoc/>
    public void NotifyProjectPathChanged(string? projectPath) => ProjectPathChanged?.Invoke(projectPath);

    /// <summary>Creates the router.</summary>
    public ProjectOpenRequestRouter(ISelectedState selectedState, Lazy<IFileCommands> fileCommands, Lazy<IProjectState> projectState)
    {
        _selectedState = selectedState;
        _fileCommands = fileCommands;
        _projectState = projectState;
        _pendingProject = null;
        _pendingElement = null;
        _isStartupComplete = false;
    }

    /// <inheritdoc/>
    public Task RequestOpenAsync(IEnumerable<string> paths)
    {
        string? project = paths.FirstOrDefault(IsProjectFile);
        return project == null ? Task.CompletedTask : RequestOpenAsync(new OpenRequest(project, null));
    }

    /// <inheritdoc/>
    public Task RequestOpenAsync(OpenRequest request)
    {
        if (!IsProjectFile(request.Project))
        {
            return Task.CompletedTask;
        }

        if (!_isStartupComplete)
        {
            _pendingProject = request.Project;
            _pendingElement = request.Element;
            return Task.CompletedTask;
        }

        return OpenAsync(request.Project, request.Element);
    }

    /// <inheritdoc/>
    public string? TakePendingStartupProject()
    {
        // The element stays: it is selected once the startup load has finished.
        string? project = _pendingProject;
        _pendingProject = null;
        return project;
    }

    /// <inheritdoc/>
    public async Task CompleteStartupAsync()
    {
        _isStartupComplete = true;
        string? element = _pendingElement;
        _pendingElement = null;
        if (TakePendingStartupProject() is { } project)
        {
            await OpenAsync(project, element);
        }
        else if (element != null)
        {
            SelectElement(element);
        }
    }

    // Same steps as File > Load Project, except that the project already open is not loaded again.
    private async Task OpenAsync(string project, string? element)
    {
        if (!IsLoaded(project))
        {
            _selectedState.SelectedInstance = null;
            _selectedState.SelectedElement = null;
            await _fileCommands.Value.LoadProjectAsync(project);
        }

        if (element != null)
        {
            SelectElement(element);
        }
    }

    private bool IsLoaded(string project) =>
        _projectState.Value.GumProjectSave?.FullFileName is { Length: > 0 } loaded &&
        new FilePath(loaded) == new FilePath(Path.GetFullPath(project));

    private void SelectElement(string elementName)
    {
        if (ObjectFinder.Self.GetElementSave(elementName) is { } element)
        {
            _selectedState.SelectedInstance = null;
            _selectedState.SelectedElement = element;
        }
    }

    private static bool IsProjectFile(string path)
    {
        string extension = Path.GetExtension(path);
        return string.Equals(extension, ".gumx", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".gumj", StringComparison.OrdinalIgnoreCase);
    }
}
