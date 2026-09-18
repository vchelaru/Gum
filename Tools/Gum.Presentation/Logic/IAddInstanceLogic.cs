using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Plugins.InternalPlugins.TreeView;

namespace Gum.Logic;

/// <summary>
/// The one way an instance of an element is added to a container. Every gesture that creates an
/// instance (drag onto a tree node or the canvas, a Standards chip click, the Add Instance dialog,
/// the right-click menus) goes through here, so validation, naming, parenting, undo, selection and
/// the remembered add destination behave the same for all of them.
/// </summary>
public interface IAddInstanceLogic
{
    /// <summary>
    /// Adds an instance of <paramref name="elementToAdd"/> into <paramref name="container"/>: an
    /// <see cref="ElementSave"/> (at its root), an <see cref="InstanceSave"/> (as its child, in its
    /// element), or a <see cref="BehaviorSave"/> (as a required instance). Shows a message and
    /// returns null when the add is not allowed (no container, a standard element or Screen target,
    /// a circular reference). Selects the new instance and, unless
    /// <paramref name="rememberContainerAsDestination"/> is false, remembers the container as the
    /// destination of the next add or paste.
    /// </summary>
    /// <param name="name">The new instance's name; unique-by-type when null.</param>
    /// <param name="position">Where in the element's flat instance list to insert; appended when null.</param>
    InstanceSave? AddInstance(ElementSave elementToAdd, object? container, string? name = null,
        DropPosition? position = null, bool rememberContainerAsDestination = true);

    /// <summary>
    /// Adds an instance of <paramref name="elementToAdd"/> into the remembered add destination,
    /// else under the selected instance, else at the selected element's root.
    /// </summary>
    InstanceSave? AddInstanceAtDestination(ElementSave elementToAdd, string? name = null);
}
