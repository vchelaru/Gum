using Gum;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.GueDeriving;

/// <summary>
/// Manual-test follow-up (surfaced while verifying issue #4367's LayerCameraSettings fix): a
/// <see cref="RectangleRuntime"/> added directly to a <see cref="RenderingLibrary.Graphics.Layer"/>
/// via <c>AddToManagers</c> (top-level, not nested under a parent) rendered its stroke at the
/// wrong size on Skia -- a small box near the origin instead of tracing the shape's actual
/// Width/Height.
///
/// Root cause: <c>SkiaShapeRuntime</c>'s two-slot fill+stroke composition mirrors the stroke
/// slot's Width/Height onto the fill's every frame from <c>SkiaShapeRuntime.PreRender()</c>. The
/// Skia <see cref="RenderingLibrary.Graphics.Renderer"/>'s per-layer render walk calls
/// <c>.PreRender()</c> on whatever object is actually registered as a Layer member -- for a
/// TOP-LEVEL renderable that's the raw contained (fill) <c>RenderableShapeBase</c>
/// (<c>GraphicalUiElement.AddToManagers</c> registers <c>mContainedObjectAsIpso</c>, not the GUE
/// wrapper), whose own <c>PreRender()</c> was a no-op -- so the GUE-level override (and its stroke
/// mirror) was never reached. A NESTED child doesn't hit this: the render walk's
/// <c>IRenderableIpso.Children</c> holds the child GUE wrapper itself, so its <c>PreRender()</c>
/// override fires normally (see <c>Render_StrokeWidthZero_DrawsNoVisibleStroke</c> for the
/// already-covered nested case).
/// </summary>
public class RectangleRuntimeTopLevelStrokeTests
{
    public RectangleRuntimeTopLevelStrokeTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    [Fact]
    public void Draw_TwoSlotRectangleAddedDirectlyToLayer_StrokeMatchesFillSize()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);
        surface.Canvas.Clear(SKColors.Black);

        RectangleRuntime rectangle = new()
        {
            X = 0,
            Y = 0,
            Width = 50,
            Height = 50,
            IsFilled = true,
            FillColor = SKColors.Red,
            StrokeColor = SKColors.Lime,
            StrokeWidth = 4,
            StrokeWidthUnits = DimensionUnitType.Absolute,
        };
        // Top-level: added directly to the Layer, not nested under Root -- the case that exposes
        // the bug (see class remarks above).
        rectangle.AddToManagers(SystemManagers.Default, SystemManagers.Default.Renderer.MainLayer);

        GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);

        // Just inside the rectangle's right edge, where the (correctly-sized) stroke should trace.
        // Before the fix the stroke stayed at its tiny construction-time default size near the
        // origin, so this pixel showed the fill's red instead.
        bitmap.GetPixel(48, 25).ShouldBe(SKColors.Lime,
            "because the stroke must scale to the rectangle's actual Width/Height (50x50), not stay at its construction-time default size");
    }
}
