using System;
using Avalonia;
using Avalonia.Controls;

namespace AvaloniaDataUi.Controls;

/// <summary>
/// A label followed by a strip of buttons. The strip gets its natural width first and the label takes
/// the rest, between <see cref="DataUiRowLayout.MinLabelWidth"/> and <see cref="MaxLabelWidth"/>, so the
/// strip wraps only once the label is at its floor. A star grid cannot express "value first, label
/// takes the remainder".
/// </summary>
internal sealed class LabelThenStripPanel : Panel
{
    private readonly Control _label;
    private readonly Control _strip;
    private double _maxLabelWidth;
    private double _labelWidth;

    public LabelThenStripPanel(Control label, Control strip, double maxLabelWidth)
    {
        _label = label;
        _strip = strip;
        _maxLabelWidth = maxLabelWidth;
        Children.Add(label);
        Children.Add(strip);
    }

    /// <summary>The label's width when there is room to spare.</summary>
    public double MaxLabelWidth
    {
        get => _maxLabelWidth;
        set
        {
            if (_maxLabelWidth != value)
            {
                _maxLabelWidth = value;
                InvalidateMeasure();
            }
        }
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        double minLabelWidth = Math.Min(DataUiRowLayout.MinLabelWidth, _maxLabelWidth);

        _strip.Measure(Size.Infinity);
        double stripNaturalWidth = _strip.DesiredSize.Width;

        _labelWidth = double.IsInfinity(availableSize.Width)
            ? _maxLabelWidth
            : Math.Clamp(availableSize.Width - stripNaturalWidth, minLabelWidth, _maxLabelWidth);

        _label.Measure(new Size(_labelWidth, availableSize.Height));
        double stripAvailable = double.IsInfinity(availableSize.Width)
            ? double.PositiveInfinity
            : Math.Max(0, availableSize.Width - _labelWidth);
        _strip.Measure(new Size(stripAvailable, availableSize.Height));

        return new Size(
            _labelWidth + _strip.DesiredSize.Width,
            Math.Max(_label.DesiredSize.Height, _strip.DesiredSize.Height));
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        _label.Arrange(new Rect(0, 0, _labelWidth, finalSize.Height));
        _strip.Arrange(new Rect(_labelWidth, 0, Math.Max(0, finalSize.Width - _labelWidth), finalSize.Height));
        return finalSize;
    }
}
