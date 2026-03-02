using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using RenderingLibrary;
using System.Collections.ObjectModel;
using System.Reflection;
using Gum.Converters;
using Gum.DataTypes;
using RenderingLibrary.Graphics;
using System.Linq;



#if FRB
using FlatRedBall.Input;
using GamepadButton = FlatRedBall.Input.Xbox360GamePad.Button;
using Microsoft.Xna.Framework.Input;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Gui;
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
using Buttons = FlatRedBall.Input.Xbox360GamePad.Button;
using static FlatRedBall.Input.Xbox360GamePad;
namespace FlatRedBall.Forms.Controls;
#elif RAYLIB
using RaylibGum.Input;
using Keys = Raylib_cs.KeyboardKey;

using Gum.GueDeriving;

#else
using MonoGameGum.Input;
using GamepadButton = Microsoft.Xna.Framework.Input.Buttons;
using Microsoft.Xna.Framework.Input;
#endif

#if !FRB
namespace Gum.Forms.Controls;
#endif

#region SelectionMode Enum
/// <summary>
/// Specifies how items can be selected in a ListBox.
/// </summary>
public enum SelectionMode
{
    /// <summary>
    /// Only one item can be selected at a time. Clicking an item deselects all others.
    /// </summary>
    Single,

    /// <summary>
    /// Multiple items can be selected. Each click toggles the selection state of the clicked item without affecting other selections.
    /// </summary>
    Multiple,

    /// <summary>
    /// Multiple items can be selected using modifier keys. Click selects one item (deselecting others),
    /// Ctrl+Click toggles individual items, and Shift+Click selects a range from the anchor to the clicked item.
    /// </summary>
    Extended
}
#endregion

#region ScrollIntoViewStyle Enums
/// <summary>
/// Specifies the scrolling behavior to use when bringing an item into view within a scrollable container.
/// </summary>
public enum ScrollIntoViewStyle
{
    /// <summary>
    /// Scrolls only if the item is not in view. Scrolls the minimum amount necessary to bring the item into view.
    /// In other words, if the item is above the visible area, then the scrolling brings the item to the top.
    /// If the item is below the visible area, then the scrolling brings the item to the bottom.
    /// If the item is already into view, no scrolling is performed.
    /// </summary>
    BringIntoView,

    /// <summary>
    /// Scrolls the item to the top of the visible area.
    /// </summary>
    Top,
    /// <summary>
    /// Scrolls the item to the center of the visible area.
    /// </summary>
    Center,
    /// <summary>
    /// Scrolls the item to the bottom of the visible area.
    /// </summary>
    Bottom
}
#endregion

/// <summary>
/// Specifies the available modes for drag-and-drop reordering operations.
/// </summary>
public enum DragDropReorderMode
{
    /// <summary>
    /// Drag+drop reordering is disabled.
    /// </summary>
    NoReorder,
    /// <summary>
    /// Indicates that the operation should be performed immediately as dragging is happening.
    /// </summary>
    Immediate
}

#if !FRB

#region RepositionDirections Enum

internal enum RepositionDirections
{
    None = 0,
    Up = 1,
    Down = 2,
    Left = 4,
    Right = 8,
    All = 15,
}

#endregion

#endif

/// <summary>
/// Represents a control that displays a collection of selectable items in a list, allowing users to select one item at
/// a time. Supports keyboard, gamepad, and pointer navigation, as well as item reordering and focus management.
/// </summary>
/// <remarks>The ListBox provides advanced selection and navigation features, including support for focus
/// management between the list as a whole and individual items, drag-and-drop reordering (when enabled), and
/// integration with various input devices. Selection changes raise the SelectionChanged event, and controller input is
/// handled via the ControllerButtonPushed event. The ListBox can be customized to display items using different visual
/// templates and supports both data-bound and manually managed item collections. Thread safety is not guaranteed; all
/// interactions should occur on the UI thread.</remarks>
public class ListBox : ItemsControl, IInputReceiver
{
    #region Fields/Properties

    /// <summary>
    /// Provides internal storage for the collection of list box items.
    /// </summary>
    /// <remarks>This field is intended for internal use and should not be accessed directly from outside the
    /// containing class. Use public members to interact with the list box items when available.
    ///
    /// In normal usage, where view models or primitives are added to the Items collection, this
    /// list is automatically kept up-to-date. However, it is possible for the Items collection to
    /// not be up-to-date with ListBoxItemsInternal. This can happen if a visual is added directly
    /// to the Items collection. In this case, the ListBox assumes that the added visual should be directly
    /// displayed. However, if the visual already has a forms control, such as a Button having been added to
    /// the list box, then the control is used as-is, and it is not even added to the ListBoxItems.
    /// </remarks>
    protected List<ListBoxItem> ListBoxItemsInternal = new List<ListBoxItem>();

    ObservableCollection<object> selectedItemsCollection = new ObservableCollection<object>();
    bool _suppressSelectionSync = false;
    SelectionMode selectionMode = SelectionMode.Single;

    ReadOnlyCollection<ListBoxItem> listBoxItemsReadOnly;
    public ReadOnlyCollection<ListBoxItem> ListBoxItems
    {
        get
        {
            if (listBoxItemsReadOnly == null)
            {
                listBoxItemsReadOnly = new ReadOnlyCollection<ListBoxItem>(ListBoxItemsInternal);
            }
            return listBoxItemsReadOnly;
        }
    }

    /// <summary>
    /// Gets the collection of currently selected items. This collection can be modified to change the selection,
    /// and supports INotifyCollectionChanged for data binding. The collection cannot be replaced, only modified.
    /// </summary>
    /// <remarks>
    /// In Single selection mode, this collection will contain at most one item.
    /// In Multiple and Extended modes, this collection can contain multiple items.
    /// Changes to this collection are synchronized with the IsSelected state of ListBoxItems.
    /// </remarks>
    public System.Collections.IList SelectedItems
    {
        get => selectedItemsCollection;
    }

    bool doListBoxItemsHaveFocus;
    public bool DoListItemsHaveFocus
    {
        get => doListBoxItemsHaveFocus;
        set
        {
            if (!IsFocused && value)
            {
                IsFocused = true;
            }

            doListBoxItemsHaveFocus = value;

            if (SelectedIndex > -1 && SelectedIndex < ListBoxItemsInternal.Count)
            {
                ListBoxItemsInternal[SelectedIndex].IsFocused = doListBoxItemsHaveFocus;
            }
        }
    }

    bool canListItemsLoseFocus = true;
    /// <summary>
    /// Whether pressing the B button (back/cancel) should result in individual items losing focus and
    /// returning focus to the top level. This should be false if the list box is the only object in the 
    /// screen which can receive focus.
    /// </summary>
    public bool CanListItemsLoseFocus
    {
        get => canListItemsLoseFocus;
        set
        {
            canListItemsLoseFocus = value;
            if (!canListItemsLoseFocus && IsFocused)
            {
                DoListItemsHaveFocus = true;
            }
        }
    }

    int selectedIndex = -1;

    public override bool IsFocused
    {
        get => base.IsFocused;
        set
        {
            base.IsFocused = value;
            if (IsFocused && canListItemsLoseFocus == false)
            {
                DoListItemsHaveFocus = true;
            }
        }
    }




    [Obsolete("Use VisualTemplate")]
    public Type ListBoxItemGumType
    {
        get => ItemGumType;
        set => ItemGumType = value;
    }

    [Obsolete("Use FrameworkElementTemplate")]
    public Type ListBoxItemFormsType
    {
        get { return ItemFormsType; }
        set { ItemFormsType = value; }
    }


    // There can be a logical conflict when dealing with list items.
    // When creating a Gum list item, the Gum object may specify a Forms
    // type. But the list can also specify a forms type. So which do we use?
    // We'll use the list item forms type unless the list box has its value set
    // explicitly. then we'll go to the list box type. This eventually should get
    // marked as obsolete and we should instead go to a VM solution.
    protected bool isItemTypeSetExplicitly = false;
    Type itemFormsType = typeof(ListBoxItem);

    protected Type ItemFormsType
    {
        get => itemFormsType;
        set
        {
            if (value != itemFormsType)
            {
                isItemTypeSetExplicitly = true;
                itemFormsType = value;
            }
        }
    }

    public object SelectedObject
    {
        get
        {
            // Return the first item in SelectedItems, or null if empty
            if (selectedItemsCollection.Count > 0)
            {
                return selectedItemsCollection[0];
            }

            return null;
        }
        set
        {
            // Clear SelectedItems and select the single item
            _suppressSelectionSync = true;
            selectedItemsCollection.Clear();

            if (value != null)
            {
                selectedItemsCollection.Add(value);
            }
            _suppressSelectionSync = false;

            // Sync the IsSelected state of all ListBoxItems
            SyncIsSelectedFromSelectedItems();

            PushValueToViewModel();
        }
    }

    public int SelectedIndex
    {
        get
        {
            // Return the index of the first item in SelectedItems, or -1 if empty
            if (selectedItemsCollection.Count > 0)
            {
                var firstSelectedItem = selectedItemsCollection[0];
                return Items.IndexOf(firstSelectedItem);
            }

            return -1;
        }
        set
        {
            if (value > -1 && value < Items.Count)
            {
                var itemToSelect = Items[value];

                // Clear SelectedItems and select the single item
                _suppressSelectionSync = true;
                selectedItemsCollection.Clear();
                selectedItemsCollection.Add(itemToSelect);
                _suppressSelectionSync = false;

                // Sync the IsSelected state of all ListBoxItems
                SyncIsSelectedFromSelectedItems();

                ScrollIndexIntoView(value);
            }
            else if (value == -1)
            {
                // Clear all selections
                _suppressSelectionSync = true;
                selectedItemsCollection.Clear();
                _suppressSelectionSync = false;

                // Sync the IsSelected state of all ListBoxItems
                SyncIsSelectedFromSelectedItems();
            }
            else
            {
                throw new IndexOutOfRangeException();
            }
        }
    }

    public List<Keys> IgnoredKeys => throw new NotImplementedException();

    public bool TakingInput => throw new NotImplementedException();

    public IInputReceiver NextInTabSequence { get; set; }

    /// <summary>
    /// Whether the primary input button (usually the A button) results in the highlighted list box item
    /// being selected and in the ListBox focus moving outside of the individual items.
    /// </summary>
    /// <remarks>
    /// This value is true, but can be changed to false if the A button should perform actions on the highlighted
    /// list box item (such as toggling a check box) without focus being moved out of the individual items.
    /// </remarks>
    public bool LoseListItemFocusOnPrimaryInput { get; set; } = true;

    /// <summary>
    /// Gets or sets the selection behavior for the ListBox.
    /// </summary>
    /// <remarks>
    /// Single mode (default): Only one item can be selected at a time.
    /// Multiple mode: Each click toggles selection without modifier keys.
    /// Extended mode: Click selects one, Ctrl+Click toggles, Shift+Click selects range.
    /// Note: Gamepad and keyboard input always use single-selection behavior regardless of this mode.
    /// </remarks>
    public SelectionMode SelectionMode
    {
        get => selectionMode;
        set
        {
            if (selectionMode != value)
            {
                selectionMode = value;

                // If switching to Single mode with multiple selections, keep only the first one
                if (selectionMode == SelectionMode.Single && selectedItemsCollection.Count > 1)
                {
                    _suppressSelectionSync = true;
                    var firstItem = selectedItemsCollection[0];
                    selectedItemsCollection.Clear();
                    selectedItemsCollection.Add(firstItem);
                    _suppressSelectionSync = false;
                }
            }
        }
    }

    #region Modifier Key Configuration

    /// <summary>
    /// The primary key used to toggle individual item selection in Extended selection mode.
    /// Default is LeftControl.
    /// </summary>
    public static Keys ToggleSelectionModifierKey { get; set; } = Keys.LeftControl;

    /// <summary>
    /// The alternate key used to toggle individual item selection in Extended selection mode.
    /// Default is RightControl.
    /// </summary>
    public static Keys AlternateToggleSelectionModifierKey { get; set; } = Keys.RightControl;

    /// <summary>
    /// The primary key used to select a range of items in Extended selection mode.
    /// Default is LeftShift.
    /// </summary>
    public static Keys RangeSelectionModifierKey { get; set; } = Keys.LeftShift;

    /// <summary>
    /// The alternate key used to select a range of items in Extended selection mode.
    /// Default is RightShift.
    /// </summary>
    public static Keys AlternateRangeSelectionModifierKey { get; set; } = Keys.RightShift;

    #endregion

    public override string DisplayMemberPath
    { 
        get => base.DisplayMemberPath;
        set
        {
            if(value != base.DisplayMemberPath)
            {
                base.DisplayMemberPath = value;

                for(int i = 0; i < Items.Count; i++)
                {
                    var listBoxItem = ListBoxItems[i];

                    if(value == string.Empty)
                    {
                        listBoxItem.UpdateToObject(Items[i]);
                    }
                    else
                    {
                        var item = Items[i];
                        var display = item.GetType()
                            .GetProperty(DisplayMemberPath)
                            .GetValue(item, null) as string;
                        listBoxItem.UpdateToObject(display);
                    }

                }
            }
        }
    }

    /// <summary>
    /// Controls the ListBox's drag+drop behavior for reordering ListBoxItems. If true,
    /// items can be reordered by pushing on an item and dragging it to the position of 
    /// another item. By default this is set to NoReorder.
    /// </summary>
    public DragDropReorderMode DragDropReorderMode
    {
        get; set;
    }

    public override bool IsEnabled
    {
        get =>base.IsEnabled;
        set
        {
            base.IsEnabled = value;
            foreach(var item in this.ListBoxItems)
            {
                item.UpdateState();
            }
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// Event raised whenever the selection changes. The object parameter is the sender (list box) and the SelectionChangedeventArgs
    /// contains information about the changed selected items.
    /// </summary>
    public event Action<object, SelectionChangedEventArgs> SelectionChanged;
    public event Action<IInputReceiver> FocusUpdate;

    /// <summary>
    /// Event raised when the user presses a button, whether at the top level or internally on
    /// an item.
    /// </summary>
    /// <remarks>
    /// Until July 2024 this was only firing at the top level. July 2024 version also raises
    /// this event when a button is pushed on an item.
    /// </remarks>
    public event Action<GamepadButton> ControllerButtonPushed;

#if FRB
    public event Action<int> GenericGamepadButtonPushed;
#endif

#endregion

    #region Initialize Methods

    public ListBox() : base()
    {
        selectedItemsCollection.CollectionChanged += HandleSelectedItemsCollectionChanged;
    }

    public ListBox(InteractiveGue visual) : base(visual)
    {
        selectedItemsCollection.CollectionChanged += HandleSelectedItemsCollectionChanged;
    }

    protected override void ReactToVisualChanged()
    {
        // do base first, so InnerPanel can get assigned by the base
        base.ReactToVisualChanged();

        if (InnerPanel?.Children.Count > 0 && this.Items?.Count > 0 == false)
        {
            foreach(var item in InnerPanel.Children)
            {
                if(item is InteractiveGue interactiveGue && interactiveGue.FormsControlAsObject is ListBoxItem listBoxItem)
                {
                    this.Items.Add(listBoxItem);
                    if(this.Items is not INotifyCollectionChanged )
                    {
                        ListBoxItemsInternal.Add(listBoxItem);
                        listBoxItem.AssignListBoxEvents(
                            HandleItemSelected, 
                            HandleItemFocused, 
                            HandleListBoxItemPushed, 
                            HandleListBoxItemClicked,
                            HandleListBoxItemDragging);
                    }
                }
            }
        }
    }


    #endregion

    #region Item Creation

    protected override FrameworkElement CreateNewItemFrameworkElement(object o)
    {
        ListBoxItem? item = null;
        if (o is ListBoxItem)
        {
            // the user provided a list box item, so just use that directly instead of creating a new one
            item = (ListBoxItem)o;
        }
        else
        {
            var visual = CreateNewVisual(o);

            if (visual is InteractiveGue interactiveGue && interactiveGue.FormsControlAsObject != null)
            {
                item = interactiveGue.FormsControlAsObject as ListBoxItem;
            }

            if(item == null)
            {
                item = CreateNewListBoxItem(visual);
            }

            CallUpdateToObject(o, item);

            item.BindingContext = o;
        }

        return item;
    }

    protected override void HandleCreatedItemVisual(GraphicalUiElement newVisual, object item)
    {
        var listBoxItem = (newVisual as InteractiveGue)?.FormsControlAsObject as ListBoxItem;

        if(listBoxItem != null)
        {
            CallUpdateToObject(item, listBoxItem);
        }
    }

    private void CallUpdateToObject(object objectToUpdateTo, ListBoxItem listBoxItem)
    {
        if (!string.IsNullOrEmpty(DisplayMemberPath))
        {
            var display = objectToUpdateTo.GetType()
                .GetProperty(DisplayMemberPath)
                .GetValue(objectToUpdateTo, null) as string;
            listBoxItem.UpdateToObject(display);

        }
        else
        {
            listBoxItem.UpdateToObject(objectToUpdateTo);
        }
    }

    private ListBoxItem CreateNewListBoxItem(InteractiveGue visual)
    {
        if (FrameworkElementTemplate != null)
        {
            var item = FrameworkElementTemplate.CreateContent();
            if (item != null && item is ListBoxItem == false)
            {
                throw new InvalidOperationException($"Could not create an item of type {item.GetType()} because it must inherit from ListBoxItem.");
            }
            return (ListBoxItem)item!;
        }
        else
        {
#if FULL_DIAGNOSTICS
            if (ItemFormsType == null)
            {
                throw new Exception($"This {GetType().Name} named {this.Name} does not have a ItemFormsType specified. " +
                    "This property must be set before adding any items");
            }
#endif

            ListBoxItem item;
            if (visual.FormsControlAsObject is ListBoxItem asListBoxItem && !isItemTypeSetExplicitly)
            {
                item = asListBoxItem;
            }
            else
            {
                var listBoxFormsConstructor = ItemFormsType.GetConstructor(new Type[] { typeof(InteractiveGue) });

                if (listBoxFormsConstructor == null)
                {
#if FRB
                    const string TypeName = "GraphicalUiElement";
#else
                    const string TypeName = "InteractiveGue";
#endif
                    string message =
                        $"Could not find a constructor for {ItemFormsType} which takes a single {TypeName} argument. " +
                        $"If you defined {ItemFormsType} without specifying a constructor, you need to add a constructor which takes a GraphicalUiElement and calls the base constructor.";
                    throw new Exception(message);
                }
                item = listBoxFormsConstructor.Invoke(new object[] { visual }) as ListBoxItem;
            }

            return item;
        }
    }

    #endregion

    #region Item Event Handlers

    private void HandleItemSelected(object? sender, EventArgs e)
    {
        if (_suppressSelectionSync)
        {
            return;
        }

        var listBoxItem = sender as ListBoxItem;
        if (listBoxItem == null || !listBoxItem.IsEnabled)
        {
            return;
        }

        var clickedIndex = ListBoxItemsInternal.IndexOf(listBoxItem);
        if (clickedIndex < 0 || clickedIndex >= Items.Count)
        {
            return;
        }

        var clickedItem = Items[clickedIndex];

        // Check if modifier keys are pressed (only for mouse/pointer input)
        bool isCtrlDown = false;
        bool isShiftDown = false;

#if (MONOGAME || KNI || FNA) && !FRB
        // Use the same pattern as KeyCombo and TextBox - check KeyboardsForUiControl.
        // If the list is empty, no modifiers are detected (consistent with the rest of Gum's keyboard input handling).
        var keyboards = FrameworkElement.KeyboardsForUiControl;
        foreach (var keyboard in keyboards)
        {
            if (keyboard.KeyDown(ToggleSelectionModifierKey) || keyboard.KeyDown(AlternateToggleSelectionModifierKey))
            {
                isCtrlDown = true;
            }
            if (keyboard.KeyDown(RangeSelectionModifierKey) || keyboard.KeyDown(AlternateRangeSelectionModifierKey))
            {
                isShiftDown = true;
            }
        }
#endif

        var args = new SelectionChangedEventArgs();

        _suppressSelectionSync = true;

        switch (SelectionMode)
        {
            case SelectionMode.Single:
                // Deselect all other items
                foreach (var item in selectedItemsCollection.ToList())
                {
                    if (item != clickedItem)
                    {
                        args.RemovedItems.Add(item);
                    }
                }
                selectedItemsCollection.Clear();

                // Select the clicked item
                if (!selectedItemsCollection.Contains(clickedItem))
                {
                    selectedItemsCollection.Add(clickedItem);
                    args.AddedItems.Add(clickedItem);
                }
                break;

            case SelectionMode.Multiple:
                // Toggle the clicked item
                if (selectedItemsCollection.Contains(clickedItem))
                {
                    selectedItemsCollection.Remove(clickedItem);
                    args.RemovedItems.Add(clickedItem);
                }
                else
                {
                    selectedItemsCollection.Add(clickedItem);
                    args.AddedItems.Add(clickedItem);
                }
                break;

            case SelectionMode.Extended:
                if (isCtrlDown)
                {
                    // Ctrl+Click: Toggle the clicked item
                    if (selectedItemsCollection.Contains(clickedItem))
                    {
                        selectedItemsCollection.Remove(clickedItem);
                        args.RemovedItems.Add(clickedItem);
                    }
                    else
                    {
                        selectedItemsCollection.Add(clickedItem);
                        args.AddedItems.Add(clickedItem);
                    }
                }
                else if (isShiftDown)
                {
                    // Shift+Click: Select range from anchor to clicked item
                    if (selectedItemsCollection.Count > 0)
                    {
                        var anchorItem = selectedItemsCollection[0];
                        var anchorIndex = Items.IndexOf(anchorItem);

                        if (anchorIndex >= 0)
                        {
                            var minIndex = System.Math.Min(anchorIndex, clickedIndex);
                            var maxIndex = System.Math.Max(anchorIndex, clickedIndex);

                            // Select all enabled items in range
                            for (int i = minIndex; i <= maxIndex; i++)
                            {
                                if (i < ListBoxItemsInternal.Count && ListBoxItemsInternal[i].IsEnabled)
                                {
                                    var itemInRange = Items[i];
                                    if (!selectedItemsCollection.Contains(itemInRange))
                                    {
                                        selectedItemsCollection.Add(itemInRange);
                                        args.AddedItems.Add(itemInRange);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // No anchor, just select the clicked item
                        selectedItemsCollection.Add(clickedItem);
                        args.AddedItems.Add(clickedItem);
                    }
                }
                else
                {
                    // Click alone: Select only the clicked item, deselect all others
                    foreach (var item in selectedItemsCollection.ToList())
                    {
                        if (item != clickedItem)
                        {
                            args.RemovedItems.Add(item);
                        }
                    }
                    selectedItemsCollection.Clear();

                    selectedItemsCollection.Add(clickedItem);
                    args.AddedItems.Add(clickedItem);
                }
                break;
        }

        _suppressSelectionSync = false;

        // Sync IsSelected state for all ListBoxItems
        SyncIsSelectedFromSelectedItems();

        // Fire SelectionChanged event
        if (args.AddedItems.Count > 0 || args.RemovedItems.Count > 0)
        {
            SelectionChanged?.Invoke(this, args);
        }

        PushValueToViewModel(nameof(SelectedObject));
        PushValueToViewModel(nameof(SelectedIndex));
    }

    private void HandleItemFocused(object? sender, EventArgs e)
    {
        OnItemFocused(sender, EventArgs.Empty);
    }

    private void HandleListBoxItemClicked(object? sender, EventArgs e)
    {
        OnItemClicked(sender, EventArgs.Empty);
    }

    private void HandleListBoxItemPushed(object? sender, EventArgs e)
    {
        OnItemPushed(sender, EventArgs.Empty);
    }

    private void HandleListBoxItemDragging(object? sender, EventArgs e)
    {
        if (sender == null || DragDropReorderMode == DragDropReorderMode.NoReorder) return;

        var visual = (InteractiveGue)sender;

        var index = visual.Parent!.Children.IndexOf(visual);

        var cursor = FrameworkElement.MainCursor;

        // are we still over the same item?
        var isOverSameItem = visual.HasCursorOver(cursor);

        if(!isOverSameItem && this.Visual.HasCursorOver(cursor))
        {
            // see if we're over any of the other children. If so, we should drop the item there:
            // we want to start at the current index and go "out" to increase the chances of finding it soon

            var childrenCount = visual.Parent.Children.Count;

            // The bounds check here is just for sanity's sake, but it should early-out sooner
            for (int i = 1; i < childrenCount; i++)
            {
                var isAboveTop = index - i < 0;
                var isBelowBottom = index + i >= childrenCount;

                if(isAboveTop && isBelowBottom)
                {
                    break;
                }
                if(!isAboveTop)
                {
                    var itemAbove = visual.Parent.Children[index - i] as InteractiveGue;
                    var isOver = itemAbove != null && itemAbove.HasCursorOver(cursor);
                    if(isOver)
                    {
                        // swap the items:
                        MoveItem(index, index - i);
                        // do we also need to change Items and ListBoxItems?

                        break;
                    }
                }

                if(!isBelowBottom)
                {
                    var itemBelow = visual.Parent.Children[index + i] as InteractiveGue;
                    var isOver = itemBelow != null && itemBelow.HasCursorOver(cursor);
                    if (isOver)
                    {
                        // swap the items:
                        MoveItem(index, index + i);

                        break;
                    }
                }

                void MoveItem(int oldIndex, int newIndex)
                {
                    var item = Items[oldIndex];

                    _suppressCollectionChangedToBase = true;

                    Items.RemoveAt(oldIndex);
                    Items.Insert(newIndex, item);

                    _suppressCollectionChangedToBase = false;

                    var isBound = this.PropertyRegistry.GetBindingExpression(nameof(Items)) != null;
                    if (Items is not INotifyCollectionChanged || !isBound)
                    {
                        // If we are bound, just move
                        visual.Parent.Children.Move(oldIndex, newIndex);
                        var itemToMove = ListBoxItemsInternal[oldIndex];

                        var listBoxItemReplaced = ListBoxItemsInternal[newIndex];
                        // This probably got highlighted by the cursor when dragging over it, so let's
                        // unhighlight it so it doesn't flicker:
                        listBoxItemReplaced.IsHighlighted = false;

                        ListBoxItemsInternal.RemoveAt(oldIndex);
                        ListBoxItemsInternal.Insert(newIndex, itemToMove);
                    }

                }
            }
        }
    }

    #endregion

    #region Selection Synchronization

    /// <summary>
    /// Synchronizes the IsSelected state of all ListBoxItems based on the SelectedItems collection.
    /// </summary>
    private void SyncIsSelectedFromSelectedItems()
    {
        if (_suppressSelectionSync)
        {
            return;
        }

        _suppressSelectionSync = true;

        var selectionChangedArgs = new SelectionChangedEventArgs();

        for (int i = 0; i < ListBoxItemsInternal.Count; i++)
        {
            var listBoxItem = ListBoxItemsInternal[i];
            var item = i < Items.Count ? Items[i] : listBoxItem;

            bool shouldBeSelected = selectedItemsCollection.Contains(item);

            if (listBoxItem.IsSelected != shouldBeSelected)
            {
                listBoxItem.IsSelected = shouldBeSelected;

                if (shouldBeSelected)
                {
                    selectionChangedArgs.AddedItems.Add(item);
                }
                else
                {
                    selectionChangedArgs.RemovedItems.Add(item);
                }
            }
        }

        _suppressSelectionSync = false;

        // Update selectedIndex for backward compatibility
        selectedIndex = SelectedIndex;

        // Fire SelectionChanged event if there were changes
        if (selectionChangedArgs.AddedItems.Count > 0 || selectionChangedArgs.RemovedItems.Count > 0)
        {
            SelectionChanged?.Invoke(this, selectionChangedArgs);
        }
    }

    /// <summary>
    /// Handles changes to the SelectedItems collection and synchronizes the IsSelected state of ListBoxItems.
    /// </summary>
    private void HandleSelectedItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (_suppressSelectionSync)
        {
            return;
        }

        _suppressSelectionSync = true;

        var selectionChangedArgs = new SelectionChangedEventArgs();

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems)
                    {
                        var index = Items.IndexOf(item);
                        if (index >= 0 && index < ListBoxItemsInternal.Count)
                        {
                            var listBoxItem = ListBoxItemsInternal[index];
                            if (!listBoxItem.IsSelected)
                            {
                                listBoxItem.IsSelected = true;
                                selectionChangedArgs.AddedItems.Add(item);
                            }
                        }
                    }
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems)
                    {
                        var index = Items.IndexOf(item);
                        if (index >= 0 && index < ListBoxItemsInternal.Count)
                        {
                            var listBoxItem = ListBoxItemsInternal[index];
                            if (listBoxItem.IsSelected)
                            {
                                listBoxItem.IsSelected = false;
                                selectionChangedArgs.RemovedItems.Add(item);
                            }
                        }
                    }
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                // Deselect all items
                for (int i = 0; i < ListBoxItemsInternal.Count; i++)
                {
                    var listBoxItem = ListBoxItemsInternal[i];
                    if (listBoxItem.IsSelected)
                    {
                        listBoxItem.IsSelected = false;
                        var item = i < Items.Count ? Items[i] : listBoxItem;
                        selectionChangedArgs.RemovedItems.Add(item);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Replace:
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems)
                    {
                        var index = Items.IndexOf(item);
                        if (index >= 0 && index < ListBoxItemsInternal.Count)
                        {
                            var listBoxItem = ListBoxItemsInternal[index];
                            if (listBoxItem.IsSelected)
                            {
                                listBoxItem.IsSelected = false;
                                selectionChangedArgs.RemovedItems.Add(item);
                            }
                        }
                    }
                }
                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems)
                    {
                        var index = Items.IndexOf(item);
                        if (index >= 0 && index < ListBoxItemsInternal.Count)
                        {
                            var listBoxItem = ListBoxItemsInternal[index];
                            if (!listBoxItem.IsSelected)
                            {
                                listBoxItem.IsSelected = true;
                                selectionChangedArgs.AddedItems.Add(item);
                            }
                        }
                    }
                }
                break;
        }

        _suppressSelectionSync = false;

        // Update selectedIndex for backward compatibility
        selectedIndex = SelectedIndex;

        // Fire SelectionChanged event if there were changes
        if (selectionChangedArgs.AddedItems.Count > 0 || selectionChangedArgs.RemovedItems.Count > 0)
        {
            SelectionChanged?.Invoke(this, selectionChangedArgs);
            PushValueToViewModel(nameof(SelectedObject));
            PushValueToViewModel(nameof(SelectedIndex));
        }
    }

    #endregion

    #region Collection Changed

    bool _suppressCollectionChangedToBase = false;
    bool _isAddingFromItemsCollection = false;
    protected override void HandleItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if(_suppressCollectionChangedToBase == false)
        {
            _isAddingFromItemsCollection = e.Action == NotifyCollectionChangedAction.Add;
            try
            {
                base.HandleItemsCollectionChanged(sender, e);
            }
            finally
            {
                _isAddingFromItemsCollection = false;
            }
        }

        // Handle removal of selected items from the Items collection
        if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
        {
            _suppressSelectionSync = true;

            foreach (var removedItem in e.OldItems)
            {
                if (selectedItemsCollection.Contains(removedItem))
                {
                    selectedItemsCollection.Remove(removedItem);
                }
            }

            _suppressSelectionSync = false;

            PushValueToViewModel(nameof(SelectedObject));
            PushValueToViewModel(nameof(SelectedIndex));
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            // Clear all selections when Items is reset
            _suppressSelectionSync = true;
            selectedItemsCollection.Clear();
            _suppressSelectionSync = false;

            PushValueToViewModel(nameof(SelectedObject));
            PushValueToViewModel(nameof(SelectedIndex));
        }
    }

    protected override void HandleCollectionItemMoved(int oldIndex, int newIndex)
    {
        var itemToMove = ListBoxItemsInternal[oldIndex];

        ListBoxItemsInternal.RemoveAt(oldIndex);
        ListBoxItemsInternal.Insert(newIndex, itemToMove);
        if (SelectedIndex == oldIndex)
        {
            SelectedIndex = newIndex;
        }
        else if (SelectedIndex == newIndex)
        {
            SelectedIndex = oldIndex;
        }
    }

    protected override void HandleCollectionNewItemCreated(FrameworkElement newItem, int newItemIndex)
    {
        if (newItem is ListBoxItem listBoxItem)
        {
            ListBoxItemsInternal.Insert(newItemIndex, listBoxItem);
            listBoxItem.AssignListBoxEvents(
                HandleItemSelected,
                HandleItemFocused,
                HandleListBoxItemPushed,
                HandleListBoxItemClicked,
                HandleListBoxItemDragging);
            if(this.IsEnabled == false)
            {
                // so this appears disabled:
                newItem.UpdateState();
            }

            if (!_isAddingFromItemsCollection && !Items.Contains(listBoxItem))
            {
                Items.Insert(newItemIndex, listBoxItem);
            }
        }
    }


    /// <summary>
    /// Handles the removal of an item from the Items collection at the specified index by also
    /// removing the corresponding ListBoxItem from the internal ListBoxItemsInternal collection.
    /// </summary>
    /// <param name="indexToRemoveFrom">The index in the Items collection.</param>
    protected override void HandleCollectionItemRemoved(int indexToRemoveFrom)
    {
        // indexToRemoveFrom is the index of the index in the Items list. 
        // See the ListBoxItems for information on why this index many not
        // align with ListBoxItemsInternal.
        // A full solution should probably start from the top of the controls and count
        // through all of them to find the ListBoxItems, but we can quickly solve this bug
        // with a bounds check:
        // https://github.com/vchelaru/Gum/issues/1380#issuecomment-3736165558
        // This also is covered in a unit test:
        // Items_Clear_ShouldWorkWhenAddingButtonVisual
        if(indexToRemoveFrom < ListBoxItemsInternal.Count)
        {
            ListBoxItemsInternal.RemoveAt(indexToRemoveFrom);
        }
    }

    protected override void HandleCollectionReset()
    {
        ListBoxItemsInternal.Clear();
    }

    protected override void HandleCollectionReplace(int index)
    {
        ListBoxItemsInternal[index].UpdateToObject(Items[index]);
    }


    #endregion

    void OnItemFocused(object sender, EventArgs args)
    {
        for (int i = 0; i < ListBoxItemsInternal.Count; i++)
        {
            var listBoxItem = ListBoxItemsInternal[i];
            if (listBoxItem != sender && listBoxItem.IsFocused)
            {
                listBoxItem.IsFocused = false;
            }
        }
    }

    #region Scroll Item into view

    /// <summary>
    /// Scrolls the ListBox so that the argument item is in view. The amount of scrolling depends on the scrollIntoViewStyle argument.
    /// </summary>
    /// <param name="item">The item to scroll into view.</param>
    /// <param name="scrollIntoViewStyle">The desired location of the item after scrolling.</param>
    public void ScrollIntoView(object item, ScrollIntoViewStyle scrollIntoViewStyle = ScrollIntoViewStyle.BringIntoView)
    {
        var itemIndex = Items.IndexOf(item);

        ScrollIndexIntoView(itemIndex, scrollIntoViewStyle);
    }

    /// <summary>
    /// Scrolls the ListBox so that the item at the argument index is in view. The amount of scrolling depends on the scrollIntoViewStyle argument.
    /// </summary>
    /// <param name="itemIndex">The index of the item to scroll into view.</param>
    /// <param name="scrollIntoViewStyle">The desired location of the item after scrolling.</param>
    public void ScrollIndexIntoView(int itemIndex, ScrollIntoViewStyle scrollIntoViewStyle = ScrollIntoViewStyle.BringIntoView)
    {
        if (itemIndex != -1)
        {
            var visual = ListBoxItemsInternal[itemIndex];

            var visualAsIpso = (IPositionedSizedObject)visual.Visual;
            var visualTop = visualAsIpso.Y;
            var visualBottom = visualAsIpso.Y + visualAsIpso.Height;

            var viewTop = -InnerPanel.Y;
            var viewBottom = -InnerPanel.Y + clipContainer.GetAbsoluteHeight();
            var isAboveView = visualTop < viewTop;
            var isBelowView = visualBottom > viewBottom;

            float amountToScroll = 0;
            switch (scrollIntoViewStyle)
            {
                case ScrollIntoViewStyle.BringIntoView:
                    if (isAboveView)
                    {
                        amountToScroll = visualTop - viewTop;
                        if(verticalScrollBar != null)
                        {
                            verticalScrollBar.Value += amountToScroll;
                        }
                    }
                    else if (isBelowView)
                    {
                        amountToScroll = visualBottom - viewBottom;
                        if (verticalScrollBar != null)
                        {
                            verticalScrollBar.Value += amountToScroll;
                        }
                    }

                    break;
                case ScrollIntoViewStyle.Top:
                    amountToScroll = visualTop - viewTop;
                    if (verticalScrollBar != null)
                    {
                        verticalScrollBar.Value += amountToScroll;
                    }
                    break;
                case ScrollIntoViewStyle.Center:

                    var viewHeight = visualAsIpso.Height;

                    var desiredViewTop = viewHeight / 2.0f + visualTop - clipContainer.GetAbsoluteHeight() / 2;

                    amountToScroll = desiredViewTop - viewTop;
                    if (verticalScrollBar != null)
                    {
                        verticalScrollBar.Value += amountToScroll;
                    }
                    break;
                case ScrollIntoViewStyle.Bottom:
                    amountToScroll = visualBottom - viewBottom;
                    if (verticalScrollBar != null)
                    {
                        verticalScrollBar.Value += amountToScroll;
                    }
                    break;
            }
        }
    }

    #endregion

    public override void UpdateState()
    {
        var category = "ListBoxCategoryState";
        if (IsEnabled == false)
        {
            if (IsFocused)
            {
                Visual.SetProperty(category, DisabledFocusedStateName);
            }
            else
            {
                Visual.SetProperty(category, DisabledStateName);
            }
        }
        else if (IsFocused)
        {
            Visual.SetProperty(category, FocusedStateName);
        }
        else
        {
            Visual.SetProperty(category, EnabledStateName);
        }

        // The default state may update the visibility of the scroll bar. Whenever setting the state
        // we should forcefully apply the list box visibility:
        base.UpdateVerticalScrollBarValues();
    }

    /// <summary>
    /// Shows the argument popup using the argument listBoxParent to determine its destination layer.
    /// </summary>
    /// <param name="popup">The popup to show, for example a dropdown from a ComboBox or MenuItem</param>
    /// <param name="listBoxParent">The parent visual, which would be something like the ComboBox.Visual</param>
    /// <param name="forceAbsoluteSize">Whether to force the popup to have absolute WidthUnits and HeightUnits.</param>
    public static void ShowPopupListBox(ScrollViewer popup, GraphicalUiElement listBoxParent, bool forceAbsoluteSize = true)
    {
        popup.IsVisible = true;
        // this thing is going to be in front of everything:
        popup.Visual.Z = float.PositiveInfinity;



        // and apply the absolutes:
        popup.Visual.XUnits = GeneralUnitType.PixelsFromSmall;
        popup.Visual.YUnits = GeneralUnitType.PixelsFromSmall;
        if(forceAbsoluteSize)
        {
            popup.Visual.WidthUnits = DimensionUnitType.Absolute;
            popup.Visual.HeightUnits = DimensionUnitType.Absolute;
        }

        // let's just make sure it's removed
        // don't remove from managers. Doing so
        // resets the binding on the ListBox which 
        // we don't want!
        //popup.Visual.RemoveFromManagers();
        popup.Visual.Parent = null;
        var layerToAddListBoxTo = GetLayerToAddTo(listBoxParent);

#if FRB
        popup.Visual.AddToManagers(listBoxParent.EffectiveManagers,
            layerToAddListBoxTo);

        var rootParent = popup.Visual.GetParentRoot();

        var parent = listBoxParent.Parent as IWindow;
        var isDominant = false;
        while(parent != null)
        {
            if(GuiManager.DominantWindows.Contains(parent))
            {
                isDominant = true;
                break;
            }

            parent = parent.Parent;
        }

        if(isDominant)
        {
            GuiManager.AddDominantWindow(popup.Visual);
        }

#else
        popup.RepositionToKeepInScreen();

        if (listBoxParent.GetTopParent() == FrameworkElement.ModalRoot)
        {
            FrameworkElement.ModalRoot.Children.Add(popup.Visual);
        }
        else
        {
            FrameworkElement.PopupRoot.Children.Add(popup.Visual);
        }

#endif


    }

    private static Layer GetLayerToAddTo(GraphicalUiElement listBoxParent)
    {
        var managers = listBoxParent.Managers ?? SystemManagers.Default;

        var layerToAddListBoxTo = managers.Renderer.MainLayer;

        var mainRoot = listBoxParent.ElementGueContainingThis ?? listBoxParent;

        // We need to loop up the parents to see if there is a layer that contains this:
        var parent = listBoxParent;
        while(parent != null)
        {
            foreach (var layer in managers.Renderer.Layers)
            {
                if (layer != layerToAddListBoxTo)
                {
                    if (layer.Renderables.Contains(parent) || layer.Renderables.Contains(parent.RenderableComponent as IRenderableIpso))
                    {
                        layerToAddListBoxTo = layer;
                        break;
                    }
                }
            }

            parent = parent.Parent as GraphicalUiElement;
        }


        return layerToAddListBoxTo;
    }

    #region IInputReceiver Methods

    public override void OnFocusUpdate()
    {
        if (DoListItemsHaveFocus)
        {
            DoListItemFocusUpdate();
        }
        else
        {
            DoTopLevelFocusUpdate();
        }

#if (MONOGAME || KNI || FNA) && !FRB
        base.HandleKeyboardFocusUpdate();
#endif

        FocusUpdate?.Invoke(this);
    }

    private void DoListItemFocusUpdate()
    {
        // NOTE: Gamepad and keyboard input always use single-selection behavior,
        // regardless of the SelectionMode property. Multi-select is only supported
        // via mouse/pointer input with modifier keys in Extended mode or click toggles in Multiple mode.

        var xboxGamepads = FrameworkElement.GamePadsForUiControl;


        for (int i = 0; i < xboxGamepads.Count; i++)
        {
            var gamepad = xboxGamepads[i];

            RepositionDirections? direction = null;

            if (gamepad.ButtonRepeatRate(GamepadButton.DPadDown) ||
                gamepad.LeftStick.AsDPadPushedRepeatRate(DPadDirection.Down))
            {
                direction = RepositionDirections.Down;
            }
            
            if (gamepad.ButtonRepeatRate(GamepadButton.DPadRight) ||
                gamepad.LeftStick.AsDPadPushedRepeatRate(DPadDirection.Right))
            {
                direction = RepositionDirections.Right;
            }

            if (gamepad.ButtonRepeatRate(GamepadButton.DPadUp) ||
                gamepad.LeftStick.AsDPadPushedRepeatRate(DPadDirection.Up))
            {
                direction = RepositionDirections.Up;
            }

            if (gamepad.ButtonRepeatRate(GamepadButton.DPadLeft) ||
                gamepad.LeftStick.AsDPadPushedRepeatRate(DPadDirection.Left))
            {
                direction = RepositionDirections.Left;
            }


            var pressedButton = (LoseListItemFocusOnPrimaryInput && gamepad.ButtonPushed(GamepadButton.A)) ||
                gamepad.ButtonPushed(GamepadButton.B);

            DoListItemFocusUpdate(direction, pressedButton);

            void RaiseIfPushedAndEnabled(GamepadButton button)
            {
                if (IsEnabled && gamepad.ButtonPushed(button))
                {
                    ControllerButtonPushed?.Invoke(button);
                }
            }

            RaiseIfPushedAndEnabled(GamepadButton.A);
            RaiseIfPushedAndEnabled(GamepadButton.B);
            RaiseIfPushedAndEnabled(GamepadButton.X);
            RaiseIfPushedAndEnabled(GamepadButton.Y);
            RaiseIfPushedAndEnabled(GamepadButton.Start);
            RaiseIfPushedAndEnabled(GamepadButton.Back);
            RaiseIfPushedAndEnabled(GamepadButton.DPadLeft);
            RaiseIfPushedAndEnabled(GamepadButton.DPadRight);

#if FRB
            RaiseIfPushedAndEnabled(Buttons.LeftStickAsDPadLeft);
            RaiseIfPushedAndEnabled(Buttons.LeftStickAsDPadRight);
#endif
        }
#if FRB

        var genericGamePads = GuiManager.GenericGamePadsForUiControl;

        for (int i = 0; i < genericGamePads.Count; i++)
        {
            var gamepad = genericGamePads[i];

            RepositionDirections? direction = null;

            if(gamepad.DPadRepeatRate(Xbox360GamePad.DPadDirection.Down) ||
                (gamepad.AnalogSticks.Length > 0 && gamepad.AnalogSticks[0].AsDPadPushedRepeatRate(Xbox360GamePad.DPadDirection.Down)))
            {
                direction = RepositionDirections.Down;
            }

            if (gamepad.DPadRepeatRate(Xbox360GamePad.DPadDirection.Right) ||
                (gamepad.AnalogSticks.Length > 0 && gamepad.AnalogSticks[0].AsDPadPushedRepeatRate(Xbox360GamePad.DPadDirection.Right)))
            {
                direction = RepositionDirections.Right;
            }

            if(gamepad.DPadRepeatRate(Xbox360GamePad.DPadDirection.Up) ||
                (gamepad.AnalogSticks.Length > 0 && gamepad.AnalogSticks[0].AsDPadPushedRepeatRate(Xbox360GamePad.DPadDirection.Up)))
            {
                direction = RepositionDirections.Up;
            }
            
            if(gamepad.DPadRepeatRate(Xbox360GamePad.DPadDirection.Left) ||
                (gamepad.AnalogSticks.Length > 0 && gamepad.AnalogSticks[0].AsDPadPushedRepeatRate(Xbox360GamePad.DPadDirection.Left)))
            {
                direction = RepositionDirections.Left;
            }

            var inputDevice = gamepad as IInputDevice;

            var pressedButton = 
                (LoseListItemFocusOnPrimaryInput && inputDevice.DefaultPrimaryActionInput.WasJustPressed) || 
                inputDevice.DefaultBackInput.WasJustPressed;

            DoListItemFocusUpdate(direction, pressedButton);

        }
#endif

#if (MONOGAME || KNI || FNA) && !FRB

        foreach (var keyboard in KeyboardsForUiControl)
        {
            var pressedButton = false;
            foreach(var item in FrameworkElement.ClickCombos)
            {
                if(item.IsComboPushed())
                {
                    pressedButton = true;
                    break;
                }
            }

            RepositionDirections? direction = null;

            if (keyboard.KeyPushed(Keys.Up) == true || keyboard.KeysTyped.Contains(Keys.Up) == true)
            {
                direction = RepositionDirections.Up;
            }
            if (keyboard.KeyPushed(Keys.Down) == true || keyboard.KeysTyped.Contains(Keys.Down) == true)
            {
                direction = RepositionDirections.Down;
            }

            DoListItemFocusUpdate(direction, pressedButton);
        }

        base.HandleKeyboardFocusUpdate();
#endif



    }

    private int? GetListBoxIndexAt(float x, float y)
    {
        for (int i = 0; i < ListBoxItemsInternal.Count; i++)
        {
            ListBoxItem listBoxItem = ListBoxItemsInternal[i];
            var item = listBoxItem.Visual;
            if (item.Visible)
            {
                var widthHalf = item.GetAbsoluteWidth() / 2.0f;
                var heightHalf = item.GetAbsoluteHeight() / 2.0f;

                var absoluteX = item.GetAbsoluteCenterX();
                var absoluteY = item.GetAbsoluteCenterY();

                if (x > absoluteX - widthHalf && x < absoluteX + widthHalf &&
                    y > absoluteY - heightHalf && y < absoluteY + heightHalf)
                {
                    return i;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// The additional offset to check when attempting to find a control when performing DPad navigation. This value is added
    /// to the InnerPanel's StackSpacing. Increasing this value is useful if objects in the ListBox are not of uniform size, but
    /// if the value is too large then navigation may skip rows or columns.
    /// </summary>
    public float AdditionalOffsetToCheckForDPadNavigation { get; set; } = 4;

    private void DoListItemFocusUpdate(RepositionDirections? direction, 
        // Whether a button was pushed to consider this a selection.
        // Currently we don't differentiate between A and B on combo
        // box. Should we in the future?
        bool pressedButton)
    {
        var wraps = InnerPanel.WrapsChildren;
        var handledByWrapping = false;

        if (direction != null)
        {
            LoseHighlight();
        }

        if (wraps && direction != null)
        {

            if (SelectedIndex > -1 && SelectedIndex < ListBoxItems.Count)
            {
                var currentSelection = this.ListBoxItemsInternal[SelectedIndex].Visual;

                var offsetToCheck = InnerPanel.StackSpacing + AdditionalOffsetToCheckForDPadNavigation;

                float xCenter = currentSelection.GetAbsoluteX() + currentSelection.GetAbsoluteWidth() / 2.0f;
                float yCenter = currentSelection.GetAbsoluteY() + currentSelection.GetAbsoluteHeight() / 2.0f;

                float x = xCenter;
                float y = yCenter;
                switch (direction.Value)
                {
                    case RepositionDirections.Left:
                        x = currentSelection.GetAbsoluteLeft() - offsetToCheck;
                        break;

                    case RepositionDirections.Right:
                        x = currentSelection.GetAbsoluteRight() + offsetToCheck;
                        break;

                    case RepositionDirections.Up:
                        y = currentSelection.GetAbsoluteTop() - offsetToCheck;
                        break;

                    case RepositionDirections.Down:
                        y = currentSelection.GetAbsoluteBottom() + offsetToCheck;
                        break;
                }

                var index = GetListBoxIndexAt(x, y);

                if (index != null)
                {
                    SelectedIndex = index.Value;
                    this.ListBoxItemsInternal[SelectedIndex].IsFocused = true;
                }
            }
            else
            {
                SelectedIndex = 0;
                this.ListBoxItemsInternal[SelectedIndex].IsFocused = true;
            }

            // The fallback behavior is kinda confusing, so let's just handle it if it wraps.
            // This could change in the future?
            handledByWrapping = true;

        }


        if (!handledByWrapping)
        {
            if (direction == RepositionDirections.Down || direction == RepositionDirections.Right)
            {
                if (Items.Count > 0)
                {
                    if (SelectedIndex < 0 && Items.Count > 0)
                    {
                        SelectedIndex = 0;
                    }
                    else if (SelectedIndex < Items.Count - 1)
                    {
                        SelectedIndex++;
                    }
                    this.ListBoxItemsInternal[SelectedIndex].IsFocused = true;
                }
            }
            else if (direction == RepositionDirections.Up || direction == RepositionDirections.Left)
            {
                if (Items.Count > 0)
                {
                    if (SelectedIndex < 0 && Items.Count > 0)
                    {
                        SelectedIndex = 0;
                    }
                    else if (SelectedIndex > 0)
                    {
                        SelectedIndex--;
                    }

                    this.ListBoxItemsInternal[SelectedIndex].IsFocused = true;
                }
            }
        }

        if (pressedButton)
        {
            if(SelectedIndex != -1)
            {
                HandleListBoxItemPushed(this.ListBoxItemsInternal[SelectedIndex], EventArgs.Empty);
            }
            if (CanListItemsLoseFocus)
            {
                DoListItemsHaveFocus = false;
            }
        }
    }

    private void LoseHighlight()
    {
        for (int i = 0; i < this.ListBoxItems.Count; i++)
        {
            var item = this.ListBoxItems[i];
            item.IsHighlighted = false;
        }
    }

    private void DoTopLevelFocusUpdate()
    {
        var gamepads = FrameworkElement.GamePadsForUiControl;

        for (int i = 0; i < gamepads.Count; i++)
        {
            var gamepad = gamepads[i];

            HandleGamepadNavigation(gamepad);

            if (gamepad.ButtonPushed(GamepadButton.A))
            {
                DoListItemsHaveFocus = true;
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
            RaiseIfPushedAndEnabled(GamepadButton.DPadLeft);
            RaiseIfPushedAndEnabled(GamepadButton.DPadRight);

#if FRB
            RaiseIfPushedAndEnabled(Buttons.LeftStickAsDPadLeft);
            RaiseIfPushedAndEnabled(Buttons.LeftStickAsDPadRight);
#endif
        }
#if FRB

        var genericGamepads = GuiManager.GenericGamePadsForUiControl;

        for (int i = 0; i < genericGamepads.Count; i++)
        {
            var gamepad = genericGamepads[i];

            HandleGamepadNavigation(gamepad);

            if ((gamepad as IInputDevice).DefaultConfirmInput.WasJustPressed)
            {
                DoListItemsHaveFocus = true;
            }

            if(IsEnabled)
            {
                for(var buttonIndex = 0; buttonIndex < gamepad.NumberOfButtons; i++)
                {
                    if(gamepad.ButtonPushed(buttonIndex))
                    {
                        GenericGamepadButtonPushed?.Invoke(buttonIndex);
                    }
                }
            }
        }
#endif

#if (MONOGAME || KNI || FNA) && !FRB

        foreach (var keyboard in KeyboardsForUiControl)
        {
            foreach(var keyCombo in FrameworkElement.ClickCombos)
            {
                if(keyCombo.IsComboPushed())
                {
                    DoListItemsHaveFocus = true;
                    break;
                }
            }
        }
#endif
    }

    public override void OnGainFocus()
    {
        IsFocused = true;
    }

    [Obsolete("use OnLoseFocus instead")]
    public void LoseFocus() => OnLoseFocus();

    /// <summary>
    /// Removes focus from the ListBox, both at the top level and at the individual item level, even if CanListItemsLoseFocus is set to false.
    /// </summary>
    public override void OnLoseFocus()
    {
        IsFocused = false;

        if (DoListItemsHaveFocus)
        {
            DoListItemsHaveFocus = false;
        }
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

    public void DoKeyboardAction(IInputReceiverKeyboard keyboard)
    {
    }


#endregion
}
