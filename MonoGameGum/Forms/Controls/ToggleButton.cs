using Gum.Wireframe;
using System;

#if FRB
using FlatRedBall.Forms.Controls.Primitives;
using FlatRedBall.Gui;
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;
#endif

#if !FRB
using Gum.Forms.Controls.Primitives;
namespace Gum.Forms.Controls;
#endif

/// <summary>
/// A button that maintains a checked/unchecked state, toggling each time it is clicked.
/// Base class for <see cref="CheckBox"/> and <see cref="RadioButton"/>.
/// </summary>
/// <remarks>
/// The visual state is driven by the "ToggleCategoryState" property, which combines
/// the interaction state (Enabled, Highlighted, Pushed, Disabled) with "On" or "Off" suffixes
/// (e.g. "EnabledOn", "HighlightedOff").
/// </remarks>
public class ToggleButton : ButtonBase
{
    #region Fields/Properties

    /// <summary>
    /// Whether this toggle supports three states: checked (true), unchecked (false), and
    /// indeterminate (null). When true, clicking the button cycles through Unchecked -> Checked -> Indeterminate.
    /// When false, clicking cycles only between Checked and Unchecked.
    /// </summary>
    public bool IsThreeState { get; set; }

    private bool? isChecked = false;

    private GraphicalUiElement? textComponent;

    private global::RenderingLibrary.Graphics.IText? coreTextObject;

    /// <summary>
    /// Gets or sets the toggle button label text. Setting this property applies localization
    /// if a <see cref="Gum.Localization.LocalizationService"/> is registered.
    /// To bypass localization, use <see cref="SetTextNoTranslate"/>.
    /// </summary>
    /// <remarks>
    /// The text is resolved through a visual child named "TextInstance"; if the backing
    /// visual lacks that child, get returns null and set is a no-op (a diagnostic is raised
    /// when FULL_DIAGNOSTICS is defined).
    /// </remarks>
    public string? Text
    {
        get
        {
#if FULL_DIAGNOSTICS
            ReportMissingTextInstance();
#endif
            return coreTextObject?.RawText;
        }
        set
        {
#if FULL_DIAGNOSTICS
            ReportMissingTextInstance();
#endif
            // go through the component instead of the core text object to force a layout refresh if necessary
            textComponent?.SetProperty("Text", value);
        }
    }

    /// <summary>
    /// Sets the toggle button text without applying localization/translation.
    /// </summary>
    /// <remarks>
    /// This is a method rather than a property because the "no translate" state is not preserved on
    /// the underlying text renderable — only the final string is stored.
    /// Use this for text that should not be localized.
    /// </remarks>
    public void SetTextNoTranslate(string? value)
    {
        textComponent?.SetProperty("TextNoTranslate", value);
    }


    /// <summary>
    /// Gets or sets the checked state of the toggle. A value of <c>true</c> means checked,
    /// <c>false</c> means unchecked, and <c>null</c> means indeterminate.
    /// </summary>
    /// <remarks>
    /// Setting this property updates the visual state and raises <see cref="Checked"/>,
    /// <see cref="Unchecked"/>, or <see cref="Indeterminate"/> as appropriate.
    /// If <see cref="IsThreeState"/> is true, clicking the button cycles through all three states.
    /// </remarks>
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
                    Checked?.Invoke(this, null!);
                }
                else if (isChecked == false)
                {
                    Unchecked?.Invoke(this, null!);
                }
                else if (isChecked == null)
                {
                    Indeterminate?.Invoke(this, null!);
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
        RefreshInternalVisualReferences();
        base.ReactToVisualChanged();

        // This forces the initial state to be correct, making sure the button is unchecked
        UpdateState();
    }

    protected override void RefreshInternalVisualReferences()
    {
        // text component is optional — visuals without a "TextInstance" child are
        // still valid (icon-only toggles, for example).
        textComponent = base.Visual?.GetGraphicalUiElementByName("TextInstance");
        coreTextObject = textComponent?.RenderableComponent as global::RenderingLibrary.Graphics.IText;
    }

#if FULL_DIAGNOSTICS
    private void ReportMissingTextInstance()
    {
        if (textComponent == null)
        {
            throw new Exception(
                "This toggle button was created with a Gum component that does not have an instance called 'TextInstance'. A 'TextInstance' instance must be added to modify the Text property.");
        }
    }
#endif

    #endregion

    #region Update To Methods

    /// <inheritdoc/>
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

    /// <summary>
    /// Called when <see cref="IsChecked"/> is set to true. Override in derived classes
    /// to add custom checked behavior (e.g. <see cref="RadioButton"/> uses this to uncheck siblings).
    /// </summary>
    protected virtual void OnChecked()
    {

    }

    protected override void OnClick()
    {
        if (IsThreeState)
        {
            if (IsChecked == false) IsChecked = true;
            else if (IsChecked == true) IsChecked = null;
            else IsChecked = false; // Indeterminate -> Unchecked
        }
        else
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
}
