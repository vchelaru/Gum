using Gum.Wireframe;
using System;

#if FRB
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls.Primitives;
#endif

#if !FRB
namespace Gum.Forms.Controls.Primitives;
#endif

/// <summary>
/// Base class for controls which display or allow a user to change a value between a Minimum and Maximum.
/// It holds only the value; it needs no particular parts on its Visual. Controls with a draggable thumb
/// (ScrollBar and Slider) derive from <see cref="ThumbRangeBase"/>.
/// </summary>
public abstract class RangeBase :
#if FRB
    FrameworkElement
#else
    Gum.Forms.Controls.FrameworkElement
#endif
{
    #region Fields/Properties

    double minimum = 0;
    /// <summary>
    /// The minimum inclusive value which can be set through the UI.
    /// </summary>
    public double Minimum
    {
        get => minimum;
        set
        {
            var oldValue = minimum;
            minimum = value;

            OnMinimumChanged(oldValue, minimum);
            // After OnMinimumChanged, which may move Value (it reports itself): Minimum is final here.
            if (oldValue != minimum)
            {
                ReportFormsPropertyChanged();
            }
        }
    }

    double maximum = 1;
    /// <summary>
    /// The maximum inclusive value which can be set through the UI.
    /// </summary>
    public double Maximum
    {
        get => maximum;
        set
        {
            var oldValue = maximum;
            if(value != oldValue)
            {
                maximum = value;

                OnMaximumChanged(oldValue, maximum);
                ReportFormsPropertyChanged();
            }
        }
    }

    double value;

    /// <summary>
    /// The current value of the RangeBase. This value is clamped to be between Minimum and Maximum.
    /// </summary>
    public double Value
    {
        get => value;
        set
        {
#if FULL_DIAGNOSTICS
            if (double.IsNaN(value))
            {
                throw new InvalidOperationException("Can't set the ScrollBar Value to NaN");
            }
#endif
            var oldValue = this.value;
            var newValue = value;


            // Cap the values first so the comparison is done against
            // the capped value
            newValue = System.Math.Min(newValue, Maximum);
            newValue = System.Math.Max(newValue, Minimum);

            if (oldValue != newValue)
            {
                this.value = newValue;

                OnValueChanged(oldValue, this.value);
                ReportFormsPropertyChanged();

                ValueChanged?.Invoke(this, EventArgs.Empty);

                OnValueChangeRaised();

                PushValueToViewModel();
            }
        }
    }

    Orientation _orientation = Orientation.Horizontal;

    public virtual Orientation Orientation
    {
        get => _orientation;
        set
        {
            if (_orientation != value)
            {
                _orientation = value;
                ReportFormsPropertyChanged();

                OrientationChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// Event raised whenever the Value property changes regardless
    /// of source. This event may be raised multiple times if the user
    /// pushes+drags on the track or thumb.
    /// </summary>
    public event EventHandler? ValueChanged;

    /// <summary>
    /// Event raised when the Orientation property is changed.
    /// </summary>
    public event EventHandler? OrientationChanged;

    #endregion

    #region Initialize

    public RangeBase() : base() { }

    public RangeBase(InteractiveGue visual) : base(visual) { }

    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();

        ReactToVisualChangedBeforeRangeDefaults();

        Minimum = 0;
        Maximum = 100;
        Value = 0;
    }

    /// <summary>
    /// Runs in <see cref="ReactToVisualChanged"/> before Minimum, Maximum and Value get their defaults,
    /// so a subclass can find the parts of the new Visual that those assignments update.
    /// </summary>
    private protected virtual void ReactToVisualChangedBeforeRangeDefaults() { }

    #endregion

    #region Value Methods

    /// <summary>
    /// Runs in the Value setter after <see cref="ValueChanged"/> is raised and before the value is
    /// pushed to the view model.
    /// </summary>
    private protected virtual void OnValueChangeRaised() { }

    protected virtual void OnValueChanged(double oldValue, double newValue) { }

    protected virtual void OnMaximumChanged(double oldMaximum, double newMaximum)
    {
        if (Value > Maximum && Maximum >= Minimum)
        {
            Value = Maximum;
        }
    }

    protected virtual void OnMinimumChanged(double oldMinimum, double newMinimum)
    {
        if (Value < Minimum && Minimum <= Maximum)
        {
            Value = Minimum;
        }
    }

    #endregion
}
