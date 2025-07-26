using Gum.Wireframe;
using System;

#if FRB
using FlatRedBall.Forms.Controls.Primitives;
using FlatRedBall.Gui;
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;
#elif RAYLIB
using Gum.Forms.Controls.Primitives;

#else
using MonoGameGum.Forms.Controls.Primitives;
namespace MonoGameGum.Forms.Controls;
#endif

public class ToggleButton : ButtonBase
{
    #region Fields/Properties

    public bool IsThreeState { get; set; }

    private bool? isChecked = false;

    public bool? IsChecked
    {
        get
        {
            return isChecked;
        }
        set
        {
            if (isChecked != value)
            {
                isChecked = value;
                UpdateState();

                if (isChecked == true)
                {
                    OnChecked();
                    Checked?.Invoke(this, null);
                }
                else if (isChecked == false)
                {
                    Unchecked?.Invoke(this, null);
                }
                else if (isChecked == null)
                {
                    Indeterminate?.Invoke(this, null);
                }

                PushValueToViewModel();
            }
        }
    }

    #endregion

    #region Events
    /// <summary>
    /// Event raised when the IsChecked value is set to true. Seperate events exist for Indeterminate and Unchecked.
    /// </summary>
    /// <remarks>
    /// The Checked/Indeterminate/Unchecked event pattern follows wpf. For more info, see:
    /// https://stackoverflow.com/questions/5574613/separate-events-for-checked-and-unchecked-state-of-wpf-checkbox-why
    /// </remarks>
    public event EventHandler Checked;

    /// <summary>
    /// Event raised when the IsChecked value is set to null.
    /// </summary>
    public event EventHandler Indeterminate;

    /// <summary>
    /// Event raised when the IsChecked value is set to false;
    /// </summary>
    public event EventHandler Unchecked;

    #endregion

    #region Initialize

    public ToggleButton() : base()
    {
        IsChecked = false;
    }

    public ToggleButton(InteractiveGue visual) : base(visual)
    {
        IsChecked = false;
    }

    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();

        // This forces the initial state to be correct, making sure the button is unchecked
        UpdateState();
    }

    #endregion

    #region Update To Methods

    public override void UpdateState()
    {
        var cursor = MainCursor;

        if (IsEnabled == false)
        {
            SetPropertyConsideringOn(DisabledStateName);
        }
        //else if (HasFocus)
        //{
        //}
        else if (GetIfIsOnThisOrChildVisual(cursor))
        {
            if (cursor.WindowPushed == Visual && cursor.PrimaryDown)
            {
                SetPropertyConsideringOn(PushedStateName);
            }
            else if (cursor.LastInputDevice != InputDevice.TouchScreen)
            {
                SetPropertyConsideringOn(HighlightedStateName);
            }
            else
            {
                SetPropertyConsideringOn(EnabledStateName);
            }
        }
        else
        {
            SetPropertyConsideringOn(EnabledStateName);
        }
    }

    private void SetPropertyConsideringOn(string stateName)
    {
        if (isChecked == true)
        {
            stateName += "On";
        }
        else
        {
            stateName += "Off";
        }
        Visual.SetProperty("ToggleCategoryState", stateName);

    }

    #endregion

    protected virtual void OnChecked()
    {

    }

    protected override void OnClick()
    {
        if (IsChecked == true)
        {
            IsChecked = false;
        }
        else // false or indeterminte
        {
            IsChecked = true;
        }
    }
}
