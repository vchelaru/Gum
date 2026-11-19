using System.Collections.Generic;
using Gum.DataTypes;
using Gum.DataTypes.Variables;

namespace Gum.Managers;

/// <summary>
/// The sibling lists of every instance in one element, each the same list
/// <see cref="InstanceExtensionMethods.GetSiblingsIncludingThis"/> returns: the element's instances
/// that share the instance's parent, in the element's order. Each instance's Parent is read once,
/// where calling that method per sibling group reads every instance's Parent again for each group.
/// </summary>
public class InstanceSiblingGroups
{
    private readonly Dictionary<InstanceSave, List<InstanceSave>> _groupOf = new Dictionary<InstanceSave, List<InstanceSave>>();

    /// <summary>Groups the instances of <paramref name="element"/> by their parent in its default state.</summary>
    public InstanceSiblingGroups(ElementSave element)
    {
        RecursiveVariableFinder finder = new RecursiveVariableFinder(element.DefaultState!);
        // A null key stands for "no parent", which a dictionary cannot hold.
        Dictionary<string, List<InstanceSave>> groupsByParent = new Dictionary<string, List<InstanceSave>>();
        List<InstanceSave> unparented = new List<InstanceSave>();

        foreach (InstanceSave instance in element.Instances)
        {
            string? parent = finder.GetValue($"{instance.Name}.Parent") as string;
            if (parent?.Contains(".") == true)
            {
                parent = parent.Substring(0, parent.IndexOf('.'));
            }
            // A parent that isn't an instance of this element counts as no parent.
            if (element.GetInstance(parent) == null)
            {
                parent = null;
            }

            List<InstanceSave> group;
            if (parent == null)
            {
                group = unparented;
            }
            else if (!groupsByParent.TryGetValue(parent, out group!))
            {
                group = new List<InstanceSave>();
                groupsByParent[parent] = group;
            }
            group.Add(instance);
            _groupOf[instance] = group;
        }
    }

    /// <summary>
    /// The instances sharing <paramref name="instance"/>'s parent, itself included, or null for an
    /// instance not in the element.
    /// </summary>
    public List<InstanceSave>? SiblingsIncludingThis(InstanceSave instance) =>
        _groupOf.TryGetValue(instance, out List<InstanceSave>? group) ? group : null;
}
