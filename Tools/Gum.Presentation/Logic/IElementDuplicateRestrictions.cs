using Gum.DataTypes;

namespace Gum.Logic;

/// <summary>
/// Applies the project's engine profile to a screen or component that is about to be duplicated
/// (the Duplicate command and an element paste), so the copy holds nothing the project does not
/// support. The source element is never changed.
/// </summary>
public interface IElementDuplicateRestrictions
{
    /// <summary>
    /// The reason the element cannot be duplicated, or null when it can: it holds an instance of a
    /// type the project does not support.
    /// </summary>
    string? GetRefusalMessage(ElementSave element);

    /// <summary>
    /// Removes the Parent values of the copy's instances whose type has no hierarchy in this project.
    /// </summary>
    void RemoveUnsupportedParents(ElementSave copy);
}
