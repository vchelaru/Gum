using Gum.DataTypes;
using Gum.Plugins.InternalPlugins.TreeView;
using System.Collections.Generic;

namespace Gum.Managers;

public interface IDragDropManager
{
    IEnumerable<string> ValidTextureExtensions { get; }

    /// <summary>
    /// Valid font file extensions for drag-and-drop operations (e.g., "ttf").
    /// </summary>
    IEnumerable<string> ValidFontExtensions { get; }

    /// <summary>
    /// Returns a human-readable reason why dragging a texture/font file onto the
    /// wireframe is currently blocked by the editor selection, or null when a file
    /// drop is allowed. Used both to gate the drop and to surface diagnostics in the
    /// output window when a drop silently does nothing. Considers only the selection
    /// state, not whether the drag payload actually contains files.
    /// </summary>
    string? GetFileDropBlockedReason();

    bool IsValidExtensionForFileDrop(string file);
    void OnFilesDroppedInTreeView(string[] files);
    void OnNodeObjectDroppedInWireframe(object draggedObject);

    /// <summary>
    /// Creates an instance of the given standard type on the Screen/Component represented by
    /// (or containing) the dropped-on tree node. Used by the Standards chip palette so dragging
    /// a chip onto the tree reuses the same creation path as dragging a Standard element node.
    /// </summary>
    /// <param name="standardElement">The standard element whose type the new instance will be.</param>
    /// <param name="targetTreeNode">The tree node the chip was dropped on.</param>
    void HandleDroppedStandardElementOnTreeNode(StandardElementSave standardElement, ITreeNode targetTreeNode);
    /// <summary>
    /// Handle a drag-drop reorder. <paramref name="dropTarget"/> describes the
    /// destination element and flat-list position; it is null for folder or
    /// behavior drops where flat-list semantics do not apply.
    /// </summary>
    void OnNodeSortingDropped(IEnumerable<ITreeNode> draggedNodes, ITreeNode targetNode, DropTarget? dropTarget);

    /// <summary>
    /// Decides whether a drag entering the wireframe should be accepted, given a
    /// framework-neutral description of the drag payload. The WinForms drag-enter
    /// glue in the view inspects the payload (file drop / node payload), calls this,
    /// then applies the decision (sets the Copy effect, surfaces the blocked reason).
    /// </summary>
    /// <param name="hasFileDrop">True when the payload contains dropped files.</param>
    /// <param name="hasNodes">True when the payload contains tree-node data.</param>
    DragAcceptDecision DecideWireframeDragEffect(bool hasFileDrop, bool hasNodes);

    void SetInstanceToPosition(float worldX, float worldY, InstanceSave instance);
    /// <inheritdoc cref="OnNodeSortingDropped"/>
    bool ValidateNodeSorting(IEnumerable<ITreeNode> draggedNodes, ITreeNode targetNode, DropTarget? dropTarget);
}
