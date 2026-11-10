using Gum.Forms.Controls.Primitives;
using Gum.Input;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using GamepadButton = Gum.Input.GamepadButton;

namespace Gum.Forms.Controls;

/// <summary>
/// A control showing a strip of tab headers and the content page of the selected tab. Each entry in
/// <see cref="ItemsControl.Items"/> makes one <see cref="TabItem"/> header in the strip (the visual's
/// InnerPanelInstance) and one <see cref="TabPage"/> in the content panel (the visual's
/// ContentPanelInstance), paired by position.
/// </summary>
/// <remarks>
/// A data item's header comes from <see cref="ItemsControl.VisualTemplate"/> (or the default TabItem
/// visual showing the item's ToString), and its page from <see cref="ContentTemplate"/> with the item as
/// BindingContext. A TabItem added to Items directly uses its <see cref="TabItem.Content"/> for the page.
/// </remarks>
public class TabControl : ItemsControl
{
    /// <summary>The name of the visual's child that holds the pages.</summary>
    public const string ContentPanelInstanceName = "ContentPanelInstance";

    // Initialized before the base constructor runs ReactToVisualChanged.
    private readonly ItemSelection<TabItem> _selection = new ItemSelection<TabItem>();
    private readonly List<TabItem> _tabItems = new List<TabItem>();
    private readonly List<TabPage> _authoredPages = new List<TabPage>();
    // Every page adopted from a Visual. The Visual owns these, so a Visual swap leaves them in place.
    private readonly HashSet<TabPage> _visualOwnedPages = new HashSet<TabPage>();

    private GraphicalUiElement? _contentPanel;
    private bool _isSyncingSelection;
    private bool _isReactingToVisual;
    // What a bound view model was last given: the empty control's -1 and null until the first push.
    private int _lastPushedSelectedIndex = -1;
    private object? _lastPushedSelectedObject;

    /// <summary>The tab headers, in strip order.</summary>
    public ReadOnlyCollection<TabItem> TabItems { get; }

    /// <summary>The pages, paired with <see cref="TabItems"/> by position. Read-only: add tabs through Items.</summary>
    public IReadOnlyList<TabPage> Pages
    {
        get
        {
            List<TabPage> pages = new List<TabPage>(_tabItems.Count);
            foreach (TabItem tab in _tabItems)
            {
                if (tab.Page != null)
                {
                    pages.Add(tab.Page);
                }
            }
            return pages;
        }
    }

    /// <summary>The container that holds the pages, found by the name ContentPanelInstance.</summary>
    public GraphicalUiElement? ContentPanel => _contentPanel;

    VisualTemplate? _contentTemplate;

    /// <summary>
    /// Creates the content of each data item's page. The created visual is placed in a TabPage, with
    /// the item as its BindingContext. Changing it rebuilds every tab.
    /// </summary>
    public VisualTemplate? ContentTemplate
    {
        get => _contentTemplate;
        set
        {
            if (value != _contentTemplate)
            {
                _contentTemplate = value;
                if (Items?.Count > 0)
                {
                    HandleItemsCollectionChanged(this,
                        new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
            }
        }
    }

    /// <summary>
    /// The index of the selected tab, or -1 when none is selected. As with ListBox, adding tabs does
    /// not select one, removing or replacing the selected tab leaves none selected, and an index out
    /// of range is ignored.
    /// </summary>
    public int SelectedIndex
    {
        get => _selection.Count > 0 ? _tabItems.IndexOf(_selection.SelectedRows[0]) : -1;
        set
        {
            if (value > -1 && value < _tabItems.Count)
            {
                SelectTab(_tabItems[value]);
            }
            else if (value == -1)
            {
                SelectTab(null);
            }
        }
    }

    /// <summary>
    /// The item in Items whose tab is selected (the TabItem itself for a tab added directly), or null.
    /// </summary>
    public object? SelectedObject
    {
        get => _selection.Count > 0 ? _selection.SelectedRows[0].DataObject : null;
        set
        {
            int index = value == null ? -1 : ItemSelection<TabItem>.IndexOfData(_tabItems, value);
            SelectTab(index > -1 ? _tabItems[index] : null);
        }
    }

    /// <summary>
    /// Raised whenever the selected tab changes. AddedItems and RemovedItems hold the data objects.
    /// </summary>
    public event Action<object, SelectionChangedEventArgs>? SelectionChanged;

    public TabControl() : base()
    {
        TabItems = new ReadOnlyCollection<TabItem>(_tabItems);
    }

    public TabControl(InteractiveGue visual) : base(visual)
    {
        TabItems = new ReadOnlyCollection<TabItem>(_tabItems);
    }

    /// <summary>
    /// Adds a tab showing <paramref name="header"/> with <paramref name="content"/> (a FrameworkElement,
    /// a GraphicalUiElement or a TabPage) in its page.
    /// </summary>
    /// <returns>The added tab.</returns>
    public TabItem AddTab(object? header, object? content = null)
    {
        TabItem tab = new TabItem
        {
            Header = header,
            Content = content
        };
        Items?.Add(tab);
        return tab;
    }

    #region Visual

    protected override void ReactToVisualChanged()
    {
        object? selectedData = _selection.Count > 0 ? _selection.SelectedRows[0].DataObject : null;
        bool hadSelection = _selection.Count > 0;

        _contentPanel = Visual.GetGraphicalUiElementByName(ContentPanelInstanceName);
        _authoredPages.Clear();
        if (_contentPanel != null)
        {
            foreach (GraphicalUiElement child in _contentPanel.Children)
            {
                if (child is InteractiveGue { FormsControlAsObject: TabPage page })
                {
                    _authoredPages.Add(page);
                }
            }
        }

        _isReactingToVisual = true;
        try
        {
            base.ReactToVisualChanged();
        }
        finally
        {
            _isReactingToVisual = false;
        }

        if (!CanAdoptVisualAuthoredItems)
        {
            // Items belongs to the caller, so the Visual's authored tabs were taken out; their pages go too.
            foreach (TabPage page in _authoredPages)
            {
                if (!IsPageInUse(page))
                {
                    page.Visual.Parent = null;
                }
            }
        }

        if (hadSelection)
        {
            // Rows were rebuilt for the new Visual, so the selection is restored by data object, as
            // ListBox does; one with no row in the new Visual is dropped.
            int index = ItemSelection<TabItem>.IndexOfData(_tabItems, selectedData);
            if (index > -1)
            {
                SelectTab(_tabItems[index]);
            }
            else
            {
                SelectionChangedEventArgs args = new SelectionChangedEventArgs();
                args.RemovedItems.Add(selectedData);
                RaiseSelectionChanged(args);
            }
        }
        PushSelectionIfChanged();
    }

    private bool IsPageInUse(TabPage page)
    {
        foreach (TabItem tab in _tabItems)
        {
            if (tab.Page == page)
            {
                return true;
            }
        }
        return false;
    }

    /// <inheritdoc/>
    protected override bool IsAuthoredRow(FrameworkElement control) => control is TabItem;

    /// <inheritdoc/>
    protected override void AdoptAuthoredRow(FrameworkElement row, int rowIndex)
    {
        TabItem tab = (TabItem)row;
        tab.DataObject = tab;
        AdoptVisualAuthoredItem(tab, rowIndex);

        if (tab.Page == null && rowIndex < _authoredPages.Count)
        {
            tab.Page = _authoredPages[rowIndex];
            _visualOwnedPages.Add(tab.Page);
        }
        InsertTab(tab, rowIndex);
    }

    #endregion

    #region Item creation

    private const string PerItemBindingContextJustification =
        "Automatic per-item BindingContext assignment when populating from a bound collection, as " +
        "ListBox does. Any by-name VM member resolution happens in the templates' own bindings, which " +
        "already carry this warning at the SetBinding call that created them.";

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = PerItemBindingContextJustification)]
    protected override FrameworkElement CreateNewItemFrameworkElement(object o)
    {
        if (o is TabItem directTab)
        {
            directTab.DataObject = directTab;
            return directTab;
        }

        TabItem tab;
        if (FrameworkElementTemplate != null)
        {
            tab = FrameworkElementTemplate.CreateContent() as TabItem
                ?? throw new InvalidOperationException(
                    "A TabControl's FrameworkElementTemplate must create a TabItem.");
        }
        else
        {
            InteractiveGue visual = CreateHeaderVisual(o);
            tab = visual.FormsControlAsObject as TabItem ?? new TabItem(visual);
        }

        tab.DataObject = o;
        tab.BindingContext = o;
        tab.Header = o;
        return tab;
    }

    private static InteractiveGue CreateHeaderVisual(object o)
    {
        if (!DefaultFormsTemplates.TryGetValue(typeof(TabItem), out VisualTemplate? template))
        {
            throw new InvalidOperationException(
                "No default visual is registered for TabItem. Set the TabControl's VisualTemplate, or " +
                "register FrameworkElement.DefaultFormsTemplates[typeof(TabItem)].");
        }
        return template.CreateContent(o, createFormsInternally: true) as InteractiveGue
            ?? throw new InvalidOperationException("The TabItem visual template must create an InteractiveGue.");
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = PerItemBindingContextJustification)]
    protected override void HandleCreatedItemVisual(GraphicalUiElement newVisual, object item)
    {
        // The VisualTemplate path creates the header visual without a Forms control.
        if (newVisual is InteractiveGue interactiveGue)
        {
            TabItem tab = interactiveGue.FormsControlAsObject as TabItem ?? new TabItem(interactiveGue);
            tab.DataObject = item;
            tab.BindingContext = item;
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = PerItemBindingContextJustification)]
    private TabPage CreatePage(TabItem tab)
    {
        object? data = tab.DataObject;
        if (data != null && !ReferenceEquals(data, tab))
        {
            TabPage dataPage = new TabPage();
            if (ContentTemplate != null)
            {
                GraphicalUiElement content = ContentTemplate.CreateContent(data, createFormsInternally: true);
                if (content is InteractiveGue { FormsControlAsObject: TabPage templatePage })
                {
                    dataPage = templatePage;
                }
                else
                {
                    dataPage.ContentContainer.Children.Add(content);
                }
            }
            dataPage.BindingContext = data;
            return dataPage;
        }

        switch (tab.Content)
        {
            case TabPage contentPage:
                return contentPage;
            case FrameworkElement element:
                {
                    TabPage page = new TabPage();
                    page.ContentContainer.Children.Add(element.Visual);
                    return page;
                }
            case GraphicalUiElement visual:
                {
                    TabPage page = new TabPage();
                    page.ContentContainer.Children.Add(visual);
                    return page;
                }
            default:
                return new TabPage();
        }
    }

    #endregion

    #region Collection changes

    protected override void HandleCollectionNewItemCreated(FrameworkElement newItem, int newItemIndex)
    {
        if (newItem is TabItem tab)
        {
            tab.DataObject ??= tab;
            InsertTab(tab, CountTabItemsInPanelBefore(newItemIndex));
        }
    }

    protected override void HandleItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        base.HandleItemsCollectionChanged(sender, e);
        // An insert, removal or move before the selected tab shifts SelectedIndex without changing
        // the selection, so it is pushed here too.
        PushSelectionIfChanged();
    }

    private void InsertTab(TabItem tab, int tabIndex)
    {
        _tabItems.Insert(tabIndex, tab);
        tab.IsSelectedChangedInternal += HandleTabIsSelectedChanged;

        tab.Page ??= CreatePage(tab);
        tab.Page.IsSelected = tab.IsSelected;
        PlacePage(tab.Page, tabIndex);
    }

    /// <summary>Puts the page in the content panel before the page of the next tab that has one there.</summary>
    private void PlacePage(TabPage page, int tabIndex)
    {
        if (_contentPanel == null || page.Visual.Parent == _contentPanel)
        {
            return;
        }

        int insertAt = _contentPanel.Children.Count;
        for (int i = tabIndex + 1; i < _tabItems.Count; i++)
        {
            GraphicalUiElement? nextPageVisual = _tabItems[i].Page?.Visual;
            if (nextPageVisual != null && nextPageVisual.Parent == _contentPanel)
            {
                insertAt = _contentPanel.Children.IndexOf(nextPageVisual);
                break;
            }
        }
        _contentPanel.Children.Insert(insertAt, page.Visual);
        page.Visual.Parent = _contentPanel;
    }

    private int CountTabItemsInPanelBefore(int panelIndex)
    {
        int count = 0;
        if (InnerPanel == null)
        {
            return count;
        }
        for (int i = 0; i < panelIndex && i < InnerPanel.Children.Count; i++)
        {
            if (InnerPanel.Children[i] is InteractiveGue { FormsControlAsObject: TabItem })
            {
                count++;
            }
        }
        return count;
    }

    protected override void HandleCollectionItemRemoved(int indexToRemoveFrom, FrameworkElement removedItem)
    {
        if (removedItem is TabItem tab)
        {
            RemoveTab(tab, detachVisualOwnedPage: true);
            _selection.HandleRowRemoved(tab);
        }
    }

    private void RemoveTab(TabItem tab, bool detachVisualOwnedPage)
    {
        _tabItems.Remove(tab);
        tab.IsSelectedChangedInternal -= HandleTabIsSelectedChanged;
        TabPage? page = tab.Page;
        if (page != null && (detachVisualOwnedPage || !_visualOwnedPages.Contains(page)) &&
            page.Visual.Parent == _contentPanel && _contentPanel != null)
        {
            page.Visual.Parent = null;
        }
        if (page != null && _visualOwnedPages.Contains(page))
        {
            // A Visual-owned page is only ever paired while its Visual is current.
            tab.Page = null;
        }
    }

    protected override void HandleCollectionItemsRemoved()
    {
        if (!_selection.HasRemovedSelection)
        {
            return;
        }

        // As with ListBox, removing the selected tab leaves nothing selected.
        SelectionChangedEventArgs args = new SelectionChangedEventArgs();
        _selection.TakeRemovedSelection(args.RemovedItems);
        SyncSelection(args);
        RaiseSelectionChanged(args);
    }

    protected override void HandleCollectionReset()
    {
        bool hadSelection = _selection.Count > 0;
        SelectionChangedEventArgs args = new SelectionChangedEventArgs();
        if (hadSelection)
        {
            args.RemovedItems.Add(_selection.SelectedRows[0].DataObject);
        }

        foreach (TabItem tab in _tabItems.ToArray())
        {
            // A Visual swap leaves the old Visual's own pages where they are.
            RemoveTab(tab, detachVisualOwnedPage: !_isReactingToVisual);
            tab.IsSelected = false;
        }
        _selection.Clear();

        if (hadSelection && !_isReactingToVisual)
        {
            RaiseSelectionChanged(args);
        }
    }

    protected override void HandleCollectionItemMoved(int oldIndex, int newIndex)
    {
        // Rebuild the tab order from the strip, then put the pages in the same order.
        _tabItems.Clear();
        if (InnerPanel != null)
        {
            foreach (GraphicalUiElement child in InnerPanel.Children)
            {
                if (child is InteractiveGue { FormsControlAsObject: TabItem tab })
                {
                    _tabItems.Add(tab);
                }
            }
        }
        ReorderPages();
    }

    private void ReorderPages()
    {
        if (_contentPanel == null)
        {
            return;
        }

        // The slots the pages occupy now, in panel order; the pages fill them in tab order so other
        // children of the content panel keep their places.
        List<int> slots = new List<int>();
        List<GraphicalUiElement> pagesInTabOrder = new List<GraphicalUiElement>();
        foreach (TabItem tab in _tabItems)
        {
            GraphicalUiElement? pageVisual = tab.Page?.Visual;
            if (pageVisual != null && pageVisual.Parent == _contentPanel)
            {
                slots.Add(_contentPanel.Children.IndexOf(pageVisual));
                pagesInTabOrder.Add(pageVisual);
            }
        }
        slots.Sort();

        for (int i = 0; i < pagesInTabOrder.Count; i++)
        {
            int currentIndex = _contentPanel.Children.IndexOf(pagesInTabOrder[i]);
            if (currentIndex != slots[i])
            {
                _contentPanel.Children.Move(currentIndex, slots[i]);
            }
        }
    }

    #endregion

    #region Selection

    private void SelectTab(TabItem? tab)
    {
        bool isAlreadyTheSelection = tab == null
            ? _selection.Count == 0
            : _selection.Count == 1 && _selection.Contains(tab);
        if (isAlreadyTheSelection)
        {
            return;
        }

        SelectionChangedEventArgs args = new SelectionChangedEventArgs();
        if (tab == null)
        {
            _selection.Clear();
        }
        else
        {
            _selection.SelectOnly(tab);
        }
        SyncSelection(args);
        RaiseSelectionChanged(args);
    }

    private void HandleTabIsSelectedChanged(TabItem tab)
    {
        if (_isSyncingSelection)
        {
            return;
        }

        // The tab already flipped its own IsSelected, so it is reported here rather than by the sync.
        SelectionChangedEventArgs args = new SelectionChangedEventArgs();
        if (tab.IsSelected && !_selection.Contains(tab))
        {
            args.AddedItems.Add(tab.DataObject);
            _selection.SelectOnly(tab);
        }
        else if (!tab.IsSelected && _selection.Contains(tab))
        {
            args.RemovedItems.Add(tab.DataObject);
            _selection.Remove(tab);
        }
        else
        {
            return;
        }
        SyncSelection(args);
        RaiseSelectionChanged(args);
    }

    private void SyncSelection(SelectionChangedEventArgs args)
    {
        _isSyncingSelection = true;
        try
        {
            _selection.SyncIsSelected(_tabItems, args.AddedItems, args.RemovedItems);
        }
        finally
        {
            _isSyncingSelection = false;
        }

        foreach (TabItem tab in _tabItems)
        {
            if (tab.Page != null)
            {
                tab.Page.IsSelected = tab.IsSelected;
            }
        }
    }

    private void RaiseSelectionChanged(SelectionChangedEventArgs args)
    {
        if (args.AddedItems.Count > 0 || args.RemovedItems.Count > 0)
        {
            SelectionChanged?.Invoke(this, args);
        }
        PushSelectionIfChanged();
    }

    /// <summary>
    /// Pushes SelectedIndex and SelectedObject to a bound view model when either differs from what was
    /// last pushed, whatever changed it, so each change is pushed once.
    /// </summary>
    private void PushSelectionIfChanged()
    {
        int index = SelectedIndex;
        object? selectedObject = SelectedObject;
        bool objectChanged = !ReferenceEquals(selectedObject, _lastPushedSelectedObject);
        bool indexChanged = index != _lastPushedSelectedIndex;
        // Both are recorded before either push, since a push can re-enter through the view model.
        _lastPushedSelectedObject = selectedObject;
        _lastPushedSelectedIndex = index;
        if (objectChanged)
        {
            PushValueToViewModel(nameof(SelectedObject));
        }
        if (indexChanged)
        {
            PushValueToViewModel(nameof(SelectedIndex));
        }
    }

    #endregion

    #region Input

    public override void OnFocusUpdate()
    {
        foreach (var gamepad in FrameworkElement.GamePadsForUiControl)
        {
            bool isOverridePressed = IsSpatialNavigationOverridePressed(gamepad);
            HandleGamepadNavigation(gamepad);

            if (IsEnabled && !isOverridePressed)
            {
                if (gamepad.ButtonRepeatRate(GamepadButton.DPadLeft) ||
                    gamepad.LeftStick.AsDPadPushedRepeatRate(DPadDirection.Left))
                {
                    StepSelection(forward: false);
                }
                else if (gamepad.ButtonRepeatRate(GamepadButton.DPadRight) ||
                    gamepad.LeftStick.AsDPadPushedRepeatRate(DPadDirection.Right))
                {
                    StepSelection(forward: true);
                }
            }
        }

        bool isKeyboardOverridePressed = IsSpatialNavigationOverridePressedByKeyboard();
        foreach (var keyboard in KeyboardsForUiControl)
        {
            if (!IsEnabled || isKeyboardOverridePressed)
            {
                continue;
            }
            if (keyboard.KeyTyped(Gum.Forms.Input.Keys.Right) == true)
            {
                StepSelection(forward: true);
            }
            if (keyboard.KeyTyped(Gum.Forms.Input.Keys.Left) == true)
            {
                StepSelection(forward: false);
            }
        }

        base.HandleKeyboardFocusUpdate();
        RaiseFocusUpdate();
    }

    /// <summary>Selects the next or previous enabled tab, staying at either end.</summary>
    private void StepSelection(bool forward)
    {
        int target = _selection.GetStepTarget(_tabItems, forward);
        if (target > -1)
        {
            SelectTab(_tabItems[target]);
        }
    }

    #endregion
}
