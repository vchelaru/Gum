using System;
using System.ComponentModel;

namespace WpfDataUi.Controls;

/// <summary>
/// The value math behind click-and-drag over a numeric field's label: accumulates horizontal pointer
/// deltas from a starting value, snaps to the drag resolution, and clamps to the field's range. The
/// view supplies relative deltas from pointer capture; nothing here moves the cursor.
/// </summary>
public class LabelDragScrubLogic
{
    private double _unroundedValue;

    /// <summary>Starts a drag at <paramref name="value"/>, treating null as 0.</summary>
    public void Begin(object? value, Type propertyType)
    {
        if (value == null)
        {
            // Scrubbing up from an unset value starts at 0.
            value = 0;
        }

        TypeConverter converter = TypeDescriptor.GetConverter(propertyType);
        _unroundedValue = (double)converter.ConvertTo(value, typeof(double))!;
    }

    /// <summary>Starts a drag at a known numeric value.</summary>
    public void Begin(double value)
    {
        _unroundedValue = value;
    }

    /// <summary>
    /// Adds a pointer movement and returns the value to show: the accumulated total snapped to
    /// <paramref name="rounding"/> and clamped to [<paramref name="min"/>, <paramref name="max"/>].
    /// </summary>
    public double ApplyDelta(double deltaX, decimal changeMultiplier, decimal? rounding, decimal? min, decimal? max)
    {
        _unroundedValue += deltaX * (double)changeMultiplier;

        // Apply the snapped accumulator directly; re-adding the raw delta would put DPI-scaled
        // fractions back into a 1px-rounded value (issue #3191).
        double rounded = TextBoxDisplayLogic.SnapDraggedValue(_unroundedValue, rounding);

        // Stick at the bound while scrubbing instead of counting past it (e.g. StrokeWidth floor of 0).
        return (double)TextBoxDisplayLogic.ClampToRange(rounded, min, max);
    }
}
