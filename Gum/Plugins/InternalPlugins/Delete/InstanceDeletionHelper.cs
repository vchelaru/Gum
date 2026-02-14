using System;
using System.Collections.Generic;
using System.Linq;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.ToolCommands;

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

        _deleteLogic.RemoveParentReferencesToInstance(instance, instance.ParentContainer);
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
            // to do a full removal, just remove parent references:
            if (parentContainer.Instances.Contains(child))
            {
                _deleteLogic.RemoveInstance(child, parentContainer);
            }
            else
            {
                _deleteLogic.RemoveParentReferencesToInstance(child, parentContainer);
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
}
