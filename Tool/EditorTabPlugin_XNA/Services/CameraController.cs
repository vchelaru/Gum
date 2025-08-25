using System;
using System.Windows.Forms;
using RenderingLibrary;
using FlatRedBall.AnimationEditorForms.Controls;
using RenderingLibrary.Graphics;
using Gum.Managers;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services
{
    public class CameraController
    {
        Camera Camera
        {
            get;
            set;
        }

        WireframeEditControl mWireframeEditControl;
        System.Drawing.Point mLastMouseLocation;

        public event Action CameraChanged;
        HotkeyManager _hotkeyManager;

        public void Initialize(Camera camera, WireframeEditControl wireframeEditControl, int defaultWidth, int defaultHeight, HotkeyManager hotkeyManager)
        {
            _hotkeyManager = hotkeyManager;
            Camera = camera;
            mWireframeEditControl = wireframeEditControl;

            Renderer.Self.Camera.X = - 30;
            Renderer.Self.Camera.Y = - 30;
        }

        internal void HandleMouseWheel(object sender, MouseEventArgs e)
        {
            float worldX, worldY;
            Camera.ScreenToWorld(e.X, e.Y, out worldX, out worldY);
            float differenceX = Camera.X - worldX;
            float differenceY = Camera.Y - worldY;

            float oldZoom = Camera.Zoom;

            if (e.Delta < 0)
            {
                mWireframeEditControl.ZoomOut();
            }
            else
            {
                mWireframeEditControl.ZoomIn();
            }

            float newDifferenceX = differenceX * oldZoom / Camera.Zoom;
            float newDifferenceY = differenceY * oldZoom / Camera.Zoom;

            Camera.X = worldX + newDifferenceX;
            Camera.Y = worldY + newDifferenceY;

            CameraChanged?.Invoke();

            var asHandleable = e as HandledMouseEventArgs;
            if (asHandleable != null)
            {
                asHandleable.Handled = true;
            }
        }

        internal void HandleMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                mLastMouseLocation = e.Location;
            }
        }

        internal void HandleMouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                int xChange = e.X - mLastMouseLocation.X;
                int yChange = e.Y - mLastMouseLocation.Y;

                Renderer.Self.Camera.Position.X -= xChange / Renderer.Self.Camera.Zoom;
                Renderer.Self.Camera.Position.Y -= yChange / Renderer.Self.Camera.Zoom;

                if (xChange != 0 || yChange != 0)
                {
                    CameraChanged?.Invoke();
                }

                mLastMouseLocation = e.Location;
            }
        }

        internal void HandleKeyPress(KeyEventArgs e)
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
                mWireframeEditControl.ZoomIn();
                CameraChanged?.Invoke();
            }

            if (_hotkeyManager.ZoomCameraOut.IsPressed(e) || _hotkeyManager.ZoomCameraOutAlternative.IsPressed(e))
            {
                mWireframeEditControl.ZoomOut();
                CameraChanged?.Invoke();
            }
        }
    }
}
