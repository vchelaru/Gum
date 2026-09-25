using System;
using System.Windows.Input;
using XnaAndWinforms;

using TextureCoordinateSelectionPlugin.RegionSelection;

namespace FlatRedBall.SpecializedXnaControls;

/// <summary>
/// The WPF texture-coordinate canvas: a <see cref="WpfGraphicsDeviceControl"/> that hosts an
/// <see cref="ImageRegionSelectionCore"/> and translates its double-click input. The owning
/// <c>MainControl</c> forwards its mouse and key events.
/// </summary>
public class ImageRegionSelectionControl : WpfGraphicsDeviceControl
{
    /// <summary>The framework-neutral canvas this control renders.</summary>
    public ImageRegionSelectionCore Core { get; }

    public ImageRegionSelectionControl()
    {
        Core = new ImageRegionSelectionCore(this);
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
