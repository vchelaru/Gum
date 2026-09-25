using Gum.Services;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.Wireframe.Editors;
using RenderingLibrary;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// The editor overlay scales its sizes by the OS display scale and divides them by the camera
/// zoom, so it matches the tool's UI on a high-DPI display (#4983).
/// </summary>
public class OverlayDisplayScaleTests : BaseTestClass
{
    [Fact]
    public void ToWorldOverlaySize_MultipliesByDisplayScaleAndDividesByZoom()
    {
        Camera camera = new Camera();
        camera.Zoom = 4;
        CanvasDisplayScale displayScale = new CanvasDisplayScale();
        displayScale.DisplayScale = 2;
        EditorContext context = EditorContextTestHelper.Create(camera: camera, displayScale: displayScale);

        float worldSize = context.ToWorldOverlaySize(8);

        worldSize.ShouldBe(4);
    }
}
