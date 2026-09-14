using System;
using System.Windows.Controls.Primitives;

namespace TextureCoordinateSelectionPlugin.Views;

/// <summary>Adapts a WPF <see cref="ScrollBar"/> to <see cref="ICameraScrollBar"/>.</summary>
public sealed class WpfCameraScrollBar : ICameraScrollBar
{
    private readonly ScrollBar _scrollBar;

    public WpfCameraScrollBar(ScrollBar scrollBar)
    {
        _scrollBar = scrollBar ?? throw new ArgumentNullException(nameof(scrollBar));
        _scrollBar.ValueChanged += (_, _) => ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public double Minimum { get => _scrollBar.Minimum; set => _scrollBar.Minimum = value; }

    /// <inheritdoc/>
    public double Maximum { get => _scrollBar.Maximum; set => _scrollBar.Maximum = value; }

    /// <inheritdoc/>
    public double ViewportSize { get => _scrollBar.ViewportSize; set => _scrollBar.ViewportSize = value; }

    /// <inheritdoc/>
    public double Value { get => _scrollBar.Value; set => _scrollBar.Value = value; }

    /// <inheritdoc/>
    public event EventHandler? ValueChanged;
}
