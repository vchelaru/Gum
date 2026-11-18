using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes.Variables;

namespace Gum.DataTypes;

/// <summary>
/// Reads which instances of an element are attached under which, through the default state's Parent values.
/// </summary>
public static class ElementSaveInstanceHierarchyExtensions
{
    /// <summary>
    /// <paramref name="instances"/> followed by every instance attached under one of them, at any depth, each
    /// listed once, in the order found. Used by copy and by Create Component.
    /// </summary>
    public static List<InstanceSave> GetInstancesAndChildrenOf(this ElementSave container, IEnumerable<InstanceSave> instances)
    {
        List<InstanceSave> found = new List<InstanceSave>();
        foreach (InstanceSave instance in instances)
        {
            if (!found.Any(item => item.Name == instance.Name))
            {
                found.Add(instance);
                AddChildrenOf(instance, container, found);
            }
        }
        return found;
    }

    private static void AddChildrenOf(InstanceSave instance, ElementSave container, List<InstanceSave> found)
    {
        foreach (VariableSave variable in container.GetDefaultStateOrThrow().Variables)
        {
            if (variable.GetRootName() != "Parent" || variable.Value is not string value || string.IsNullOrEmpty(value)
                || (value != instance.Name && !value.StartsWith(instance.Name + ".")))
            {
                continue;
            }
            InstanceSave? child = container.GetInstance(variable.SourceObject);
            if (child != null && !found.Any(item => item.Name == child.Name))
            {
                found.Add(child);
                AddChildrenOf(child, container, found);
            }
        }
    }
}
