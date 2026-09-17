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
/// <see cref="InputLibrary.Keyboard"/> when they sample it each frame. Positions and bounds are
/// converted from the control's own device-independent units (DIU) to physical pixels, matching
/// <see cref="IInputHostControl"/>'s contract and the render target's physical-pixel sizing
/// (#4811, parity with the WPF head's #4681/#4682 fix) - otherwise hit-testing/dragging would
/// desync from the rendered content by the display's scale factor.
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
        // Exited is routed directly to the control, not tunneled or bubbled.
        control.AddHandler(InputElement.PointerExitedEvent, HandlePointerExited, RoutingStrategies.Direct, handledEventsToo: true);
        control.AddHandler(InputElement.PointerCaptureLostEvent, HandleCaptureLost, routes, handledEventsToo: true);
        control.AddHandler(InputElement.KeyDownEvent, HandleKeyDown, routes, handledEventsToo: true);
        control.AddHandler(InputElement.KeyUpEvent, HandleKeyUp, routes, handledEventsToo: true);
        control.LostFocus += (_, _) => _keysDown.Clear();

        // A native OS drag-and-drop (dragging a tree item or a file from Explorer onto the
        // canvas) does not raise PointerMoved on the control it's hovering, so without this the
        // tracked position freezes at wherever the pointer was before the drag started and a
        // drop places the new instance there instead of under the cursor (#4704).
        control.AddHandler(DragDrop.DragEnterEvent, HandleDrag, routes, handledEventsToo: true);
        control.AddHandler(DragDrop.DragOverEvent, HandleDrag, routes, handledEventsToo: true);
        control.AddHandler(DragDrop.DropEvent, HandleDrag, routes, handledEventsToo: true);
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
    public int Width => ToPhysicalPixels(_control.Bounds.Width, RenderScaling);

    /// <inheritdoc/>
    public int Height => ToPhysicalPixels(_control.Bounds.Height, RenderScaling);

    private double RenderScaling => TopLevel.GetTopLevel(_control)?.RenderScaling ?? 1.0;

    /// <summary>
    /// Converts a device-independent (DIU) value to its physical-pixel equivalent for the given
    /// render scale, rounding to the nearest pixel. No clamping - unlike a render target's size, a
    /// control dimension or point coordinate can legitimately be zero or negative (e.g. the cursor
    /// outside the control, to the left of or above its origin). Pure/static so it's unit-testable
    /// without a live Avalonia visual tree.
    /// </summary>
    public static int ToPhysicalPixels(double diuValue, double dpiScale) =>
        (int)Math.Round(diuValue * dpiScale);

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
        _pointerPosition = ToPhysicalPixels(e.GetPosition(_control));
        PointerPointProperties properties = e.GetCurrentPoint(_control).Properties;
        _isLeftDown = properties.IsLeftButtonPressed;
        _isRightDown = properties.IsRightButtonPressed;
        _isMiddleDown = properties.IsMiddleButtonPressed;
    }

    private void HandleDrag(object? sender, DragEventArgs e)
    {
        _pointerPosition = ToPhysicalPixels(e.GetPosition(_control));
    }

    private Point ToPhysicalPixels(Point diuPosition)
    {
        double scale = RenderScaling;
        return new Point(ToPhysicalPixels(diuPosition.X, scale), ToPhysicalPixels(diuPosition.Y, scale));
    }

    // The last in-bounds position must not outlive the pointer's visit: Cursor.IsInWindow reads any
    // position inside the bounds as "over the canvas", and the canvas then clears a highlight the tree
    // set while the pointer is over the tree (#4694). A captured pointer raises no exit, so a drag
    // that leaves the canvas keeps its position.
    private void HandlePointerExited(object? sender, PointerEventArgs e)
    {
        _pointerPosition = new Point(-1, -1);
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
