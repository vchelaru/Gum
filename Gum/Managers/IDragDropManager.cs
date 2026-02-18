using Gum.DataTypes;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Gum.Managers;

public interface IDragDropManager
{
    InputLibrary.Cursor Cursor { get; }
    IEnumerable<string> ValidTextureExtensions { get; }

    bool IsValidExtensionForFileDrop(string file);
    void OnFilesDroppedInTreeView(string[] files);
    void OnNodeObjectDroppedInWireframe(object draggedObject);
    void OnNodeSortingDropped(IEnumerable<ITreeNode> draggedNodes, ITreeNode targetNode, int index);
    void OnWireframeDragEnter(object? sender, DragEventArgs e);
    void SetInstanceToPosition(float worldX, float worldY, InstanceSave instance);
    bool ValidateNodeSorting(IEnumerable<ITreeNode> draggedNodes, ITreeNode targetNode, int index);
    void HandleKeyPress(KeyPressEventArgs e);
}
