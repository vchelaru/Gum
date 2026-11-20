using System.Linq;
using Gum.DataTypes;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// The flags of an instance the tool's Variables tab shows next to its name: <c>Locked</c> on every instance and
/// <c>IsSlot</c> on an instance in a Component. What <c>gumcli edit</c>'s
/// <c>setInstanceProperty</c> can set.
/// </summary>
public class InstancePropertyLogic
{
    /// <summary>The names <see cref="Set"/> accepts.</summary>
    public static readonly string[] PropertyNames = { "Locked", "IsSlot" };

    /// <summary>
    /// Why <paramref name="property"/> cannot be set on an instance of <paramref name="container"/>, or null when it
    /// can: it must be one of <see cref="PropertyNames"/>, and a slot lives in a Component.
    /// </summary>
    public string? GetWhyCannotSet(ElementSave container, string property)
    {
        if (!PropertyNames.Contains(property))
        {
            return $"\"{property}\" is not an instance property edit can set. Use one of: {string.Join(", ", PropertyNames)}.";
        }
        if (property == "IsSlot" && container is not ComponentSave)
        {
            return $"{container.Name} is not a Component, and only an instance in a Component can be a slot.";
        }
        return null;
    }

    /// <summary>
    /// Sets <paramref name="property"/> on <paramref name="instance"/>. Call <see cref="GetWhyCannotSet"/> first.
    /// </summary>
    public void Set(InstanceSave instance, string property, bool value)
    {
        if (property == "Locked")
        {
            instance.Locked = value;
        }
        else
        {
            instance.IsSlot = value;
        }
    }
}
