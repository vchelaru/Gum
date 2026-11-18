using Gum.GueDeriving;
using Gum.RenderingLibrary;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// <see cref="Blend.Subtract"/> (#8263): rgb = max(dest - src * srcAlpha, 0), dest alpha kept. No
/// <see cref="SKBlendMode"/> does this (Difference is |dest - src|), so it goes through a custom blender.
/// </summary>
public class SubtractBlendTests
{
    [Theory]
    [InlineData(255, 255, 123)]
    [InlineData(82, 255, 0)]
    [InlineData(255, 128, 189)]
    public void Subtract_OverOpaqueGray_GivesClampedDifference(int behind, int sourceAlpha, int expected)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(32, 32));
        Gum.GumService.Default.Initialize(surface.Canvas, 32, 32);

        ContainerRuntime root = new() { Width = 32, Height = 32 };
        root.Children.Add(NewFill(new SKColor((byte)behind, (byte)behind, (byte)behind), Blend.Normal));
        root.Children.Add(NewFill(new SKColor(132, 132, 132, (byte)sourceAlpha), Blend.Subtract));
        Gum.GumService.Default.Root.Children.Add(root);
        Gum.GumService.Default.Draw();
        Gum.GumService.Default.Root.Children.Remove(root);

        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        SKColor pixel = bitmap.GetPixel(16, 16);

        string message = $"was {pixel.Red},{pixel.Green},{pixel.Blue},{pixel.Alpha}";
        ((int)pixel.Red).ShouldBeInRange(expected - 1, expected + 1, message);
        ((int)pixel.Green).ShouldBeInRange(expected - 1, expected + 1, message);
        ((int)pixel.Blue).ShouldBeInRange(expected - 1, expected + 1, message);
        pixel.Alpha.ShouldBe((byte)255, message);
    }

    // The container composites its premultiplied bake through the same blender, so it subtracts once.
    [Fact]
    public void Subtract_OnRenderTargetContainer_WithTranslucentContent_SubtractsOnce()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(32, 32));
        Gum.GumService.Default.Initialize(surface.Canvas, 32, 32);

        ContainerRuntime root = new() { Width = 32, Height = 32 };
        root.Children.Add(NewFill(new SKColor(255, 255, 255), Blend.Normal));
        ContainerRuntime container = new() { Width = 32, Height = 32, IsRenderTarget = true, Blend = Blend.Subtract };
        container.Children.Add(NewFill(new SKColor(132, 132, 132, 128), Blend.Normal));
        root.Children.Add(container);
        Gum.GumService.Default.Root.Children.Add(root);
        Gum.GumService.Default.Draw();
        Gum.GumService.Default.Root.Children.Remove(root);

        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        SKColor pixel = bitmap.GetPixel(16, 16);

        string message = $"was {pixel.Red},{pixel.Green},{pixel.Blue},{pixel.Alpha}";
        ((int)pixel.Red).ShouldBeInRange(188, 190, message);
        pixel.Alpha.ShouldBe((byte)255, message);
    }

    private static RectangleRuntime NewFill(SKColor color, Blend blend) => new()
    {
        Width = 32, Height = 32, IsFilled = true, StrokeWidth = 0, FillColor = color, Blend = blend,
    };
}
