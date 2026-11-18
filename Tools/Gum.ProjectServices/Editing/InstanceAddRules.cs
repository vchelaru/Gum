using Gum.DataTypes;
using Gum.Services;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// The project rules for adding an instance of one element to another, shared by every way the tool adds
/// instances and by <c>gumcli edit</c>.
/// </summary>
public interface IInstanceAddRules
{
    /// <summary>
    /// Why an instance of <paramref name="elementToAdd"/> cannot be added to <paramref name="target"/>, or null
    /// when it can: a standard element holds no instances, a Screen cannot be an instance, the add must not create a
    /// circular reference, and an element whose file is missing cannot be edited.
    /// </summary>
    string? GetWhyCannotAdd(ElementSave elementToAdd, ElementSave target);
}

/// <inheritdoc/>
public class InstanceAddRules : IInstanceAddRules
{
    private readonly ICircularReferenceManager _circularReferenceManager;

    public InstanceAddRules(ICircularReferenceManager circularReferenceManager)
    {
        _circularReferenceManager = circularReferenceManager;
    }

    /// <inheritdoc/>
    public string? GetWhyCannotAdd(ElementSave elementToAdd, ElementSave target)
    {
        if (target is StandardElementSave)
        {
            return $"Standard type {target} cannot contain objects instances, so {elementToAdd} cannot be added here";
        }
        if (elementToAdd is ScreenSave)
        {
            return "Screens can't be added to other Screens or Components";
        }
        if (!_circularReferenceManager.CanTypeBeAddedToElement(target, elementToAdd.Name))
        {
            return $"Cannot add {elementToAdd.Name} to {target.Name} because it would create a circular reference";
        }
        if (target.IsSourceFileMissing)
        {
            return $"The source file for {target.Name} is missing, so it cannot be edited";
        }
        return null;
    }
}
