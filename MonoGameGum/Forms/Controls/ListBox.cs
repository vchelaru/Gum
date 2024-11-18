using Gum.Wireframe;
using Microsoft.Xna.Framework.Input;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.Controls;

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

public class ListBox : ItemsControl
{
    #region Fields/Properties

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

    int selectedIndex = -1;

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
            if (selectedIndex > -1 && selectedIndex < Items.Count)
            {
                return Items[selectedIndex];
            }
            else
            {
                return null;
            }
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
                if (ListBoxItemsInternal[value].IsEnabled)
                {
                    ListBoxItemsInternal[value].IsSelected = true;
                }

                if (SelectedObject != null)
                {
                    ScrollIntoView(SelectedObject);
                }
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

    //public IInputReceiver NextInTabSequence { get; set; }

    /// <summary>
    /// Whether the primary input button (usually the A button) results in the highlighted list box item
    /// being selected and in the ListBox focus moving outside of the individual items.
    /// </summary>
    /// <remarks>
    /// This value is true, but can be changed to false if the A button should perform actions on the highlighted
    /// list box item (such as toggling a check box) without focus being moved out of the individual items.
    /// </remarks>
    public bool LoseListItemFocusOnPrimaryInput { get; set; } = true;

    #endregion

    #region Events

    /// <summary>
    /// Event raised whenever the selection changes. The object parameter is the sender (list box) and the SelectionChangedeventArgs
    /// contains information about the changed selected items.
    /// </summary>
    public event Action<object, SelectionChangedEventArgs> SelectionChanged;
    //public event FocusUpdateDelegate FocusUpdate;

    /// <summary>
    /// Event raised when the user presses a button at the top-level (if the list box has focus, but the individual items do not)
    /// </summary>
    //public event Action<Xbox360GamePad.Button> ControllerButtonPushed;
    public event Action<int> GenericGamepadButtonPushed;

    #endregion

    #region Initialize Methods

    public ListBox() : base()
    {
    }

    public ListBox(InteractiveGue visual) : base(visual)
    {
    }

    protected override void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        base.HandleCollectionChanged(sender, e);

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

    #endregion

    protected override void OnItemSelected(object sender, SelectionChangedEventArgs args)
    {
        base.OnItemSelected(sender, args);

        selectedIndex = ListBoxItemsInternal.IndexOf(sender as ListBoxItem);
        if (selectedIndex > -1)
        {
            args.AddedItems.Add(Items[selectedIndex]);
        }

        SelectionChanged?.Invoke(this, args);

        PushValueToViewModel(nameof(SelectedObject));
        PushValueToViewModel(nameof(SelectedIndex));
    }

    /// <summary>
    /// Scrolls the list view so that the argument item is in view. The amount of scrolling depends on the scrollIntoViewStyle argument.
    /// </summary>
    /// <param name="item">The item to scroll into view.</param>
    /// <param name="scrollIntoViewStyle">The desired location of the item after scrolling.</param>
    public void ScrollIntoView(object item, ScrollIntoViewStyle scrollIntoViewStyle = ScrollIntoViewStyle.BringIntoView)
    {
        var itemIndex = Items.IndexOf(item);

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

    public override void UpdateState()
    {
        var category = "ListBoxCategoryState";
        if (IsEnabled == false)
        {
            if (IsFocused)
            {
                Visual.SetProperty(category, "DisabledFocused");
            }
            else
            {
                Visual.SetProperty(category, "Disabled");
            }
        }
        else if (IsFocused)
        {
            Visual.SetProperty(category, "Focused");
        }
        else
        {
            Visual.SetProperty(category, "Enabled");
        }
    }

    // When we add keyboard and gamepad support, go back to FRB and add the IInputReceiver methods
}
