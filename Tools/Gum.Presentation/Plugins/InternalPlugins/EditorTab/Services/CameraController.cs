using System;
using Gum.Input;
using RenderingLibrary;
using Gum.Managers;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

/// <summary>
/// Camera pan and zoom from mouse and hotkey input, shared by the wireframe canvas and the
/// texture-coordinate canvas: middle-drag, Space+left-drag or a pan scroll (macOS trackpad) pans,
/// the wheel zooms toward the cursor, and the camera hotkeys step position and zoom. Framework-neutral - each canvas
/// translates its own input into <see cref="GumMouseEventArgs"/>/<see cref="GumKeyEventArgs"/>
/// and forwards them here.
/// </summary>
public class CameraController
{
    // Set by Initialize, which each canvas calls right after construction.
    Camera Camera
    {
        get;
        set;
    } = null!;

    IZoomController _zoomController = null!;
    readonly WheelZoomAccumulator _wheelZoomAccumulator = new();

    int _lastMouseX;
    int _lastMouseY;
    bool _isPanning;
    bool _isSpaceDown;
    bool _isSpacePanning;

    public event Action? CameraChanged;
    IHotkeyManager _hotkeyManager = null!;

    /// <summary>Whether a middle-button or Space+left-button drag is currently panning the camera.</summary>
    public bool IsPanning => _isPanning;

    /// <summary>Binds the controller to the camera it moves and the zoom steps it drives.</summary>
    public void Initialize(Camera camera, IZoomController zoomController, IHotkeyManager hotkeyManager)
    {
        _hotkeyManager = hotkeyManager;
        _zoomController = zoomController;
        Camera = camera;
    }

    public void HandleMouseWheel(GumMouseEventArgs e)
    {
        e.Handled = true;

        if (e.IsPanScroll)
        {
            Camera.X -= e.PanX / Camera.Zoom;
            Camera.Y -= e.PanY / Camera.Zoom;
            CameraChanged?.Invoke();
            return;
        }

        int step = _wheelZoomAccumulator.Consume(e.Delta);
        if (step == 0)
        {
            return;
        }

        float worldX, worldY;
        Camera.ScreenToWorld(e.X, e.Y, out worldX, out worldY);
        float differenceX = Camera.X - worldX;
        float differenceY = Camera.Y - worldY;

        float oldZoom = Camera.Zoom;

        if (step < 0)
        {
            _zoomController.ZoomOut();
        }
        else
        {
            _zoomController.ZoomIn();
        }

        float newDifferenceX = differenceX * oldZoom / Camera.Zoom;
        float newDifferenceY = differenceY * oldZoom / Camera.Zoom;

        Camera.X = worldX + newDifferenceX;
        Camera.Y = worldY + newDifferenceY;

        CameraChanged?.Invoke();
    }

    public void HandleMouseDown(GumMouseEventArgs e)
    {
        if (e.Button == GumMouseButton.Middle || (e.Button == GumMouseButton.Left && _isSpaceDown))
        {
            _isPanning = true;
            _isSpacePanning = e.Button == GumMouseButton.Left;
            _lastMouseX = e.X;
            _lastMouseY = e.Y;
        }
    }

    public void HandleMouseUp(GumMouseEventArgs e)
    {
        if (e.Button == GumMouseButton.Middle || (e.Button == GumMouseButton.Left && _isSpacePanning))
        {
            _isPanning = false;
            _isSpacePanning = false;
        }
    }

    public void HandleMouseMove(GumMouseEventArgs e)
    {
        // _isPanning (set/cleared only by HandleMouseDown/HandleMouseUp) is the source of truth for
        // whether a drag is in progress, rather than e.Button alone: a move event can report the
        // middle button as currently held without HandleMouseDown having run first for this press
        // (e.g. a stray/synchronized move), in which case _lastMouseX/Y would still be the position
        // from the end of a previous, already-released drag, and the whole idle-period distance
        // would get applied as one jump.
        bool isPanningButtonHeld = e.Button == GumMouseButton.Middle || (e.Button == GumMouseButton.Left && _isSpacePanning);
        if (_isPanning && isPanningButtonHeld)
        {
            int xChange = e.X - _lastMouseX;
            int yChange = e.Y - _lastMouseY;

            Camera.Position.X -= xChange / Camera.Zoom;
            Camera.Position.Y -= yChange / Camera.Zoom;

            if (xChange != 0 || yChange != 0)
            {
                CameraChanged?.Invoke();
            }

            _lastMouseX = e.X;
            _lastMouseY = e.Y;
        }
        else if (_isPanning && e.Button == GumMouseButton.None)
        {
            // The release happened off the canvas and never reached HandleMouseUp (no mouse
            // capture on WPF), so a move with nothing held is the only signal the drag is over.
            _isPanning = false;
            _isSpacePanning = false;
        }
    }

    /// <summary>Tracks Space release so a left-button pan that Space started stops, mid-drag if needed.</summary>
    public void HandleKeyUp(GumKeyEventArgs e)
    {
        if (e.Key == GumKey.Space)
        {
            _isSpaceDown = false;
            if (_isSpacePanning)
            {
                _isPanning = false;
                _isSpacePanning = false;
            }
        }
    }

    public void HandleKeyPress(GumKeyEventArgs e)
    {
        if (e.Key == GumKey.Space)
        {
            _isSpaceDown = true;
        }

        if (_hotkeyManager.MoveCameraLeft.IsPressed(e))
        {
            Camera.X -= 10 / Camera.Zoom;
            CameraChanged?.Invoke();
        }
        if (_hotkeyManager.MoveCameraRight.IsPressed(e))
        {
            Camera.X += 10 / Camera.Zoom;
            CameraChanged?.Invoke();
        }
        if (_hotkeyManager.MoveCameraUp.IsPressed(e))
        {
            Camera.Y -= 10 / Camera.Zoom;
            CameraChanged?.Invoke();
        }
        if (_hotkeyManager.MoveCameraDown.IsPressed(e))
        {
            Camera.Y += 10 / Camera.Zoom;
            CameraChanged?.Invoke();
        }

        if (_hotkeyManager.ZoomCameraIn.IsPressed(e) || _hotkeyManager.ZoomCameraInAlternative.IsPressed(e))
        {
            _zoomController.ZoomIn();
            CameraChanged?.Invoke();
        }

        if (_hotkeyManager.ZoomCameraOut.IsPressed(e) || _hotkeyManager.ZoomCameraOutAlternative.IsPressed(e))
        {
            _zoomController.ZoomOut();
            CameraChanged?.Invoke();
        }
    }
}
