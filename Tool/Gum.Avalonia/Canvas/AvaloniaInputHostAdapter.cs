using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using InputLibrary;
using Microsoft.Xna.Framework.Input;
using AvaloniaCursor = Avalonia.Input.Cursor;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;

namespace Gum.Avalonia.Canvas;

/// <summary>
/// Adapts an Avalonia <see cref="Control"/> to <see cref="IInputHostControl"/>. Avalonia has no
/// polled input, so this records pointer and key events on the control (handled ones included)
/// and hands the latest state to <see cref="InputLibrary.Cursor"/> and
/// <see cref="InputLibrary.Keyboard"/> when they sample it each frame. Positions are the
/// control's own device-independent units, the same units its bounds use.
/// </summary>
public sealed class AvaloniaInputHostAdapter : IInputHostControl
{
    private readonly Control _control;
    private readonly HashSet<XnaKeys> _keysDown = new HashSet<XnaKeys>();
    private Point _pointerPosition;
    private bool _isLeftDown;
    private bool _isRightDown;
    private bool _isMiddleDown;
    private CursorKind _cursorKind;

    /// <summary>Starts tracking input on <paramref name="control"/>.</summary>
    public AvaloniaInputHostAdapter(Control control)
    {
        _control = control ?? throw new ArgumentNullException(nameof(control));
        _pointerPosition = new Point(-1, -1);
        _cursorKind = CursorKind.Arrow;

        const RoutingStrategies routes = RoutingStrategies.Tunnel | RoutingStrategies.Bubble;
        control.AddHandler(InputElement.PointerMovedEvent, HandlePointer, routes, handledEventsToo: true);
        control.AddHandler(InputElement.PointerPressedEvent, HandlePointer, routes, handledEventsToo: true);
        control.AddHandler(InputElement.PointerReleasedEvent, HandlePointer, routes, handledEventsToo: true);
        control.AddHandler(InputElement.PointerExitedEvent, HandlePointer, routes, handledEventsToo: true);
        control.AddHandler(InputElement.PointerCaptureLostEvent, HandleCaptureLost, routes, handledEventsToo: true);
        control.AddHandler(InputElement.KeyDownEvent, HandleKeyDown, routes, handledEventsToo: true);
        control.AddHandler(InputElement.KeyUpEvent, HandleKeyUp, routes, handledEventsToo: true);
        control.LostFocus += (_, _) => _keysDown.Clear();
    }

    /// <inheritdoc/>
    public bool Focused
    {
        get
        {
            // A focused control in an inactive window must not register clicks, or another
            // window sitting on top would still drive the canvas.
            Window? window = TopLevel.GetTopLevel(_control) as Window;
            return _control.IsFocused && window?.IsActive != false;
        }
    }

    /// <inheritdoc/>
    public int Width => (int)_control.Bounds.Width;

    /// <inheritdoc/>
    public int Height => (int)_control.Bounds.Height;

    /// <inheritdoc/>
    public CursorKind Cursor
    {
        get => _cursorKind;
        set
        {
            _cursorKind = value;
            _control.Cursor = new AvaloniaCursor(ToStandardCursor(value));
        }
    }

    /// <inheritdoc/>
    public HostPointerState GetPointerState() =>
        new HostPointerState((float)_pointerPosition.X, (float)_pointerPosition.Y, _isLeftDown, _isRightDown, _isMiddleDown);

    /// <inheritdoc/>
    public KeyboardState GetKeyboardState()
    {
        XnaKeys[] keys = new XnaKeys[_keysDown.Count];
        _keysDown.CopyTo(keys);
        return new KeyboardState(keys);
    }

    /// <summary>Maps an Avalonia key to the XNA key the polled editor input speaks, or null.</summary>
    public static XnaKeys? ToXnaKey(Key key) => key switch
    {
        Key.Return => XnaKeys.Enter,
        Key.LeftCtrl => XnaKeys.LeftControl,
        Key.RightCtrl => XnaKeys.RightControl,
        Key.OemPeriod => XnaKeys.OemPeriod,
        Key.OemComma => XnaKeys.OemComma,
        _ => Enum.TryParse(key.ToString(), ignoreCase: true, out XnaKeys parsed) ? parsed : null,
    };

    /// <summary>Maps a neutral cursor kind to the Avalonia standard cursor that draws it.</summary>
    public static StandardCursorType ToStandardCursor(CursorKind kind) => kind switch
    {
        CursorKind.Cross => StandardCursorType.Cross,
        CursorKind.Hand => StandardCursorType.Hand,
        CursorKind.SizeAll => StandardCursorType.SizeAll,
        CursorKind.SizeNS => StandardCursorType.SizeNorthSouth,
        CursorKind.SizeWE => StandardCursorType.SizeWestEast,
        CursorKind.SizeNESW => StandardCursorType.TopRightCorner,
        CursorKind.SizeNWSE => StandardCursorType.TopLeftCorner,
        _ => StandardCursorType.Arrow,
    };

    private void HandlePointer(object? sender, PointerEventArgs e)
    {
        _pointerPosition = e.GetPosition(_control);
        PointerPointProperties properties = e.GetCurrentPoint(_control).Properties;
        _isLeftDown = properties.IsLeftButtonPressed;
        _isRightDown = properties.IsRightButtonPressed;
        _isMiddleDown = properties.IsMiddleButtonPressed;
    }

    private void HandleCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        _isLeftDown = false;
        _isRightDown = false;
        _isMiddleDown = false;
    }

    private void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        if (ToXnaKey(e.Key) is XnaKeys key)
        {
            _keysDown.Add(key);
        }
    }

    private void HandleKeyUp(object? sender, KeyEventArgs e)
    {
        if (ToXnaKey(e.Key) is XnaKeys key)
        {
            _keysDown.Remove(key);
        }
    }
}
