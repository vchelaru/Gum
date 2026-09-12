using RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

public class CameraDpiCompensationExtensionsTests
{
    [Fact]
    public void BeginDpiCompensatedRender_MultipliesZoomByDpiScale()
    {
        Camera camera = new Camera { Zoom = 1.5f };

        using (camera.BeginDpiCompensatedRender(dpiScale: 2.0))
        {
            camera.Zoom.ShouldBe(3.0f);
        }
    }

    [Fact]
    public void Dispose_RestoresOriginalZoom()
    {
        Camera camera = new Camera { Zoom = 1.5f };

        camera.BeginDpiCompensatedRender(dpiScale: 2.0).Dispose();

        camera.Zoom.ShouldBe(1.5f);
    }

    [Fact]
    public void Dispose_NormalizesClientWidthAndHeightBackToLogicalSize()
    {
        // Simulates Renderer.Draw setting ClientWidth/Height from a physical-pixel-sized
        // viewport while the scope is active (RenderingLibrary\Graphics\Renderer.cs sets these
        // unconditionally from GraphicsDevice.Viewport every Draw call).
        Camera camera = new Camera { ClientWidth = 800, ClientHeight = 600 };

        using (camera.BeginDpiCompensatedRender(dpiScale: 2.0))
        {
            camera.ClientWidth = 1600;
            camera.ClientHeight = 1200;
        }

        camera.ClientWidth.ShouldBe(800);
        camera.ClientHeight.ShouldBe(600);
    }

    [Fact]
    public void DpiScaleOfOne_LeavesZoomAndClientSizeUnchanged()
    {
        Camera camera = new Camera { Zoom = 1.0f, ClientWidth = 800, ClientHeight = 600 };

        using (camera.BeginDpiCompensatedRender(dpiScale: 1.0))
        {
            camera.Zoom.ShouldBe(1.0f);
            camera.ClientWidth = 800;
            camera.ClientHeight = 600;
        }

        camera.Zoom.ShouldBe(1.0f);
        camera.ClientWidth.ShouldBe(800);
        camera.ClientHeight.ShouldBe(600);
    }
}
