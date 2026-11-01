using RenderingLibrary.Math.Geometry;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Rasterizes the sprites a dotted line draws (nearest-neighbor, sampled at pixel centers) and measures
/// the length of every lit and unlit run. Every run except the two at the line's ends must be exactly
/// one texel (2 screen pixels) at every length, position and zoom (#7428).
/// </summary>
public class LinePrimitiveDottedPixelTests
{
    private const int DottedTextureWidth = 2;

    // The dotted line is drawn with .25 * zoom repetitions per world unit: a 4 pixel period at any zoom.
    private static List<int> MeasureRuns(float lengthWorld, float startScreen, float zoom)
    {
        var runs = LinePrimitive.LayoutRepeatedTexture(lengthWorld, DottedTextureWidth, .25f * zoom);

        float startWorld = startScreen / zoom;
        int firstPixel = (int)System.Math.Floor(startScreen);
        int lastPixel = (int)System.Math.Ceiling((startWorld + lengthWorld) * zoom);

        var lengths = new List<int>();
        bool? previous = null;
        int current = 0;

        for (int pixel = firstPixel; pixel <= lastPixel; pixel++)
        {
            float world = (pixel + .5f) / zoom - startWorld;
            bool? lit = null;
            foreach (var run in runs)
            {
                if (world >= run.Offset && world < run.Offset + run.Length)
                {
                    int texel = (int)System.Math.Floor((world - run.Offset) / (run.Length / run.SourceWidth));
                    lit = (run.SourceX + texel) % DottedTextureWidth == 0;
                }
            }

            if (lit == null)
            {
                continue;
            }
            if (lit == previous)
            {
                current++;
            }
            else
            {
                if (previous != null)
                {
                    lengths.Add(current);
                }
                previous = lit;
                current = 1;
            }
        }
        lengths.Add(current);
        return lengths;
    }

    [Fact]
    public void LayoutRepeatedTexture_DottedRuns_ShouldBeExactlyTwoPixelsAtEveryLengthPositionAndZoom()
    {
        var failures = new List<string>();

        foreach (float zoom in new[] { 1f, 2f, .5f, 3f })
        {
            foreach (float startScreen in new[] { 0f, 100f, 100.37f, 100.8f })
            {
                for (int length = 20; length <= 400; length++)
                {
                    var runs = MeasureRuns(length, startScreen, zoom);
                    for (int i = 0; i < runs.Count; i++)
                    {
                        bool isEnd = i == 0 || i == runs.Count - 1;
                        bool bad = isEnd ? runs[i] > 2 : runs[i] != 2;
                        if (bad)
                        {
                            failures.Add($"zoom {zoom} start {startScreen} length {length}: run {i} is {runs[i]} px");
                            break;
                        }
                    }
                }
            }
        }

        failures.Count.ShouldBe(0, string.Join("\n", failures.GetRange(0, System.Math.Min(failures.Count, 15))));
    }
}
