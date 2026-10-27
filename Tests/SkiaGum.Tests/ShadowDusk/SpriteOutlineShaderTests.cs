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
/// Runs the Outline.slang file the samples ship through ShadowDusk and SkiaGum, so a break in the
/// shader, the conversion or the parameter plumbing shows up here and not only by eye in a sample.
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

    // The csproj copies the sample's Outline.slang next to the test assembly.
    private static string SampleShaderPath => Path.Combine(AppContext.BaseDirectory, "Outline.slang");

    // A 24x24 red square in the middle of a 48x48 render target, outlined in blue.
    private static SKBitmap DrawOutlinedSquare(float thickness)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(48, 48));
        GumService.Default.Initialize(surface.Canvas, 48, 48);

        ContainerRuntime container = new()
        {
            Width = 48,
            Height = 48,
            IsRenderTarget = true,
        };
        container.Children.Add(new RectangleRuntime
        {
            X = 12,
            Y = 12,
            Width = 24,
            Height = 24,
            IsFilled = true,
            FillColor = SKColors.Red,
        });
        GumService.Default.Root.Children.Add(container);

        container.SetShaderParameter("TexelSize", 1f / 48f, 1f / 48f);
        container.SetShaderParameter("Thickness", thickness);
        container.SetShaderParameter("OutlineColor", 0f, 0f, 1f, 1f);
        container.SourceShaderFile = SampleShaderPath;

        GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    [Fact]
    public void Outline_DrawsTheColorJustOutsideTheContent_AndLeavesTheContentAlone()
    {
        using SKBitmap bitmap = DrawOutlinedSquare(thickness: 2f);

        // Inside the square is still red.
        SKColor inside = bitmap.GetPixel(24, 24);
        inside.Red.ShouldBeGreaterThan((byte)200);
        inside.Blue.ShouldBeLessThan((byte)60);

        // One pixel outside the left edge (the square starts at x = 12) is the blue outline.
        SKColor outline = bitmap.GetPixel(11, 24);
        outline.Blue.ShouldBeGreaterThan((byte)200);
        outline.Red.ShouldBeLessThan((byte)60);
        outline.Alpha.ShouldBeGreaterThan((byte)200);
    }

    [Fact]
    public void Outline_StopsAtThickness()
    {
        using SKBitmap bitmap = DrawOutlinedSquare(thickness: 2f);

        // Two pixels out is still outline, three pixels out is not.
        bitmap.GetPixel(10, 24).Alpha.ShouldBeGreaterThan((byte)200);
        bitmap.GetPixel(9, 24).Alpha.ShouldBe((byte)0);
    }
}
