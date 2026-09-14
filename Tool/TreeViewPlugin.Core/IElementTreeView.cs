using System;
using System.Collections.Generic;
using Gum.Controls;
using Gum.Input;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.TreeView.ViewModels;
using Gum.ViewModels;

namespace Gum.Plugins.InternalPlugins.TreeView;

/// <summary>
/// The Project panel as <see cref="ElementTreeViewManager"/> sees it: the element tree, its search
/// box and results list, the collapse buttons, and the Standards chip palette. Each head implements
/// it with its own controls; everything that decides what the panel shows stays in the manager.
/// </summary>
public interface IElementTreeView
{
    /// <summary>The control to host in the Project tab.</summary>
    object Content { get; }

    /// <summary>The tree's root nodes.</summary>
    GumTreeNodeCollection Nodes { get; }

    /// <summary>The tree's selection, which the view feeds with pointer and key input.</summary>
    TreeSelectionModel Selection { get; }

    /// <summary>Whether the pointer is over the tree.</summary>
    bool IsPointerOver { get; }

    /// <summary>The node whose row is under the pointer, or null.</summary>
    GumTreeNode? NodeUnderPointer { get; }

    /// <summary>Whether keyboard focus is in the tree.</summary>
    bool IsKeyboardFocusWithin { get; }

    /// <summary>Whether the "Include Variables" search option is checked.</summary>
    bool IsDeepSearchChecked { get; }

    /// <summary>Supplies the right-click menu for the current selection. An empty list suppresses the menu.</summary>
    Func<IReadOnlyList<ContextMenuItemViewModel>>? ContextMenuProvider { get; set; }

    /// <summary>Supplies the name of the element a palette chip would add to, or null when there is none.</summary>
    Func<string?>? CurrentElementNameProvider { get; set; }

    /// <summary>Moves keyboard focus to the tree.</summary>
    void FocusTree();

    /// <summary>Moves keyboard focus to the search box.</summary>
    void FocusSearch();

    /// <summary>Clears the search box, which also returns the panel to the tree.</summary>
    void ClearSearchText();

    /// <summary>Expands <paramref name="node"/>'s ancestors and scrolls it into view.</summary>
    void EnsureVisible(GumTreeNode node);

    /// <summary>
    /// Shows <paramref name="results"/> in place of the tree with the first one selected, or the tree
    /// again when <paramref name="results"/> is null.
    /// </summary>
    void ShowSearchResults(IReadOnlyList<SearchItemViewModel>? results);

    /// <summary>Shows or hides the Standards chip palette.</summary>
    void SetStandardsPaletteVisible(bool isVisible);

    /// <summary>Replaces the palette's chips with one per standard type name, in the given order.</summary>
    void RefreshStandardsPaletteChips(IReadOnlyList<string> standardTypeNames);

    /// <summary>Highlights the chip for <paramref name="typeName"/>, or none when it is null.</summary>
    void SetSelectedStandardType(string? typeName);

    /// <summary>Re-resolves everything the panel draws from theme resources.</summary>
    void ApplyThemeColors();

    /// <summary>Scales the panel's tool buttons to the UI font size.</summary>
    void UpdateCollapseButtonSizes(double baseFontSize);

    /// <summary>Raised when the search text changes.</summary>
    event Action<string?>? SearchTextChanged;

    /// <summary>Raised when a search result is chosen.</summary>
    event Action<SearchItemViewModel?>? SearchResultChosen;

    /// <summary>Raised when "Include Variables" is checked.</summary>
    event Action? DeepSearchChecked;

    /// <summary>Raised by the Collapse All button.</summary>
    event Action? CollapseAllRequested;

    /// <summary>Raised by the Collapse To Element Level button.</summary>
    event Action? CollapseToElementLevelRequested;

    /// <summary>Raised when the user expands or collapses a node.</summary>
    event Action? NodeExpansionChangedByUser;

    /// <summary>Raised on every pointer move over the tree, with the node under the pointer.</summary>
    event Action<GumTreeNode?>? PointerMoved;

    /// <summary>
    /// Raised for key presses the tree's own navigation left unhandled. The view marks the key
    /// handled when <see cref="GumKeyEventArgs.Handled"/> comes back true.
    /// </summary>
    event Action<GumKeyEventArgs>? KeyDown;

    /// <summary>Raised when the tree catches an exception while reacting to input.</summary>
    event Action<Exception>? UnhandledException;

    /// <summary>Raised while nodes are dragged over a row, to decide whether the drop is legal.</summary>
    event EventHandler<TreeDropValidationEventArgs>? ValidateSortingDrop;

    /// <summary>Raised when nodes are dropped onto the tree.</summary>
    event EventHandler<TreeDropEventArgs>? NodeSortingDropped;

    /// <summary>Raised while anything other than tree nodes (files, a palette chip) is dragged over the tree.</summary>
    event EventHandler<TreeExternalDragEventArgs>? ExternalDragOver;

    /// <summary>Raised when files or a palette chip are dropped onto the tree.</summary>
    event EventHandler<TreeExternalDragEventArgs>? ExternalDrop;

    /// <summary>Raised when a drag started from the tree ends, whether it dropped or was cancelled.</summary>
    event Action? DragEnded;

    /// <summary>Raised when the palette's "add to current" action is chosen for a standard type.</summary>
    event Action<string>? AddStandardToCurrentRequested;

    /// <summary>Raised when the palette's "edit defaults" action is chosen for a standard type.</summary>
    event Action<string>? EditStandardDefaultsRequested;
}

/// <summary>Creates the head's <see cref="IElementTreeView"/>. Registered by each head.</summary>
public interface IElementTreeViewFactory
{
    /// <summary>Creates the panel. Called once, on the UI thread, when the tree initializes.</summary>
    IElementTreeView Create();
}

/// <summary>
/// A drag of something other than tree nodes over (or onto) the element tree: dropped files or a
/// Standards palette chip.
/// </summary>
public sealed class TreeExternalDragEventArgs : EventArgs
{
    /// <summary>Creates the event for the given payload and the row under the pointer.</summary>
    public TreeExternalDragEventArgs(string[]? files, string? standardElementTypeName, GumTreeNode? targetNode)
    {
        Files = files;
        StandardElementTypeName = standardElementTypeName;
        TargetNode = targetNode;
    }

    /// <summary>Paths of dropped files, or null when the payload has none.</summary>
    public string[]? Files { get; }

    /// <summary>The standard type of a dragged palette chip, or null.</summary>
    public string? StandardElementTypeName { get; }

    /// <summary>The node whose row is under the pointer, or null.</summary>
    public GumTreeNode? TargetNode { get; }

    /// <summary>Set by the handler during a drag-over: whether a drop here would be accepted.</summary>
    public bool Accepted { get; set; }
}
