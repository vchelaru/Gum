using Gum.Wireframe;
using MonoGameGum.Forms.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.Controls;

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

                //PushValueToViewModel();
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
        var cursor = FrameworkElement.MainCursor;

        var isTouchScreen = false;

        if (IsEnabled == false)
        {
            SetPropertyConsideringOn("Disabled");
        }
        //else if (HasFocus)
        //{
        //}
        else if (GetIfIsOnThisOrChildVisual(cursor))
        {
            if (cursor.WindowPushed == Visual && cursor.PrimaryDown)
            {
                SetPropertyConsideringOn("Pushed");
            }
            else if (!isTouchScreen)
            {
                SetPropertyConsideringOn("Highlighted");
            }
            else
            {
                SetPropertyConsideringOn("Enabled");
            }
        }
        else
        {
            SetPropertyConsideringOn("Enabled");
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
