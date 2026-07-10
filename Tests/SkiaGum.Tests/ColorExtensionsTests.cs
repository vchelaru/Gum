using Gum.Forms.DefaultVisuals.V3;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests;

/// <summary>
/// Pins the Skia (<c>#if SKIA</c>) component-accessor mapping added to <see cref="ColorExtensions"/>
/// for #3562: SkiaSharp's <see cref="SKColor"/> exposes Red/Green/Blue/Alpha, whereas the XNA/Raylib
/// Color types use R/G/B/A. A channel swap would still compile but corrupt the luminance weighting,
/// so these tests exercise non-uniform inputs to guard the mapping.
/// </summary>
public class ColorExtensionsTests
{
    [Fact]
    public void Adjust_DarkensTowardZero_AndPreservesAlpha()
    {
        // -50% => each channel * 0.5; alpha is carried through untouched.
        SKColor result = new SKColor(200, 100, 40, 180).Adjust(-50f);

        result.Red.ShouldBe((byte)100);
        result.Green.ShouldBe((byte)50);
        result.Blue.ShouldBe((byte)20);
        result.Alpha.ShouldBe((byte)180);
    }

    [Fact]
    public void Adjust_LightensTowardWhite()
    {
        // +50% => 100 + (255 - 100) * 0.5 = 177.5 -> 177.
        SKColor result = new SKColor(100, 100, 100, 255).Adjust(50f);

        result.Red.ShouldBe((byte)177);
        result.Green.ShouldBe((byte)177);
        result.Blue.ShouldBe((byte)177);
    }

    [Fact]
    public void ToGrayscale_WeightsChannelsByLuminance_AndPreservesAlpha()
    {
        // Pure red through the standard luminance weights: 255 * 0.299 = 76.245 -> 76.
        // A Red/Blue swap would instead apply the 0.114 blue weight, so this pins Red specifically.
        SKColor result = new SKColor(255, 0, 0, 128).ToGrayscale();

        result.Red.ShouldBe((byte)76);
        result.Green.ShouldBe((byte)76);
        result.Blue.ShouldBe((byte)76);
        result.Alpha.ShouldBe((byte)128);
    }
}
