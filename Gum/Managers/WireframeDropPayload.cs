using System.Collections.Generic;

namespace Gum.Managers;

/// <summary>
/// The framework-neutral contents of a drag payload dropped onto (or dragged over) the wireframe
/// canvas. Extracted from a native drag-and-drop data object by a framework-specific reader — see
/// <see cref="WpfWireframeDropPayloadReader"/> for the WPF side. Mirrors the three drop kinds the
/// WinForms wireframe glue in <c>MainEditorTabPlugin</c> recognizes: a Standards-palette chip, one
/// or more dragged tree nodes (by their <c>Tag</c>), or dropped files.
/// </summary>
public sealed record WireframeDropPayload(
    string? StandardElementTypeName,
    IReadOnlyList<object>? NodeTags,
    string[]? Files)
{
    /// <summary>True when the payload is a Standards-palette chip drag.</summary>
    public bool HasStandardChip => StandardElementTypeName != null;

    /// <summary>True when the payload carries one or more dragged tree nodes.</summary>
    public bool HasNodes => NodeTags is { Count: > 0 };

    /// <summary>True when the payload carries one or more dropped files.</summary>
    public bool HasFileDrop => Files is { Length: > 0 };

    /// <summary>
    /// Resolves which of the payload's drop kinds should be acted on, in the same precedence the
    /// WinForms wireframe glue applies (<c>MainEditorTabPlugin.OnWireframeDrop</c>): a
    /// Standards-palette chip wins, then dragged tree nodes, then a file drop.
    /// </summary>
    public WireframeDropAction ResolveAction()
    {
        if (HasStandardChip)
        {
            return new WireframeDropAction.StandardChip(StandardElementTypeName!);
        }
        if (HasNodes)
        {
            return new WireframeDropAction.Nodes(NodeTags!);
        }
        if (HasFileDrop)
        {
            return new WireframeDropAction.FileDrop(Files!);
        }
        return new WireframeDropAction.None();
    }
}
