using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Gum.Controls;

namespace Gum.Avalonia.Plugins.EditorTab;

/// <summary>
/// Adapts an Avalonia <see cref="ScrollBar"/> to <see cref="IScrollBar"/>, so scrolling logic
/// written against WinForms range semantics can drive it. Owns the conversion the WPF adapter
/// owns: the interface's <c>Maximum</c> is the end of the scrolled area with the last screenful
/// unreachable, and <see cref="LargeChange"/> maps onto the viewport size so the thumb is sized
/// proportionally.
/// </summary>
public sealed class AvaloniaScrollBarAdapter : IScrollBar
{
    private readonly ScrollBar _scrollBar;
    private int _minimum;
    private int _maximum;
    private int _largeChange;

    /// <summary>Creates the adapter over <paramref name="scrollBar"/>.</summary>
    public AvaloniaScrollBarAdapter(ScrollBar scrollBar)
    {
        _scrollBar = scrollBar ?? throw new ArgumentNullException(nameof(scrollBar));
        _minimum = 0;
        _maximum = 100;
        _largeChange = 10;
        ApplyRange();
        _scrollBar.ValueChanged += (_, _) => ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>The scroll bar this adapts.</summary>
    public ScrollBar Element => _scrollBar;

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
        _scrollBar.Minimum = _minimum;
        _scrollBar.Maximum = Math.Max(_minimum, _maximum - _largeChange + 1);
        _scrollBar.LargeChange = _largeChange;
        _scrollBar.ViewportSize = _largeChange;
    }
}

/// <summary>
/// Adapts an Avalonia <see cref="Control"/> to <see cref="IScrollSurface"/> so the scroll logic
/// can size its bars from the canvas without referencing Avalonia.
/// </summary>
public sealed class ControlScrollSurfaceAdapter : IScrollSurface
{
    private readonly Control _control;

    /// <summary>Creates the adapter over <paramref name="control"/>.</summary>
    public ControlScrollSurfaceAdapter(Control control)
    {
        _control = control ?? throw new ArgumentNullException(nameof(control));
        _control.SizeChanged += (_, _) => SizeChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public int Width => (int)_control.Bounds.Width;

    /// <inheritdoc/>
    public int Height => (int)_control.Bounds.Height;

    /// <inheritdoc/>
    public event EventHandler? SizeChanged;
}
