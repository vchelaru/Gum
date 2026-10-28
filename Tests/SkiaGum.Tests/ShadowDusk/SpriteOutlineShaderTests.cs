using System;
using System.IO;
using Gum;
using Gum.GueDeriving;
using RenderingLibrary;
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
        pixel.ToString().ShouldBe(expected.ToString(), $"pixel at {x},{y}");
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
            container.SetShaderParameter("OutlineBlue", 255f);
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
            container.SetShaderParameter("OutlineBlue", 255f);
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
            container.SetShaderParameter("OutlineBlue", 255f);
        });

        ShouldBeOutline(bitmap, 38, 52, SKColors.Blue);
        ShouldBeEmpty(bitmap, 37, 52);
        ShouldBeOutline(bitmap, 65, 52, SKColors.Blue);
        ShouldBeEmpty(bitmap, 66, 52);
    }

    [Fact]
    public void Outline_WithNothingSet_DrawsAOnePixelBlackOutline()
    {
        using SKBitmap bitmap = DrawOutlinedSquare(48, 12, setParameters: null);

        ShouldBeOutline(bitmap, 24, 24, SKColors.Red);
        ShouldBeOutline(bitmap, 11, 24, SKColors.Black);
        ShouldBeOutline(bitmap, 11, 11, SKColors.Black);
        ShouldBeEmpty(bitmap, 10, 24);
    }

    [Fact]
    public void Outline_WithPixelSizeThree_IsThreeTargetPixelsPerArtPixel()
    {
        // The square is 24 target pixels, so 8 art pixels at a PixelSize of 3; Thickness 1 is 3 pixels.
        using SKBitmap bitmap = DrawOutlinedSquare(48, 12, container =>
        {
            container.SetShaderParameter("PixelSize", 3f);
            container.SetShaderParameter("OutlineGreen", 255f);
        });

        SKColor green = SKColors.Lime;
        ShouldBeOutline(bitmap, 11, 24, green);
        ShouldBeOutline(bitmap, 9, 24, green);
        ShouldBeEmpty(bitmap, 8, 24);
        ShouldBeOutline(bitmap, 9, 9, green);
        ShouldBeEmpty(bitmap, 8, 8);
    }

    [Fact]
    public void Outline_WithThicknessSetToZero_DrawsNoOutline_AtAnyPixelSize()
    {
        using SKBitmap bitmap = DrawOutlinedSquare(48, 12, container =>
        {
            container.SetShaderParameter("Thickness", 0f);
            container.SetShaderParameter("PixelSize", 3f);
        });

        ShouldBeOutline(bitmap, 24, 24, SKColors.Red);
        ShouldBeEmpty(bitmap, 11, 24);
        ShouldBeEmpty(bitmap, 11, 11);
        ShouldBeEmpty(bitmap, 9, 24);
    }

    // Two containers share the shader (A at x = 0, B at x = 64): what A set must not reach B.
    private static SKBitmap DrawTwoContainers(Action<ContainerRuntime> setA, Action<ContainerRuntime> setB)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(112, 48));
        GumService.Default.Initialize(surface.Canvas, 112, 48);
        foreach ((float x, Action<ContainerRuntime> set) in new[] { (0f, setA), (64f, setB) })
        {
            ContainerRuntime container = new() { X = x, Width = 48, Height = 48, IsRenderTarget = true };
            container.Children.Add(new RectangleRuntime
            {
                X = 12, Y = 12, Width = 24, Height = 24, IsFilled = true, StrokeWidth = 0, FillColor = SKColors.Red,
            });
            GumService.Default.Root.Children.Add(container);
            set(container);
            container.SourceShaderFile = ShippedShaderPath;
        }
        GumService.Default.Draw();
        using SKImage image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    [Fact]
    public void Outline_AfterAThicknessZeroContainer_AnUnsetContainerDrawsOnePixel()
    {
        using SKBitmap bitmap = DrawTwoContainers(a => a.SetShaderParameter("Thickness", 0f), b => { });

        ShouldBeEmpty(bitmap, 11, 24);
        ShouldBeOutline(bitmap, 75, 24, SKColors.Black);
        ShouldBeEmpty(bitmap, 74, 24);
    }

    [Fact]
    public void Outline_ASetTexelSize_WinsOverTheOneGumFillsIn()
    {
        using SKBitmap bitmap = DrawTwoContainers(a => a.SetShaderParameter("TexelSize", 2f / 48f, 2f / 48f), b => { });

        ShouldBeOutline(bitmap, 10, 24, SKColors.Black);
        ShouldBeEmpty(bitmap, 9, 24);
        ShouldBeOutline(bitmap, 75, 24, SKColors.Black);
        ShouldBeEmpty(bitmap, 74, 24);
    }

    [Fact]
    public void Outline_OfANegativeThickness_DrawsNoOutline()
    {
        using SKBitmap bitmap = DrawOutlinedSquare(48, 12, container => container.SetShaderParameter("Thickness", -2f));

        ShouldBeEmpty(bitmap, 11, 24);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-3f)]
    public void Outline_WithAPixelSizeBelowOne_CountsItAsOne(float pixelSize)
    {
        using SKBitmap bitmap = DrawOutlinedSquare(48, 12, container => container.SetShaderParameter("PixelSize", pixelSize));

        ShouldBeOutline(bitmap, 11, 24, SKColors.Black);
        ShouldBeEmpty(bitmap, 10, 24);
    }

    [Fact]
    public void Outline_OfASpriteTouchingTheRightAndBottomEdges_DrawsNothingOnTheOppositeEdges()
    {
        // The square covers 24 to 47, the far edges of the 48 pixel container.
        using SKBitmap bitmap = DrawOutlinedSquare(48, 24, setParameters: null);

        ShouldBeEmpty(bitmap, 0, 36);
        ShouldBeEmpty(bitmap, 36, 0);
        ShouldBeOutline(bitmap, 23, 36, SKColors.Black);
    }

    // The outline is measured in canvas pixels, so it grows and shrinks with the camera zoom exactly
    // like the content does: Thickness * PixelSize canvas pixels is that many times zoom device pixels.
    [Theory]
    [InlineData(1f, 1f, 1f)]
    [InlineData(2f, 1f, 1f)]
    [InlineData(0.5f, 1f, 1f)]
    [InlineData(1.5f, 1f, 1f)]
    [InlineData(2f, 2f, 1f)]
    [InlineData(2f, 1f, 3f)]
    public void Outline_ScalesWithCameraZoom(float zoom, float thickness, float pixelSize)
    {
        // A 24x24 canvas pixel square at 12,12 in a 48x48 canvas pixel container, drawn at the zoom.
        int deviceSize = (int)(48 * zoom);
        using SKSurface surface = SKSurface.Create(new SKImageInfo(deviceSize, deviceSize));
        GumService.Default.Initialize(surface.Canvas, deviceSize, deviceSize);
        SystemManagers.Default.Renderer.Camera.Zoom = zoom;
        ContainerRuntime container = new() { Width = 48, Height = 48, IsRenderTarget = true };
        container.Children.Add(new RectangleRuntime
        {
            X = 12, Y = 12, Width = 24, Height = 24, IsFilled = true, StrokeWidth = 0, FillColor = SKColors.Red,
        });
        GumService.Default.Root.Children.Add(container);
        container.SetShaderParameter("Thickness", thickness);
        container.SetShaderParameter("PixelSize", pixelSize);
        container.SourceShaderFile = ShippedShaderPath;
        GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);

        // Count the outline (black) pixels to the left of the square on its middle row.
        int row = (int)(24 * zoom);
        int squareLeft = (int)(12 * zoom);
        int outlineWidth = 0;
        for (int x = squareLeft - 1; x >= 0 && bitmap.GetPixel(x, row).Alpha > 127; x--)
        {
            outlineWidth++;
        }

        float expected = thickness * pixelSize * zoom;
        outlineWidth.ShouldBeInRange((int)System.Math.Floor(expected), (int)System.Math.Ceiling(expected));
    }

    // Writes a picture of the outline for a person to look at; only runs when GUM_SCREENSHOT_DIR is set.
    [Fact]
    public void Outline_RendersPixelArtPicture_WhenAskedFor()
    {
        string? directory = Environment.GetEnvironmentVariable("GUM_SCREENSHOT_DIR");
        if (string.IsNullOrEmpty(directory))
        {
            return;
        }

        string[] art =
        {
            "..XX..",
            ".XXXX.",
            "XXXXXX",
            "XXXXXX",
            ".XXXX.",
            "..XX..",
        };
        (float thickness, int pixelSize, bool outlined)[] variants =
        {
            (1, 1, false), (1, 1, true), (2, 1, true), (1, 3, true), (2, 3, true),
        };
        const int scaleUp = 6;
        int cell = (art.Length + 6) * 3;
        using SKBitmap sheet = new SKBitmap(cell * scaleUp * variants.Length, cell * scaleUp);
        using SKCanvas sheetCanvas = new SKCanvas(sheet);
        sheetCanvas.Clear(SKColors.White);

        for (int variant = 0; variant < variants.Length; variant++)
        {
            (float thickness, int pixelSize, bool outlined) = variants[variant];
            int size = (art.Length + 6) * pixelSize;
            using SKSurface surface = SKSurface.Create(new SKImageInfo(size, size));
            GumService.Default.Initialize(surface.Canvas, size, size);
            ContainerRuntime container = new() { Width = size, Height = size, IsRenderTarget = true };
            for (int row = 0; row < art.Length; row++)
            {
                for (int column = 0; column < art[row].Length; column++)
                {
                    if (art[row][column] == 'X')
                    {
                        container.Children.Add(new RectangleRuntime
                        {
                            X = (column + 3) * pixelSize,
                            Y = (row + 3) * pixelSize,
                            Width = pixelSize,
                            Height = pixelSize,
                            IsFilled = true,
                            StrokeWidth = 0,
                            FillColor = new SKColor(220, 60, 60),
                        });
                    }
                }
            }
            GumService.Default.Root.Children.Add(container);
            if (outlined)
            {
                container.SetShaderParameter("Thickness", thickness);
                container.SetShaderParameter("PixelSize", (float)pixelSize);
                container.SourceShaderFile = ShippedShaderPath;
            }
            GumService.Default.Draw();

            using SKImage image = surface.Snapshot();
            SKRect destination = new SKRect(variant * cell * scaleUp, 0, (variant + 1) * cell * scaleUp, cell * scaleUp);
            sheetCanvas.DrawImage(image, destination, new SKSamplingOptions(SKFilterMode.Nearest));
        }

        using SKImage sheetImage = SKImage.FromBitmap(sheet);
        using SKData data = sheetImage.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(Path.Combine(directory, "6984-outline.png"), data.ToArray());
    }
}
