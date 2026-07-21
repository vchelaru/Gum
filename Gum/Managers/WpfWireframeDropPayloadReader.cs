using System.Collections.Generic;
using System.Linq;
using System.Windows;
using WinFormsTreeNode = System.Windows.Forms.TreeNode;

namespace Gum.Managers;

/// <summary>
/// Reads a WPF drag payload (<see cref="IDataObject"/>) into a framework-neutral
/// <see cref="WireframeDropPayload"/>. The WPF counterpart to the WinForms extraction that lives
/// inline in <c>MainEditorTabPlugin.OnWireframeDragEnter</c>/<c>OnWireframeDrop</c> (which uses
/// <c>CommonFormsAndControls.MultiSelectTreeView.ExtractDraggedNodes</c> for node detection, plus
/// direct <see cref="System.Windows.Forms.DataFormats.FileDrop"/> and
/// <see cref="DragDropManager.StandardElementNameDataFormat"/> checks).
///
/// Not yet wired to a live control — <c>WireframeControl</c> is still a
/// <see cref="System.Windows.Forms.Control"/> (issue #3833). This exists so a future WPF-native
/// <c>WireframeControl</c>, or any other WPF element that needs the same drop payloads, can wire
/// <c>AllowDrop</c>/<c>DragEnter</c>/<c>DragOver</c>/<c>Drop</c> to it directly. Tree-node drops
/// still carry WinForms <see cref="WinFormsTreeNode"/> objects because the element tree view itself
/// hasn't moved to WPF yet — this reader's node extraction will need to move with it when it does.
/// </summary>
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

    // Deliberately does not use GetDataPresent(typeof(WinFormsTreeNode[]))/GetDataPresent(typeof(WinFormsTreeNode)):
    // the drag source (MultiSelectTreeView.Theming's native reorder path, which starts the OLE drag
    // this WPF drop target ultimately receives) wraps the payload with no explicit format, so the
    // underlying OLE data object keys it by the payload's *runtime* type full name. A TreeNode
    // subclass (GumTreeNode, which is what the element tree view actually constructs) is then stored
    // under its own type name, so an exact typeof(TreeNode) lookup misses every dragged node - the
    // same failure mode MultiSelectTreeView.ExtractDraggedNodes fixes for the tree's own internal
    // reorder drop. Scanning the actual formats present and pattern-matching on TreeNode[]/TreeNode is
    // robust to any subclass. Note the multi-select shape is an array (SelectedNodes.ToArray(), i.e.
    // WinFormsTreeNode[]), not a List<WinFormsTreeNode> - matching MultiSelectTreeView.Theming's
    // actual DoDragDrop payload, not the container type the data started in.
    private static List<object>? ReadNodeTags(IDataObject data)
    {
        foreach (string format in data.GetFormats())
        {
            if (data.GetData(format) is WinFormsTreeNode[] nodes)
            {
                return nodes.Select(node => node.Tag).ToList();
            }
            if (data.GetData(format) is WinFormsTreeNode singleNode)
            {
                return new List<object> { singleNode.Tag };
            }
        }

        return null;
    }
}
