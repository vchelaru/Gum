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
    public void ScreenToWorld_WithMainCameraOffsetAndNoLayerSettings_UsesMainCamera()
    {
        // If no LayerCameraSettings is assigned, the layer should fall through to the main
        // camera's position exactly as Camera.ScreenToWorld would.
        var camera = CreateTopLeftCamera();
        camera.X = 50;
        camera.Y = 30;

        var layer = new Layer();

        layer.ScreenToWorld(camera, 100, 150, out var worldX, out var worldY);

        worldX.ShouldBe(150);
        worldY.ShouldBe(180);
    }

    [Fact]
    public void ScreenToWorld_WithMainCameraOffsetAndLayerPosition_AddsTheTwo()
    {
        // When IsInScreenSpace is false, the layer's Position is additive with the main camera.
        var camera = CreateTopLeftCamera();
        camera.X = 50;
        camera.Y = 30;

        var layer = new Layer
        {
            LayerCameraSettings = new LayerCameraSettings
            {
                Position = new System.Numerics.Vector2(10, 20),
            },
        };

        layer.ScreenToWorld(camera, 100, 150, out var worldX, out var worldY);

        worldX.ShouldBe(160);
        worldY.ShouldBe(200);
    }

    [Fact]
    public void ScreenToWorld_WithZoom_OverridesMainCameraZoom()
    {
        var camera = CreateTopLeftCamera();
        camera.Zoom = 4;

        var layer = new Layer
        {
            LayerCameraSettings = new LayerCameraSettings
            {
                Zoom = 2,
            },
        };

        layer.ScreenToWorld(camera, 100, 50, out var worldX, out var worldY);

        // With layer zoom = 2, world = screen / 2.
        worldX.ShouldBe(50);
        worldY.ShouldBe(25);
    }

    [Fact]
    public void ScreenToWorld_WithZoomAndPosition_AppliesBothCorrectly()
    {
        var camera = CreateTopLeftCamera();

        var layer = new Layer
        {
            LayerCameraSettings = new LayerCameraSettings
            {
                Zoom = 2,
                Position = new System.Numerics.Vector2(10, 20),
            },
        };

        layer.ScreenToWorld(camera, 100, 50, out var worldX, out var worldY);

        // Transform: world -> (world - pos) * zoom = screen. Inverse: world = screen/zoom + pos.
        worldX.ShouldBe(60);
        worldY.ShouldBe(45);
    }

    [Fact]
    public void ScreenToWorld_WithIsInScreenSpaceAndMainCameraZoom_IgnoresMainCameraZoom()
    {
        // IsInScreenSpace means "ignore the main camera entirely." This covers zoom too:
        // a screen-space HUD should not scale when the world camera zooms in. This keeps
        // hit-testing consistent with rendering, which uses zoom=1 in this configuration.
        var camera = CreateTopLeftCamera();
        camera.Zoom = 4;
        camera.X = 1000;
        camera.Y = 1000;

        var layer = new Layer
        {
            LayerCameraSettings = new LayerCameraSettings
            {
                IsInScreenSpace = true,
            },
        };

        layer.ScreenToWorld(camera, 100, 150, out var worldX, out var worldY);

        worldX.ShouldBe(100);
        worldY.ShouldBe(150);
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
