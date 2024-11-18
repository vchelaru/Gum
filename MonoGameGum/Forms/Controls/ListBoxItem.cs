using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.Controls;

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
        Visual.Push += this.HandlePush;
        Visual.Click += this.HandleClick;
        Visual.RollOn += this.HandleRollOn;
        Visual.RollOff += this.HandleRollOff;

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
        UpdateState();
    }

    private void HandleRollOff(object sender, EventArgs args)
    {
        UpdateState();
    }

    private void HandlePush(object sender, EventArgs args)
    {
        var isMouse = true;
        if (isMouse)
        {
            IsSelected = true;

            Clicked?.Invoke(this, null);
        }
    }

    private void HandleClick(object sender, EventArgs args)
    {
        var isTouchScreen = false;
        if (isTouchScreen &&
            // FRB uses "no slide" here for touch screens
            // We don't have that yet.
            MainCursor.PrimaryClick)
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
        var isTouchScreen = false;
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
        else if (!isTouchScreen && GetIfIsOnThisOrChildVisual(cursor) && IsEnabled)
        {
            Visual.SetProperty(category, "Highlighted");
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
