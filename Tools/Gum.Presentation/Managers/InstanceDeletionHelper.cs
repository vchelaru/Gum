using System;
using System.Collections.Generic;
using System.Linq;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Managers;
using ToolsUtilities;

namespace Gum.Gui.Plugins;

/// <summary>
/// Helper class that encapsulates logic for deleting instances and their children.
/// This separation makes it easier to reuse deletion logic across single and multi-select scenarios.
/// </summary>
public class InstanceDeletionHelper
{
    private readonly IDeleteLogic _deleteLogic;
    private readonly IGuiCommands _guiCommands;
    private readonly IWireframeCommands _wireframeCommands;
    private readonly IFileCommands _fileCommands;

    public InstanceDeletionHelper(
        IDeleteLogic deleteLogic,
        IGuiCommands guiCommands,
        IWireframeCommands wireframeCommands,
        IFileCommands fileCommands)
    {
        _deleteLogic = deleteLogic;
        _guiCommands = guiCommands;
        _wireframeCommands = wireframeCommands;
        _fileCommands = fileCommands;
    }

    /// <summary>
    /// Checks if an instance has any child instances (instances that have this instance as their parent).
    /// </summary>
    public bool InstanceHasChildren(InstanceSave? instance)
    {
        if (instance?.ParentContainer == null)
            return false;

        var parentContainer = instance.ParentContainer;
        var allVariables = parentContainer.AllStates.SelectMany(item => item.Variables);
        var instanceName = instance.Name;

        return allVariables.Any(item =>
            item.SetsValue &&
            item.Value != null &&
            item.Value is string asString &&
            (asString == instanceName || asString.StartsWith(instanceName + ".")) &&
            item.GetRootName() == "Parent");
    }

    /// <summary>
    /// Checks if any instance in the collection has child instances.
    /// This is useful for determining whether to show the "delete children" option for multi-select scenarios.
    /// </summary>
    public bool AnyInstanceHasChildren(IEnumerable<InstanceSave> instances)
    {
        return instances.Any(InstanceHasChildren);
    }

    /// <summary>
    /// Returns all direct children of the specified instance (instances that have this instance as their parent).
    /// </summary>
    public InstanceSave[] GetChildrenOf(InstanceSave? instance)
    {
        if (instance?.ParentContainer == null)
            return System.Array.Empty<InstanceSave>();

        var container = instance.ParentContainer;
        var defaultState = container.DefaultState;

        if (defaultState == null)
            return System.Array.Empty<InstanceSave>();

        var variablesUsingInstanceAsParent = defaultState.Variables
            .Where(item =>
                item.Value is string asString &&
                (asString == instance.Name || asString.StartsWith(instance.Name + ".")) &&
                item.SetsValue &&
                item.GetRootName() == "Parent");

        var instanceNames = variablesUsingInstanceAsParent
            .Select(item => item.SourceObject)
            .Distinct()
            .ToArray();

        List<InstanceSave> instanceSaveList = new List<InstanceSave>();

        foreach (var instanceName in instanceNames)
        {
            var childInstance = container.GetInstance(instanceName);

            if (childInstance != null)
            {
                instanceSaveList.Add(childInstance);
            }
        }

        return instanceSaveList.ToArray();
    }

    /// <summary>
    /// Detaches all children from the specified instance by removing parent references,
    /// but does not delete the children themselves.
    /// </summary>
    public void DetachChildrenFromInstance(InstanceSave? instance)
    {
        if (instance?.ParentContainer == null)
            return;

        _deleteLogic.RemoveReferencesToInstance(instance, instance.ParentContainer);
    }

    /// <summary>
    /// Recursively deletes all children of the specified instance, starting from the bottom of the hierarchy.
    /// </summary>
    public void RecursivelyDeleteChildrenOf(InstanceSave? instance)
    {
        if (instance?.ParentContainer == null)
            return;

        var childrenOfInstance = GetChildrenOf(instance);
        var parentContainer = instance.ParentContainer;

        foreach (var child in childrenOfInstance)
        {
            // we want to do this bottom up, so go recursively first.
            RecursivelyDeleteChildrenOf(child);

            // This child may have been removed by the main Delete command. If so, then no need
            // to do a full removal, just remove references to it:
            if (parentContainer.Instances.Contains(child))
            {
                _deleteLogic.RemoveInstance(child, parentContainer);
            }
            else
            {
                _deleteLogic.RemoveReferencesToInstance(child, parentContainer);
            }
        }
    }

    /// <summary>
    /// Detaches children from multiple instances, useful for multi-select deletion scenarios.
    /// </summary>
    public void DetachChildrenFromInstances(IEnumerable<InstanceSave> instances)
    {
        foreach (var instance in instances)
        {
            DetachChildrenFromInstance(instance);
        }
    }

    /// <summary>
    /// Recursively deletes all children of multiple instances, useful for multi-select deletion scenarios.
    /// Refreshes the UI once after all deletions are complete.
    /// </summary>
    public void RecursivelyDeleteChildrenOfInstances(IEnumerable<InstanceSave> instances, ElementSave parentElement)
    {
        foreach (var instance in instances)
        {
            RecursivelyDeleteChildrenOf(instance);
        }

        // Refresh UI once after all deletions
        _guiCommands.RefreshElementTreeView(parentElement);
        _wireframeCommands.Refresh();
        _fileCommands.TryAutoSaveElement(parentElement);
    }

    /// <summary>
    /// Detaches and/or recursively deletes the children of the given instances, according to
    /// the caller's chosen delete-children option. Instances are grouped by parent container
    /// when deleting children, since a multi-select delete may span multiple elements.
    /// </summary>
    public void PerformMultipleInstancesDelete(
        IEnumerable<InstanceSave> instances,
        bool shouldDetachChildren,
        bool shouldDeleteChildren)
    {
        var instancesList = instances.ToList();
        if (instancesList.Count == 0)
        {
            return;
        }

        if (shouldDetachChildren)
        {
            DetachChildrenFromInstances(instancesList);
        }
        if (shouldDeleteChildren)
        {
            // Group by parent container since instances may belong to different elements
            var instancesByParent = instancesList
                .GroupBy(i => i.ParentContainer)
                .Where(g => g.Key != null);

            foreach (var group in instancesByParent)
            {
                RecursivelyDeleteChildrenOfInstances(group.ToList(), group.Key);
            }
        }
    }

    /// <summary>
    /// Returns the full path to the XML file backing the given object (an <see cref="ElementSave"/>
    /// or <see cref="BehaviorSave"/>), or null for an <see cref="InstanceSave"/> (which has no XML
    /// file of its own).
    /// </summary>
    public FilePath? GetFileNameForObject(object deletedObject)
    {
        return deletedObject switch
        {
            ElementSave elementSave => _fileCommands.GetFullPathXmlFile(elementSave, elementSave.Name),
            BehaviorSave behaviorSave => _fileCommands.GetFullPathXmlFile(behaviorSave),
            InstanceSave => null,
            _ => throw new NotImplementedException($"Unsupported object type: {deletedObject?.GetType().Name}")
        };
    }

    /// <summary>
    /// Decides whether the "Delete XML file?" option should be offered for the given object being
    /// deleted. Instances have no XML file of their own. An element only offers this option when
    /// its name is not shared by another element in the project (e.g. duplicates added directly to
    /// the .gumx) — deleting the shared XML file in that case would remove the base file out from
    /// under any surviving duplicate.
    /// </summary>
    public bool ShouldOfferDeleteXmlOption(object objectToDelete)
    {
        if (objectToDelete is InstanceSave)
        {
            return false;
        }

        if (objectToDelete is ElementSave elementSave)
        {
            var numberOfMatches = ObjectFinder.Self.GumProjectSave?.AllElements
                .Count(item => item.Name == elementSave.Name) ?? 0;
            return numberOfMatches < 2;
        }

        return true;
    }
}
