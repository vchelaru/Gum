using Gum.GueDeriving;
using Raylib_cs;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Issue #7297: the dropshadow of a hollow (stroke-only) shape follows the visible stroke ring, as
/// MonoGame and Skia draw it. It used to be the whole silhouette, so a hollow shape glowed inside.
/// </summary>
public class HollowShapeShadowTests : BaseTestClass
{
    private const int CanvasSize = 400;
    private const int ShapeSize = 100;
    private const int ShapeMin = (CanvasSize - ShapeSize) / 2;
    private const float Blur = 16f;

    private static Image Render(bool circle)
    {
        Gum.Wireframe.GraphicalUiElement shape;
        if (circle)
        {
            CircleRuntime c = new();
            Apply(c);
            shape = c;
        }
        else
        {
            RectangleRuntime r = new();
            Apply(r);
            shape = r;
        }
        shape.X = ShapeMin;
        shape.Y = ShapeMin;
        shape.Width = ShapeSize;
        shape.Height = ShapeSize;
        GumService.Default.Root.Children.Add(shape);
        GumService.Default.Root.UpdateLayout();

        RenderTexture2D host = LoadRenderTexture(CanvasSize, CanvasSize);
        try
        {
            BeginDrawing();
            BeginTextureMode(host);
            ClearBackground(Color.Black);
            GumService.Default.Draw();
            EndTextureMode();
            EndDrawing();
            return LoadImageFromTexture(host.Texture);
        }
        finally
        {
            UnloadRenderTexture(host);
            GumService.Default.Root.Children.Clear();
        }
    }

    private static void Apply(CircleRuntime c)
    {
        c.IsFilled = false;
        c.StrokeWidth = 4;
        c.Color = Color.White;
        c.HasDropshadow = true;
        c.DropshadowColor = Color.White;
        c.DropshadowOffsetX = 0;
        c.DropshadowOffsetY = 0;
        c.DropshadowBlur = Blur;
    }

    private static void Apply(RectangleRuntime r)
    {
        r.IsFilled = false;
        r.StrokeWidth = 4;
        r.Color = Color.White;
        r.HasDropshadow = true;
        r.DropshadowColor = Color.White;
        r.DropshadowOffsetX = 0;
        r.DropshadowOffsetY = 0;
        r.DropshadowBlur = Blur;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Draw_HollowShapeWithDropshadow_HasNoGlowInsideTheRing(bool circle)
    {
        Image image = Render(circle);
        try
        {
            int center = CanvasSize / 2;
            int centerPixel = GetImageColor(image, center, center).R;
            // 8 px inside the ring's inner edge, on the left side: the ring's own blur is
            // faint there, a whole-silhouette shadow is nearly opaque.
            int nearInside = GetImageColor(image, ShapeMin + 4 + 8, center).R;
            int outside = GetImageColor(image, ShapeMin - 4, center).R;

            centerPixel.ShouldBeLessThan(5, "center of the hollow shape");
            nearInside.ShouldBeLessThan(120, "8 px inside the ring");
            outside.ShouldBeGreaterThan(40, "just outside the ring");
        }
        finally
        {
            UnloadImage(image);
        }
    }
}
