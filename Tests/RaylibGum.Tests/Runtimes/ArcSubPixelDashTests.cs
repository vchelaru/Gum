using Gum.GueDeriving;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Runtimes;

/// <summary>
/// #7404 - dashes shorter than a pixel read as the average coverage on Skia, so a dashed Arc made of them is a
/// solid stroke at alpha * dash / (dash + gap). The width 4 expected values were measured on the same scene rendered
/// on Skia (a full ring of opaque blue on white, mean red over the stroke, away from its edges). The width 1, 2 and 8
/// expected values are the analytic average coverage (Skia's measurement at width 1 is distorted), not Skia readings.
/// </summary>
public class ArcSubPixelDashTests : BaseTestClass
{
    private const double Tolerance = 6;

    // Ordinary 2.5 px dashes are aliased by raylib and sit 6.7 below Skia on the 140x80 ring; they are not sub-pixel and unchanged.
    private const double SweepTolerance = 7;

    private static double Render(float width, float height, float thickness, bool isEndRounded, float dash, float gap)
    {
        ContainerRuntime container = new() { Width = 200, Height = 200, IsRenderTarget = true };
        RectangleRuntime background = new()
        {
            Width = 200, Height = 200, IsFilled = true,
            FillColor = new Color((byte)255, (byte)255, (byte)255, (byte)255)
        };
        container.Children.Add(background);
        ArcRuntime arc = new() { StartAngle = 0, SweepAngle = 360, Thickness = thickness, IsEndRounded = isEndRounded };
        arc.X = 100 - width / 2;
        arc.Y = 100 - height / 2;
        arc.Width = width;
        arc.Height = height;
        arc.StrokeDashLength = dash;
        arc.StrokeGapLength = gap;
        arc.Red = 0; arc.Green = 0; arc.Blue = 255; arc.Alpha = 255;
        container.Children.Add(arc);
        Gum.GumService.Default.Root.Children.Add(container);
        Gum.GumService.Default.Root.UpdateLayout();
        BeginDrawing();
        Gum.GumService.Default.Draw();
        EndDrawing();

        Image image = LoadImageFromTexture(Renderer.Self.TryGetBakedRenderTargetFor(container)!.Value.Texture);
        ImageFlipVertical(ref image);
        double a = width / 2 - thickness / 2, b = height / 2 - thickness / 2, sum = 0;
        int count = 0;
        for (int y = 0; y < 200; y++)
        {
            for (int x = 0; x < 200; x++)
            {
                double u = (x + 0.5 - 100) / a, v = (y + 0.5 - 100) / b;
                double r = System.Math.Sqrt(u * u + v * v);
                if (System.Math.Abs(r - 1) * System.Math.Min(a, b) < thickness / 2 - thickness / 4)
                {
                    sum += GetImageColor(image, x, y).R;
                    count++;
                }
            }
        }
        UnloadImage(image);
        Gum.GumService.Default.Root.Children.Clear();
        return sum / count;
    }

    [Theory]
    [InlineData(200, 100, 0.25f, 0.25f, 129.6)]
    [InlineData(200, 100, 0.5f, 0.5f, 129.8)]
    [InlineData(200, 100, 0.5f, 1.5f, 192.2)]
    [InlineData(200, 100, 1.5f, 0.5f, 67.8)]
    [InlineData(140, 80, 0.25f, 0.25f, 128.0)]
    [InlineData(140, 80, 0.5f, 0.5f, 128.5)]
    [InlineData(140, 80, 0.5f, 1.5f, 191.4)]
    [InlineData(140, 80, 1.5f, 0.5f, 64.6)]
    public void SubPixelDashes_ButtCaps_MatchSkiaAverageCoverage(float width, float height, float dash, float gap, double skiaRed)
    {
        Render(width, height, 4, false, dash, gap).ShouldBe(skiaRed, Tolerance);
    }

    [Theory]
    [InlineData(200, 100, 2.5)]
    [InlineData(140, 80, 2.5)]
    public void SubPixelDashes_RoundCaps_AreFullyCovered(float width, float height, double skiaRed)
    {
        Render(width, height, 4, true, 0.5f, 0.5f).ShouldBe(skiaRed, Tolerance);
    }

    // Expected red is the analytic average coverage of opaque blue on white: 255 * (1 - dash / (dash + gap)).
    [Theory]
    [InlineData(8, 0.25f, 0.25f, 127.5)]
    [InlineData(8, 0.5f, 1.5f, 191.25)]
    [InlineData(8, 1.5f, 0.5f, 63.75)]
    [InlineData(2, 0.25f, 0.25f, 127.5)]
    [InlineData(2, 0.5f, 1.5f, 191.25)]
    [InlineData(1, 0.25f, 0.25f, 127.5)]
    [InlineData(1, 0.5f, 1.5f, 191.25)]
    public void SubPixelDashes_ButtCaps_MatchAnalyticAverageCoverage(float thickness, float dash, float gap, double expectedRed)
    {
        Render(200, 100, thickness, false, dash, gap).ShouldBe(expectedRed, Tolerance);
    }

    // Round caps overlap into a fully covered stroke (analytic: the caps add the thickness to every dash).
    [Theory]
    [InlineData(8)]
    [InlineData(2)]
    [InlineData(1)]
    public void SubPixelDashes_RoundCaps_AnalyticFullCoverage(float thickness)
    {
        Render(200, 100, thickness, true, 0.5f, 0.5f).ShouldBe(0, Tolerance);
    }

    // A pattern of 3 px or less on a thin stroke takes the undashed path (TryGetUndashedColor), an intended choice:
    // at that size dashes read as the average coverage anyway. 1 + 1 is a 2 px pattern, 1.5 + 1.5 sits on the 3 px limit.
    [Theory]
    [InlineData(2, 1f, 1f)]
    [InlineData(2, 1.5f, 1.5f)]
    [InlineData(1, 1f, 1f)]
    public void ThinStroke_PatternUpToThreePixels_IsDrawnAsAverageCoverage(float thickness, float dash, float gap)
    {
        Render(200, 100, thickness, false, dash, gap).ShouldBe(127.5, Tolerance);
    }

    [Theory]
    [InlineData(200, 100, 129.5)]
    [InlineData(140, 80, 128.0)]
    public void GrowingDashes_KeepTheSameAverageBrightnessAsSkia(float width, float height, double skiaRed)
    {
        List<string> outside = new();
        for (int step = 3; step <= 30; step++)
        {
            float dash = step / 10f;
            double red = Render(width, height, 4, false, dash, dash);
            if (System.Math.Abs(red - skiaRed) > SweepTolerance)
            {
                outside.Add($"{dash:F1}: {red:F1}");
            }
        }
        outside.ShouldBeEmpty($"dashes with a mean red more than {SweepTolerance} from Skia's {skiaRed}");
    }
}
