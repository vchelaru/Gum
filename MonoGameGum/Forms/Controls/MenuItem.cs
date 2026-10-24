using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.Wireframe;
using RenderingLibrary;

#if FRB
using FlatRedBall.Input;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Gui;
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;
#endif

#if !FRB
namespace Gum.Forms.Controls;
#endif

public class MenuItem : ItemsControl
#if !FRB
    , Gum.Wireframe.IUpdateEveryFrame
#endif
{
    #region Fields/Properties

    public const string MenuItemCategoryState = "MenuItemCategoryState";
    internal double timeOpened = -1;

    bool isSelected;
    public bool IsSelected
    {
        get { return isSelected; }
        set
        {
            if (value != isSelected)
            {
                isSelected = value;

                if (isSelected)
                {
                    Selected?.Invoke(this, EventArgs.Empty);
                }
                // force it or else it won't revert to highlighted until the user moves the mouse
                UpdateState(force: true);

                if(isSelected == false)
                {
                    HidePopupRecursively();
                }
                else
                {
                    TryShowPopup();
                }
            }
        }
    }

    // Tracks whether Clicked has already been raised for the current mouse hold so
    // a re-fired push edge cannot raise it repeatedly while the button stays down.
    // See HandlePush and issue #3077.
    bool _clickRaisedDuringPush;

    protected List<MenuItem> MenuItemsInternal = new List<MenuItem>();

    GraphicalUiElement? text;
    protected global::RenderingLibrary.Graphics.IText? coreText;

    GraphicalUiElement? SubmenuIndicatorInstance;

    internal bool SelectOnHighlight { get; set; } = false;

    /// <summary>
    /// Gets or sets the menu item header text. Setting this property applies localization
    /// if a <see cref="Gum.Localization.LocalizationService"/> is registered.
    /// To bypass localization, use <see cref="SetHeaderNoTranslate"/>.
    /// </summary>
    public virtual string? Header
    {
        get
        {
#if FULL_DIAGNOSTICS
            ReportMissingTextInstance();
#endif
            return coreText?.RawText;
        }
        set
        {
#if FULL_DIAGNOSTICS
            ReportMissingTextInstance();
#endif
            _codeText.Record(value, translate: true);
            if (value != coreText?.RawText)
            {
                // go through the component instead of the core text object to apply localization and force a layout refresh
                text?.SetProperty("Text", value);
            }
        }
    }

    /// <summary>
    /// Sets the menu item header text without applying localization/translation.
    /// </summary>
    /// <remarks>
    /// This is a method rather than a property because the "no translate" state is not preserved on
    /// the underlying text renderable — only the final string is stored.
    /// Use this for header text that should not be localized.
    /// </remarks>
    public void SetHeaderNoTranslate(string? value)
    {
        _codeText.Record(value, translate: false);
        if (value != coreText?.RawText)
        {
            text?.SetProperty("TextNoTranslate", value);
        }
    }

    bool isHighlighted;
    public bool IsHighlighted
    {
        get => isHighlighted;
        set
        {
            if (isHighlighted != value)
            {
                isHighlighted = value;

                if (isHighlighted)
                {
                    // Mouse hover and keyboard navigation share one highlight per level.
                    HighlightGained?.Invoke(this, EventArgs.Empty);
                }

                if(SelectOnHighlight && isHighlighted)
                {
                    IsSelected = true;
                }
                else
                {
                    UpdateState();
                }
            }
        }
    }

    /// <summary>
    /// Raised when this item becomes highlighted, by the cursor or by keyboard/gamepad navigation, so the
    /// owner can clear the highlight of this item's siblings.
    /// </summary>
    internal event EventHandler? HighlightGained;

    /// <summary>
    /// Sets the highlight from keyboard or gamepad navigation. Unlike <see cref="IsHighlighted"/>, this
    /// never opens the submenu (SelectOnHighlight is for hover switching only), and it shows the
    /// Highlighted state even though the cursor did not move.
    /// </summary>
    internal void SetHighlightFromNavigation(bool value)
    {
        if (isHighlighted == value)
        {
            return;
        }
        isHighlighted = value;
        if (isHighlighted)
        {
            HighlightGained?.Invoke(this, EventArgs.Empty);
        }
        UpdateState(force: true);
    }

    /// <summary>
    /// The MenuItems in the open submenu, in display order. Empty while the submenu is closed.
    /// </summary>
    internal IReadOnlyList<MenuItem> OpenSubItems => MenuItemsInternal;

    /// <summary>
    /// Activates this item from keyboard or gamepad input: selects it and raises <see cref="Clicked"/>.
    /// </summary>
    internal void ActivateFromNavigation()
    {
        IsSelected = true;
        Clicked?.Invoke(this, EventArgs.Empty);
    }

    MenuItem? _parentMenuItem;
    internal MenuItem? ParentMenuItem
    {
        get => _parentMenuItem;
        set
        {
            _parentMenuItem = value;
            RefreshSubmenuIndicatorVisibility();
        }
    }

    public VisualTemplate? ScrollViewerVisualTemplate { get; set; } = null;

    // The sub-item container removed from the Visual in UpdateMenuItems. Kept so its CollectionChanged
    // can be unhooked, and so it can be put back (with its original parent and position) when the Visual
    // is replaced, leaving that Visual intact if it is assigned again.
    GraphicalUiElement? _subscribedSubItemContainer;
    GraphicalUiElement? _subItemContainerOriginalParent;
    int _subItemContainerOriginalIndex;

    #endregion

    #region Events

    public event EventHandler? Selected;
    /// <summary>
    /// Event raised when the item is activated. With a mouse, this fires on push, matching
    /// WPF menu behavior; with touch, it fires on release; with a keyboard or gamepad, it fires
    /// when the focused Menu activates the highlighted item (Enter or the A button).
    /// </summary>
    public event EventHandler? Clicked;

    #endregion

    #region Initialization
    public MenuItem() : base()
    {
    }

    public MenuItem(InteractiveGue visual) : base(visual)
    {
    }

    protected override void ReactToVisualChanged()
    {
#if FRB
        Visual.Push += _=> this.HandlePush(this, EventArgs.Empty);
        Visual.Click += _ => this.HandleClick(this, EventArgs.Empty);
        Visual.RollOn += _ => this.HandleRollOn(this, EventArgs.Empty);
        Visual.RollOff += _ => this.HandleRollOff(this, EventArgs.Empty);
        Visual.RollOver += _ => this.HandleRollOver(this, EventArgs.Empty);
#else
        Visual.Push += this.HandlePush;
        Visual.Click += this.HandleClick;
        Visual.RollOn += this.HandleRollOn;
        Visual.RollOff += this.HandleRollOff;
        Visual.RollOver += this.HandleRollOver;
#endif
        RefreshInternalVisualReferences();

        UpdateMenuItems();   

        // Just in case it needs to set the state to "enabled"
        UpdateState();
        base.ReactToVisualChanged();
    }

    /// <inheritdoc/>
    protected override void ReactToVisualRemoved()
    {
        // The popup belongs to the Visual being removed, so close it before the Visual goes.
        CloseSubmenuForVisualRemoved();

        // Before the base call, which drops the authored sub-items from Items.
        RestoreSubItemContainer();

        base.ReactToVisualRemoved();

        // FRB1 only: these lambdas cannot be unsubscribed when the Visual is replaced, so the old
        // Visual keeps driving this control after a swap (see #6208). Not fixed until a FRB1 project needs it.
#if !FRB
        Visual.Push -= this.HandlePush;
        Visual.Click -= this.HandleClick;
        Visual.RollOn -= this.HandleRollOn;
        Visual.RollOff -= this.HandleRollOff;
        Visual.RollOver -= this.HandleRollOver;
#endif
        Visual.Children.CollectionChanged -= HandleVisualChildrenChanged;

        text = null;
        coreText = null;
        SubmenuIndicatorInstance = null;
    }

    private void UpdateMenuItems()
    {
        var containerInstance = Visual.GetGraphicalUiElementByName(
            "SubItemContainerInstance");

        if (containerInstance != null)
        {
            // Snapshot: detaching a child from the container modifies Children while it is enumerated.
            foreach (var child in containerInstance.Children.ToList())
            {
                if (CanAdoptVisualAuthoredItems &&
                    child is InteractiveGue interactiveGue && interactiveGue.FormsControlAsObject is MenuItem menuItem)
                {
                    child.Parent = null;
                    AdoptVisualAuthoredItem(menuItem);
                }
            }

            containerInstance.Children.CollectionChanged += HandleSubItemContainerChanged;
            _subscribedSubItemContainer = containerInstance;

            _subItemContainerOriginalParent = containerInstance.Parent as GraphicalUiElement;
            _subItemContainerOriginalIndex = _subItemContainerOriginalParent?.Children.IndexOf(containerInstance) ?? -1;
            containerInstance.Parent = null;
        }
    }

    /// <summary>
    /// Puts the authored sub-items and their container back into the Visual being removed, so the
    /// Visual is unchanged if it is assigned again.
    /// </summary>
    private void RestoreSubItemContainer()
    {
        GraphicalUiElement? container = _subscribedSubItemContainer;
        if (container == null)
        {
            return;
        }

        container.Children.CollectionChanged -= HandleSubItemContainerChanged;
        _subscribedSubItemContainer = null;

        if (Items != null)
        {
            foreach (object item in Items)
            {
                if (item is MenuItem menuItem && IsVisualAuthoredItem(menuItem))
                {
                    menuItem.Visual.Parent = container;
                }
            }
        }

        GraphicalUiElement? originalParent = _subItemContainerOriginalParent;
        _subItemContainerOriginalParent = null;
        if (originalParent != null && _subItemContainerOriginalIndex > -1)
        {
            originalParent.Children.Insert(
                global::System.Math.Min(_subItemContainerOriginalIndex, originalParent.Children.Count), container);
        }
    }

    private void HandleSubItemContainerChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        GraphicalUiElement? container = _subscribedSubItemContainer;
        List<MenuItem>? items = null;
        if(CanAdoptVisualAuthoredItems && e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            items = new List<MenuItem>();

            foreach (var item in e.NewItems)
            {
                if(item is InteractiveGue gue && gue.FormsControlAsObject is MenuItem menuitem)
                {
                    items.Add(menuitem);
                }
            }
        }

        if(items != null)
        {
#if FRB
            _=FlatRedBall.Instructions.InstructionManager.DoOnMainThreadAsync(() =>
#else
            (global::RenderingLibrary.IGumService.Default
                ?? throw new InvalidOperationException("Cannot add menu items because IGumService.Default is not initialized"))
                .DeferredQueue.Enqueue(() =>
#endif
            {
                // The Visual may have been replaced before the queue ran.
                if (container != null && _subscribedSubItemContainer == container && CanAdoptVisualAuthoredItems)
                {
                    foreach (var item in items)
                    {
                        // This item could have been added through a state.
                        // If that's the case, layout will be suspended. Let's
                        // force resume it now:
                        item.Visual.ResumeLayout(recursive:true);
                        AdoptVisualAuthoredItem(item);
                    }
                }
            });
        }
    }

    protected override void RefreshInternalVisualReferences()
    {
        // The new visual's text is authoritative; text set on the previous visual does not carry over.
        _codeText.Clear();

        // optional
        text = Visual.GetGraphicalUiElementByName("TextInstance");
        coreText = text?.RenderableComponent as global::RenderingLibrary.Graphics.IText;
        SubmenuIndicatorInstance = Visual.GetGraphicalUiElementByName("SubmenuIndicatorInstance");

        // ScrollViewer.ReactToVisualChanged runs this again through the virtual call, so unhook first
        // to keep a single subscription.
        Visual.Children.CollectionChanged -= HandleVisualChildrenChanged;
        Visual.Children.CollectionChanged += HandleVisualChildrenChanged;

        RefreshSubmenuIndicatorVisibility();
    }

    private void HandleVisualChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // February 5, 2025
        // I wanted to make this
        // work the same as the ListBox,
        // but children added to ListBox are
        // kept in the ListBox.Visual's inner panel children. Here, the MenuItem 
        // doesn't actually have a proper list of 
        // children to add to. For it to do so, we would
        // need to have a ListBox just like ComboBox has, and
        // then we could add to it. For now, users must add through
        // code.
        //if(e.Action == NotifyCollectionChangedAction.Add)
        //{
        //    var newItems = e.NewItems;

        //    foreach(var item in newItems)
        //    {
                
        //        if(item is InteractiveGue interactiveGue &&
        //            interactiveGue.FormsControlAsObject is MenuItem menuItem)
        //        {
        //            Visual.Children.Remove(interactiveGue);
        //            this.Items.Add(menuItem);
        //        }
        //    }
        //}
    }

#endregion

    #region Event Handlers
    private void HandleRollOn(object? sender, EventArgs args)
    {
        var cursor = MainCursor;

        if (cursor.XChange != 0 || cursor.YChange != 0)
        {
            UpdateIsHighlightedFromCursor(cursor);
        }

        UpdateState();
    }


    private void HandleRollOver(object? sender, EventArgs args)
    {
        var cursor = MainCursor;

        if (cursor.XChange != 0 || cursor.YChange != 0)
        {
            UpdateIsHighlightedFromCursor(cursor);
        }

        UpdateState();
    }

#if FRB
    private void UpdateIsHighlightedFromCursor(Cursor cursor)
#else
    private void UpdateIsHighlightedFromCursor(ICursor cursor)
#endif
    {
        IsHighlighted = cursor.LastInputDevice != InputDevice.TouchScreen &&
            GetIfIsOnThisOrChildVisual(cursor) && IsEnabled;
    }

    private void HandleRollOff(object? sender, EventArgs args)
    {
        IsHighlighted = false;

        // The cursor left this item, so the current hold is over as far as this
        // item is concerned - allow the next push to raise Clicked again.
        _clickRaisedDuringPush = false;

        UpdateState();
    }

    private void HandlePush(object? sender, EventArgs args)
    {
        if (MainCursor.LastInputDevice == InputDevice.Mouse)
        {
            // Menu items activate on push (matching WPF).
            IsSelected = true;

            // Push (cursor.PrimaryPush) is normally a single-frame edge, but it can
            // re-fire across frames while the button stays physically held - most
            // notably when a Clicked handler calls Cursor.ClearInputValues(), which
            // resets the last-frame mouse state so the very next frame reports a
            // fresh push. Guard so Clicked is raised only once per hold; the guard
            // is cleared on release (HandleClick) or roll-off. See issue #3077.
            if (!_clickRaisedDuringPush)
            {
                _clickRaisedDuringPush = true;
                Clicked?.Invoke(this, EventArgs.Empty);
            }
        }
    }


    private void HandleClick(object? sender, EventArgs args)
    {
        if (MainCursor.LastInputDevice == InputDevice.Mouse)
        {
            // The button was released, ending the current hold, so the next push
            // is allowed to raise Clicked again.
            _clickRaisedDuringPush = false;
        }
        else if (MainCursor.LastInputDevice == InputDevice.TouchScreen &&
            MainCursor.PrimaryClickNoSlide)
        {
            IsSelected = true;

            Clicked?.Invoke(this, EventArgs.Empty);
        }
    }
    #endregion

    protected override void HandleItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        base.HandleItemsCollectionChanged(sender, e);

        RefreshSubmenuIndicatorVisibility();

        if (itemsPopup != null)
        {
            RefreshOpenPopup();
        }
    }

    private void RefreshSubmenuIndicatorVisibility()
    {
        if (SubmenuIndicatorInstance != null)
        {
            SubmenuIndicatorInstance.Visible =
                this.Items?.Count > 0 &&
                // If ParentMenuItem is null, then this
                // is a top-level menu item
                this.ParentMenuItem != null;
        }
    }

    internal void SetSelectOnHighlightRecursively(bool value)
    {
        SelectOnHighlight = value;
        foreach (var item in MenuItemsInternal)
        {
            item.SetSelectOnHighlightRecursively(value);
        }
    }

    /// <summary>
    /// Returns whether the argument itemVisual is the visual for either
    /// this MenuItem, or any MenuItem contained as a sub-item.
    /// </summary>
    /// <param name="itemVisual"></param>
    /// <returns></returns>
    public bool IsRecursiveMenuItem(GraphicalUiElement? itemVisual)
    {
        foreach (var menuItem in this.MenuItemsInternal)
        {
            if (menuItem.Visual == itemVisual)
            {
                return true;
            }
            if (menuItem.IsRecursiveMenuItem(itemVisual))
            {
                return true;
            }
        }
        return false;
    }

    private void HandleSubItemSelected(object? sender, EventArgs e)
    {
        for (int i = 0; i < MenuItemsInternal.Count; i++)
        {
            var listBoxItem = MenuItemsInternal[i];
            if (listBoxItem != sender && listBoxItem.IsSelected)
            {
                var deselectedItem = listBoxItem.BindingContext ?? listBoxItem;
                //args.RemovedItems.Add(deselectedItem);
                listBoxItem.IsSelected = false;
            }
        }
    }

    #region Popup ListBox (sub-items)

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Automatic per-item BindingContext assignment when populating sub-items from a " +
            "bound collection. Any by-name VM member resolution happens in the item template's own " +
            "bindings, which already carry this warning at the SetBinding call that created them.")]
    internal void TryShowPopup()
    {
        if (this.Items?.Count > 0 && itemsPopup == null)
        {
            timeOpened = MainCursor.LastPrimaryPushTime;

            var visualTemplateVisual = ScrollViewerVisualTemplate?.CreateContent(null) as InteractiveGue;
            // A popup with no template sizes itself to its rows, including one built from the project's component.
            isPopupSizedByItems = visualTemplateVisual == null;
#if !FRB
            visualTemplateVisual ??= CreateProjectScrollViewerVisual();
#endif

            if(visualTemplateVisual == null)
            {
                itemsPopup = new ScrollViewer();
            }
            else
            {
                if(visualTemplateVisual.FormsControlAsObject is ScrollViewer templateScrollViewer)
                {
                    itemsPopup = templateScrollViewer;
                }
                else if(visualTemplateVisual.FormsControlAsObject != null)
                {
                    throw new InvalidOperationException("Error showing Menuitem sub-item " +
                        $"ScrollViewer through template because {visualTemplateVisual} is associated with a forms control " +
                        $"{visualTemplateVisual.FormsControlAsObject}");
                }
                else
                {
                    itemsPopup = new ScrollViewer(visualTemplateVisual);
                }
            }
            //itemsPopup.InnerPanel.Height = 0;
            //itemsPopup.InnerPanel.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            //itemsPopup.InnerPanel.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

            // sometimes the Gum project includes children, so let's remove them:
            itemsPopup.InnerPanel.Children.Clear();
            MenuItemsInternal.Clear();
            popupRows.Clear();
            generatedPopupRows.Clear();

            bool suspendedPanel = SuspendPopupPanelLayout();
            try
            {
                foreach (var item in Items)
                {
                    FrameworkElement row = CreatePopupRow(item);
                    popupRows.Add(row);
                    AttachPopupRow(row);

                    itemsPopup.InnerPanel.Children.Add(row.Visual);
                }
            }
            finally
            {
                ResumePopupPanelLayout(suspendedPanel);
            }

#if !FRB
            // The popup's size must be resolved before it is placed, because placing clamps it to the screen (#6686).
            if (isPopupSizedByItems)
            {
                SizePopupToItems();
            }
#endif

            PlaceItemsPopup();

#if !FRB
            // An open submenu needs Activity every frame (IUpdateEveryFrame.IsActivityNeeded), which
            // ancestors that already went idle would otherwise skip.
            Visual.InvalidateSubtreeCaches();

            WatchAncestorsWhileOpen();
#endif
#if FRB
            if (isPopupSizedByItems)
            {
                SizePopupToItems();
            }

            GuiManager.SortZAndLayerBased();
            itemsPopup.RepositionToKeepInScreen();
#endif
        }
    }

    // Suspends the popup panel so a bulk row change lays the rows out once on resume rather than once per
    // row (#6705). Returns false, and leaves the panel alone, when it is already suspended by someone else.
    private bool SuspendPopupPanelLayout()
    {
        GraphicalUiElement panel = itemsPopup!.InnerPanel;
        if (panel.IsLayoutSuspended)
        {
            return false;
        }
        panel.SuspendLayout();
        return true;
    }

    private void ResumePopupPanelLayout(bool suspendedPanel)
    {
        if (suspendedPanel)
        {
            itemsPopup!.InnerPanel.ResumeLayout();
        }
    }

    // The row controls in the open popup, in Items order, including controls that are not MenuItems.
    readonly List<FrameworkElement> popupRows = new List<FrameworkElement>();

    // The rows this item created for data objects in Items (as opposed to controls supplied in Items).
    readonly HashSet<MenuItem> generatedPopupRows = new HashSet<MenuItem>();

    // True when the popup sizes itself to its rows (no ScrollViewerVisualTemplate), false when a
    // ScrollViewerVisualTemplate supplied it.
    bool isPopupSizedByItems;

#if !FRB
    // In a from-file project, a MenuItem with no ScrollViewerVisualTemplate builds its popup from the
    // project's ScrollViewer component, picked by the same rule as the Tooltip (#6720). As with the
    // Tooltip, a ScrollViewer template user code registered wins over the component. Null when that
    // happens, when no project is loaded, or when it has no ScrollViewer component: the popup is then a
    // plain ScrollViewer from DefaultFormsTemplates.
    private static InteractiveGue? CreateProjectScrollViewerVisual()
    {
        bool isUserTemplate = DefaultFormsTemplates.TryGetValue(typeof(ScrollViewer), out VisualTemplate? existing)
            && existing is not BuiltInVisualTemplate;
        if (isUserTemplate)
        {
            return null;
        }
        global::Gum.DataTypes.ComponentSave? component = DefaultImplementationLookup.Find(
            global::Gum.Managers.ObjectFinder.Self.GumProjectSave,
            global::Gum.DataTypes.Behaviors.StandardFormsBehaviorNames.ScrollViewerBehaviorName);
        return component == null
            ? null
            : global::GumRuntime.ElementSaveExtensions.ToGraphicalUiElement(component, ISystemManagers.Default, addToManagers: false) as InteractiveGue;
    }
#endif

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Automatic per-item BindingContext assignment when populating sub-items from a " +
            "bound collection. Any by-name VM member resolution happens in the item template's own " +
            "bindings, which already carry this warning at the SetBinding call that created them.")]
    private FrameworkElement CreatePopupRow(object item)
    {
        if (item is FrameworkElement asFrameworkElement)
        {
            return asFrameworkElement;
        }

        MenuItem menuItem = new MenuItem();
        menuItem.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        menuItem.UpdateToObject(item);
        menuItem.BindingContext = item;
        generatedPopupRows.Add(menuItem);
        return menuItem;
    }

    // Makes a popup row one of this item's sub-items: highlight behavior, parent, and sibling deselection.
    private void AttachPopupRow(FrameworkElement row)
    {
        if (row is MenuItem menuItem)
        {
            menuItem.SelectOnHighlight = this.SelectOnHighlight;
            MenuItemsInternal.Add(menuItem);
            menuItem.ParentMenuItem = this;
            menuItem.Selected += HandleSubItemSelected;
            menuItem.HighlightGained += HandleSubItemHighlightGained;
        }
    }

    private void HandleSubItemHighlightGained(object? sender, EventArgs e)
    {
        foreach (MenuItem item in MenuItemsInternal)
        {
            if (item != sender)
            {
                item.SetHighlightFromNavigation(false);
            }
        }
    }

    /// <summary>
    /// Clears the highlight of every row in this item's open submenu and in the submenus open below it.
    /// </summary>
    internal void ClearSubItemHighlightsRecursively()
    {
        foreach (MenuItem item in MenuItemsInternal)
        {
            item.ClearSubItemHighlightsRecursively();
            item.SetHighlightFromNavigation(false);
        }
    }

    // Undoes AttachPopupRow, leaving the row's Visual where it is.
    private void DetachPopupRow(FrameworkElement row)
    {
        if (row is MenuItem menuItem)
        {
            menuItem.Selected -= HandleSubItemSelected;
            menuItem.HighlightGained -= HandleSubItemHighlightGained;
            menuItem.ParentMenuItem = null;
        }
    }

    // Called when the popup closes: sub-items stop reporting to this item until the next open.
    private void ReleasePopupRows()
    {
        foreach (MenuItem menuItem in MenuItemsInternal)
        {
            menuItem.Selected -= HandleSubItemSelected;
            menuItem.HighlightGained -= HandleSubItemHighlightGained;
        }
        popupRows.Clear();
        generatedPopupRows.Clear();
    }

    /// <summary>
    /// Brings the open popup in line with Items after it changed: rows for removed items leave (closing
    /// any submenu they have open), rows for new items join, and rows that stay are untouched so their
    /// highlight, selection and open submenu survive. The popup is then sized and placed again.
    /// </summary>
    private void RefreshOpenPopup()
    {
        if (Items == null || Items.Count == 0)
        {
            // Nothing left to show. Deselect rather than only tear down, so the item can open again.
            // The rows leave with the popup, so no cursor roll-off will clear their highlight.
            foreach (FrameworkElement leaving in popupRows)
            {
                if (leaving is MenuItem leavingMenuItem)
                {
                    leavingMenuItem.IsHighlighted = false;
                }
            }
            IsSelected = false;
            HidePopupRecursively();
            return;
        }

        ScrollViewer popup = itemsPopup!;
        bool suspendedPanel = SuspendPopupPanelLayout();
        try
        {
            ReconcilePopupRows(popup, Items);
        }
        finally
        {
            ResumePopupPanelLayout(suspendedPanel);
        }

#if !FRB
        // The rows changed the popup's size, so it has to be sized and clamped again.
        if (isPopupSizedByItems)
        {
            SizePopupToItems();
        }

        PlaceItemsPopup();
#endif
#if FRB
        PlaceItemsPopup();
        if (isPopupSizedByItems)
        {
            SizePopupToItems();
        }

        GuiManager.SortZAndLayerBased();
        popup.RepositionToKeepInScreen();
#endif
    }

    // Puts the open popup's rows in line with Items, keeping rows whose item is still there.
    private void ReconcilePopupRows(ScrollViewer popup, System.Collections.IList currentItems)
    {
        List<FrameworkElement> oldRows = new List<FrameworkElement>(popupRows);
        List<FrameworkElement> unclaimedRows = new List<FrameworkElement>(oldRows);
        List<FrameworkElement> newRows = new List<FrameworkElement>(currentItems.Count);

        foreach (object item in currentItems)
        {
            FrameworkElement? row = null;
            if (item is FrameworkElement asFrameworkElement)
            {
                int index = unclaimedRows.IndexOf(asFrameworkElement);
                if (index > -1)
                {
                    unclaimedRows.RemoveAt(index);
                    row = asFrameworkElement;
                }
            }
            else
            {
                for (int i = 0; i < unclaimedRows.Count; i++)
                {
                    if (unclaimedRows[i] is MenuItem generated && generatedPopupRows.Contains(generated) &&
                        Equals(generated.BindingContext, item))
                    {
                        row = generated;
                        unclaimedRows.RemoveAt(i);
                        break;
                    }
                }
            }

            newRows.Add(row ?? CreatePopupRow(item));
        }

        // Rows left unclaimed belong to removed items.
        foreach (FrameworkElement removed in unclaimedRows)
        {
            if (removed is MenuItem removedMenuItem)
            {
                // Close its submenu first: the popup it opened is not in this popup and would be left behind.
                removedMenuItem.HidePopupRecursively();
                removedMenuItem.IsSelected = false;
                // It left the popup, so no cursor roll-off will clear its highlight.
                removedMenuItem.IsHighlighted = false;
                generatedPopupRows.Remove(removedMenuItem);
            }
            DetachPopupRow(removed);
            removed.Visual.Parent = null;
        }

        for (int i = 0; i < newRows.Count; i++)
        {
            FrameworkElement row = newRows[i];
            if (!oldRows.Contains(row))
            {
                AttachPopupRow(row);
            }

            GraphicalUiElement visual = row.Visual;
            if (popup.InnerPanel.Children.Count > i && popup.InnerPanel.Children[i] == visual)
            {
                continue;
            }

            // A row that moved is moved within the panel rather than detached, which would close its own submenu.
            int currentIndex = popup.InnerPanel.Children.IndexOf(visual);
            if (currentIndex > -1)
            {
                popup.InnerPanel.Children.Move(currentIndex, i);
            }
            else
            {
                popup.InnerPanel.Children.Insert(i, visual);
            }
        }

        popupRows.Clear();
        popupRows.AddRange(newRows);

        MenuItemsInternal.Clear();
        foreach (FrameworkElement row in newRows)
        {
            if (row is MenuItem menuItem)
            {
                MenuItemsInternal.Add(menuItem);
            }
        }
    }

    /// <summary>
    /// For a plain ScrollViewer popup (no visual template), makes the popup and everything between
    /// it and its inner panel size to the items, then resolves that layout.
    /// </summary>
    private void SizePopupToItems()
    {
        ScrollViewer popup = itemsPopup!;

        // let's suppress layout for performance reasons, and to prevent issues with scrollbars:
        var wasSuppressed = GraphicalUiElement.IsAllLayoutSuspended;
        GraphicalUiElement.IsAllLayoutSuspended = true;

        try
        {
            popup.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
            popup.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;

            popup.VerticalSizeMode = SizeMode.Auto;
            popup.HorizontalSizeMode = SizeMode.Auto;

            // We can't make assumptions about the structure of the popup, so we have to just recursively
            // climb up from the inner panel and size it according to its children.
            var parentForSizing = popup.InnerPanel;
            while(parentForSizing != null)
            {
                parentForSizing.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
                parentForSizing.Width = 0;
                parentForSizing.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
                parentForSizing.Height = 0;

                if (parentForSizing == popup.Visual)
                {
                    break;
                }
                parentForSizing = parentForSizing.Parent;
            }
        }
        finally
        {
            GraphicalUiElement.IsAllLayoutSuspended = wasSuppressed;
        }

        if (!wasSuppressed)
        {
            popup.Visual.UpdateLayout();
        }
    }

    /// <summary>
    /// Puts the sub-item popup in the popup layer next to this item: below it for a top-level item,
    /// to its right when this item is in a vertical stack (a submenu). Clamps it to the screen.
    /// Used when the popup opens and again whenever this item's absolute rectangle changes.
    /// </summary>
    private void PlaceItemsPopup()
    {
        var popupVisual = itemsPopup!.Visual;
        popupVisual.X = this.Visual.AbsoluteLeft;
        popupVisual.Y = this.Visual.GetAbsoluteBottom();
        var parent = this.Visual.Parent;
        if (parent != null)
        {
            if (parent is GraphicalUiElement asGue && asGue.ChildrenLayout == global::Gum.Managers.ChildrenLayout.TopToBottomStack)
            {
                popupVisual.X = this.Visual.GetAbsoluteRight();
                popupVisual.Y = this.Visual.AbsoluteTop;
            }
        }

        ListBox.ShowPopupListBox(itemsPopup, this.Visual, forceAbsoluteSize:false);

        _placedAbsoluteX = Visual.AbsoluteX;
        _placedAbsoluteY = Visual.AbsoluteY;
        _placedAbsoluteWidth = Visual.AbsoluteWidth;
        _placedAbsoluteHeight = Visual.AbsoluteHeight;

#if !FRB
        // Placing moved this popup to the end of the popup root, which would draw it over any
        // submenu already open from one of its items. Placing those again puts them back on top, in
        // order, and also moves them with this popup (#6688).
        foreach (MenuItem item in MenuItemsInternal)
        {
            if (item.IsPopupVisible)
            {
                item.PlaceItemsPopup();
            }
        }
#endif
    }

#if !FRB
    // Only an open submenu needs the per-frame check; a closed MenuItem must not stop its ancestors
    // from going idle. TryShowPopup and the close paths invalidate the idle caches.
    bool Gum.Wireframe.IUpdateEveryFrame.IsActivityNeeded => IsPopupVisible;

    /// <summary>
    /// While the submenu is open, places it again whenever this item's absolute rectangle changes
    /// (canvas resize, UI scale, or this item or an ancestor moving). Does nothing when closed.
    /// </summary>
    void Gum.Wireframe.IUpdateEveryFrame.Activity(double secondDifference)
    {
        if (!IsPopupVisible)
        {
            return;
        }

        if (Visual.AbsoluteX != _placedAbsoluteX ||
            Visual.AbsoluteY != _placedAbsoluteY ||
            Visual.AbsoluteWidth != _placedAbsoluteWidth ||
            Visual.AbsoluteHeight != _placedAbsoluteHeight)
        {
            PlaceItemsPopup();
        }
    }
#endif

    public void HidePopupRecursively()
    {
        if(this.IsPopupVisible)
        {
            HideChildrenPopupsRecursively();

            if (Visual.EffectiveManagers != null && itemsPopup?.IsVisible == true)
            {
                itemsPopup.IsVisible = false;
                itemsPopup.Visual.RemoveFromManagers();

                Visual.EffectiveManagers.Renderer.MainLayer.Remove(itemsPopup.Visual);
#if FRB
#else
                itemsPopup.Visual.GetTopParent()?.Children.Remove(itemsPopup.Visual);
#endif
                itemsPopup = null;
#if !FRB
                StopWatchingAncestors();
                Visual.InvalidateSubtreeCaches();
#endif
            }
#if FULL_DIAGNOSTICS
            else if (itemsPopup?.IsVisible == true)
            {
                throw new InvalidOperationException(
                    $"MenuItem '{Visual.Name ?? "(unnamed)"}' cannot close its popup because its Visual has no " +
                    "EffectiveManagers. If this MenuItem's tree is not added to a managed root (e.g. it is only " +
                    "drawn through GumBatch), call AttachManagersOnly(...) on the root container.");
            }
#endif
            foreach(var item in this.MenuItemsInternal)
            {
                item.IsSelected = false;
            }

            ReleasePopupRows();
        }
    }

    /// <summary>
    /// Closes the sub-item popup when this item's Visual is replaced or removed. Unlike
    /// <see cref="HidePopupRecursively"/> this raises no events and does not require the Visual to
    /// have managers, because the Visual it would update is going away.
    /// </summary>
    private void CloseSubmenuForVisualRemoved()
    {
        TearDownPopup();
        isSelected = false;
    }

    private void TearDownPopup()
    {
        foreach (MenuItem item in MenuItemsInternal)
        {
            item.TearDownPopup();
            if (item.isSelected)
            {
                item.isSelected = false;
                item.UpdateState(force: true);
            }
        }

        if (itemsPopup != null)
        {
            // This item may already be out of the tree (no managers), but the popup is in the popup
            // layer, which has them.
            var managers = Visual.EffectiveManagers
#if !FRB
                ?? itemsPopup.Visual.EffectiveManagers
#endif
                ;
            itemsPopup.IsVisible = false;
            if (managers != null)
            {
                itemsPopup.Visual.RemoveFromManagers();
                managers.Renderer.MainLayer.Remove(itemsPopup.Visual);
            }
            itemsPopup.Visual.Parent = null;
            itemsPopup = null;
            ReleasePopupRows();
#if !FRB
            StopWatchingAncestors();
            Visual.InvalidateSubtreeCaches();
#endif
        }
    }

#if !FRB
    // While the submenu is open its popup is in the popup layer, so hiding this item or an ancestor, or
    // detaching it from the tree, would leave the popup on screen: AnimateSelf skips hidden elements,
    // so Activity never ticks to notice. Instead of polling, an open submenu listens to the visibility
    // and parent changes of its Visual and each ancestor, and closes itself (#6688).
    readonly List<GraphicalUiElement> _watchedAncestors = new();

    private void WatchAncestorsWhileOpen()
    {
        StopWatchingAncestors();

        // A submenu item stops at the popup that holds it. That popup is moved out of and back into the
        // popup root whenever its owner places it again, and the owner closes this item's submenu when
        // it closes itself, so nothing above that popup concerns this item.
        GraphicalUiElement? owningPopup = ParentMenuItem?.itemsPopup?.Visual;
        GraphicalUiElement? current = Visual;
        while (current != null && current != owningPopup)
        {
            current.VisibleChanged += HandleWatchedVisibleChanged;
            current.ParentChanged += HandleWatchedParentChanged;
            _watchedAncestors.Add(current);
            current = current.Parent as GraphicalUiElement;
        }
    }

    private void StopWatchingAncestors()
    {
        for (int i = 0; i < _watchedAncestors.Count; i++)
        {
            _watchedAncestors[i].VisibleChanged -= HandleWatchedVisibleChanged;
            _watchedAncestors[i].ParentChanged -= HandleWatchedParentChanged;
        }
        _watchedAncestors.Clear();
    }

    private void HandleWatchedVisibleChanged(object? sender, EventArgs args)
    {
        if (!Visual.AbsoluteVisible)
        {
            CloseSubmenuBecauseTreeChanged();
        }
    }

    private void HandleWatchedParentChanged(object? sender, GraphicalUiElement.ParentChangedEventArgs args)
    {
        if (args.NewValue == null || !Visual.AbsoluteVisible)
        {
            // Detached from the tree, or moved somewhere hidden.
            CloseSubmenuBecauseTreeChanged();
        }
        else
        {
            // Moved to another parent: the popup follows through Activity, and the ancestors to listen to changed.
            WatchAncestorsWhileOpen();
        }
    }

    private void CloseSubmenuBecauseTreeChanged()
    {
        TearDownPopup();
        if (isSelected)
        {
            isSelected = false;
            UpdateState(force: true);
        }
    }
#endif

    public void HideChildrenPopupsRecursively()
    {
        foreach (var item in MenuItemsInternal)
        {
            item.HidePopupRecursively();
        }
    }

    internal ScrollViewer? itemsPopup;

    // This item's absolute rectangle when the popup was last placed; a change means the popup is stale.
    float _placedAbsoluteX;
    float _placedAbsoluteY;
    float _placedAbsoluteWidth;
    float _placedAbsoluteHeight;

    public bool IsPopupVisible => itemsPopup?.IsVisible == true;

    #endregion

    #region Update to state/object

    public virtual void UpdateToObject(object? o)
    {
        if (coreText != null)
        {
            coreText.RawText = o?.ToString() ?? string.Empty;
        }
    }

    // Manual restore rather than RegisterRuntimeProperty because
    // ReactToVisualChanged can fire multiple times (visual reassignment),
    // which would accumulate duplicate registrations. Only text assigned through
    // the property is restored; text the Visual authored is re-applied from the
    // Visual's state instead.
    private CodeAssignedText _codeText;

    /// <inheritdoc/>
    public override void ApplyRuntimeProperties()
    {
        _codeText.Restore(text);
        base.ApplyRuntimeProperties();
    }

    public override void UpdateState()
    {
        UpdateState(false);
    }

    void UpdateState(bool force)
    {
        var cursor = MainCursor;

        const string category = MenuItemCategoryState;

        if (IsEnabled == false)
        {
            Visual.SetProperty(category, DisabledStateName);
        }
        else if (IsFocused)
        {
            Visual.SetProperty(category, FocusedStateName);
        }
        else if (IsSelected)
        {
            Visual.SetProperty(category, SelectedStateName);
        }
        else if (IsHighlighted)
        {
            // If the cursor has moved, highlight. This prevents highlighting from
            // happening when the cursor is not moving, and the user is moving the focus
            // with the gamepad. 
            // Vic says - I'm not sure if this is the solution that I like, but let's start with it...
            if (cursor.XChange != 0 || cursor.YChange != 0 || force)
            {
                Visual.SetProperty(category, HighlightedStateName);
            }
            // otherwise - do nothing?
        }
        else
        {
            Visual.SetProperty(category, EnabledStateName);
        }
    }

    #endregion

    #region Utilities

    public override string ToString()
    {
        return Header ?? base.ToString();
    }

#if FULL_DIAGNOSTICS
    private void ReportMissingTextInstance()
    {
        if (coreText == null)
        {
            throw new Exception(
                $"This MenuItem was created with a Gum component ({Visual?.ElementSave}) " +
                "that does not have an instance called 'TextInstance'. " +
                "A 'TextInstance' instance must be added to modify the Menu's Header property.");
        }
    }
#endif

    #endregion
}
