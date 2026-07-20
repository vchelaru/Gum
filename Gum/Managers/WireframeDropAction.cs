using System.Collections.Generic;

namespace Gum.Managers;

/// <summary>
/// The action a wireframe drop should perform, resolved from a <see cref="WireframeDropPayload"/>
/// by <see cref="WireframeDropPayload.ResolveAction"/>. Each variant carries exactly the data its
/// handler needs — mirrors the <see cref="Gum.Plugins.InternalPlugins.TreeView.DropPosition"/>
/// pattern used elsewhere for drag-and-drop.
/// </summary>
public abstract record WireframeDropAction
{
    private WireframeDropAction() { }

    /// <summary>No recognized payload was present in the drag data; nothing should happen.</summary>
    public sealed record None : WireframeDropAction;

    /// <summary>
    /// A Standards-palette chip was dropped; create an instance of the named standard type at the
    /// drop position.
    /// </summary>
    public sealed record StandardChip(string StandardElementTypeName) : WireframeDropAction;

    /// <summary>
    /// One or more tree nodes were dropped; create/reparent an instance for each dragged node's
    /// <c>Tag</c>.
    /// </summary>
    public sealed record Nodes(IReadOnlyList<object> Tags) : WireframeDropAction;

    /// <summary>One or more files were dropped; import/create instances from the dropped files.</summary>
    public sealed record FileDrop(string[] Files) : WireframeDropAction;
}
