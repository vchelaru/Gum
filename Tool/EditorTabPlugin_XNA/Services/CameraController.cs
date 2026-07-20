using System;
using Gum.Input;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Gum.Managers;
using EditorTabPlugin_XNA.ViewModels;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

public class CameraController
{
    Camera Camera
    {
        get;
        set;
    }

    EditorViewModel _editorViewModel;

    int _lastMouseX;
    int _lastMouseY;

    public event Action? CameraChanged;
    IHotkeyManager _hotkeyManager;

    public void Initialize(Camera camera, 
        EditorViewModel viewModel, int defaultWidth, int defaultHeight, IHotkeyManager hotkeyManager)
    {
        _hotkeyManager = hotkeyManager;
        _editorViewModel = viewModel;
        Camera = camera;

        Renderer.Self.Camera.X = - 30;
        Renderer.Self.Camera.Y = - 30;
    }

    internal void HandleMouseWheel(GumMouseEventArgs e)
    {
        float worldX, worldY;
        Camera.ScreenToWorld(e.X, e.Y, out worldX, out worldY);
        float differenceX = Camera.X - worldX;
        float differenceY = Camera.Y - worldY;

        float oldZoom = Camera.Zoom;

        if (e.Delta < 0)
        {
            _editorViewModel.ZoomOut();
        }
        else
        {
            _editorViewModel.ZoomIn();
        }

        float newDifferenceX = differenceX * oldZoom / Camera.Zoom;
        float newDifferenceY = differenceY * oldZoom / Camera.Zoom;

        Camera.X = worldX + newDifferenceX;
        Camera.Y = worldY + newDifferenceY;

        CameraChanged?.Invoke();

        e.Handled = true;
    }

    internal void HandleMouseDown(GumMouseEventArgs e)
    {
        if (e.Button == GumMouseButton.Middle)
        {
            _lastMouseX = e.X;
            _lastMouseY = e.Y;
        }
    }

    internal void HandleMouseMove(GumMouseEventArgs e)
    {
        if (e.Button == GumMouseButton.Middle)
        {
            int xChange = e.X - _lastMouseX;
            int yChange = e.Y - _lastMouseY;

            Renderer.Self.Camera.Position.X -= xChange / Renderer.Self.Camera.Zoom;
            Renderer.Self.Camera.Position.Y -= yChange / Renderer.Self.Camera.Zoom;

            if (xChange != 0 || yChange != 0)
            {
                CameraChanged?.Invoke();
            }

            _lastMouseX = e.X;
            _lastMouseY = e.Y;
        }
    }

    internal void HandleKeyPress(GumKeyEventArgs e)
    {
        if (_hotkeyManager.MoveCameraLeft.IsPressed(e))
        {
            SystemManagers.Default.Renderer.Camera.X -= 10 / SystemManagers.Default.Renderer.Camera.Zoom;
            CameraChanged?.Invoke();
        }
        if (_hotkeyManager.MoveCameraRight.IsPressed(e))
        {
            SystemManagers.Default.Renderer.Camera.X += 10 / SystemManagers.Default.Renderer.Camera.Zoom;
            CameraChanged?.Invoke();
        }
        if (_hotkeyManager.MoveCameraUp.IsPressed(e))
        {
            SystemManagers.Default.Renderer.Camera.Y -= 10 / SystemManagers.Default.Renderer.Camera.Zoom;
            CameraChanged?.Invoke();
        }
        if (_hotkeyManager.MoveCameraDown.IsPressed(e))
        {
            SystemManagers.Default.Renderer.Camera.Y += 10 / SystemManagers.Default.Renderer.Camera.Zoom;
            CameraChanged?.Invoke();
        }

        if (_hotkeyManager.ZoomCameraIn.IsPressed(e) || _hotkeyManager.ZoomCameraInAlternative.IsPressed(e))
        {
            _editorViewModel.ZoomIn();
            CameraChanged?.Invoke();
        }

        if (_hotkeyManager.ZoomCameraOut.IsPressed(e) || _hotkeyManager.ZoomCameraOutAlternative.IsPressed(e))
        {
            _editorViewModel.ZoomOut();
            CameraChanged?.Invoke();
        }
    }
}
