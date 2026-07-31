using System.Windows;
using System.Windows.Input;
using WinForms = System.Windows.Forms;
using WpfPoint = System.Windows.Point;

namespace Gum.Input;

/// <summary>
/// Translates WinForms and WPF mouse event types to Gum's framework-neutral <see cref="GumMouseEventArgs"/>
/// at the editor-host boundary (e.g. <c>WireframeControl</c>), so the wireframe editing subsystem
/// (camera panning/zoom, etc.) need not reference either framework.
/// </summary>
public static class MouseExtensions
{
    /// <summary>Maps a WinForms <see cref="WinForms.MouseButtons"/> value to the matching <see cref="GumMouseButton"/>.</summary>
    public static GumMouseButton ToGumMouseButton(this WinForms.MouseButtons buttons) =>
        buttons switch
        {
            WinForms.MouseButtons.Left => GumMouseButton.Left,
            WinForms.MouseButtons.Right => GumMouseButton.Right,
            WinForms.MouseButtons.Middle => GumMouseButton.Middle,
            _ => GumMouseButton.None,
        };

    /// <summary>Maps a WPF <see cref="MouseButton"/> value to the matching <see cref="GumMouseButton"/>.</summary>
    public static GumMouseButton ToGumMouseButton(this MouseButton button) =>
        button switch
        {
            MouseButton.Left => GumMouseButton.Left,
            MouseButton.Right => GumMouseButton.Right,
            MouseButton.Middle => GumMouseButton.Middle,
            _ => GumMouseButton.None,
        };

    /// <summary>Builds a framework-neutral <see cref="GumMouseEventArgs"/> from a WinForms mouse event.</summary>
    public static GumMouseEventArgs ToGumMouseEventArgs(this WinForms.MouseEventArgs e)
    {
        return new GumMouseEventArgs
        {
            X = e.X,
            Y = e.Y,
            Button = e.Button.ToGumMouseButton(),
            Delta = e.Delta,
        };
    }

    /// <summary>
    /// Builds a framework-neutral <see cref="GumMouseEventArgs"/> from a WPF mouse event, with the
    /// position expressed in <paramref name="relativeTo"/>'s coordinates.
    /// </summary>
    public static GumMouseEventArgs ToGumMouseEventArgs(this MouseEventArgs e, IInputElement relativeTo)
    {
        WpfPoint position = e.GetPosition(relativeTo);

        return new GumMouseEventArgs
        {
            X = (int)position.X,
            Y = (int)position.Y,
            Button = GetButton(e),
            Delta = e is MouseWheelEventArgs wheelArgs ? wheelArgs.Delta : 0,
            Handled = e.Handled,
        };
    }

    // A button event names the button that changed; a move/wheel event doesn't, so report whichever
    // button is currently held - that is what WinForms' MouseEventArgs.Button carries during a drag.
    private static GumMouseButton GetButton(MouseEventArgs e)
    {
        if (e is MouseButtonEventArgs buttonArgs)
        {
            return buttonArgs.ChangedButton.ToGumMouseButton();
        }

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            return GumMouseButton.Left;
        }
        if (e.RightButton == MouseButtonState.Pressed)
        {
            return GumMouseButton.Right;
        }
        if (e.MiddleButton == MouseButtonState.Pressed)
        {
            return GumMouseButton.Middle;
        }

        return GumMouseButton.None;
    }
}
