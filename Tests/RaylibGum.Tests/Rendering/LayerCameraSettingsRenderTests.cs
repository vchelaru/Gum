using System.Numerics;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Issue #4367: raylib's <see cref="Renderer"/> built a single Camera2D from the main
/// <see cref="RenderingLibrary.Camera"/> for the whole frame, so a <see cref="Layer.LayerCameraSettings"/>
/// override (position/zoom/IsInScreenSpace) was honored by <see cref="Layer.ScreenToWorld"/>/
/// <see cref="Layer.WorldToScreen"/> (hit-testing) but never consulted when actually drawing —
/// a screen-space HUD layer rendered zoomed/panned with everything else instead of staying fixed.
/// </summary>
public class LayerCameraSettingsRenderTests : BaseTestClass
{
    [Fact]
    public void AddLayer_Parameterless_AddsNewLayerToLayers()
    {
        int before = Renderer.Self.Layers.Count;

        Layer layer = Renderer.Self.AddLayer();

        Renderer.Self.Layers.Count.ShouldBe(before + 1);
        Renderer.Self.Layers.ShouldContain(layer);

        Renderer.Self.RemoveLayer(layer);
    }

    [Fact]
    public void AddLayer_WithExistingLayerInstance_AddsItToLayers()
    {
        Layer layer = new Layer();
        int before = Renderer.Self.Layers.Count;

        Renderer.Self.AddLayer(layer);

        Renderer.Self.Layers.Count.ShouldBe(before + 1);
        Renderer.Self.Layers.ShouldContain(layer);

        Renderer.Self.RemoveLayer(layer);
    }

    [Fact]
    public void RemoveLayer_RemovesPreviouslyAddedLayer()
    {
        Layer layer = Renderer.Self.AddLayer();

        Renderer.Self.RemoveLayer(layer);

        Renderer.Self.Layers.ShouldNotContain(layer);
    }

    [Fact]
    public void Draw_LayerWithScreenSpaceCameraSettings_UsesScreenSpaceCameraForDrawing()
    {
        Renderer.Self.Camera.Zoom = 3f;
        Renderer.Self.Camera.X = 500f;
        Renderer.Self.Camera.Y = 500f;

        Layer hudLayer = Renderer.Self.AddLayer();
        hudLayer.LayerCameraSettings = new LayerCameraSettings
        {
            IsInScreenSpace = true,
        };

        try
        {
            BeginDrawing();
            GumService.Default.Draw();
            EndDrawing();

            // The HUD layer is drawn last, so ActiveCamera2D reflects its effective camera after
            // Draw completes. Before the fix, every layer shared the main camera's Zoom/Target
            // regardless of LayerCameraSettings.
            Renderer.Self.ActiveCamera2D.Zoom.ShouldBe(1f);
            Renderer.Self.ActiveCamera2D.Target.ShouldBe(Vector2.Zero);
        }
        finally
        {
            Renderer.Self.RemoveLayer(hudLayer);
            Renderer.Self.Camera.Zoom = 1f;
            Renderer.Self.Camera.X = 0f;
            Renderer.Self.Camera.Y = 0f;
        }
    }

    [Fact]
    public void Draw_LayerWithNoCameraSettings_StillUsesMainCamera()
    {
        Renderer.Self.Camera.Zoom = 2f;
        Renderer.Self.Camera.X = 40f;
        Renderer.Self.Camera.Y = 60f;

        try
        {
            BeginDrawing();
            GumService.Default.Draw();
            EndDrawing();

            Renderer.Self.ActiveCamera2D.Zoom.ShouldBe(2f);
            Renderer.Self.ActiveCamera2D.Target.ShouldBe(new Vector2(40f, 60f));
        }
        finally
        {
            Renderer.Self.Camera.Zoom = 1f;
            Renderer.Self.Camera.X = 0f;
            Renderer.Self.Camera.Y = 0f;
        }
    }
}
