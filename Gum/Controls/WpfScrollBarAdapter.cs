using System;
using WpfScrollBar = System.Windows.Controls.Primitives.ScrollBar;

namespace Gum.Controls;

/// <summary>
/// Adapts a WPF <see cref="WpfScrollBar"/> to <see cref="IScrollBar"/>, so scrolling logic written
/// against WinForms range semantics can drive a WPF-native scroll bar. The WPF counterpart to
/// <see cref="ThemedScrollBar"/>, which implements the interface directly.
/// </summary>
/// <remarks>
/// WPF treats <c>Maximum</c> as the largest reachable value, whereas <see cref="IScrollBar"/>
/// treats it as the end of the scrolled area with the last screenful unreachable. This class owns
/// that conversion, and maps <see cref="LargeChange"/> onto <c>ViewportSize</c> so the thumb is
/// sized proportionally.
/// </remarks>
public class WpfScrollBarAdapter : IScrollBar
{
    private readonly WpfScrollBar _scrollBar;

    private int _minimum;
    private int _maximum;
    private int _largeChange;

    /// <summary>The element to host in a WPF visual tree.</summary>
    public WpfScrollBar Element => _scrollBar;

    public WpfScrollBarAdapter(WpfScrollBar scrollBar)
    {
        if (scrollBar == null)
        {
            throw new ArgumentNullException(nameof(scrollBar));
        }

        _scrollBar = scrollBar;
        _minimum = 0;
        _maximum = 100;
        _largeChange = 10;
        ApplyRange();

        _scrollBar.ValueChanged += (_, _) => ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public int Minimum
    {
        get => _minimum;
        set
        {
            _minimum = value;
            ApplyRange();
        }
    }

    /// <inheritdoc/>
    public int Maximum
    {
        get => _maximum;
        set
        {
            _maximum = Math.Max(value, _minimum + 1);
            ApplyRange();
        }
    }

    /// <inheritdoc/>
    public int LargeChange
    {
        get => _largeChange;
        set
        {
            _largeChange = Math.Max(1, value);
            ApplyRange();
        }
    }

    /// <inheritdoc/>
    public int Value
    {
        get => (int)_scrollBar.Value;
        set => _scrollBar.Value = value;
    }

    /// <inheritdoc/>
    public event EventHandler? ValueChanged;

    private void ApplyRange()
    {
        // Maximum goes first: WPF coerces it up to whatever Minimum currently is, so assigning
        // Minimum afterwards is what re-coerces Maximum back down to the value asked for here.
        _scrollBar.Maximum = Math.Max(_minimum, _maximum - _largeChange + 1);
        _scrollBar.Minimum = _minimum;
        _scrollBar.LargeChange = _largeChange;
        _scrollBar.ViewportSize = _largeChange;
    }
}
