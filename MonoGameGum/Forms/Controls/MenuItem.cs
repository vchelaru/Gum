using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.Wireframe;
using MonoGameGum;
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

    protected List<MenuItem> MenuItemsInternal = new List<MenuItem>();

    GraphicalUiElement? text;
    protected global::RenderingLibrary.Graphics.Text? coreText;

    GraphicalUiElement? SubmenuIndicatorInstance;

    internal bool SelectOnHighlight { get; set; } = false;

    public virtual string? Header
    {
        get
        {
#if FULL_DIAGNOSTICS
            ReportMissingTextInstance();
#endif
            return coreText!.RawText;
        }
        set
        {
#if FULL_DIAGNOSTICS
            ReportMissingTextInstance();
#endif
            if (value != coreText!.RawText)
            {
                coreText.RawText = value;
            }
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

    MenuItem _parentMenuItem;
    internal MenuItem ParentMenuItem
    {
        get => _parentMenuItem;
        set
        {
            _parentMenuItem = value;
            RefreshSubmenuIndicatorVisibility();
        }
    }

    public VisualTemplate? ScrollViewerVisualTemplate { get; set; } = null;

    InteractiveGue lastVisual;

    #endregion

    #region Events

    public event EventHandler Selected;
    public event EventHandler Clicked;

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

    private void UpdateMenuItems()
    {
        var containerInstance = Visual.GetGraphicalUiElementByName(
            "SubItemContainerInstance");

        if (containerInstance != null)
        {
            foreach (var child in containerInstance.Children)
            {
                if (child is InteractiveGue interactiveGue && interactiveGue.FormsControlAsObject is MenuItem menuItem)
                {
                    child.Parent = null;
                    Items.Add(menuItem);
                }
            }

            containerInstance.Children.CollectionChanged += HandleSubItemContainerChanged;

            containerInstance.Parent = null;
        }
    }

    private void HandleSubItemContainerChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        List<MenuItem> items = null;
        if(e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
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
            global::MonoGameGum.GumService.Default.DeferredQueue.Enqueue(() =>
#endif
            {
                foreach (var item in items)
                {
                    // This item could have been added through a state. 
                    // If that's the case, layout will be suspended. Let's
                    // force resume it now:
                    item.Visual.ResumeLayout(recursive:true);
                    this.Items.Add(item);
                }
            });
        }
    }

    protected virtual void RefreshInternalVisualReferences()
    {
        if (lastVisual != null && lastVisual != Visual)
        {
            lastVisual.Children.CollectionChanged -= HandleVisualChildrenChanged;
        }

        // optional
        text = Visual.GetGraphicalUiElementByName("TextInstance");
        coreText = text?.RenderableComponent as global::RenderingLibrary.Graphics.Text;
        SubmenuIndicatorInstance = Visual.GetGraphicalUiElementByName("SubmenuIndicatorInstance");

        Visual.Children.CollectionChanged += HandleVisualChildrenChanged;

        lastVisual = Visual;

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
    private void HandleRollOn(object sender, EventArgs args)
    {
        var cursor = MainCursor;

        if (cursor.XChange != 0 || cursor.YChange != 0)
        {
            UpdateIsHighlightedFromCursor(cursor);
        }

        UpdateState();
    }


    private void HandleRollOver(object sender, EventArgs args)
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

    private void HandleRollOff(object sender, EventArgs args)
    {
        IsHighlighted = false;

        UpdateState();
    }

    private void HandlePush(object sender, EventArgs args)
    {
        if (MainCursor.LastInputDevice == InputDevice.Mouse)
        {
            IsSelected = true;
            Clicked?.Invoke(this, null);
            
        }
    }


    private void HandleClick(object sender, EventArgs args)
    {
        if (MainCursor.LastInputDevice == InputDevice.TouchScreen &&
            MainCursor.PrimaryClickNoSlide)
        {
            IsSelected = true;

            Clicked?.Invoke(this, null);

        }
    }
    #endregion

    protected override void HandleItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        base.HandleItemsCollectionChanged(sender, e);

        RefreshSubmenuIndicatorVisibility();
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
    public bool IsRecursiveMenuItem(GraphicalUiElement itemVisual)
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

    internal void TryShowPopup()
    {
        if (this.Items?.Count > 0 && itemsPopup == null)
        {
            timeOpened = MainCursor.LastPrimaryPushTime;

            var visualTemplateVisual = ScrollViewerVisualTemplate?.CreateContent(null) as InteractiveGue;

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

            foreach (var item in Items)
            {
                MenuItem menuItem;
                FrameworkElement frameworkElementItem;

                if (item is FrameworkElement asFrameworkElement)
                {
                    frameworkElementItem = asFrameworkElement;
                    menuItem = frameworkElementItem as MenuItem;
                }
                else
                {
                    menuItem = new MenuItem();
                    frameworkElementItem = menuItem;
                    menuItem.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
                    menuItem.UpdateToObject(item);
                    menuItem.BindingContext = item;
                }
                
                if(menuItem != null)
                {
                    menuItem.SelectOnHighlight = this.SelectOnHighlight;
                    MenuItemsInternal.Add(menuItem);
                    menuItem.ParentMenuItem = this;
                    menuItem.Selected += HandleSubItemSelected;
                }


                itemsPopup.InnerPanel.Children.Add(frameworkElementItem.Visual);
            }

            itemsPopup.Visual.X = this.Visual.AbsoluteLeft;
            itemsPopup.Visual.Y = this.Visual.GetAbsoluteBottom();
            var parent = this.Visual.Parent;
            if (parent != null)
            {
                if (parent is GraphicalUiElement asGue && asGue.ChildrenLayout == global::Gum.Managers.ChildrenLayout.TopToBottomStack)
                {
                    itemsPopup.Visual.X = this.Visual.GetAbsoluteRight();
                    itemsPopup.Visual.Y = this.Visual.AbsoluteTop;
                }
            }


            ListBox.ShowPopupListBox(itemsPopup, this.Visual, forceAbsoluteSize:false);
            if (visualTemplateVisual == null)
            {
                // let's suppress layout for performance reasons, and to prevent issues with scrollbars:
                var wasSuppressed = GraphicalUiElement.IsAllLayoutSuspended;
                GraphicalUiElement.IsAllLayoutSuspended = true;

                //var oldHorizontalScrollBarVisibility = itemsPopup.HorizontalScrollBarVisibility;
                //var oldVerticalScrollBarVisibility = itemsPopup.VerticalScrollBarVisibility;

                itemsPopup.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
                itemsPopup.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;

                itemsPopup.VerticalSizeMode = SizeMode.Auto;
                itemsPopup.HorizontalSizeMode = SizeMode.Auto;

                // We can't make assumptions about the structure of the popup, so we have to just recursively
                // climb up from the inner panel and size it according to its children.
                var parentForSizing = itemsPopup.InnerPanel;
                while(parentForSizing != null)
                {
                    parentForSizing.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
                    parentForSizing.Width = 0;
                    parentForSizing.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
                    parentForSizing.Height = 0;

                    if (parentForSizing == itemsPopup.Visual)
                    {
                        break;
                    }
                    parentForSizing = parentForSizing.Parent;
                }

                GraphicalUiElement.IsAllLayoutSuspended = wasSuppressed;
                if (!wasSuppressed)
                {
                    itemsPopup.Visual.UpdateLayout();
                }
            }

#if FRB
            GuiManager.SortZAndLayerBased();
            itemsPopup.RepositionToKeepInScreen();
#endif
        }
    }

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

            }
            foreach(var item in this.MenuItemsInternal)
            {
                item.IsSelected = false;
            }
        }
    }

    public void HideChildrenPopupsRecursively()
    {
        foreach (var item in MenuItemsInternal)
        {
            item.HidePopupRecursively();
        }
    }

    ScrollViewer itemsPopup;

    public bool IsPopupVisible => itemsPopup?.IsVisible == true;

    #endregion

    #region Update to state/object

    public virtual void UpdateToObject(object o)
    {
        if (coreText != null)
        {
            coreText.RawText = o?.ToString() ?? string.Empty;
        }
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
