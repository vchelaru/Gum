using System;
using System.Windows.Input;
using XnaAndWinforms;

namespace FlatRedBall.SpecializedXnaControls;

/// <summary>
/// The WPF texture-coordinate canvas: a <see cref="WpfGraphicsDeviceControl"/> that hosts an
/// <see cref="ImageRegionSelectionCore"/> and translates its WPF wheel and double-click input.
/// </summary>
public class ImageRegionSelectionControl : WpfGraphicsDeviceControl
{
    /// <summary>The framework-neutral canvas this control renders.</summary>
    public ImageRegionSelectionCore Core { get; }

    public ImageRegionSelectionControl()
    {
        Core = new ImageRegionSelectionCore(this);
        MouseWheel += (_, e) =>
        {
            if (Core.HandleMouseWheel(e.Delta))
            {
                // Stop a containing scroll viewer from also scrolling on the same wheel tick.
                e.Handled = true;
            }
        };
    }

    /// <inheritdoc/>
    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
        {
            Core.RaiseDoubleClick();
        }
    }

    /// <inheritdoc/>
    protected override void Draw() => Core.Draw();
}
