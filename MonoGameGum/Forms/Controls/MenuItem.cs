﻿using System;
using System.Collections.Generic;
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
#else
namespace MonoGameGum.Forms.Controls;
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
                    Selected?.Invoke(this, null);
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

    GraphicalUiElement text;
    protected RenderingLibrary.Graphics.Text coreText;

    internal bool SelectOnHighlight { get; set; } = false;

    public string Header
    {
        get => coreText.RawText;
        set
        {
            if (value != coreText.RawText)
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

    internal MenuItem ParentMenuItem
    {
        get;set;
    }

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
        // optional
        text = Visual.GetGraphicalUiElementByName("TextInstance");
        coreText = text?.RenderableComponent as RenderingLibrary.Graphics.Text;

        // Just in case it needs to set the state to "enabled"
        UpdateState();


        base.ReactToVisualChanged();
    }

    #endregion

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

    internal void SetSelectOnHighlightRecursively(bool value)
    {
        SelectOnHighlight = value;
        foreach (var item in MenuItemsInternal)
        {
            item.SetSelectOnHighlightRecursively(value);
        }
    }

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

    #region Popup

    internal void TryShowPopup()
    {
        if (this.Items?.Count > 0 && itemsPopup == null)
        {
            timeOpened = MainCursor.LastPrimaryPushTime;
            itemsPopup = new ScrollViewer();

            //itemsPopup.InnerPanel.Height = 0;
            //itemsPopup.InnerPanel.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            //itemsPopup.InnerPanel.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

            foreach (var item in Items)
            {
                MenuItem menuItem;
                if (item is MenuItem asMenuitem)
                {
                    menuItem = asMenuitem;
                }
                else
                {
                    menuItem = new MenuItem();
                    menuItem.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
                    menuItem.UpdateToObject(item);
                    menuItem.BindingContext = item;
                }
                
                menuItem.SelectOnHighlight = this.SelectOnHighlight;

                MenuItemsInternal.Add(menuItem);
                menuItem.ParentMenuItem = this;
                menuItem.Selected += HandleSubItemSelected;

                itemsPopup.InnerPanel.Children.Add(menuItem.Visual);
            }

            itemsPopup.Visual.X = this.Visual.AbsoluteLeft;
            itemsPopup.Visual.Y = this.Visual.GetAbsoluteBottom();
            var parent = this.Visual.Parent;
            if (parent != null)
            {
                if (parent is GraphicalUiElement asGue && asGue.ChildrenLayout == Gum.Managers.ChildrenLayout.TopToBottomStack)
                {
                    itemsPopup.Visual.X = this.Visual.GetAbsoluteRight();
                    itemsPopup.Visual.Y = this.Visual.AbsoluteTop;
                }
            }

            itemsPopup.Width = 200;
            itemsPopup.Height = 400;


            ListBox.ShowPopupListBox(itemsPopup, this.Visual);

            itemsPopup.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            itemsPopup.Visual.Height = 0;
            itemsPopup.ClipContainer.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            itemsPopup.ClipContainer.Height = 0;

            itemsPopup.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            itemsPopup.Visual.Width = 0;
            itemsPopup.ClipContainer.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            itemsPopup.ClipContainer.Width = 0;
            itemsPopup.InnerPanel.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            itemsPopup.InnerPanel.Width = 0;

#if FRB
                GuiManager.SortZAndLayerBased();
                listBox.RepositionToKeepInScreen();
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

    private void HandleClick(object sender, EventArgs args)
    {
        if (MainCursor.LastInputDevice == InputDevice.TouchScreen &&
            MainCursor.PrimaryClickNoSlide)
        {
            IsSelected = true;

            Clicked?.Invoke(this, null);

        }
    }


    public virtual void UpdateToObject(object o)
    {
        if (coreText != null)
        {
            coreText.RawText = o?.ToString();
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

        //if(IsEnabled == false)
        //{
        //    // todo?
        //}

        if (IsFocused)
        {
            Visual.SetProperty(category, "Focused");
        }
        else if (IsSelected)
        {
            Visual.SetProperty(category, "Selected");
        }
        else if (IsHighlighted)
        {
            // If the cursor has moved, highlight. This prevents highlighting from
            // happening when the cursor is not moving, and the user is moving the focus
            // with the gamepad. 
            // Vic says - I'm not sure if this is the solution that I like, but let's start with it...
            if (cursor.XChange != 0 || cursor.YChange != 0 || force)
            {
                Visual.SetProperty(category, "Highlighted");
            }
            // otherwise - do nothing?
        }
        else
        {
            Visual.SetProperty(category, "Enabled");
        }
    }

    public override string ToString()
    {
        return Header ?? base.ToString();
    }
}