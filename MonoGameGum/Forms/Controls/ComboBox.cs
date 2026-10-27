using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Gum.DataTypes;
using Gum.Converters;
using System.Collections;
using RenderingLibrary;




#if FRB
using MonoGameGum.Forms.Controls;
using Microsoft.Xna.Framework.Input;
using static FlatRedBall.Input.Xbox360GamePad;
using FlatRedBall.Gui;
using FlatRedBall.Input;
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
using Buttons = FlatRedBall.Input.Xbox360GamePad.Button;
using GamepadButton = FlatRedBall.Input.Xbox360GamePad.Button;
namespace FlatRedBall.Forms.Controls;
#else
using Gum.Input;
using GamepadButton = Gum.Input.GamepadButton;
using Keys = Gum.Forms.Input.Keys;
#endif


#if !FRB
namespace Gum.Forms.Controls;

#endif

public class ComboBox :
#if FRB
    FrameworkElement,
#else
    Gum.Forms.Controls.FrameworkElement, 
#endif 
    IInputReceiver
#if !FRB
    , Gum.Wireframe.IUpdateEveryFrame
#endif
{
    #region Fields/Properties

    // Required parts of the visual, assigned whenever a visual is set.
    ListBox listBox = null!;
    GraphicalUiElement textComponent = null!;
    global::RenderingLibrary.Graphics.IText coreTextObject = null!;

    /// <summary>
    /// Replaces the internal ListBox with the provided instance.
    /// This replaces the Items, SelectedObject, and SelectedIndex unless
    /// the properties are bound.
    /// </summary>
    /// <exception cref="ArgumentNullException">The assigned value is null.</exception>
    public ListBox ListBox
    {
        get
        {
            EnsureListBox();
            return listBox;
        }
        [UnconditionalSuppressMessage("Trimming", "IL2026",
            Justification = "Only forwards a DisplayMemberPath the caller already set, and that " +
                "setter carries the RequiresUnreferencedCode warning at the caller.")]
        set
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            // If the user replaces this, it's up to the user to set the positions
            // and parenting properly!
            if(value != listBox)
            {
                // The old list box may be the open popup; closing it first removes it from the popup
                // layer and puts its layout values back (#6684).
                if (listBox != null && listBox.IsVisible)
                {
                    HideListBox();
                }

                listBox = value;

                listBox.IsVisible = false;

                SubscribeToListBox();

                if (!string.IsNullOrEmpty(_displayMemberPath))
                {
                    listBox.DisplayMemberPath = _displayMemberPath;
                }

                if (!IsDataBound(nameof(Text)))
                {
                    UpdateToObject(listBox.SelectedObject);
                }
            }
        }
    }

#if !FRB
    private static VisualTemplate? _processWideFromFileDropDownTemplate;

    // Set by FormsUtilities when a from-file project has a ListBoxBehavior component (#6728). A ComboBox
    // whose visual has no ListBoxInstance child builds its drop-down from it on the first open.
    internal static VisualTemplate? FromFileDropDownTemplate
    {
        get { global::Gum.GumContext? own = global::Gum.GumContext.Own; return own != null ? own.FromFileComboBoxDropDownTemplate : _processWideFromFileDropDownTemplate; }
        set { global::Gum.GumContext? own = global::Gum.GumContext.Own; if (own != null) { own.FromFileComboBoxDropDownTemplate = value; } else { _processWideFromFileDropDownTemplate = value; } }
    }
#endif

    // The list box whose events this combo box is currently subscribed to, so the subscription
    // can move when the list box is replaced.
    ListBox? _subscribedListBox;

    private void SubscribeToListBox()
    {
        if (listBox != null && _subscribedListBox != listBox)
        {
            if (_subscribedListBox != null)
            {
                _subscribedListBox.SelectionChanged -= HandleSelectionChanged;
                _subscribedListBox.ItemPushed -= HandleListBoxItemPushed;
            }

            listBox.SelectionChanged += HandleSelectionChanged;
            listBox.ItemPushed += HandleListBoxItemPushed;
            _subscribedListBox = listBox;
        }
    }

    /// <summary>
    /// Gets or sets the displayed text for the selected item. This property is data-driven
    /// (typically set from the selected item's ToString) and intentionally does not apply
    /// localization. To localize ComboBox items, translate values before adding them to the Items collection.
    /// </summary>
    public string? Text
    {
        get => coreTextObject.RawText;
        set
        {
            if (value != Text)
            {
                SetDisplayedText(value);

                PushValueToViewModel();
            }
        }
    }

    string? _displayMemberPath;
    /// <summary>
    /// The name of a property to read off each item for display, instead of the item's ToString.
    /// </summary>
    public string? DisplayMemberPath
    {
        get => _displayMemberPath;
        [RequiresUnreferencedCode(ItemsControl.DisplayMemberPathTrimMessage)]
        [UnconditionalSuppressMessage("Trimming", "IL2112",
            Justification = ItemsControl.PreservedNotCalledJustification)]
        set
        {
            _displayMemberPath = value;

            // The drop-down rows show the same member as the text box.
            if (listBox != null)
            {
                listBox.DisplayMemberPath = value ?? string.Empty;
            }

            UpdateToObject(SelectedObject);
        }
    }
    
    public IList? Items

    {
        get => listBox != null ? listBox.Items : _pendingItems;
        set
        {
            if (value != Items)
            {
                if (listBox != null)
                {
                    listBox.Items = value;
                }
                else
                {
                    // Like ListBox: swapping Items drops the selection, even when the new items contain it.
                    SetPendingItems(value);
                    SetPendingSelection(-1);
                }

                PushValueToViewModel();
            }
        }
    }


    /// <summary>
    /// Obsolete. Use <see cref="VisualTemplate"/> instead to control the visual (GraphicalUiElement)
    /// type used for each item in the drop-down list.
    /// </summary>
    [Obsolete("Use VisualTemplate")]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type? ListBoxItemGumType
    {
        get { EnsureListBox(); return listBox.ListBoxItemGumType; }
        [UnconditionalSuppressMessage("Trimming", "IL2114",
            Justification = ItemsControl.PreservedNotCalledJustification)]
        set
        {
#if FULL_DIAGNOSTICS
            if (listBox == null)
            {
                throw new Exception("Visual must be set before assigning the ListBoxItemType");
            }
#endif
            EnsureListBox();
            listBox.ListBoxItemGumType = value;
        }
    }

    /// <summary>
    /// Obsolete. Use <see cref="FrameworkElementTemplate"/> instead to control the Forms control
    /// type used for each item in the drop-down list.
    /// </summary>
    [Obsolete("Use FrameworkElementTemplate")]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type ListBoxItemFormsType
    {
        get { EnsureListBox(); return listBox.ListBoxItemFormsType; }
        [UnconditionalSuppressMessage("Trimming", "IL2114",
            Justification = ItemsControl.PreservedNotCalledJustification)]
        set
        {
#if FULL_DIAGNOSTICS
            if (listBox == null)
            {
                throw new Exception("Visual must be set before assigning the ListBoxItemType");
            }
#endif
            EnsureListBox();
            listBox.ListBoxItemFormsType = value;
        }
    }

    /// <summary>
    /// Gets or sets the template used to create the Forms control (FrameworkElement) for each item
    /// in the drop-down list. When set, each item added to <see cref="Items"/> will have its
    /// FrameworkElement created by calling <see cref="FrameworkElementTemplate.CreateContent"/>,
    /// with the item assigned as the BindingContext. This controls the behavioral/logical layer
    /// of each list item. To control the visual (GraphicalUiElement) layer instead, use
    /// <see cref="VisualTemplate"/>. This property forwards to the internal ListBox.
    /// </summary>
    public FrameworkElementTemplate? FrameworkElementTemplate
    {
        get => ListBox.FrameworkElementTemplate;
        set => ListBox.FrameworkElementTemplate = value;
    }

    /// <summary>
    /// Gets or sets the template used to create the visual (GraphicalUiElement) for each item
    /// in the drop-down list. When set, each item added to <see cref="Items"/> will have its
    /// visual created by calling <see cref="VisualTemplate.CreateContent"/>. Changing this
    /// property clears and recreates all existing item visuals. To control the Forms control
    /// (FrameworkElement) layer instead, use <see cref="FrameworkElementTemplate"/>.
    /// This property forwards to the internal ListBox.
    /// </summary>
    public VisualTemplate? VisualTemplate
    {
        get => ListBox.VisualTemplate;
        set => ListBox.VisualTemplate = value;
    }

    public object? SelectedObject
    {
        get => listBox != null ? listBox.SelectedObject : _pendingSelectedObject;
        set
        {
            if (listBox != null)
            {
                listBox.SelectedObject = value;
            }
            else
            {
                // Like ListBox: an object that is not in Items clears the selection.
                SetPendingSelection(value == null ? -1 : (Items?.IndexOf(value) ?? -1));
            }
        }
    }
    public int SelectedIndex
    {
        get
        {
            if (listBox != null)
            {
                return listBox.SelectedIndex;
            }
            return _pendingSelectedIndex;
        }
        set
        {
            if (listBox != null)
            {
                listBox.SelectedIndex = value;
            }
            else
            {
                IList? items = Items;
                // Like ListBox (#6737): -1 clears the selection and any other out-of-range index is ignored.
                if (value == -1 || (items != null && value >= 0 && value < items.Count))
                {
                    SetPendingSelection(value);
                }
            }
        }
    }

    // While a lazy drop-down is not built yet (#6728), the ComboBox holds the items and the selection
    // itself and hands them to the ListBox when it is built on the first open.
    VisualTemplate? _lazyDropDownTemplate;
    IList? _pendingItems;
    // The selection is kept by position, as ListBox keeps it by row: inserting or removing other items
    // moves the index, and removing or replacing the selected item clears it.
    int _pendingSelectedIndex = -1;
    object? _pendingSelectedObject;

    private void SetPendingItems(IList? items)
    {
        if (_pendingItems is System.Collections.Specialized.INotifyCollectionChanged oldItems)
        {
            oldItems.CollectionChanged -= HandlePendingItemsChanged;
        }
        _pendingItems = items;
        if (_pendingItems is System.Collections.Specialized.INotifyCollectionChanged newItems)
        {
            newItems.CollectionChanged += HandlePendingItemsChanged;
        }
    }

    // Mirrors how ListBox updates its selection when Items changes: removing or replacing the selected
    // item, or resetting the collection, clears the selection; other changes shift it.
    private void HandlePendingItemsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (_pendingSelectedIndex < 0)
        {
            return;
        }
        switch (e.Action)
        {
            case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                if (e.NewStartingIndex >= 0 && e.NewStartingIndex <= _pendingSelectedIndex)
                {
                    _pendingSelectedIndex += e.NewItems?.Count ?? 1;
                }
                break;
            case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                int removedCount = e.OldItems?.Count ?? 1;
                if (e.OldStartingIndex < 0 ||
                    (e.OldStartingIndex <= _pendingSelectedIndex && _pendingSelectedIndex < e.OldStartingIndex + removedCount))
                {
                    SetPendingSelection(-1);
                }
                else if (e.OldStartingIndex < _pendingSelectedIndex)
                {
                    _pendingSelectedIndex -= removedCount;
                }
                break;
            case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                if (e.OldStartingIndex == _pendingSelectedIndex)
                {
                    _pendingSelectedIndex = e.NewStartingIndex;
                }
                else
                {
                    if (e.OldStartingIndex < _pendingSelectedIndex) _pendingSelectedIndex--;
                    if (e.NewStartingIndex <= _pendingSelectedIndex) _pendingSelectedIndex++;
                }
                break;
            case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                int replacedCount = e.OldItems?.Count ?? 1;
                if (e.OldStartingIndex < 0 ||
                    (e.OldStartingIndex <= _pendingSelectedIndex && _pendingSelectedIndex < e.OldStartingIndex + replacedCount))
                {
                    SetPendingSelection(-1);
                }
                break;
            case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                SetPendingSelection(-1);
                break;
        }
    }

    private void SetPendingSelection(int index)
    {
        if (index == _pendingSelectedIndex)
        {
            return;
        }
        object? selected = index >= 0 ? _pendingItems![index] : null;
        SelectionChangedEventArgs args = new SelectionChangedEventArgs();
        if (_pendingSelectedIndex >= 0)
        {
            args.RemovedItems.Add(_pendingSelectedObject);
        }
        if (index >= 0)
        {
            args.AddedItems.Add(selected);
        }
        _pendingSelectedIndex = index;
        _pendingSelectedObject = selected;
        RaiseSelectionChanged(selected, args);
    }

    // Builds the drop-down from the lazy template the first time it is needed. Does nothing when the
    // ListBox already exists (a ListBoxInstance child, an assigned ListBox, or an earlier build).
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Only forwards a DisplayMemberPath the caller already set, and that " +
            "setter carries the RequiresUnreferencedCode warning at the caller.")]
    private void EnsureListBox()
    {
        if (listBox != null || _lazyDropDownTemplate == null)
        {
            return;
        }

        InteractiveGue dropDownVisual = _lazyDropDownTemplate.CreateContent(null) as InteractiveGue
            ?? throw new InvalidOperationException(
                $"The combo box {Visual.Name} has no ListBoxInstance and the project's ListBox component could not be built");
        ListBox built = dropDownVisual.FormsControlAsObject as ListBox ?? new ListBox(dropDownVisual);

        // Placed like a ListBoxInstance child: below the ComboBox, at its width.
        dropDownVisual.Name = "ListBoxInstance";
        dropDownVisual.XUnits = GeneralUnitType.PixelsFromSmall;
        dropDownVisual.YUnits = GeneralUnitType.PixelsFromLarge;
        dropDownVisual.X = 0;
        dropDownVisual.Y = 0;
        dropDownVisual.WidthUnits = DimensionUnitType.RelativeToParent;
        dropDownVisual.Width = 0;
        Visual.RaiseChildrenEventsOutsideOfBounds = true;

        built.IsVisible = false;
        if (!string.IsNullOrEmpty(_displayMemberPath))
        {
            built.DisplayMemberPath = _displayMemberPath;
        }
        IList? items = _pendingItems;
        int selectedIndex = _pendingSelectedIndex;
        SetPendingItems(null);
        _pendingSelectedIndex = -1;
        _pendingSelectedObject = null;
        built.Items = items;
        built.SelectedIndex = selectedIndex;

        listBox = built;
        SubscribeToListBox();
    }

    public List<Keys> IgnoredKeys => throw new NotImplementedException();

    public bool TakingInput => throw new NotImplementedException();

    public IInputReceiver NextInTabSequence { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    // These values are the list box values before the list box has been expanded.
    // The reason we need these is because the list box may have values (like Width)
    // which depend on the combo box. Normal gum objects automatically handle layout,
    // but the ListBox needs to be detached from its parent ComboBox when it is shown so
    // that it can be moved above all other UI (a sorting issue). But since the ListBox is
    // detached from its parent, we can't have it automatically update its size to the parent.
    // Therefore, the list box showing process is as follows:
    // 1. Apply the values below to the list box (force it to have the same values it had before it was shown)
    // 2. Let the Gum layout engine do its updating
    // 3. Read the absolute x,y,width, and height (this happens in the show function)
    // 4. Detach the list box from its parent and add it with a positive infinity Z so it sorts on top
    // 5. Apply the absolute values to the list box
    // This will result in the list box shown at the proper location and size.
    DimensionUnitType listBoxWidthUnits;
    DimensionUnitType listBoxHeightUnits;
    GeneralUnitType listBoxXUnits;
    GeneralUnitType listBoxYUnits;
    float listBoxX;
    float listBoxY;
    float listBoxWidth;
    float listBoxHeight;

    public bool IsDropDownOpen
    {
        get => listBox != null && listBox.IsVisible;
        set
        {
            if (value && IsDropDownOpen == false)
            {
                OpenDropDown();
            }
            else if (!value && IsDropDownOpen)
            {
                HideListBox();
            }
        }
    }

    bool _hadFocusOnListBoxOpened;

    // What the popup was last placed against, so an open drop-down can be placed again when the
    // ComboBox moves or resizes (including through an ancestor) or the canvas is resized.
    PopupPlacement _popupPlacement;

    #endregion

    #region Events

    public event Action<object, SelectionChangedEventArgs>? SelectionChanged;
    /// <summary>
    /// Raised every frame while this control has input focus. Can be used
    /// to perform custom per-frame logic while the control is focused.
    /// </summary>
    public event Action<IInputReceiver>? FocusUpdate;
    public event Action<GamepadButton>? ControllerButtonPushed;
#pragma warning disable CS0067 // raised only in the FRB build
    public event Action<int>? GenericGamepadButtonPushed;
#pragma warning restore CS0067

    #endregion

    #region Initialize Methods

    public ComboBox() : base() { }

    public ComboBox(InteractiveGue visual) : base(visual) { }

    protected override void ReactToVisualChanged()
    {
        RefreshInternalVisualReferences();

#if FRB
        Visual.Click += _=> this.HandleClick(this, EventArgs.Empty);
        Visual.Push += _ => this.HandlePush(this, EventArgs.Empty);
        Visual.LosePush += _ => this.HandleLosePush(this, EventArgs.Empty);
        Visual.RollOn += _ => this.HandleRollOn(this, EventArgs.Empty);
        Visual.RollOff += _ => this.HandleRollOff(this, EventArgs.Empty);
#else
        Visual.Click += this.HandleClick;
        Visual.Push += this.HandlePush;
        Visual.LosePush += this.HandleLosePush;
        Visual.RollOn += this.HandleRollOn;
        Visual.RollOff += this.HandleRollOff;
#endif

        SubscribeToListBox();

        base.ReactToVisualChanged();

        UpdateState();
    }

    /// <inheritdoc/>
    protected override void ReactToVisualRemoved()
    {
        // The popup lives outside this Visual; close it while the old Visual is still available (#6684).
        if (listBox != null && listBox.IsVisible)
        {
            HideListBox();
        }

        base.ReactToVisualRemoved();

        // FRB1 only: these lambdas cannot be unsubscribed when the Visual is replaced, so the old
        // Visual keeps driving this control after a swap (see #6208). Not fixed until a FRB1 project needs it.
#if !FRB
        Visual.Click -= this.HandleClick;
        Visual.Push -= this.HandlePush;
        Visual.LosePush -= this.HandleLosePush;
        Visual.RollOn -= this.HandleRollOn;
        Visual.RollOff -= this.HandleRollOff;
#endif

        if (_subscribedListBox != null)
        {
            _subscribedListBox.SelectionChanged -= HandleSelectionChanged;
            _subscribedListBox.ItemPushed -= HandleListBoxItemPushed;
            _subscribedListBox = null;
        }
    }

    protected override void RefreshInternalVisualReferences()
    {
        var listBoxInstance = Visual.GetGraphicalUiElementByName("ListBoxInstance") as InteractiveGue;
        VisualTemplate? lazyDropDownTemplate = null;
#if !FRB
        lazyDropDownTemplate = listBoxInstance == null ? FromFileDropDownTemplate : null;
#endif
        if (listBoxInstance == null && lazyDropDownTemplate == null)
        {
            throw new Exception("Gum object must have an object called \"ListBoxInstance\"");
        }
        textComponent = base.Visual.GetGraphicalUiElementByName("TextInstance")
            ?? throw new Exception("Gum object must have an object called \"TextInstance\"");

        coreTextObject = textComponent.RenderableComponent as global::RenderingLibrary.Graphics.IText
            ?? throw new Exception("The combo box's \"TextInstance\" must be a text object");

        if (listBoxInstance == null)
        {
            // A from-file ComboBox with no ListBoxInstance child: the drop-down is built on the first open.
            _lazyDropDownTemplate = lazyDropDownTemplate;
            if (listBox == null && _pendingItems == null)
            {
                SetPendingItems(new System.Collections.ObjectModel.ObservableCollection<object>());
            }
            Text = null;
            return;
        }

        // remove it because it's gotta be a "popup"

        Visual.Children.Remove(listBoxInstance);
        listBoxInstance.RemoveFromManagers();

        if (listBoxInstance.FormsControlAsObject == null)
        {
            listBox = new ListBox(listBoxInstance);
        }
        else
        {
            listBox = listBoxInstance.FormsControlAsObject as ListBox
                ?? throw new Exception(
                    $"The ListBoxInstance Gum component inside the combo box {Visual.Name} is of type " +
                    $"{listBoxInstance.FormsControlAsObject.GetType().Name}, but it should be of type ListBox");

        }
        var effectiveParent = listBox.Visual.EffectiveParentGue as InteractiveGue;
        if (effectiveParent != null)
        {
            effectiveParent.RaiseChildrenEventsOutsideOfBounds = true;
        }
        else
        {
            Visual.RaiseChildrenEventsOutsideOfBounds = true;
        }

        listBox.IsVisible = false;
        Text = null;
    }

    #endregion

    #region Event Handler Methods

    private void HandleClick(object? sender, EventArgs args)
    {
        UpdateState();
    }

    private void HandleRollOn(object? sender, EventArgs args)
    {
        UpdateState();
    }

    private void HandleRollOff(object? sender, EventArgs args)
    {
        UpdateState();
    }

    private void HandlePush(object? sender, EventArgs args)
    {
        if (IsDropDownOpen)
        {
            HideListBox();
        }
        else
        {
            OpenDropDown();
        }

    }

    private void OpenDropDown()
    {
        ShowListBoxInstance();

        // A selection made while closed was queued by the list box (it cannot scroll until it is
        // laid out); now that the drop-down is laid out, bring it into view (#6643).
        listBox.ApplyPendingScrollIntoView();
    }

    private void ShowListBoxInstance()
    {
        EnsureListBox();

        _hadFocusOnListBoxOpened = IsFocused;

#if !FRB
        // An open drop-down needs Activity every frame (IUpdateEveryFrame.IsActivityNeeded), which
        // ancestors that already went idle would otherwise skip.
        Visual.InvalidateSubtreeCaches();
#endif

        var wasSuspenced = GraphicalUiElement.IsAllLayoutSuspended;
        if(!wasSuspenced)
        {
            GraphicalUiElement.IsAllLayoutSuspended = true;
        }
        try
        {
            listBox.IsVisible = true;
        }
        finally
        {
            if(!wasSuspenced)
            {
                GraphicalUiElement.IsAllLayoutSuspended = false;
            }
        }

        if(!wasSuspenced)
        {
            listBox.Visual.UpdateLayout();
        }

        // The values the list box has while attached to this ComboBox are only available now, before
        // it is detached into the popup layer:
        listBoxWidthUnits = listBox.Visual.WidthUnits;
        listBoxHeightUnits = listBox.Visual.HeightUnits;
        listBoxXUnits = listBox.Visual.XUnits;
        listBoxYUnits = listBox.Visual.YUnits;

        listBoxX = listBox.Visual.X;
        listBoxY = listBox.Visual.Y;
        listBoxWidth = listBox.Visual.Width;
        listBoxHeight = listBox.Visual.Height;

        PlaceListBoxRelativeToComboBox();

#if FRB
        GuiManager.AddNextPushAction(TryHideFromPush);
        GuiManager.SortZAndLayerBased();
        listBox.RepositionToKeepInScreen();

#else

        InteractiveGue.AddNextPushAction(TryHideFromPush, Visual);

        WatchAncestorsWhileOpen();
#endif


        UpdateState();
    }

    /// <summary>
    /// Puts the list box in the popup layer at the pixel position that matches this ComboBox's
    /// current layout. Expects the list box's pre-popup layout values to be saved in the listBox*
    /// fields; they are re-applied first so this also works when the list box is already a popup.
    /// </summary>
    private void PlaceListBoxRelativeToComboBox()
    {
        var listBoxVisual = listBox.Visual;

        // A list box that is already a popup has absolute units; go back to the values
        // it has when attached to the ComboBox so the layout engine resolves them again.
        listBoxVisual.Parent = null;
        RestoreListBoxLayoutValues();
#if !FRB
        RestoreShrunkLevels();
#endif

        // Adding this to the Visual will force a layout....
        this.Visual.Children.Add(listBoxVisual);
        // ... so grab the absolute values:
        var x = listBoxVisual.AbsoluteX;
        var y = listBoxVisual.AbsoluteY;
        var width = listBoxVisual.AbsoluteWidth;
        var height = listBoxVisual.AbsoluteHeight;

#if !FRB
        // A drop-down taller than the root it opens in is shrunk to that root's height and scrolls;
        // ShowPopupListBox then moves it to the root's top, as it does any popup that does not fit (#6769).
        float availableHeight = ListBox.GetPopupTargetRoot(this.Visual).AbsoluteHeight;
        if (height > availableHeight)
        {
            FillShrunkLevels();
            height = availableHeight;
        }
#endif

        // Now that we have the values, detach it:
        this.Visual.Children.Remove(listBoxVisual);
        listBox.X = x;
        listBox.Y = y;
        listBoxVisual.WidthUnits = DimensionUnitType.Absolute;
        listBoxVisual.HeightUnits = DimensionUnitType.Absolute;
        listBox.Width = width;
        listBox.Height = height;
        ListBox.ShowPopupListBox(listBox, this.Visual);

        // Recorded last, with the list box detached: this is the state later frames compare against,
        // and a ComboBox sized to its children has a different rect while the list box is attached.
        _popupPlacement.Record(Visual, listBoxVisual);
    }

    private void RestoreListBoxLayoutValues()
    {
        listBox.Visual.XUnits = listBoxXUnits;
        listBox.Visual.YUnits = listBoxYUnits;
        listBox.Visual.WidthUnits = listBoxWidthUnits;
        listBox.Visual.HeightUnits = listBoxHeightUnits;

        listBox.Visual.X = listBoxX;
        listBox.Visual.Y = listBoxY;
        listBox.Visual.Width = listBoxWidth;
        listBox.Visual.Height = listBoxHeight;
    }

#if !FRB
    // The levels between the list box and its rows that are sized to their children (a theme's
    // FitChildren drop-down) with their own values, while the drop-down is shrunk and they fill instead.
    readonly List<(GraphicalUiElement level, DimensionUnitType heightUnits, float height)> _shrunkLevels = new();

    /// <summary>
    /// Makes every level between the list box and its rows that is sized to its children fill its parent
    /// instead, keeping the margin it has now, so the levels follow the shrunk list box and clip the rows
    /// to it. Expects the list box laid out at its natural size.
    /// </summary>
    private void FillShrunkLevels()
    {
        var level = listBox.InnerPanel.Parent as GraphicalUiElement;
        while (level != null && level != listBox.Visual && level.Parent is GraphicalUiElement parent)
        {
            if (level.HeightUnits == DimensionUnitType.RelativeToChildren)
            {
                _shrunkLevels.Add((level, level.HeightUnits, level.Height));
            }
            level = parent;
        }

        // Every margin is measured before any level changes, since each change lays the others out again.
        var margins = new float[_shrunkLevels.Count];
        for (int i = 0; i < _shrunkLevels.Count; i++)
        {
            GraphicalUiElement shrunkLevel = _shrunkLevels[i].level;
            margins[i] = shrunkLevel.AbsoluteHeight - ((GraphicalUiElement)shrunkLevel.Parent!).AbsoluteHeight;
        }
        for (int i = 0; i < _shrunkLevels.Count; i++)
        {
            _shrunkLevels[i].level.HeightUnits = DimensionUnitType.RelativeToParent;
            _shrunkLevels[i].level.Height = margins[i];
        }
    }

    private void RestoreShrunkLevels()
    {
        foreach (var (level, heightUnits, height) in _shrunkLevels)
        {
            level.HeightUnits = heightUnits;
            level.Height = height;
        }
        _shrunkLevels.Clear();
    }

    /// <summary>
    /// While the drop-down is open, places it again whenever this ComboBox's absolute rectangle or the
    /// size of the root the drop-down is in changes (canvas resize, UI scale, or this control or an
    /// ancestor moving). Does nothing when closed.
    /// </summary>
    // Only an open drop-down needs the per-frame check; a closed ComboBox must not stop its ancestors
    // from going idle. ShowListBoxInstance invalidates the idle caches when the drop-down opens.
    bool Gum.Wireframe.IUpdateEveryFrame.IsActivityNeeded => IsDropDownOpen;

    void Gum.Wireframe.IUpdateEveryFrame.Activity(double secondDifference)
    {
        if (!IsDropDownOpen)
        {
            return;
        }

        if (_popupPlacement.DidBoundsChange(listBox.Visual) || _popupPlacement.DidOwnerChange(Visual))
        {
            PlaceListBoxRelativeToComboBox();
        }
    }
#endif

#if !FRB
    // While open, the popup is in the popup layer, so hiding this ComboBox or an ancestor, or detaching
    // it from the tree, would leave the popup on screen: AnimateSelf skips hidden elements, so Activity
    // never ticks to notice. Instead of polling, the drop-down listens to the visibility and parent
    // changes of the Visual and each ancestor, and closes itself the way a native combo box does (#6681).
    readonly List<GraphicalUiElement> _watchedAncestors = new();

    private void WatchAncestorsWhileOpen()
    {
        StopWatchingAncestors();

        GraphicalUiElement? current = Visual;
        while (current != null)
        {
            current.VisibleChanged += HandleWatchedVisibleChanged;
            current.ParentChanged += HandleWatchedParentChanged;
            _watchedAncestors.Add(current);
            current = current.Parent;
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
            HideListBox(restoreFocus: false);
        }
    }

    private void HandleWatchedParentChanged(object? sender, GraphicalUiElement.ParentChangedEventArgs args)
    {
        if (args.NewValue == null || !Visual.AbsoluteVisible)
        {
            // Detached from the tree, or moved somewhere hidden. This runs inside the Children change of
            // the old or new parent; when that parent is also the popup's root (a ComboBox directly in
            // PopupRoot or ModalRoot), removing the popup now would change the notifying collection.
            GraphicalUiElement? popupParent = listBox.Visual.Parent;
            bool popupRootIsNotifying = popupParent != null &&
                (popupParent == args.OldValue || popupParent == args.NewValue);
            HideListBox(restoreFocus: false, deferRemovingPopup: popupRootIsNotifying);
        }
        else
        {
            // Moved to another parent: the popup follows through Activity, and the ancestors to listen to changed.
            WatchAncestorsWhileOpen();
        }
    }
#endif

    private void TryHideFromPush()
    {
        // A push action queued by an earlier open can run after the drop-down closed (or its ListBox or
        // Visual was replaced); there is nothing to hide, and it must not queue itself again.
        if (!IsDropDownOpen)
        {
            return;
        }

        var cursor = MainCursor;


        var clickedOnThisOrChild =
            cursor.VisualOver == this.Visual ||
            (cursor.VisualOver != null && cursor.VisualOver.IsInParentChain(this.Visual)) ||
            (cursor.VisualOver != null && cursor.VisualOver.IsInParentChain(this.listBox.Visual))
            ;

        if (clickedOnThisOrChild == false)
        {
            HideListBox();
        }
        else
        {
            // not sure why we have to re-add this here on a push.
            // Update - because when we click on something like the scroll
            // view in the popup listbox, this event gets raised. We don't want
            // to close this, but we may still want to next click if it's outside
            // of the list box. If the user keeps clicking on the scrollbar, this event
            // keeps getting raised, and it re-adds itself.
#if FRB
            GuiManager.AddNextPushAction(TryHideFromPush);
#else
            InteractiveGue.AddNextPushAction(TryHideFromPush, Visual);
#endif
        }
    }

    /// <param name="restoreFocus">Whether focus returns to this ComboBox if it had focus when opened.</param>
    /// <param name="deferRemovingPopup">When true, the popup is hidden now but only removed from its root
    /// on the next GumService update, for callers that run inside a change of the root's Children.</param>
    private void HideListBox(bool restoreFocus = true, bool deferRemovingPopup = false)
    {
#if !FRB
        StopWatchingAncestors();
#endif

        if (listBox != null && listBox.IsVisible)
        {
            // The ComboBox may already be out of the tree (its Visual has no managers), but the popup is
            // in the popup layer, which does.
            var managers = Visual?.EffectiveManagers ?? listBox.Visual.EffectiveManagers;
            ListBox hiddenListBox = listBox;

            listBox.IsVisible = false;

#if !FRB
            // A drop-down closed while shrunk gives its levels back their own layout values (#6769).
            RestoreShrunkLevels();

            if (deferRemovingPopup && global::RenderingLibrary.IGumService.Default != null)
            {
                // Put the layout values back now so opening again before the removal runs saves the
                // right values; the removal skips a popup that was opened again in the meantime.
                RestoreListBoxLayoutValues();
                global::RenderingLibrary.IGumService.Default.DeferredQueue.Enqueue(() =>
                {
                    if (!hiddenListBox.IsVisible)
                    {
                        RemovePopupFromLayers(hiddenListBox, managers);
                    }
                });
            }
            else
#endif
            {
                RemovePopupFromLayers(hiddenListBox, managers);
                RestoreListBoxLayoutValues();
            }

            UpdateState();

            if (restoreFocus && _hadFocusOnListBoxOpened)
            {
                IsFocused = true;
            }
        }
    }

    private static void RemovePopupFromLayers(ListBox popup, ISystemManagers? managers)
    {
        // Don't do this, because doing so removes the bindings
        //popup.Visual.RemoveFromManagers();
        popup.Visual.Parent = null;

        managers?.Renderer.MainLayer.Remove(popup.Visual);
#if FRB
#else
        popup.Visual.GetTopParent()?.Children.Remove(popup.Visual);
#endif
    }

    private void HandleLosePush(object? sender, EventArgs args)
    {
        UpdateState();
    }

    private void HandleSelectionChanged(object? sender, SelectionChangedEventArgs args)
    {
        if (!IsDropDownOpen && listBox.SelectedIndex > -1)
        {
            // Queues the scroll in the list box until the drop-down is opened. This also covers
            // selections that did not scroll themselves, such as SelectedObject.
            listBox.ScrollIndexIntoView(listBox.SelectedIndex);
        }

        RaiseSelectionChanged(listBox.SelectedObject, args);
    }

    private void RaiseSelectionChanged(object? selectedObject, SelectionChangedEventArgs args)
    {
        // If we bind the Text, then don't set this here, the binding will take care of it
        if (!IsDataBound(nameof(Text)))
        {
            UpdateToObject(selectedObject);
        }

        PushValueToViewModel(nameof(SelectedObject));
        PushValueToViewModel(nameof(SelectedIndex));

        SelectionChanged?.Invoke(this, args);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075",
        Justification = "Only reflects when DisplayMemberPath is set, and its setter carries the " +
            "RequiresUnreferencedCode warning that surfaces the risk at the caller.")]
    public virtual void UpdateToObject(object? o)
    {
        if(!string.IsNullOrEmpty(DisplayMemberPath ))
        {
            var display = o
                ?.GetType()
                ?.GetProperty(DisplayMemberPath)
                ?.GetValue(o, null) as string;

            SetDisplayedText(display?.ToString());
        }
        else
        {
            SetDisplayedText(o?.ToString());
        }
    }

    // Through the TextInstance's SetProperty so layout that depends on the text (a ComboBox sized to
    // it, a sibling stacked after it) follows the change (#6736). TextNoTranslate because ComboBox
    // text is data-driven and never localized.
    private void SetDisplayedText(string? value) => textComponent.SetProperty("TextNoTranslate", value);

    private void HandleListBoxItemPushed(object? sender, EventArgs args)
    {
        HideListBox();
    }

    #endregion

    #region UpdateTo Methods

    private string? _savedText;
    private bool _hasSavedText;

    /// <inheritdoc/>
    public override void SaveRuntimeProperties()
    {
        _savedText = coreTextObject?.RawText;
        _hasSavedText = coreTextObject != null;
        base.SaveRuntimeProperties();
    }

    /// <inheritdoc/>
    public override void ApplyRuntimeProperties()
    {
        if (_hasSavedText && coreTextObject != null)
        {
            coreTextObject.RawText = _savedText;
        }
        _savedText = null;
        _hasSavedText = false;
        base.ApplyRuntimeProperties();
    }

    public override void UpdateState()
    {
        const string category = "ComboBoxCategoryState";

        var state = base.GetDesiredState();

        Visual.SetProperty(category, state);
    }

    #endregion

    #region IInputReceiver Methods

    public IInputReceiver? ParentInputReceiver =>
    this.GetParentInputReceiver();

    public void OnFocusUpdatePreview(RoutedEventArgs args)
    {
    }
    public void OnFocusUpdate()
    {
#if !FRB
        // A disabled control can hold focus (AllowFocusWhenDisabled) or be disabled while open; it does not act on input.
        if (IsDropDownOpen && !IsEnabled)
        {
            // skip input
        }
        else
#endif
        if (IsDropDownOpen)
        {
            DoOpenDropDownFocusUpdate();
        }
        else
        {
            DoClosedDropDownFocusUpdate();
        }

#if !FRB
        base.HandleKeyboardFocusUpdate();
#endif

        FocusUpdate?.Invoke(this);
    }

    private void DoOpenDropDownFocusUpdate()
    {
        var xboxGamepads = FrameworkElement.GamePadsForUiControl;

        for (int i = 0; i < xboxGamepads.Count; i++)
        {
            var gamepad = xboxGamepads[i];

            var movedDown = gamepad.ButtonRepeatRate(GamepadButton.DPadDown) ||
                gamepad.LeftStick.AsDPadPushedRepeatRate(DPadDirection.Down);

            var movedUp = gamepad.ButtonRepeatRate(GamepadButton.DPadUp) ||
                     gamepad.LeftStick.AsDPadPushedRepeatRate(DPadDirection.Up);

            var pressedButton = gamepad.ButtonPushed(GamepadButton.A) ||
                gamepad.ButtonPushed(GamepadButton.B);
            DoDropDownOpenFocusUpdate(movedDown, movedUp, pressedButton);
        }
#if FRB

        var genericGamepads = GuiManager.GenericGamePadsForUiControl;
        for(int i = 0; i < genericGamepads.Count; i++)
        {
            var gamepad = genericGamepads[i];

            var movedDown = gamepad.DPadRepeatRate(Xbox360GamePad.DPadDirection.Down) ||
                (gamepad.AnalogSticks.Length > 0 && gamepad.AnalogSticks[0].AsDPadPushedRepeatRate(Xbox360GamePad.DPadDirection.Down));
            var movedUp = gamepad.DPadRepeatRate(Xbox360GamePad.DPadDirection.Up) ||
                (gamepad.AnalogSticks.Length > 0 && gamepad.AnalogSticks[0].AsDPadPushedRepeatRate(Xbox360GamePad.DPadDirection.Up));

            var inputDevice = gamepad as IInputDevice;

            var pressedButton = inputDevice.DefaultPrimaryActionInput.WasJustPressed || inputDevice.DefaultBackInput.WasJustPressed;

            DoDropDownOpenFocusUpdate(movedDown, movedUp, pressedButton);
        }
#endif
    }

    int ItemCount => Items?.Count ?? 0;

    private void DoDropDownOpenFocusUpdate(bool movedDown, bool movedUp, bool pressedButton)
    {
        if (movedDown)
        {
            if (ItemCount > 0)
            {
                if (SelectedIndex < 0 && ItemCount > 0)
                {
                    SelectedIndex = 0;
                }
                else if (SelectedIndex < ItemCount - 1)
                {
                    SelectedIndex++;
                }

                this.listBox.ListBoxItems[SelectedIndex].IsFocused = true;
            }
        }
        else if (movedUp)
        {
            if (ItemCount > 0)
            {
                if (SelectedIndex < 0 && ItemCount > 0)
                {
                    SelectedIndex = 0;
                }
                else if (SelectedIndex > 0)
                {
                    SelectedIndex--;
                }

                this.listBox.ListBoxItems[SelectedIndex].IsFocused = true;
            }
        }

        if (pressedButton)
        {
            IsDropDownOpen = false;
        }
    }

    private void DoClosedDropDownFocusUpdate()
    {
        var gamepads = FrameworkElement.GamePadsForUiControl;

        for (int i = 0; i < gamepads.Count; i++)
        {
            var gamepad = gamepads[i];

            HandleGamepadNavigation(gamepad);

            if (IsEnabled && gamepad.ButtonPushed(GamepadButton.A))
            {
                OpenAndFocusListBox();
            }

            void RaiseIfPushedAndEnabled(GamepadButton button)
            {
                if (IsEnabled && gamepad.ButtonPushed(button))
                {
                    ControllerButtonPushed?.Invoke(button);
                }
            }

            RaiseIfPushedAndEnabled(GamepadButton.B);
            RaiseIfPushedAndEnabled(GamepadButton.X);
            RaiseIfPushedAndEnabled(GamepadButton.Y);
            RaiseIfPushedAndEnabled(GamepadButton.Start);
            RaiseIfPushedAndEnabled(GamepadButton.Back);
        }

        void OpenAndFocusListBox()
        {
            IsDropDownOpen = true;

            this.listBox.IsFocused = true;
            this.listBox.DoListItemsHaveFocus = true;
            
            if (SelectedIndex > -1 && SelectedIndex < ItemCount)
            {
                this.listBox.ListBoxItems[SelectedIndex].IsFocused = true;
            }
            
        }
#if FRB

        var genericGamepads = GuiManager.GenericGamePadsForUiControl;
        for(int i = 0; i < genericGamepads.Count; i++)
        {
            var gamepad = genericGamepads[i];

            HandleGamepadNavigation(gamepad);

            if ((gamepad as IInputDevice).DefaultConfirmInput.WasJustPressed)
            {
                IsDropDownOpen = IsDropDownOpen = true;

                if (SelectedIndex > -1 && SelectedIndex < ItemCount)
                {
                    this.listBox.ListBoxItems[SelectedIndex].IsFocused = true;
                }
            }

            if(IsEnabled)
            {
                for(int buttonIndex = 0; buttonIndex < gamepad.NumberOfButtons; i++)
                {
                    if(gamepad.ButtonPushed(buttonIndex))
                    {
                        GenericGamepadButtonPushed?.Invoke(buttonIndex);
                    }
                }
            }
        }
#endif
#if !FRB

        foreach (var keyboard in KeyboardsForUiControl)
        {
            var wasPushed = false;
            var wasReleased = false;
            foreach(var item in FrameworkElement.ClickCombos)
            {
                if(IsEnabled && item.IsComboPushed())
                {
                    wasPushed = true;
                }
                if(item.IsComboReleased())
                {
                    wasReleased = true;
                    UpdateState();
                }
            }
            if(wasPushed)
            {
                InteractiveGue.LastVisualPushed = this.Visual;
                this.HandleClick(this, new InputEventArgs() { InputDevice = keyboard });

                OpenAndFocusListBox();
                UpdateState();
            }
            if(wasReleased)
            {
                UpdateState();
            }
        }


        base.HandleKeyboardFocusUpdate();
#endif


    }

    public void OnGainFocus()
    {
        IsFocused = true;
    }

    [Obsolete("Use OnLoseFocus")]
    public void LoseFocus() => OnLoseFocus();
    public void OnLoseFocus()
    {
        IsFocused = false;
    }

    public void ReceiveInput()
    {
    }

    public void HandleKeyDown(Keys key, bool isShiftDown, bool isAltDown, bool isCtrlDown)
    {
    }

    public void HandleCharEntered(char character)
    {
    }

#if !FRB
    public void DoKeyboardAction(IInputReceiverKeyboard keyboard)
    {
        foreach (Keys key in keyboard.KeysTyped)
        {
            HandleKeyDown(key,
                keyboard.IsShiftDown,
                keyboard.IsAltDown,
                keyboard.IsCtrlDown);
        }
    }
#endif

#endregion
}
