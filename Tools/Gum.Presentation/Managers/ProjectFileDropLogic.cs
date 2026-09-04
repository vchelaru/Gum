using Gum.Commands;
using Gum.DataTypes;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Managers;

/// <summary>
/// Decides whether a set of dropped files is a request to open a Gum project, and opens it. Backs
/// the app-wide drop handler on the main window, so dropping a .gumx/.gumj anywhere in the tool
/// behaves like File &gt; Load Project.
/// </summary>
public interface IProjectFileDropLogic
{
    /// <summary>
    /// The project file that a drop of <paramref name="droppedFiles"/> would open, or null if the
    /// drop carries no project file. A pure query - safe to call repeatedly while dragging.
    /// </summary>
    string? GetProjectFileToOpen(IEnumerable<string>? droppedFiles);

    /// <summary>
    /// Opens the project among <paramref name="droppedFiles"/>, if there is one. Returns true when
    /// the drop was consumed, so the caller can stop it from reaching the control underneath.
    /// </summary>
    bool TryOpenDroppedProject(IEnumerable<string>? droppedFiles);
}

/// <inheritdoc cref="IProjectFileDropLogic"/>
public class ProjectFileDropLogic : IProjectFileDropLogic
{
    private readonly IFileCommands _fileCommands;

    public ProjectFileDropLogic(IFileCommands fileCommands)
    {
        _fileCommands = fileCommands;
    }

    /// <inheritdoc/>
    public string? GetProjectFileToOpen(IEnumerable<string>? droppedFiles) =>
        droppedFiles?.FirstOrDefault(GumProjectSave.IsProjectFile);

    /// <inheritdoc/>
    public bool TryOpenDroppedProject(IEnumerable<string>? droppedFiles)
    {
        if (GetProjectFileToOpen(droppedFiles) is not { } projectFile)
        {
            return false;
        }

        _fileCommands.LoadProject(projectFile);
        return true;
    }
}
