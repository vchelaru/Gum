using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderer;

/// <summary>
/// Issue #4367: SkiaGum's <see cref="RenderingLibrary.Graphics.Renderer"/> applied only the main
/// <see cref="Camera"/> when drawing, so a <see cref="Layer.LayerCameraSettings"/> override
/// (position/zoom/IsInScreenSpace) was honored by <see cref="Layer.ScreenToWorld"/>/
/// <see cref="Layer.WorldToScreen"/> (hit-testing) but never consulted when actually drawing —
/// a screen-space HUD layer rendered zoomed/panned with everything else instead of staying fixed.
/// </summary>
public class LayerCameraSettingsRenderTests
{
    public LayerCameraSettingsRenderTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    [Fact]
    public void AddLayer_Parameterless_AddsNewLayerToLayers()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);

        int before = SystemManagers.Default.Renderer.Layers.Count;

        Layer layer = SystemManagers.Default.Renderer.AddLayer();

        SystemManagers.Default.Renderer.Layers.Count.ShouldBe(before + 1);
        SystemManagers.Default.Renderer.Layers.ShouldContain(layer);
    }

    [Fact]
    public void RemoveLayer_RemovesPreviouslyAddedLayer()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);

        Layer layer = SystemManagers.Default.Renderer.AddLayer();

        SystemManagers.Default.Renderer.RemoveLayer(layer);

        SystemManagers.Default.Renderer.Layers.ShouldNotContain(layer);
    }

    [Fact]
    public void Draw_LayerWithScreenSpaceCameraSettings_IgnoresMainCameraPositionAndZoom()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);
        surface.Canvas.Clear(SKColors.Black);

        // A wildly offset/zoomed main camera. If the HUD layer below honors it (the bug), the
        // rectangle renders far off-screen and pixel (5, 5) stays background.
        SystemManagers.Default.Renderer.Camera.Zoom = 5f;
        SystemManagers.Default.Renderer.Camera.X = 1000f;
        SystemManagers.Default.Renderer.Camera.Y = 1000f;

        Layer hudLayer = SystemManagers.Default.Renderer.AddLayer();
        hudLayer.LayerCameraSettings = new LayerCameraSettings
        {
            IsInScreenSpace = true,
        };

        RectangleRuntime rectangle = new()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            IsFilled = true,
            FillColor = SKColors.Red,
        };
        rectangle.AddToManagers(SystemManagers.Default, hudLayer);

        GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);

        bitmap.GetPixel(5, 5).ShouldBe(SKColors.Red,
            "because IsInScreenSpace must render fixed on screen, ignoring the main camera's position and zoom");
    }
}
