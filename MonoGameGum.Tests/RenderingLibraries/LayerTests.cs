using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

public class LayerTests : BaseTestClass
{
    private static Camera CreateTopLeftCamera()
    {
        var camera = new Camera
        {
            ClientWidth = 800,
            ClientHeight = 600,
            CameraCenterOnScreen = CameraCenterOnScreen.TopLeft,
        };
        return camera;
    }

    [Fact]
    public void ScreenToWorld_WithNoLayerCameraSettings_ReturnsScreenAsWorld()
    {
        var camera = CreateTopLeftCamera();
        var layer = new Layer();

        layer.ScreenToWorld(camera, 100, 150, out var worldX, out var worldY);

        worldX.ShouldBe(100);
        worldY.ShouldBe(150);
    }

    [Fact]
    public void ScreenToWorld_WithLayerPositionY_TreatsPositionAsCameraOffset()
    {
        // LayerCameraSettings.Position is a camera position, not a layer offset. A positive Y
        // moves the camera down, which makes content on the layer appear to shift UP on screen.
        // Equivalently, the cursor at a given screen Y maps to a LARGER world Y on this layer.
        var camera = CreateTopLeftCamera();
        var layer = new Layer
        {
            LayerCameraSettings = new LayerCameraSettings
            {
                Position = new System.Numerics.Vector2(0, 200),
            },
        };

        layer.ScreenToWorld(camera, 100, 250, out var worldX, out var worldY);

        worldX.ShouldBe(100);
        worldY.ShouldBe(450);
    }

    [Fact]
    public void ScreenToWorld_WithLayerPositionX_TreatsPositionAsCameraOffset()
    {
        // Same camera-offset semantics on the X axis: a positive X moves the camera right,
        // so content shifts left on screen and the cursor maps to a larger world X.
        var camera = CreateTopLeftCamera();
        var layer = new Layer
        {
            LayerCameraSettings = new LayerCameraSettings
            {
                Position = new System.Numerics.Vector2(75, 0),
            },
        };

        layer.ScreenToWorld(camera, 125, 50, out var worldX, out var worldY);

        worldX.ShouldBe(200);
        worldY.ShouldBe(50);
    }

    [Fact]
    public void ScreenToWorld_WithIsInScreenSpaceAndLayerPosition_IgnoresMainCameraAndAppliesLayerPosition()
    {
        // IsInScreenSpace causes the main camera's position to be ignored, but
        // LayerCameraSettings.Position is still applied on top of the screen origin.
        var camera = CreateTopLeftCamera();
        camera.X = 1000;
        camera.Y = 1000;

        var layer = new Layer
        {
            LayerCameraSettings = new LayerCameraSettings
            {
                IsInScreenSpace = true,
                Position = new System.Numerics.Vector2(0, 200),
            },
        };

        layer.ScreenToWorld(camera, 100, 250, out var worldX, out var worldY);

        worldX.ShouldBe(100);
        worldY.ShouldBe(450);
    }
}
