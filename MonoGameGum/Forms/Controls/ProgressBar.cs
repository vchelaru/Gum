using Gum.DataTypes;
using Gum.Wireframe;

#if FRB
using FlatRedBall.Forms.Controls.Primitives;
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;
#else
using Gum.Forms.Controls.Primitives;
namespace Gum.Forms.Controls;
#endif

/// <summary>
/// A control which displays progress as a value between Minimum and Maximum. It does not take input.
/// The Visual's IndicatorInstance is sized as a percentage of its parent (usually TrackInstance)
/// according to Value. Setting IsIndeterminate shows activity without a known amount of progress.
/// </summary>
public class ProgressBar : RangeBase
{
    #region Fields/Properties

    /// <summary>The state category this control sets: Determinate or Indeterminate.</summary>
    public const string ProgressBarCategoryName = "ProgressBarCategory";

    /// <summary>The state applied while IsIndeterminate is false.</summary>
    public const string DeterminateStateName = "Determinate";

    /// <summary>The state applied while IsIndeterminate is true.</summary>
    public const string IndeterminateStateName = "Indeterminate";

    GraphicalUiElement? _indicator;

    bool _isIndeterminate;

    /// <summary>
    /// Whether the progress bar shows ongoing activity rather than Value. While true, the visual is set to
    /// its Indeterminate state, which owns how the indicator looks and moves, and Value no longer sizes
    /// the indicator.
    /// </summary>
    public bool IsIndeterminate
    {
        get => _isIndeterminate;
        set
        {
            if (value != _isIndeterminate)
            {
                _isIndeterminate = value;
                UpdateState();
                UpdateIndicatorWidth();
                ReportFormsPropertyChanged();
            }
        }
    }

    #endregion

    #region Initialize

    public ProgressBar() : base() { }

    public ProgressBar(InteractiveGue visual) : base(visual) { }

    private protected override void ReactToVisualChangedBeforeRangeDefaults()
    {
        _indicator = Visual.GetGraphicalUiElementByName("IndicatorInstance");
    }

    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();

        UpdateState();
        // The range defaults assigned in base may not change Value, so the new indicator is sized here.
        UpdateIndicatorWidth();
    }

    #endregion

    #region Value Methods

    protected override void OnValueChanged(double oldValue, double newValue)
    {
        base.OnValueChanged(oldValue, newValue);
        UpdateIndicatorWidth();
    }

    protected override void OnMinimumChanged(double oldMinimum, double newMinimum)
    {
        base.OnMinimumChanged(oldMinimum, newMinimum);
        UpdateIndicatorWidth();
    }

    protected override void OnMaximumChanged(double oldMaximum, double newMaximum)
    {
        base.OnMaximumChanged(oldMaximum, newMaximum);
        UpdateIndicatorWidth();
    }

    private void UpdateIndicatorWidth()
    {
        // While indeterminate the visual's Indeterminate state owns the indicator.
        if (_indicator == null || IsIndeterminate)
        {
            return;
        }

        double ratio = Maximum > Minimum
            ? (Value - Minimum) / (Maximum - Minimum)
            : 0;

        _indicator.WidthUnits = DimensionUnitType.PercentageOfParent;
        _indicator.Width = (float)(ratio * 100);
    }

    #endregion

    #region UpdateTo Methods

    public override void UpdateState()
    {
        if (Visual == null)
        {
            return;
        }
        Visual.SetProperty(ProgressBarCategoryName + "State",
            IsIndeterminate ? IndeterminateStateName : DeterminateStateName);
    }

    #endregion
}
