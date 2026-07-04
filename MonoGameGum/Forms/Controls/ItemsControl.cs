using Gum.Wireframe;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

#if FRB
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;
#endif

#if !FRB
namespace Gum.Forms.Controls;

#endif

public class ItemsControl : ScrollViewer
{
    #region Fields/Properties

    [Obsolete("Use VisualTemplate")]
    protected Type? ItemGumType { get; set; }

    public virtual string DisplayMemberPath
    {
        get;
        set;
    } = string.Empty;


    IList? items = new ObservableCollection<object>();
    /// <summary>
    /// The items contained by this ItemsControl. This can contain regular
    /// data such as strings, instances of ViewModels, or FrameworkElement instances
    /// such as ListBoxItem instances.
    /// 
    /// Typically the Items is assigned or bound to an ObservableCollection. In this case
    /// the ItemsControl automatically creates FrameworkElements (such as ListBoxItem instances)
    /// in response to items being added or removed from the Items list.
    /// </summary>
    /// <remarks>
    /// The Items list is not guaranteed to reflect the items contained in the ItemsControl
    /// if Visual elements have been added directly to the InnerPanel. This can happen if the
    /// Visual's InnerPanel's Children are directly added to, or if an instance of an ItemsControl,
    /// such as a ListBox, is loaded from the Gum tool pre-filled with ListBoxItems. 
    /// 
    /// If items are not added directly to the InnerPanel, then the Items list and the internal InnerPanel
    /// Children will remain in sync since the ItemsControl automatically creates FrameworkElement instances
    /// in response to the Items.
    /// </remarks>
    public IList? Items
    {
        get => items;
        set
        {
            if (items != value)
            {
                var wasSuppressed = GraphicalUiElement.IsAllLayoutSuspended;
                GraphicalUiElement.IsAllLayoutSuspended = true;

                if (items != null)
                {
                    ClearVisualsInternal();
                    HandleItemsCollectionChanged(this,
                        new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }

                if (items is INotifyCollectionChanged notifyCollectionChanged)
                {
                    notifyCollectionChanged.CollectionChanged -= HandleItemsCollectionChanged;
                }
                items = value;
                ForceUpdateToItems();

                GraphicalUiElement.IsAllLayoutSuspended = wasSuppressed;
                if (!wasSuppressed)
                {
                    Visual.ResumeLayout(recursive: true);
                }
            }
        }
    }

    private void ForceUpdateToItems()
    {
        if (items is INotifyCollectionChanged newNotifyCollectionChanged)
        {
            newNotifyCollectionChanged.CollectionChanged += HandleItemsCollectionChanged;
        }

        if (items?.Count > 0)
        {
            // refresh!
            var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                items, startingIndex: 0);
            HandleItemsCollectionChanged(this, args);
        }
    }

    FrameworkElementTemplate? _frameworkElementTemplate;
    public FrameworkElementTemplate? FrameworkElementTemplate 
    {
        get => _frameworkElementTemplate;
        set
        {
            if(value != _frameworkElementTemplate)
            {
                _frameworkElementTemplate = value;
                var wasSuppressed = GraphicalUiElement.IsAllLayoutSuspended;
                GraphicalUiElement.IsAllLayoutSuspended = true;

                ClearVisualsInternal();

                if (items?.Count > 0)
                {
                    // refresh!
                    var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                        items, startingIndex: 0);
                    HandleItemsCollectionChanged(this, args);
                }

                GraphicalUiElement.IsAllLayoutSuspended = wasSuppressed;
                if (!wasSuppressed)
                {
                    Visual.ResumeLayout(recursive: true);
                }
            }
        }
    }

    VisualTemplate visualTemplate;
    public VisualTemplate VisualTemplate
    {
        get => visualTemplate;
        set
        {
            if (value != visualTemplate)
            {
                visualTemplate = value;

                if (items != null)
                {
                    ClearVisualsInternal();

                    if (items.Count > 0)
                    {
                        // refresh!
                        var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items, startingIndex: 0);
                        HandleItemsCollectionChanged(this, args);
                    }
                }
            }
        }
    }

    public event EventHandler<NotifyCollectionChangedEventArgs> ItemsCollectionChanged;

    public Orientation? Orientation
    {
        get
        {
            if(InnerPanel?.ChildrenLayout == global::Gum.Managers.ChildrenLayout.TopToBottomStack)
            {
                return Controls.Orientation.Vertical;
            }
            else if(InnerPanel?.ChildrenLayout == global::Gum.Managers.ChildrenLayout.LeftToRightStack)
            {
                return Controls.Orientation.Horizontal;
            }
            else
            {
                return null;
            }
        }
        set
        {
            if(value.HasValue && InnerPanel != null)
            {
                if(value == Controls.Orientation.Horizontal)
                {
                    InnerPanel.ChildrenLayout =
                        global::Gum.Managers.ChildrenLayout.LeftToRightStack;
                }
                else
                {
                    InnerPanel.ChildrenLayout =
                        global::Gum.Managers.ChildrenLayout.TopToBottomStack;
                }
            }
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// Occurs when an item is clicked. A click is defined by the cursor
    /// primary button being pressed last frame, and released this frame.
    /// </summary>
    public event EventHandler ItemClicked;
    /// <summary>
    /// Occurs when an item is pushed. A push is defined by the cursor
    /// primary button being released last frame, and pressed this frame.
    /// </summary>
    public event EventHandler ItemPushed;

    #endregion

    #region Constructor/Initialize

    public ItemsControl() : base()
    {
        ForceUpdateToItems();
    }

    public ItemsControl(InteractiveGue visual) : base(visual)
    {
        ForceUpdateToItems();
    }

    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();

        if (InnerPanel != null)
        {
            InnerPanel.Children.CollectionChanged += HandleInnerPanelCollectionChanged;
        }
    }

    #endregion

    #region Create New Item

    protected virtual FrameworkElement CreateNewItemFrameworkElement(object o)
    {
        if(FrameworkElementTemplate != null)
        {
            var frameworkElement = FrameworkElementTemplate.CreateContent();
            frameworkElement.BindingContext = o;
            return frameworkElement;
        }
        else
        {

            var label = new Label();
            label.Text = o?.ToString() ?? string.Empty;
            label.BindingContext = o;
            return label;
        }
    }

    protected virtual InteractiveGue CreateNewVisual(object vm)
    {
        if (VisualTemplate != null || DefaultFormsTemplates.ContainsKey(typeof(ListBoxItem)))
        {
            var template = VisualTemplate ?? DefaultFormsTemplates[typeof(ListBoxItem)];
            var toReturn = template.CreateContent(vm);

            if (toReturn != null && toReturn is not InteractiveGue)
            {
                throw new InvalidOperationException(
                    "The visual template for this ListBox returned an instance which did not inherit from InteractiveGue. This is a requirement.");
            }


            return toReturn as InteractiveGue;
        }
        else
        {
            var listBoxItemGumType = ItemGumType;

#pragma warning disable CS0618 // we need this to support old projects
            if (listBoxItemGumType == null && DefaultFormsComponents.ContainsKey(typeof(ListBoxItem)))
            {
                listBoxItemGumType = DefaultFormsComponents[typeof(ListBoxItem)];
            }
#pragma warning restore CS0618 // Type or member is obsolete
#if FULL_DIAGNOSTICS
            if (listBoxItemGumType == null)
            {
                throw new Exception($"This {GetType().Name} named {this.Name} does not have a ItemGumType specified, " +
                    $"nor does the DefaultFormsTemplates have an entry for ListBoxItem. " +
                    "This property must be set before adding any items");
            }
#endif
            // vic says - this uses reflection, could be made faster, somehow...

            var gumConstructor = listBoxItemGumType.GetConstructor(new[] { typeof(bool), typeof(bool) });
            var visual = gumConstructor.Invoke(new object[] { true, true }) as InteractiveGue;
            return visual;
        }
    }

    #endregion

    /// <inheritdoc/>
    protected override void ReactToVisualRemoved()
    {
        base.ReactToVisualRemoved();

        if (InnerPanel != null)
        {
            InnerPanel.Children.CollectionChanged -= HandleInnerPanelCollectionChanged;
        }
    }

    private void HandleInnerPanelCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                {

                    int absoluteIndex = e.NewStartingIndex;
                    foreach (var item in e.NewItems)
                    {
                        var asGue = item as InteractiveGue;

                        var newFrameworkItem = asGue?.FormsControlAsObject as FrameworkElement;

                        if (newFrameworkItem != null)
                        {
                            HandleCollectionNewItemCreated(newFrameworkItem, absoluteIndex);
                        }
                        absoluteIndex++;
                    }
                }
                break;
            case NotifyCollectionChangedAction.Move:
                // todo - we need to raise an event here when items get moved
                // https://github.com/vchelaru/Gum/issues/557
                break;
            case NotifyCollectionChangedAction.Remove:
                {
                    if(e.OldItems != null)
                    {
                        // Reverse order this so that as we are removing, the internal list count change doesn't
                        // cause an out of bounds exception
                        for(int i = e.OldItems.Count - 1; i > -1; i--)
                        {
                            var indexInItems = e.OldStartingIndex + i;
                            var asGue = e.OldItems[i] as InteractiveGue;
                            var newFrameworkItem = asGue?.FormsControlAsObject as FrameworkElement;
                            if (newFrameworkItem != null)
                            {
                                HandleCollectionItemRemoved(indexInItems, newFrameworkItem);
                            }
                        }
                    }
                }
                break;
            case NotifyCollectionChangedAction.Replace:
                var index = e.NewStartingIndex;

                //var listItem = InnerPanel.Children[index];
                HandleCollectionReplace(index);
                break;
            case NotifyCollectionChangedAction.Reset:
                HandleCollectionReset();

                break;
        }
    }

    #region Decorations (issue #3305)

    // Decorations are inert visuals (separators, headers, chrome) that live in InnerPanel.Children
    // so they render between rows, but are in neither Items nor (for ListBox) ListBoxItems. Each is
    // anchored to a data item so it follows that item on reorder and is removed with it. Because a
    // decoration occupies an InnerPanel slot without an Items slot, the Items <-> InnerPanel index
    // mapping is no longer 1:1; the translation helpers here keep item visuals inserted, moved, and
    // removed at the correct panel slot. Follow-up to the reference-based selection work in #556.

    private class DecorationInfo
    {
        public GraphicalUiElement Visual = null!;
        public object? Anchor;
        public bool After;
    }

    private readonly List<DecorationInfo> _decorations = new();
    private bool _isReconcilingDecorations;

    /// <summary>
    /// Adds an inert decoration (such as a separator) after the last item currently in Items. If
    /// Items is empty the decoration is placed at the end of the panel. The decoration is never
    /// added to Items and never becomes selectable. See issue #3305.
    /// </summary>
    public void AddDecoration(GraphicalUiElement visual)
    {
        object? anchor = items != null && items.Count > 0 ? items[items.Count - 1] : null;
        AddDecorationInternal(visual, anchor, after: true);
    }

    /// <summary>
    /// Adds an inert decoration immediately after the given item's row. The decoration follows the
    /// item if the list is reordered and is removed if the item is removed. See issue #3305.
    /// </summary>
    public void InsertDecorationAfter(object item, GraphicalUiElement visual)
    {
        if (items == null || !items.Contains(item))
        {
            throw new ArgumentException("The anchor item is not in the Items collection.", nameof(item));
        }
        AddDecorationInternal(visual, item, after: true);
    }

    /// <summary>
    /// Adds an inert decoration immediately before the given item's row. The decoration follows the
    /// item if the list is reordered and is removed if the item is removed. See issue #3305.
    /// </summary>
    public void InsertDecorationBefore(object item, GraphicalUiElement visual)
    {
        if (items == null || !items.Contains(item))
        {
            throw new ArgumentException("The anchor item is not in the Items collection.", nameof(item));
        }
        AddDecorationInternal(visual, item, after: false);
    }

    /// <summary>
    /// Removes a decoration previously added via <see cref="AddDecoration"/> /
    /// <see cref="InsertDecorationAfter"/> / <see cref="InsertDecorationBefore"/>. Returns true if
    /// the visual was a tracked decoration. See issue #3305.
    /// </summary>
    public bool RemoveDecoration(GraphicalUiElement visual)
    {
        var info = _decorations.FirstOrDefault(d => d.Visual == visual);
        if (info == null)
        {
            return false;
        }
        _decorations.Remove(info);
        if (visual.Parent == InnerPanel)
        {
            visual.Parent = null;
        }
        return true;
    }

    private void AddDecorationInternal(GraphicalUiElement visual, object? anchor, bool after)
    {
        var existing = _decorations.FirstOrDefault(d => d.Visual == visual);
        if (existing != null)
        {
            // Re-anchor an already-tracked decoration rather than adding it twice.
            existing.Anchor = anchor;
            existing.After = after;
        }
        else
        {
            _decorations.Add(new DecorationInfo { Visual = visual, Anchor = anchor, After = after });
            if (InnerPanel != null && visual.Parent != InnerPanel)
            {
                // The final position is fixed up by ReconcileDecorations; just attach for now.
                visual.Parent = InnerPanel;
            }
        }

        ReconcileDecorations();
    }

    /// <summary>
    /// Whether the given InnerPanel child is a decoration rather than an item visual. Used to skip
    /// decoration slots when translating between Items-space and InnerPanel-space indices.
    /// </summary>
    private bool IsDecoration(GraphicalUiElement child)
    {
        for (int i = 0; i < _decorations.Count; i++)
        {
            if (_decorations[i].Visual == child)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns the InnerPanel.Children index of the item visual at the given Items-space index
    /// (skipping decorations), or InnerPanel.Children.Count if there is no such item visual (for
    /// example when appending). See issue #3305.
    /// </summary>
    protected int GetPanelIndexForItemIndex(int itemIndex)
    {
        if (InnerPanel == null)
        {
            return 0;
        }
        var children = InnerPanel.Children;
        int seen = 0;
        for (int p = 0; p < children.Count; p++)
        {
            if (IsDecoration(children[p]))
            {
                continue;
            }
            if (seen == itemIndex)
            {
                return p;
            }
            seen++;
        }
        return children.Count;
    }

    /// <summary>
    /// Returns the Items-space index of the item visual at the given InnerPanel.Children index
    /// (skipping decorations), or -1 if the panel index is out of range or refers to a decoration.
    /// Inverse of <see cref="GetPanelIndexForItemIndex"/>. Resolving an item's position this way -
    /// by counting panel positions rather than searching Items for a matching value - lets callers
    /// (e.g. ListBox's selection tracking) recover the exact Items-space index of a specific row
    /// even when the same data value appears at more than one index (issue #3509).
    /// </summary>
    protected int GetItemIndexForPanelIndex(int panelIndex)
    {
        if (InnerPanel == null || panelIndex < 0 || panelIndex >= InnerPanel.Children.Count)
        {
            return -1;
        }
        var children = InnerPanel.Children;
        if (IsDecoration(children[panelIndex]))
        {
            return -1;
        }
        int itemIndex = 0;
        for (int p = 0; p < panelIndex; p++)
        {
            if (!IsDecoration(children[p]))
            {
                itemIndex++;
            }
        }
        return itemIndex;
    }

    /// <summary>
    /// Moves the item visual at <paramref name="oldItemIndex"/> (Items space) so it becomes the
    /// item visual at <paramref name="newItemIndex"/>, ignoring decoration children. Decorations
    /// are repositioned afterward by <see cref="ReconcileDecorations"/>. See issue #3305.
    /// </summary>
    private void MoveItemVisual(int oldItemIndex, int newItemIndex)
    {
        if (InnerPanel == null)
        {
            return;
        }
        var children = InnerPanel.Children;
        int from = GetPanelIndexForItemIndex(oldItemIndex);
        if (from < 0 || from >= children.Count)
        {
            return;
        }

        // Compute the destination in post-removal index space (what ObservableCollection.Move
        // expects): walk the item visuals other than the one being moved and find the slot the
        // moved visual must occupy to become the newItemIndex-th item visual.
        int to = children.Count - 1; // default: append at the end after removal
        int seen = 0;
        for (int p = 0; p < children.Count; p++)
        {
            if (p == from || IsDecoration(children[p]))
            {
                continue;
            }
            if (seen == newItemIndex)
            {
                to = p < from ? p : p - 1;
                break;
            }
            seen++;
        }

        children.Move(from, to);
    }

    /// <summary>
    /// Drops decorations whose anchor item was removed and repositions the survivors adjacent to
    /// their anchor. Only decoration visuals are moved; item visuals keep their relative order so
    /// ListBox's ListBoxItems stays aligned. See issue #3305.
    /// </summary>
    protected void ReconcileDecorations()
    {
        if (InnerPanel == null || _decorations.Count == 0 || _isReconcilingDecorations)
        {
            return;
        }

        _isReconcilingDecorations = true;
        try
        {
            var children = InnerPanel.Children;

            // Drop decorations whose anchor item is no longer present.
            for (int i = _decorations.Count - 1; i >= 0; i--)
            {
                var decoration = _decorations[i];
                bool anchorMissing = decoration.Anchor != null &&
                    (items == null || !items.Contains(decoration.Anchor));
                if (anchorMissing)
                {
                    if (decoration.Visual.Parent == InnerPanel)
                    {
                        decoration.Visual.Parent = null;
                    }
                    _decorations.RemoveAt(i);
                }
            }

            // Detach surviving decorations so the panel holds item visuals only, in Items order
            // (the base maintains that order). Decorations have no forms control, so removing and
            // re-adding them does not touch Items or ListBoxItems.
            foreach (var decoration in _decorations)
            {
                if (decoration.Visual.Parent == InnerPanel)
                {
                    decoration.Visual.Parent = null;
                }
            }

            // Re-insert each decoration adjacent to its anchor.
            foreach (var decoration in _decorations)
            {
                int insertIndex = GetDecorationInsertIndex(decoration);
                if (insertIndex < 0)
                {
                    insertIndex = 0;
                }
                else if (insertIndex > children.Count)
                {
                    insertIndex = children.Count;
                }
                children.Insert(insertIndex, decoration.Visual);
                decoration.Visual.Parent = InnerPanel;
            }
        }
        finally
        {
            _isReconcilingDecorations = false;
        }
    }

    private int GetDecorationInsertIndex(DecorationInfo decoration)
    {
        var children = InnerPanel!.Children;
        if (decoration.Anchor == null)
        {
            return decoration.After ? children.Count : 0;
        }

        int anchorItemIndex = items?.IndexOf(decoration.Anchor) ?? -1;
        if (anchorItemIndex < 0)
        {
            return children.Count;
        }

        int anchorPanelIndex = GetPanelIndexForItemIndex(anchorItemIndex);
        return decoration.After ? anchorPanelIndex + 1 : anchorPanelIndex;
    }

    #endregion

    #region Event Handler methods

    protected virtual void HandleItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                {
                    // e.NewStartingIndex is an Items-space index. A decoration occupies an
                    // InnerPanel.Children slot without an Items slot, so translate to the matching
                    // panel slot (the position of the i-th item visual, skipping decorations) so a
                    // new item added after a decoration lands in the correct place. See issue #3305.
                    int index = _decorations.Count > 0
                        ? GetPanelIndexForItemIndex(e.NewStartingIndex)
                        : e.NewStartingIndex;

                    var shouldSuppressLayout = e.NewItems.Count > 0 && InnerPanel != null;
                    var wasSuppressed = GraphicalUiElement.IsAllLayoutSuspended;
                    if (shouldSuppressLayout)
                    {
                        GraphicalUiElement.IsAllLayoutSuspended = true;
                    }

                    foreach (var item in e.NewItems)
                    {
                        GraphicalUiElement? newVisual = null;
                        if (item is FrameworkElement existingItemFrameworkElement)
                        {
                            newVisual = existingItemFrameworkElement.Visual;
                        }
                        else if(item is GraphicalUiElement existingItemGue)
                        {
                            newVisual = existingItemGue;
                        }
                        else if(this.VisualTemplate != null)
                        {
                            // August 19, 2025
                            // Not sure if this
                            // should set createFormsInternally
                            // to true. ItemsControls probably want
                            // this true, but ListBoxes probably don't
                            // since they want to force a ListBoxItem. For
                            // now let's make it false, and revisit later.
                            newVisual = VisualTemplate.CreateContent(item, createFormsInternally:false);

                            // the visual template should respect the item (BindingContext), but just in case it doesn't:
                            if(newVisual is InteractiveGue interactivegue)
                            {
                                interactivegue.BindingContext = item;
                            }

                            HandleCreatedItemVisual(newVisual, item);
                        }
                        else
                        {
                            newVisual = CreateNewItemFrameworkElement(item).Visual;

                        }

                        if (newVisual.Parent != InnerPanel)
                        {
                            InnerPanel?.Children.Insert(index, newVisual);
                            newVisual.Parent = InnerPanel;
                        }
                        // handled by the panel being updated:
                        //HandleCollectionNewItemCreated(newItem, index);

                        index++;
                    }
                    if (shouldSuppressLayout)
                    {
                        GraphicalUiElement.IsAllLayoutSuspended = wasSuppressed;
                        if (!wasSuppressed)
                        {
                            // Resume regardless of IsVisible. ResumeLayout(recursive: true)
                            // calls UpdateFontRecursive which is what realizes any
                            // IsFontDirty=true TextRuntime children. The dirty flag is set
                            // during item creation because IsAllLayoutSuspended above
                            // deferred the font load — if we skip the resume when the
                            // ItemsControl happens to be invisible, those deferred loads
                            // never get processed and the items render with the renderer's
                            // fallback font when the control is later shown.
                            //
                            // Concrete repro: a ComboBox's dropdown ListBox is created with
                            // Visible=false. Items added to it before the dropdown is opened
                            // would have stale font state until first open, with no path
                            // to realize the font even at open time. See
                            // ListBoxTests.ItemsAddedWhileInvisible_ShouldHaveFontsResolved.
                            InnerPanel?.ResumeLayout(recursive: true);
                        }
                    }
                }

                break;
            case NotifyCollectionChangedAction.Move:
                var oldIndex = e.OldStartingIndex;
                var newIndex = e.NewStartingIndex;

                // need to move the item to the new index:
                if (InnerPanel != null)
                {
                    if (_decorations.Count > 0)
                    {
                        // Indices are Items-space; move only the corresponding item visual among
                        // the item visuals, leaving decorations to be repositioned by the reconcile
                        // pass below so they follow their anchor item. See issue #3305.
                        MoveItemVisual(oldIndex, newIndex);
                    }
                    else if (oldIndex < InnerPanel.Children.Count)
                    {
                        InnerPanel.Children.Move(oldIndex, newIndex);
                    }
                }

                HandleCollectionItemMoved(e.OldStartingIndex, e.NewStartingIndex);

                break;
            case NotifyCollectionChangedAction.Remove:
                {
                    // e.OldStartingIndex is an Items-space index; translate to the panel slot of the
                    // removed item's visual so a decoration occupying an earlier slot can't shift the
                    // removal onto the wrong child. See issue #3305.
                    var index = _decorations.Count > 0
                        ? GetPanelIndexForItemIndex(e.OldStartingIndex)
                        : e.OldStartingIndex;

                    if (InnerPanel != null && index >= 0 && index < InnerPanel.Children.Count)
                    {
                        var listItem = InnerPanel.Children[index];
                        listItem.Parent = null;
                    }

                    //HandleCollectionItemRemoved(index);
                }
                break;
            case NotifyCollectionChangedAction.Replace:
                {
                    //var index = e.NewStartingIndex;

                    //var listItem = InnerPanel.Children[index];
                    //HandleCollectionReplace(index);

                }
                break;
            case NotifyCollectionChangedAction.Reset:
                ClearVisualsInternal();
                break;
        }

        // Items changed, so reposition decorations relative to their anchor items and drop any
        // whose anchor was removed. See issue #3305.
        if (_decorations.Count > 0)
        {
            ReconcileDecorations();
        }

        ItemsCollectionChanged?.Invoke(sender, e);
    }

    protected virtual void HandleCreatedItemVisual(GraphicalUiElement newVisual, object item)
    {

    }

    protected virtual void HandleCollectionNewItemCreated(FrameworkElement newItem, int newItemIndex) { }
    protected virtual void HandleCollectionItemRemoved(int indexToRemoveFrom, FrameworkElement removedItem) { }
    protected virtual void HandleCollectionReset() { }
    protected virtual void HandleCollectionReplace(int index) { }
    protected virtual void HandleCollectionItemMoved(int oldIndex, int newIndex) { }

    private void ClearVisualsInternal()
    {
        // Decorations are anchored to items; clearing the items invalidates every anchor, so drop
        // the decoration bookkeeping too (their visuals are removed by the Children.Clear below).
        // See issue #3305.
        foreach (var decoration in _decorations)
        {
            if (decoration.Visual.Parent == InnerPanel)
            {
                decoration.Visual.Parent = null;
            }
        }
        _decorations.Clear();

        if (InnerPanel != null)
        {
            InnerPanel.Children!.Clear();

            for (int i = InnerPanel.Children.Count - 1; i > -1; i--)
            {
                InnerPanel.Children[i].Parent = null;
            }
        }
    }


    protected void OnItemClicked(object sender, EventArgs args)
    {
        ItemClicked?.Invoke(sender, args);
    }

    protected void OnItemPushed(object sender, EventArgs args)
    {
        ItemPushed?.Invoke(sender, args);
    }

    #endregion

    #region Update To

#if FRB
    protected override void OnBindingContextChanged(object sender, BindingContextChangedEventArgs args)
    {
        if (IsDataBound(nameof(Items)) && 
            args is { OldBindingContext: not null, NewBindingContext: null })
        {
            this.Items = null;
        }
    }
#endif
    #endregion
}
