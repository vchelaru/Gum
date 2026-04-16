using Gum.ProjectServices.CodeGeneration;
using Gum.ToolStates;

namespace CodeOutputPlugin.Manager;

/// <summary>
/// Adapts the Gum tool's <see cref="IProjectState"/> to the engine-side
/// <see cref="IProjectDirectoryProvider"/>. Because IProjectState.ProjectDirectory
/// is computed on each call, services consuming this provider automatically see
/// the current project directory after the user switches projects.
/// </summary>
internal class ProjectStateDirectoryProvider : IProjectDirectoryProvider
{
    private readonly IProjectState _projectState;

    public ProjectStateDirectoryProvider(IProjectState projectState)
    {
        _projectState = projectState;
    }

    /// <inheritdoc/>
    public string? ProjectDirectory => _projectState.ProjectDirectory;
}
