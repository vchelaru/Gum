﻿using Gum.Wireframe;
using MonoGameGum.Input;
using System;

#if FRB
using FlatRedBall.Gui;
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;
#else
namespace MonoGameGum.Forms.Controls;
#endif

public class ListBoxItem : FrameworkElement
{
    #region Fields/Properties

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
                UpdateState();
            }
        }
    }

    GraphicalUiElement text;
    protected RenderingLibrary.Graphics.Text coreText;

    internal bool IsHighlightSuppressed { get; set; } = false;

    bool isHighlighted;
    public bool IsHighlighted
    {
        get => isHighlighted;
        set
        {
            if (isHighlighted != value)
            {
                isHighlighted = value;
                UpdateState();
            }
        }
    }

    #endregion

    #region Events

    public event EventHandler Selected;
    public event EventHandler Clicked;

    #endregion

    #region Initialize

    public ListBoxItem() : base() { }

    public ListBoxItem(InteractiveGue visual) : base(visual) { }

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

    #region Update To

    public virtual void UpdateToObject(object o)
    {
        if (coreText != null)
        {
            coreText.RawText = o?.ToString();
        }
    }

    public override void UpdateState()
    {
        var cursor = MainCursor;

        const string category = "ListBoxItemCategoryState";

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
            if (cursor.XChange != 0 || cursor.YChange != 0)
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

    #endregion

    #region Utilities

    public override string ToString()
    {
        return coreText.RawText;
    }

    #endregion


}
