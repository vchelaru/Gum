using System;
using System.Linq;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;

namespace Gum.Logic;

/// <summary>
/// Renames a behavior and everything that names it: the references on elements, the project's
/// behavior reference and the behavior's file. Elements reference a behavior by name, so a rename
/// that skipped any of them would leave a dangling reference. Shared by the Rename Behavior command
/// and by undo/redo of that rename (#7019), which are the same operation in opposite directions.
/// </summary>
public class BehaviorRenamer
{
    private readonly IReferenceFinder _referenceFinder;
    private readonly Func<GumProjectSave> _getProject;
    private readonly IFileCommands _fileCommands;
    private readonly IGuiCommands _guiCommands;

    public BehaviorRenamer(IReferenceFinder referenceFinder,
        Func<GumProjectSave> getProject,
        IFileCommands fileCommands,
        IGuiCommands guiCommands)
    {
        _referenceFinder = referenceFinder;
        _getProject = getProject;
        _fileCommands = fileCommands;
        _guiCommands = guiCommands;
    }

    /// <summary>Gives <paramref name="behavior"/> <paramref name="newName"/> and updates everything that names it.</summary>
    public void Rename(BehaviorSave behavior, string newName)
    {
        var oldName = behavior.Name;
        var oldFile = _fileCommands.GetFullPathXmlFile(behavior);

        // Update behavior references on all elements that reference the old name
        var references = _referenceFinder.GetReferencesToBehavior(behavior, oldName);
        foreach (var (_, reference) in references.ElementsWithBehaviorReference)
            reference.BehaviorName = newName;

        // Update the project-level BehaviorReference entry
        var projectRef = _getProject().BehaviorReferences
            .FirstOrDefault(r => r.Name == oldName);
        if (projectRef != null)
            projectRef.Name = newName;

        behavior.Name = newName;

        // Delete the old file and save under the new name
        if (oldFile?.Exists() == true)
            System.IO.File.Delete(oldFile.FullPath);

        _fileCommands.TryAutoSaveBehavior(behavior);
        _fileCommands.TryAutoSaveProject();

        // Save all elements whose behavior references changed
        foreach (var (container, _) in references.ElementsWithBehaviorReference)
            _fileCommands.TryAutoSaveElement(container);

        _guiCommands.RefreshElementTreeView();
    }
}
