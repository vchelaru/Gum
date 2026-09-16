using System;
using Gum.Input;
using RenderingLibrary;
using Gum.Managers;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

public class CameraController
{
    Camera Camera
    {
        get;
        set;
    }

    IZoomController _zoomController;
    readonly WheelZoomAccumulator _wheelZoomAccumulator = new();

    int _lastMouseX;
    int _lastMouseY;
    bool _isPanning;
    bool _isSpaceDown;
    bool _isSpacePanning;

    public event Action? CameraChanged;
    IHotkeyManager _hotkeyManager;

    /// <summary>Whether a middle-button or Space+left-button drag is currently panning the camera.</summary>
    public bool IsPanning => _isPanning;

    public void Initialize(Camera camera, IZoomController zoomController, IHotkeyManager hotkeyManager)
    {
        _hotkeyManager = hotkeyManager;
        _zoomController = zoomController;
        Camera = camera;

        Camera.X = -30;
        Camera.Y = -30;
    }

    public void HandleMouseWheel(GumMouseEventArgs e)
    {
        e.Handled = true;

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
