using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Gum.Plugins.InternalPlugins.TreeView;
using ToolsUtilities;

namespace Gum.Managers;

/// <summary>
/// A node in the element tree. This is the model the WPF tree binds to, and the concrete
/// implementation of <see cref="ITreeNodeMutable"/> that ElementTreeViewManager builds.
/// </summary>
/// <remarks>
/// <para>
/// It owns tree structure and per-node display state; it does not know about the control rendering
/// it. Selection and expansion are ordinary observable properties rather than widget calls, which
/// is what lets expansion survive a rebuild and lets multi-selection be expressed on the model
/// instead of by mutating per-row colors.
/// </para>
/// <para>
/// The navigation surface (<see cref="FirstNode"/>, <see cref="NextVisibleNode"/>,
/// <see cref="Level"/>, <see cref="Index"/>, …) deliberately mirrors the WinForms
/// <c>TreeNode</c> members it replaces, so the extracted selection/navigation decision logic and
/// its tests carry over by retyping rather than rewriting.
/// </para>
/// </remarks>
public class GumTreeNode : ITreeNodeMutable, INotifyPropertyChanged
{
    private readonly GumTreeNodeCollection _nodes;
    private GumTreeNodeCollection? _owningCollection;
    private string _text = string.Empty;
    private object? _tag;
    private int _imageIndex;
    private bool _isExpanded;
    private bool _isSelected;
    private bool _isHot;

    /// <summary>
    /// Separator used by <see cref="FullPath"/>. Backslash, matching the WinForms tree this
    /// replaces - <c>CopyPasteLogic</c> slices a <c>"Components\\"</c> prefix off the result.
    /// </summary>
    private const string PathSeparator = "\\";

    public GumTreeNode()
    {
        _nodes = new GumTreeNodeCollection(this);
    }

    public GumTreeNode(string text) : this()
    {
        _text = text;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    #region Structure

    /// <summary>
    /// This node's children. Named to match the WinForms collection it replaces so the tree-building
    /// code reads unchanged.
    /// </summary>
    public GumTreeNodeCollection Nodes => _nodes;

    public GumTreeNode? Parent => _owningCollection?.Owner;

    ITreeNode? ITreeNode.Parent => Parent;

    ITreeNodeMutable? ITreeNodeMutable.Parent => Parent;

    public IEnumerable<ITreeNode> Children => _nodes;

    /// <summary>
    /// This node's depth, with root nodes at 0.
    /// </summary>
    public int Level
    {
        get
        {
            int level = 0;
            for (GumTreeNode? current = Parent; current != null; current = current.Parent)
            {
                level++;
            }
            return level;
        }
    }

    /// <summary>
    /// This node's position among its siblings, or -1 if it belongs to no collection.
    /// </summary>
    public int Index => _owningCollection?.IndexOf(this) ?? -1;

    public GumTreeNode? FirstNode => _nodes.Count > 0 ? _nodes[0] : null;

    public GumTreeNode? LastNode => _nodes.Count > 0 ? _nodes[_nodes.Count - 1] : null;

    public GumTreeNode? NextNode
    {
        get
        {
            int index = Index;
            return index >= 0 && index + 1 < _owningCollection!.Count
                ? _owningCollection[index + 1]
                : null;
        }
    }

    public GumTreeNode? PrevNode
    {
        get
        {
            int index = Index;
            return index > 0 ? _owningCollection![index - 1] : null;
        }
    }

    /// <summary>
    /// The next node in top-to-bottom display order, skipping the children of collapsed nodes.
    /// Null at the bottom of the tree.
    /// </summary>
    public GumTreeNode? NextVisibleNode
    {
        get
        {
            if (IsExpanded && FirstNode != null)
            {
                return FirstNode;
            }

            for (GumTreeNode? current = this; current != null; current = current.Parent)
            {
                if (current.NextNode is { } sibling)
                {
                    return sibling;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// The previous node in top-to-bottom display order, skipping the children of collapsed nodes.
    /// Null at the top of the tree.
    /// </summary>
    public GumTreeNode? PrevVisibleNode
    {
        get
        {
            if (PrevNode is not { } previousSibling)
            {
                return Parent;
            }

            // The visible node immediately above a sibling is that sibling's deepest expanded
            // descendant, not the sibling itself.
            GumTreeNode deepest = previousSibling;
            while (deepest.IsExpanded && deepest.LastNode is { } lastChild)
            {
                deepest = lastChild;
            }

            return deepest;
        }
    }

    #endregion

    #region Display state

    public string Text
    {
        get => _text;
        set => Set(ref _text, value);
    }

    public object? Tag
    {
        get => _tag;
        set => Set(ref _tag, value);
    }

    /// <summary>
    /// Index into the tree's icon set. The values live on <see cref="TreeNodeImageIndices"/>.
    /// </summary>
    public int ImageIndex
    {
        get => _imageIndex;
        set => Set(ref _imageIndex, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => Set(ref _isExpanded, value);
    }

    /// <summary>
    /// Whether this node is part of the current selection. The tree binds each row's selected
    /// visual to this, which is what makes multi-selection possible on a control that natively
    /// tracks only one selected item.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => Set(ref _isSelected, value);
    }

    /// <summary>
    /// Whether this node is drawn as hovered. Set both by the pointer and by the wireframe, which
    /// highlights the row for whatever the cursor is over on the canvas.
    /// </summary>
    public bool IsHot
    {
        get => _isHot;
        set => Set(ref _isHot, value);
    }

    /// <summary>
    /// Whether this node has children, exposed so a row can show or hide its expander without the
    /// view reaching into the collection.
    /// </summary>
    public bool HasChildren => _nodes.Count > 0;

    /// <summary>
    /// The <c>\</c>-joined text path from the root to this node.
    /// </summary>
    public string FullPath
    {
        get
        {
            List<string> parts = new();
            for (GumTreeNode? current = this; current != null; current = current.Parent)
            {
                parts.Insert(0, current.Text);
            }
            return string.Join(PathSeparator, parts);
        }
    }

    FilePath? ITreeNode.GetFullFilePath() => this.GetTreeNodeFullFilePath();

    #endregion

    #region Expansion

    public void Expand() => IsExpanded = true;

    public void Collapse() => IsExpanded = false;

    /// <summary>
    /// Expands every ancestor so this node is reachable in the display. Does not expand this node
    /// itself, and does not scroll - scrolling is the view's job.
    /// </summary>
    public void EnsureAncestorsExpanded()
    {
        for (GumTreeNode? current = Parent; current != null; current = current.Parent)
        {
            current.IsExpanded = true;
        }
    }

    /// <summary>
    /// This node and every descendant, in display order, regardless of expansion.
    /// </summary>
    public IEnumerable<GumTreeNode> SelfAndDescendants()
    {
        yield return this;

        foreach (GumTreeNode child in _nodes)
        {
            foreach (GumTreeNode descendant in child.SelfAndDescendants())
            {
                yield return descendant;
            }
        }
    }

    #endregion

    #region ITreeNodeMutable

    public void SetTag(object? tag) => Tag = tag;

    public ITreeNodeMutable AddChild(string text)
    {
        GumTreeNode child = new(text);
        _nodes.Add(child);
        return child;
    }

    public void AddChild(ITreeNodeMutable child) => _nodes.Add(RequireGumTreeNode(child));

    public void InsertChild(int index, ITreeNodeMutable child) =>
        _nodes.Insert(index, RequireGumTreeNode(child));

    public void RemoveChild(ITreeNodeMutable child) => _nodes.Remove(RequireGumTreeNode(child));

    public void RemoveChildAt(int index) => _nodes.RemoveAt(index);

    public void ClearChildren() => _nodes.Clear();

    public void RemoveSelf() => _owningCollection?.Remove(this);

    public int IndexOfChild(ITreeNodeMutable child) => _nodes.IndexOf(RequireGumTreeNode(child));

    public int ChildCount => _nodes.Count;

    public ITreeNodeMutable GetChildAt(int index) => _nodes[index];

    #endregion

    public override string ToString() => Text;

    #region Collection plumbing

    internal GumTreeNodeCollection? OwningCollection => _owningCollection;

    internal void Attach(GumTreeNodeCollection collection)
    {
        _owningCollection = collection;
        OnPropertyChanged(nameof(Parent));
    }

    internal void Detach()
    {
        _owningCollection = null;
        OnPropertyChanged(nameof(Parent));
    }

    internal void DetachFromCurrentCollection() => _owningCollection?.Remove(this);

    internal void RaiseChildCountChanged() => OnPropertyChanged(nameof(HasChildren));

    #endregion

    private static GumTreeNode RequireGumTreeNode(ITreeNodeMutable node)
    {
        if (node is GumTreeNode gumTreeNode)
        {
            return gumTreeNode;
        }

        throw new ArgumentException(
            $"{nameof(node)} must be a {nameof(GumTreeNode)} to take part in the element tree.",
            nameof(node));
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
    }

    private void OnPropertyChanged(string? propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
