using System;
using System.IO;
using Gum;
using Gum.GueDeriving;
using Shouldly;
using SkiaGum;
using SkiaGum.Content;
using SkiaSharp;

namespace SkiaGum.Tests.ShadowDusk;

/// <summary>
/// Runs the Outline.slang file the tool ships through ShadowDusk and SkiaGum, so a break in the
/// shader, the conversion or the parameter plumbing shows up here and not only by eye in a sample.
/// The outline has to be pixel exact: Thickness N is exactly N whole pixels on every side.
/// </summary>
public class SpriteOutlineShaderTests : IDisposable
{
    public SpriteOutlineShaderTests()
    {
        Gum.Wireframe.GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        GumService.Default.UseShadowDusk();
    }

    public void Dispose()
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = SkSlRuntimeEffectLoader.Load;
    }

    // The csproj copies the shipped Outline.slang next to the test assembly.
    private static string ShippedShaderPath => Path.Combine(AppContext.BaseDirectory, "Outline.slang");

    // A 24x24 red square whose top left is at (squareLeft, squareLeft) in a render target of
    // targetSize, with the parameters the caller sets (none means the shader's defaults).
    private static SKBitmap DrawOutlinedSquare(int targetSize, int squareLeft, Action<ContainerRuntime>? setParameters)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(targetSize, targetSize));
        GumService.Default.Initialize(surface.Canvas, targetSize, targetSize);

        ContainerRuntime container = new()
        {
            Width = targetSize,
            Height = targetSize,
            IsRenderTarget = true,
        };
        container.Children.Add(new RectangleRuntime
        {
            X = squareLeft,
            Y = squareLeft,
            Width = 24,
            Height = 24,
            IsFilled = true,
            FillColor = SKColors.Red,
        });
        GumService.Default.Root.Children.Add(container);

        setParameters?.Invoke(container);
        container.SourceShaderFile = ShippedShaderPath;

        GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    private static void ShouldBeOutline(SKBitmap bitmap, int x, int y, SKColor expected)
    {
        SKColor pixel = bitmap.GetPixel(x, y);
        pixel.Alpha.ShouldBe((byte)255, $"alpha at {x},{y}");
        pixel.Red.ShouldBe(expected.Red, $"red at {x},{y}");
        pixel.Green.ShouldBe(expected.Green, $"green at {x},{y}");
        pixel.Blue.ShouldBe(expected.Blue, $"blue at {x},{y}");
    }

    private static void ShouldBeEmpty(SKBitmap bitmap, int x, int y) =>
        bitmap.GetPixel(x, y).Alpha.ShouldBe((byte)0, $"alpha at {x},{y}");

    [Fact]
    public void Outline_OfThicknessOne_IsOneWholePixelOnEverySide_IncludingTheCorners()
    {
        using SKBitmap bitmap = DrawOutlinedSquare(48, 12, container =>
        {
            container.SetShaderParameter("Thickness", 1f);
            container.SetShaderParameter("OutlineColor", 0f, 0f, 1f, 1f);
        });

        SKColor blue = SKColors.Blue;
        ShouldBeOutline(bitmap, 11, 24, blue); // left
        ShouldBeOutline(bitmap, 36, 24, blue); // right (the square covers 12 to 35)
        ShouldBeOutline(bitmap, 24, 11, blue); // top
        ShouldBeOutline(bitmap, 24, 36, blue); // bottom
        ShouldBeOutline(bitmap, 11, 11, blue); // diagonal corner
        ShouldBeOutline(bitmap, 36, 36, blue); // diagonal corner
        ShouldBeEmpty(bitmap, 10, 24);
        ShouldBeEmpty(bitmap, 24, 10);
        ShouldBeEmpty(bitmap, 10, 10);
        ShouldBeEmpty(bitmap, 37, 24);
    }

    [Fact]
    public void Outline_OfThicknessTwo_IsTwoWholePixelsOnEverySide()
    {
        using SKBitmap bitmap = DrawOutlinedSquare(48, 12, container =>
        {
            container.SetShaderParameter("Thickness", 2f);
            container.SetShaderParameter("OutlineColor", 0f, 0f, 1f, 1f);
        });

        ShouldBeOutline(bitmap, 11, 24, SKColors.Blue);
        ShouldBeOutline(bitmap, 10, 24, SKColors.Blue);
        ShouldBeEmpty(bitmap, 9, 24);
        ShouldBeOutline(bitmap, 37, 24, SKColors.Blue);
        ShouldBeEmpty(bitmap, 38, 24);
        ShouldBeOutline(bitmap, 24, 10, SKColors.Blue);
        ShouldBeEmpty(bitmap, 24, 9);
        ShouldBeOutline(bitmap, 24, 37, SKColors.Blue);
        ShouldBeEmpty(bitmap, 24, 38);
        ShouldBeOutline(bitmap, 10, 10, SKColors.Blue); // the corner
        ShouldBeEmpty(bitmap, 9, 9);
    }

    [Fact]
    public void Outline_NeedsNoTexelSize_WhateverTheRenderTargetSize()
    {
        // A 96x96 target: a hand-set texel size would have to change with it, the shader reads it.
        using SKBitmap bitmap = DrawOutlinedSquare(96, 40, container =>
        {
            container.SetShaderParameter("Thickness", 2f);
            container.SetShaderParameter("OutlineColor", 0f, 0f, 1f, 1f);
        });

        ShouldBeOutline(bitmap, 38, 52, SKColors.Blue);
        ShouldBeEmpty(bitmap, 37, 52);
        ShouldBeOutline(bitmap, 65, 52, SKColors.Blue);
        ShouldBeEmpty(bitmap, 66, 52);
    }

    [Fact]
    public void Outline_WithHalfAlphaColor_DrawsAHalfCoveredOutline()
    {
        using SKBitmap bitmap = DrawOutlinedSquare(48, 12, container =>
        {
            container.SetShaderParameter("Thickness", 1f);
            container.SetShaderParameter("OutlineColor", 0f, 0f, 1f, 0.5f);
        });

        SKColor pixel = bitmap.GetPixel(11, 24);
        pixel.Alpha.ShouldBeInRange((byte)126, (byte)129);
    }
}
