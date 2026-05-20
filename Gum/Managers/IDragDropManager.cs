using Gum.DataTypes;
using Gum.Plugins.InternalPlugins.TreeView;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Gum.Managers;

public interface IDragDropManager
{
    InputLibrary.Cursor Cursor { get; }
    IEnumerable<string> ValidTextureExtensions { get; }

    /// <summary>
    /// Valid font file extensions for drag-and-drop operations (e.g., "ttf").
    /// </summary>
    IEnumerable<string> ValidFontExtensions { get; }

    bool IsValidExtensionForFileDrop(string file);
    void OnFilesDroppedInTreeView(string[] files);
    void OnNodeObjectDroppedInWireframe(object draggedObject);
    /// <summary>
    /// Handle a drag-drop reorder. <paramref name="dropTarget"/> describes the
    /// destination element and flat-list position; it is null for folder or
    /// behavior drops where flat-list semantics do not apply.
    /// </summary>
    void OnNodeSortingDropped(IEnumerable<ITreeNode> draggedNodes, ITreeNode targetNode, DropTarget? dropTarget);
    void OnWireframeDragEnter(object? sender, DragEventArgs e);
    void SetInstanceToPosition(float worldX, float worldY, InstanceSave instance);
    /// <inheritdoc cref="OnNodeSortingDropped"/>
    bool ValidateNodeSorting(IEnumerable<ITreeNode> draggedNodes, ITreeNode targetNode, DropTarget? dropTarget);
    void HandleKeyPress(KeyPressEventArgs e);
}
