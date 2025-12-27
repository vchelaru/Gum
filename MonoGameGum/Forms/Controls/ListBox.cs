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


#region ScrollIntoViewStyle Enums
public enum ScrollIntoViewStyle
{
    /// <summary>
    /// Scrolls only if the item is not in view. Scrolls the minimum amount necessary to bring the item into view.
    /// In other words, if the item is above the visible area, then the scrolling brings the item to the top.
    /// If the item is below the visible area, then the scrolling brings the item to the bottom.
    /// If the item is already into view, no scrolling is performed.
    /// </summary>
    BringIntoView,

    Top,
    Center,
    Bottom
}
#endregion


public enum DragDropReorderMode
{
    NoReorder,
    Immediate
}

#if !FRB

#region RepositionDirections Enum

public enum RepositionDirections
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

public class ListBox : ItemsControl, IInputReceiver
{
    #region Fields/Properties

    protected List<ListBoxItem> ListBoxItemsInternal = new List<ListBoxItem>();

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

    public Type ListBoxItemGumType
    {
        get => base.ItemGumType;
        set => base.ItemGumType = value;
    }

    public Type ListBoxItemFormsType
    {
        get { return base.ItemFormsType; }
        set { base.ItemFormsType = value; }
    }

    public object SelectedObject
    {
        get
        {
            if (selectedIndex > -1)
            {
                if(Items.Count == 0 && SelectedIndex < ListBoxItems.Count)
                {
                    // This could be a ListBox with only 
                    // backing visuals and not Items
                    return ListBoxItems[selectedIndex];
                }
                else if(selectedIndex < Items.Count)
                {
                    return Items[selectedIndex];
                }
            }

            return null;
        }
        set
        {
            var index = Items?.IndexOf(value) ?? -1;

            SelectedIndex = index;

            PushValueToViewModel();
        }
    }

    public int SelectedIndex
    {
        get
        {
            return selectedIndex;
        }
        set
        {
            if (value > -1 && value < ListBoxItemsInternal.Count)
            {
                selectedIndex = value;

                var selectionChangedArgs = new SelectionChangedEventArgs();

                var item = ListBoxItemsInternal[value];
                if (item.IsEnabled)
                {
                    for (int i = 0; i < ListBoxItemsInternal.Count; i++)
                    {
                        var listBoxItem = ListBoxItemsInternal[i];

                        if (listBoxItem.IsSelected && listBoxItem != item)
                        {
                            selectionChangedArgs.RemovedItems.Add(ListBoxItemsInternal[i]);
                            listBoxItem.IsSelected = false;
                        }
                    }

                    if (item.IsSelected == false)
                    {
                        selectionChangedArgs.AddedItems.Add(item);
                        item.IsSelected = true;
                    }
                    item.IsSelected = true;
                }

                SelectionChanged?.Invoke(this, selectionChangedArgs);


                ScrollIndexIntoView(SelectedIndex);
            }
            else if (value == -1)
            {
                // do we just set it to the value before doing any logic?
                selectedIndex = -1;

                var selectionChangedArgs = new SelectionChangedEventArgs();

                for (int i = 0; i < ListBoxItemsInternal.Count; i++)
                {
                    var listBoxItem = ListBoxItemsInternal[i];

                    if (listBoxItem.IsSelected)
                    {
                        selectionChangedArgs.RemovedItems.Add(ListBoxItemsInternal[i]);
                        listBoxItem.IsSelected = false;
                    }
                }


                SelectionChanged?.Invoke(this, selectionChangedArgs);

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
    }

    public ListBox(InteractiveGue visual) : base(visual)
    {
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
        var args = new SelectionChangedEventArgs();

        for (int i = 0; i < ListBoxItemsInternal.Count; i++)
        {
            var listBoxItemAtI = ListBoxItemsInternal[i];
            if (listBoxItemAtI != sender && listBoxItemAtI.IsSelected)
            {
                var deselectedItem = listBoxItemAtI.BindingContext ?? listBoxItemAtI;
                args.RemovedItems.Add(deselectedItem);
                listBoxItemAtI.IsSelected = false;
            }
        }

        var listBoxItem = sender as ListBoxItem;
        selectedIndex = ListBoxItemsInternal.IndexOf(listBoxItem);

        // Items.Count could be smaller than ListBoxItemsInternal if the ListBoxItems
        // were added directl on the visual
        if (selectedIndex > -1 && selectedIndex < Items.Count)
        {
            args.AddedItems.Add(Items[selectedIndex]);
        }
        else if(listBoxItem != null)
        {
            args.AddedItems.Add(listBoxItem);
        }

        SelectionChanged?.Invoke(this, args);

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

    #region Collection Changed

    bool _suppressCollectionChangedToBase = false;
    protected override void HandleItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if(_suppressCollectionChangedToBase == false)
        {
            base.HandleItemsCollectionChanged(sender, e);
        }

        if (e.Action == NotifyCollectionChangedAction.Remove &&
            (e.OldStartingIndex == selectedIndex ||
                selectedIndex >= Items.Count))
        {
            // we removed the selected item, so update the VM:

            PushValueToViewModel(nameof(SelectedObject));
            PushValueToViewModel(nameof(SelectedIndex));
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset && selectedIndex >= 0)
        {
            SelectedIndex = -1;
            PushValueToViewModel(nameof(SelectedObject));
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
        }
    }



    protected override void HandleCollectionItemRemoved(int indexToRemoveFrom)
    {
        ListBoxItemsInternal.RemoveAt(indexToRemoveFrom);
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
    /// Scrolls the list view so that the argument item is in view. The amount of scrolling depends on the scrollIntoViewStyle argument.
    /// </summary>
    /// <param name="item">The item to scroll into view.</param>
    /// <param name="scrollIntoViewStyle">The desired location of the item after scrolling.</param>
    public void ScrollIntoView(object item, ScrollIntoViewStyle scrollIntoViewStyle = ScrollIntoViewStyle.BringIntoView)
    {
        var itemIndex = Items.IndexOf(item);

        ScrollIndexIntoView(itemIndex, scrollIntoViewStyle);
    }

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
                        verticalScrollBar.Value += amountToScroll;
                    }
                    else if (isBelowView)
                    {
                        amountToScroll = visualBottom - viewBottom;
                        verticalScrollBar.Value += amountToScroll;
                    }

                    break;
                case ScrollIntoViewStyle.Top:
                    amountToScroll = visualTop - viewTop;
                    verticalScrollBar.Value += amountToScroll;
                    break;
                case ScrollIntoViewStyle.Center:

                    var viewHeight = visualAsIpso.Height;

                    var desiredViewTop = viewHeight / 2.0f + visualTop - clipContainer.GetAbsoluteHeight() / 2;

                    amountToScroll = desiredViewTop - viewTop;
                    verticalScrollBar.Value += amountToScroll;

                    break;
                case ScrollIntoViewStyle.Bottom:
                    amountToScroll = visualBottom - viewBottom;
                    verticalScrollBar.Value += amountToScroll;
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
        popup.Visual.RemoveFromManagers();

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
