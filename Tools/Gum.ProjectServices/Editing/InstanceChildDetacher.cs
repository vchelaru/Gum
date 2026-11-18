using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Moves an instance's children up one level before the instance is removed: the tool's "Delete only parent(s)"
/// option, which is its default.
/// </summary>
public interface IInstanceChildDetacher
{
    /// <summary>
    /// Reparents <paramref name="instance"/>'s children onto the instance's own parent, or onto the element when the
    /// instance has no parent. A child parented to a named object inside the instance loses its parent.
    /// </summary>
    void DetachChildren(InstanceSave instance, ElementSave element);
}

/// <inheritdoc/>
public class InstanceChildDetacher : IInstanceChildDetacher
{
    /// <inheritdoc/>
    public void DetachChildren(InstanceSave instance, ElementSave element)
    {
        string? grandparentValue = element.DefaultState?.Variables
            .FirstOrDefault(v => v.SourceObject == instance.Name && v.GetRootName() == "Parent")
            ?.Value as string;

        foreach (StateSave state in element.AllStates)
        {
            for (int i = state.Variables.Count - 1; i > -1; i--)
            {
                VariableSave variable = state.Variables[i];
                if (variable.GetRootName() != "Parent" || variable.Value is not string valueAsString)
                {
                    continue;
                }

                if (valueAsString == instance.Name)
                {
                    if (grandparentValue == null)
                    {
                        state.Variables.RemoveAt(i);
                    }
                    else
                    {
                        variable.Value = grandparentValue;
                    }
                }
                else if (valueAsString.StartsWith(instance.Name + "."))
                {
                    // A named object inside the removed instance does not exist under the grandparent.
                    state.Variables.RemoveAt(i);
                }
            }
        }
    }
}
