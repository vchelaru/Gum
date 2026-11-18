using Gum.DataTypes;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Adds an instance to an element: the data change behind the tool's add-instance commands, without tree,
/// canvas, selection or plugin updates.
/// </summary>
public interface IInstanceCreationLogic
{
    /// <summary>
    /// Adds <paramref name="instance"/> to <paramref name="element"/> at <paramref name="desiredIndex"/> (the end when
    /// null) and, when <paramref name="parentName"/> is given, sets its Parent in the default state.
    /// </summary>
    void AddInstance(ElementSave element, InstanceSave instance, string? parentName, int? desiredIndex);
}

/// <inheritdoc/>
public class InstanceCreationLogic : IInstanceCreationLogic
{
    /// <inheritdoc/>
    public void AddInstance(ElementSave element, InstanceSave instance, string? parentName, int? desiredIndex)
    {
        if (desiredIndex == null)
        {
            element.Instances.Add(instance);
        }
        else
        {
            element.Instances.Insert(desiredIndex.Value, instance);
        }

        if (!string.IsNullOrEmpty(parentName))
        {
            element.GetDefaultStateOrThrow().SetValue($"{instance.Name}.Parent", parentName, "string");
        }
    }
}
