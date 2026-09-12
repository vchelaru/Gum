using System;

namespace RenderingLibrary
{
    /// <summary>
    /// Lets a WPF-hosted render surface (the Gum tool's canvas controls) draw at physical-pixel
    /// resolution on a scaled display without changing what the camera shows. WPF reports control
    /// sizes and mouse coordinates in device-independent units (DIU), so <see cref="Camera.Zoom"/>
    /// and hit-testing must stay DIU-based; but if the render target itself is only DIU-sized, WPF's
    /// own DPI compositing stretches the resulting bitmap to fill the larger physical-pixel area,
    /// blurring it (#4681). Rendering into a physical-pixel-sized target instead keeps it crisp, but
    /// needs the camera's zoom multiplied by the same DPI scale for that one draw so the same
    /// logical content fills the larger target rather than only a fraction of it.
    /// </summary>
    public static class CameraDpiCompensationExtensions
    {
        /// <summary>
        /// Temporarily multiplies <paramref name="camera"/>'s <see cref="Camera.Zoom"/> by
        /// <paramref name="dpiScale"/> for the duration of the returned scope. Dispose restores the
        /// original <see cref="Camera.Zoom"/>, and also divides <see cref="Camera.ClientWidth"/> and
        /// <see cref="Camera.ClientHeight"/> by <paramref name="dpiScale"/> - <c>Renderer.Draw</c>
        /// unconditionally overwrites both from the (now physical-pixel-sized) GraphicsDevice
        /// viewport while the scope is active, and code that reads them afterward (hit-testing,
        /// panning, fit-to-view) expects the DIU-based size the camera has everywhere else.
        /// </summary>
        public static IDisposable BeginDpiCompensatedRender(this Camera camera, double dpiScale) =>
            new DpiCompensatedRenderScope(camera, dpiScale);

        private sealed class DpiCompensatedRenderScope : IDisposable
        {
            private readonly Camera _camera;
            private readonly double _dpiScale;
            private readonly float _previousZoom;

            public DpiCompensatedRenderScope(Camera camera, double dpiScale)
            {
                _camera = camera;
                _dpiScale = dpiScale;
                _previousZoom = camera.Zoom;
                camera.Zoom = (float)(_previousZoom * dpiScale);
            }

            public void Dispose()
            {
                _camera.Zoom = _previousZoom;
                _camera.ClientWidth = (int)System.Math.Round(_camera.ClientWidth / _dpiScale);
                _camera.ClientHeight = (int)System.Math.Round(_camera.ClientHeight / _dpiScale);
            }
        }
    }
}
