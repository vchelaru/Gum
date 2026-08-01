using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Gum.Managers;

/// <summary>
/// Reads a WPF drag payload (<see cref="IDataObject"/>) into a framework-neutral
/// <see cref="WireframeDropPayload"/>, for the wireframe canvas and anything else that accepts the
/// same drops.
/// </summary>
/// <remarks>
/// Recognizes the three payloads the canvas acts on: a Standards-palette chip
/// (<see cref="DragDropManager.StandardElementNameDataFormat"/>), a drag out of the element tree or
/// the flat search results (<see cref="TreeDragPayload"/>), and dropped files.
/// </remarks>
public static class WpfWireframeDropPayloadReader
{
    /// <summary>Extracts the framework-neutral drop payload from a WPF drag data object.</summary>
    public static WireframeDropPayload Read(IDataObject? data)
    {
        if (data == null)
        {
            return new WireframeDropPayload(null, null, null);
        }

        string? standardElementTypeName =
            data.GetDataPresent(DragDropManager.StandardElementNameDataFormat)
                ? data.GetData(DragDropManager.StandardElementNameDataFormat) as string
                : null;

        List<object>? nodeTags = ReadNodeTags(data);

        string[]? files = data.GetDataPresent(DataFormats.FileDrop)
            ? data.GetData(DataFormats.FileDrop) as string[]
            : null;

        return new WireframeDropPayload(standardElementTypeName, nodeTags, files);
    }

    // The data object carries only a marker format; the dragged items themselves travel alongside it
    // in TreeDragPayload. A tag can legitimately be null - a folder row stands for nothing droppable -
    // and is kept rather than filtered, so the drag is still recognized as a node drag and the drop
    // simply does nothing with it.
    private static List<object>? ReadNodeTags(IDataObject data) =>
        data.GetDataPresent(TreeDragPayload.DataFormat) && TreeDragPayload.Tags is { } tags
            ? tags.Select(tag => tag!).ToList()
            : null;
}
