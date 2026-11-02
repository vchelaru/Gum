using Gum.GueDeriving;
using Gum.Wireframe;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// A filled render-target container at a fractional position and size, composited into a wrapper
/// render target, must keep its last pixel row (#7073). Before the fix the blit used the fractional
/// world size, which squeezed the baked texture and dropped the row a .5 edge cut in half.
/// </summary>
public class RenderTargetFractionalCompositeTests : BaseTestClass
{
    const int WrapperSize = 240;

    // Draws a red filled container into a wrapper render target and returns the red pixel count and
    // the number of rows they span.
    private static (int Count, int Rows) Render(float x, float y, float width, float height, float rotation)
    {
        ContainerRuntime container = new()
        {
            X = x, Y = y, Width = width, Height = height, Rotation = rotation, IsRenderTarget = true,
        };
#pragma warning disable CS0618
        container.AddChild(new ColoredRectangleRuntime
        {
            X = 0, Y = 0, Width = width, Height = height, Color = new Color((byte)255, (byte)0, (byte)0, (byte)255),
        });
#pragma warning restore CS0618
        ContainerRuntime wrapper = new() { X = 0, Y = 0, Width = WrapperSize, Height = WrapperSize, IsRenderTarget = true };
        wrapper.AddChild(container);
        GumService.Default.Root.Children.Add(wrapper);
        GumService.Default.Root.UpdateLayout();
        try
        {
            // Two frames, as the first bakes before the container's size is settled on some paths.
            for (int i = 0; i < 2; i++)
            {
                BeginDrawing();
                ClearBackground(Color.Black);
                GumService.Default.Draw();
                EndDrawing();
            }

            RenderTexture2D baked = Renderer.Self.TryGetBakedRenderTargetFor(wrapper)!.Value;
            Image image = LoadImageFromTexture(baked.Texture);
            try
            {
                int count = 0;
                int minY = int.MaxValue;
                int maxY = -1;
                for (int row = 0; row < WrapperSize; row++)
                {
                    for (int column = 0; column < WrapperSize; column++)
                    {
                        // A render texture is stored bottom-up in GL, so flip the row.
                        if (GetImageColor(image, column, WrapperSize - 1 - row).R > 20)
                        {
                            count++;
                            minY = System.Math.Min(minY, row);
                            maxY = System.Math.Max(maxY, row);
                        }
                    }
                }
                return (count, maxY < 0 ? 0 : maxY - minY + 1);
            }
            finally
            {
                UnloadImage(image);
            }
        }
        finally
        {
            GumService.Default.Root.Children.Clear();
        }
    }

    [Fact]
    public void Draw_FractionallyPositionedAndSizedContainer_KeepsItsLastRow()
    {
        (int count, int rows) = Render(10.25f, 20.75f, 42.5f, 29.5f, 0f);

        rows.ShouldBe(30);
        count.ShouldBe(42 * 30);
    }

    [Fact]
    public void Draw_RotatedFractionalContainer_KeepsItsEdgeCoverage()
    {
        (int count, _) = Render(60f, 60f, 42.5f, 29.5f, 30f);

        // The container covers about 42.5 * 29.5 = 1254 pixels; the squeezed blit covered 1223.
        count.ShouldBeGreaterThan(1245);
    }
}
