using Gum.GueDeriving;
using Gum.RenderingLibrary;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Spike for #8263: no SKBlendMode is clamp(dst - src); an SkSL runtime blender wired through
/// ApplyBlend gives it on a Gum RectangleRuntime drawn by SkiaGum's renderer.
/// </summary>
public class SubtractBlendSpike8263Tests
{
    [Theory]
    [InlineData(255, 123)]
    [InlineData(82, 0)]
    public void SubtractBlender_OverOpaqueGray_GivesClampedDifference(int behind, int expected)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(32, 32));
        Gum.GumService.Default.Initialize(surface.Canvas, 32, 32);

        ContainerRuntime root = new() { Width = 32, Height = 32 };
        root.Children.Add(NewFill(new SKColor((byte)behind, (byte)behind, (byte)behind), Blend.Normal));
        root.Children.Add(NewFill(new SKColor(132, 132, 132), Blend.Subtract));
        Gum.GumService.Default.Root.Children.Add(root);
        Gum.GumService.Default.Draw();
        Gum.GumService.Default.Root.Children.Remove(root);

        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        SKColor pixel = bitmap.GetPixel(16, 16);

        System.IO.File.AppendAllText(
            "/private/tmp/claude-504/-Users-vicpersonal--claude-spares-spare-OiarL5/5e830539-c122-4f0b-b5cd-adeb57ad90ee/scratchpad/8263-skia.txt",
            $"behind={behind} -> {pixel.Red},{pixel.Green},{pixel.Blue},{pixel.Alpha}\n");

        ((int)pixel.Red).ShouldBeInRange(expected - 1, expected + 1);
        ((int)pixel.Green).ShouldBeInRange(expected - 1, expected + 1);
        ((int)pixel.Blue).ShouldBeInRange(expected - 1, expected + 1);
        pixel.Alpha.ShouldBe((byte)255);
    }

    private static RectangleRuntime NewFill(SKColor color, Blend blend) => new()
    {
        Width = 32, Height = 32, IsFilled = true, StrokeWidth = 0, FillColor = color, Blend = blend,
    };
}
