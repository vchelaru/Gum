using System.Collections.Generic;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;

namespace Gum.Logic;

// These reference-result data types (returned by IReferenceFinder) were relocated out of the
// tool assembly's Gum/Logic/RenameLogic.cs into the headless Gum.Presentation assembly
// (ADR-0005 Phase 3) so the IReferenceFinder port can live headless alongside the other
// relocated rename-change types (see RenameChangeTypes.cs). They are framework-neutral
// (GumDataTypes only). The namespace is intentionally kept as Gum.Logic so the many tool-side
// consumers compile unchanged.

#region ElementReferences Class

public class ElementReferences
{
    // Screens or components where BaseType == oldName
    public List<ElementSave> ElementsWithBaseTypeReference = new();
    // Instances in any element where BaseType == oldName
    public List<(ElementSave Container, InstanceSave Instance)> InstancesWithBaseTypeReference = new();
    // ContainedType variables whose value == oldName
    public List<(ElementSave Container, VariableSave Variable)> ContainedTypeVariableReferences = new();
    // VariableReferences list entries whose right-hand side references oldName
    public List<VariableReferenceChange> VariableReferenceChanges = new();

    public string GetChangesDetails()
    {
        var details = string.Empty;

        if (ElementsWithBaseTypeReference.Count > 0)
        {
            if (!string.IsNullOrEmpty(details)) details += "\n\n";
            details += "The following elements will have their base type updated:";
            foreach (var element in ElementsWithBaseTypeReference)
            {
                details += $"\n• {element.Name}";
            }
        }

        if (InstancesWithBaseTypeReference.Count > 0)
        {
            if (!string.IsNullOrEmpty(details)) details += "\n\n";
            details += "The following instances will have their base type updated:";
            foreach (var (container, instance) in InstancesWithBaseTypeReference)
            {
                details += $"\n• {instance.Name} in {container.Name}";
            }
        }

        if (ContainedTypeVariableReferences.Count > 0)
        {
            if (!string.IsNullOrEmpty(details)) details += "\n\n";
            details += "The following ContainedType variables will be updated:";
            foreach (var (container, variable) in ContainedTypeVariableReferences)
            {
                details += $"\n• {variable.Name} in {container.Name}";
            }
        }

        if (VariableReferenceChanges.Count > 0)
        {
            if (!string.IsNullOrEmpty(details)) details += "\n\n";
            details += "The following variable references will be updated:";
            foreach (var change in VariableReferenceChanges)
            {
                try
                {
                    var line = change.VariableReferenceList.ValueAsIList[change.LineIndex];
                    details += $"\n• {line} in {change.Container.Name}";
                }
                catch { }
            }
        }

        return details;
    }

    public void ExcludeContainersBeingDeleted(IEnumerable<ElementSave> deletedElements)
    {
        var deletedSet = new HashSet<ElementSave>(deletedElements);
        InstancesWithBaseTypeReference.RemoveAll(pair => deletedSet.Contains(pair.Container));
        ElementsWithBaseTypeReference.RemoveAll(e => deletedSet.Contains(e));
    }

    public string GetDeleteImpactDetails()
    {
        var details = string.Empty;

        if (ElementsWithBaseTypeReference.Count > 0)
        {
            if (!string.IsNullOrEmpty(details)) details += "\n\n";
            details += "The following elements inherit from this and will lose their base type:";
            foreach (var element in ElementsWithBaseTypeReference)
            {
                details += $"\n• {element.Name}";
            }
        }

        if (InstancesWithBaseTypeReference.Count > 0)
        {
            if (!string.IsNullOrEmpty(details)) details += "\n\n";
            details += "The following instances will lose their type and become invalid:";
            foreach (var (container, instance) in InstancesWithBaseTypeReference)
            {
                details += $"\n• {instance.Name} in {container.Name}";
            }
        }

        if (ContainedTypeVariableReferences.Count > 0)
        {
            if (!string.IsNullOrEmpty(details)) details += "\n\n";
            details += "The following ContainedType variables will become invalid:";
            foreach (var (container, variable) in ContainedTypeVariableReferences)
            {
                details += $"\n• {variable.Name} in {container.Name}";
            }
        }

        if (VariableReferenceChanges.Count > 0)
        {
            if (!string.IsNullOrEmpty(details)) details += "\n\n";
            details += "The following variable references will become invalid:";
            foreach (var change in VariableReferenceChanges)
            {
                try
                {
                    var line = change.VariableReferenceList.ValueAsIList[change.LineIndex];
                    details += $"\n• {line} in {change.Container.Name}";
                }
                catch { }
            }
        }

        return details;
    }
}

#endregion

#region InstanceReferences Class

public class InstanceReferences
{
    // Variables across all states in the containing element that reference the instance by name
    public List<(ElementSave Container, VariableSave Variable)> VariablesToRename = new();
    // Events in the containing element whose source object matches the instance name
    public List<EventSave> EventsToRename = new();
    // Whether any DefaultChildContainer in the containing element equals the instance's old name
    public bool DefaultChildContainerWillChange;
    // Parent variable references in other elements that point through DefaultChildContainer
    public List<(ElementSave Container, VariableSave Variable)> ParentVariablesInOtherElements = new();
    // VariableReferences list entries that contain the instance name on the left or right side
    public List<VariableReferenceChange> VariableReferenceChanges = new();

    public string GetChangesDetails(bool includeVariablesWithinElement = true)
    {
        var details = string.Empty;

        if (includeVariablesWithinElement && VariablesToRename.Count > 0)
        {
            details += "The following variables will be renamed:";
            foreach (var (container, variable) in VariablesToRename)
            {
                details += $"\n• {variable.Name} in {container.Name}";
            }
        }

        if (EventsToRename.Count > 0)
        {
            if (!string.IsNullOrEmpty(details)) details += "\n\n";
            details += "The following events will be renamed:";
            foreach (var evt in EventsToRename)
            {
                details += $"\n• {evt.Name}";
            }
        }

        if (DefaultChildContainerWillChange)
        {
            if (!string.IsNullOrEmpty(details)) details += "\n\n";
            details += "The DefaultChildContainer reference will be updated.";
        }

        if (ParentVariablesInOtherElements.Count > 0)
        {
            if (!string.IsNullOrEmpty(details)) details += "\n\n";
            details += "The following Parent variables in other elements will be updated:";
            foreach (var (container, variable) in ParentVariablesInOtherElements)
            {
                details += $"\n• {variable.Name} ({variable.Value}) in {container.Name}";
            }
        }

        if (VariableReferenceChanges.Count > 0)
        {
            if (!string.IsNullOrEmpty(details)) details += "\n\n";
            details += "The following variable references will be updated:";
            foreach (var change in VariableReferenceChanges)
            {
                try
                {
                    var line = change.VariableReferenceList.ValueAsIList[change.LineIndex];
                    details += $"\n• {line} in {change.Container.Name}";
                }
                catch { }
            }
        }

        return details;
    }

    /// <summary>
    /// Returns a description of the orphaned references that will remain invalid after
    /// this instance is deleted. Variables and events directly on the instance are
    /// auto-cleaned by the delete logic; this reports only the references that will not
    /// be automatically removed.
    /// </summary>
    public string GetDeleteImpactDetails()
    {
        var details = string.Empty;

        if (DefaultChildContainerWillChange)
        {
            details += "The DefaultChildContainer reference will become invalid.";
        }

        if (ParentVariablesInOtherElements.Count > 0)
        {
            if (!string.IsNullOrEmpty(details)) details += "\n\n";
            details += "The following Parent variables in other elements will become invalid:";
            foreach (var (container, variable) in ParentVariablesInOtherElements)
            {
                details += $"\n• {variable.Name} ({variable.Value}) in {container.Name}";
            }
        }

        if (VariableReferenceChanges.Count > 0)
        {
            if (!string.IsNullOrEmpty(details)) details += "\n\n";
            details += "The following variable references will become invalid:";
            foreach (var change in VariableReferenceChanges)
            {
                try
                {
                    var line = change.VariableReferenceList.ValueAsIList[change.LineIndex];
                    details += $"\n• {line} in {change.Container.Name}";
                }
                catch { }
            }
        }

        return details;
    }
}

#endregion

#region StateReferences Class

public class StateReferences
{
    // Variables in referencing elements whose value == oldStateName and which need updating
    public List<(ElementSave Container, VariableSave Variable)> VariablesToUpdate = new();

    public string GetChangesDetails()
    {
        if (VariablesToUpdate.Count == 0)
        {
            return string.Empty;
        }

        var details = "This will also update the following variables:";
        foreach (var (container, variable) in VariablesToUpdate)
        {
            details += $"\n• {variable.Name} in {container.Name}";
        }

        return details;
    }

    /// <summary>
    /// Returns a description of which variables reference this state and will become
    /// invalid after deletion (orphaned references are not automatically cleaned up).
    /// </summary>
    public string GetDeleteImpactDetails()
    {
        if (VariablesToUpdate.Count == 0)
        {
            return string.Empty;
        }

        var details = "The following variables reference this state and will become invalid:";
        foreach (var (container, variable) in VariablesToUpdate)
        {
            details += $"\n• {variable.Name} in {container.Name}";
        }

        return details;
    }
}

#endregion

#region BehaviorReferences Class

public class BehaviorReferences
{
    // ElementBehaviorReference entries in any screen or component that reference the old behavior name
    public List<(ElementSave Container, ElementBehaviorReference Reference)> ElementsWithBehaviorReference = new();

    public string GetChangesDetails()
    {
        if (ElementsWithBehaviorReference.Count == 0)
        {
            return string.Empty;
        }

        string details = "The following elements will have their behavior reference updated:";
        foreach (var (container, _) in ElementsWithBehaviorReference)
        {
            details += $"\n• {container.Name}";
        }
        return details;
    }

    /// <summary>
    /// Returns a description of which elements reference this behavior and will be
    /// affected by its deletion.
    /// </summary>
    public string GetDeleteImpactDetails()
    {
        if (ElementsWithBehaviorReference.Count == 0)
        {
            return string.Empty;
        }

        string details = "The following elements reference this behavior:";
        foreach (var (container, _) in ElementsWithBehaviorReference)
        {
            details += $"\n• {container.Name}";
        }
        return details;
    }
}

#endregion

#region CategoryReferences Class

public class CategoryReferences
{
    // Variables in referencing elements/components whose Type matches the old category name
    public List<VariableChange> VariableChanges = new();

    public string GetChangesDetails()
    {
        if (VariableChanges.Count == 0)
        {
            return string.Empty;
        }

        var details = "The following variables will be affected:";
        foreach (var change in VariableChanges)
        {
            var containerDisplay = change.Container is ElementSave elementSave
                ? elementSave.Name
                : change.Container.ToString();
            details += $"\n• {change.Variable.Name} in {containerDisplay}";
        }
        return details;
    }

    /// <summary>
    /// Returns a description of which variables use this category type and will become
    /// invalid after deletion.
    /// </summary>
    public string GetDeleteImpactDetails()
    {
        if (VariableChanges.Count == 0)
        {
            return string.Empty;
        }

        var details = "The following variables use this category type and will become invalid:";
        foreach (var change in VariableChanges)
        {
            var containerDisplay = change.Container is ElementSave elementSave
                ? elementSave.Name
                : change.Container.ToString();
            details += $"\n• {change.Variable.Name} in {containerDisplay}";
        }
        return details;
    }
}

#endregion
