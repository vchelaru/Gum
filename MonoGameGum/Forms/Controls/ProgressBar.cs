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
/// according to Value.
/// </summary>
public class ProgressBar : RangeBase
{
    GraphicalUiElement? _indicator;

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
        if (_indicator == null)
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
}
