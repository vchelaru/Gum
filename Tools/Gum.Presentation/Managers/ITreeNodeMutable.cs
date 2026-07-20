namespace Gum.Managers;

/// <summary>
/// Mutation-capable extension of <see cref="ITreeNode"/>: adds an icon index plus child
/// add/insert/remove operations, covering ElementTreeViewManager's node-construction and refresh
/// surface (which <see cref="ITreeNode"/> intentionally leaves out — it only needs to be read-only
/// for its existing search/predicate callers).
/// </summary>
/// <remarks>
/// Implement this directly on the concrete node type (see <c>GumTreeNode</c> in the Gum tool
/// project) rather than via a per-call wrapper like <c>TreeNodeWrapper</c>. ElementTreeViewManager's
/// refresh/construction paths touch every node in an element's subtree on every edit, so wrapping
/// each node in a new heap object per access — the approach used for <see cref="ITreeNode"/>'s
/// low-frequency, read-only callers — would regress that hot path. A node type that already
/// implements this interface pays zero extra allocation to be used through it.
/// </remarks>
public interface ITreeNodeMutable : ITreeNode
{
    /// <summary>
    /// The node's icon index in whatever image list the concrete implementation is bound to.
    /// </summary>
    int ImageIndex { get; set; }

    /// <summary>
    /// Sets <see cref="ITreeNode.Tag"/>. A method rather than a widened settable property so it
    /// doesn't have to hide/re-declare the read-only <see cref="ITreeNode.Tag"/> it extends.
    /// </summary>
    void SetTag(object? tag);

    /// <summary>
    /// Appends a new child node with the given display text and returns it.
    /// </summary>
    ITreeNodeMutable AddChild(string text);

    /// <summary>
    /// Appends an existing node as a child.
    /// </summary>
    void AddChild(ITreeNodeMutable child);

    /// <summary>
    /// Inserts a child at the given index.
    /// </summary>
    void InsertChild(int index, ITreeNodeMutable child);

    /// <summary>
    /// Removes a child node, if present.
    /// </summary>
    void RemoveChild(ITreeNodeMutable child);

    /// <summary>
    /// Removes the child at the given index.
    /// </summary>
    void RemoveChildAt(int index);

    /// <summary>
    /// Removes all child nodes.
    /// </summary>
    void ClearChildren();
}
