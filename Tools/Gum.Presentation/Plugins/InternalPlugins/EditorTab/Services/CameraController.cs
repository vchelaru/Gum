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

    int _lastMouseX;
    int _lastMouseY;

    public event Action? CameraChanged;
    IHotkeyManager _hotkeyManager;

    public void Initialize(Camera camera,
        IZoomController zoomController, int defaultWidth, int defaultHeight, IHotkeyManager hotkeyManager)
    {
        _hotkeyManager = hotkeyManager;
        _zoomController = zoomController;
        Camera = camera;

        Camera.X = -30;
        Camera.Y = -30;
    }

    public void HandleMouseWheel(GumMouseEventArgs e)
    {
        float worldX, worldY;
        Camera.ScreenToWorld(e.X, e.Y, out worldX, out worldY);
        float differenceX = Camera.X - worldX;
        float differenceY = Camera.Y - worldY;

        float oldZoom = Camera.Zoom;

        if (e.Delta < 0)
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

        e.Handled = true;
    }

    public void HandleMouseDown(GumMouseEventArgs e)
    {
        if (e.Button == GumMouseButton.Middle)
        {
            _lastMouseX = e.X;
            _lastMouseY = e.Y;
        }
    }

    public void HandleMouseMove(GumMouseEventArgs e)
    {
        if (e.Button == GumMouseButton.Middle)
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

    public void HandleKeyPress(GumKeyEventArgs e)
    {
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
