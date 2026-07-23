using System;
using System.IO;
using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderer;

/// <summary>
/// Pixel-readback tests for the SkiaGum render-target post-process shader path (issue #3998). A
/// render-target container carrying a <see cref="ContainerRuntime.RenderTargetEffect"/> (or a
/// <see cref="ContainerRuntime.SourceShaderFile"/> that resolves into one) has that compiled SkSL
/// effect bound as the "inputImage" child for the composite draw, so it post-processes the whole
/// container. Mirrors <c>RaylibGum.Tests.Rendering.RenderTargetShaderTests</c> in shape, but reads
/// pixels directly off the top-level <see cref="SKSurface"/> (Skia's top-level surface is directly
/// readable, unlike raylib's bottom-up render texture).
/// </summary>
public class RenderTargetShaderTests
{
    // Contract SkSL fixture (issue #3998): a fixed "inputImage" child shader collapses the sampled
    // color to luma, so a solid-red composite reads back as R ≈ G ≈ B, which a straight blit never
    // would.
    private const string GrayscaleSksl = @"
uniform shader inputImage;
half4 main(float2 coord) {
    half4 texel = inputImage.eval(coord);
    half gray = dot(texel.rgb, half3(0.299, 0.587, 0.114));
    return half4(gray, gray, gray, texel.a);
}
";

    public RenderTargetShaderTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    private static SKRuntimeEffect CompileGrayscaleEffect()
    {
        SKRuntimeEffect effect = SKRuntimeEffect.CreateShader(GrayscaleSksl, out string errors);
        string.IsNullOrEmpty(errors).ShouldBeTrue(errors);
        return effect;
    }

    private static ContainerRuntime BuildRedRenderTarget()
    {
        ContainerRuntime renderTarget = new()
        {
            X = 4,
            Y = 4,
            Width = 40,
            Height = 40,
            IsRenderTarget = true,
        };
        renderTarget.Children.Add(new RectangleRuntime
        {
            Width = 30,
            Height = 30,
            IsFilled = true,
            FillColor = SKColors.Red,
        });
        return renderTarget;
    }

    [Fact]
    public void Draw_RenderTargetEffect_GraysTheCompositedPixels()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);

        ContainerRuntime renderTarget = BuildRedRenderTarget();
        GumService.Default.Root.Children.Add(renderTarget);

        // Control: no effect, the composited rectangle reads back clearly red.
        GumService.Default.Draw();
        using (SKImage withoutShaderImage = surface.Snapshot())
        using (SKBitmap withoutShaderBitmap = SKBitmap.FromImage(withoutShaderImage))
        {
            SKColor withoutShader = withoutShaderBitmap.GetPixel(19, 19);
            withoutShader.Red.ShouldBeGreaterThan((byte)200);
            ((int)withoutShader.Red - withoutShader.Green).ShouldBeGreaterThan(80);
        }

        using SKRuntimeEffect effect = CompileGrayscaleEffect();
        renderTarget.RenderTargetEffect = effect;
        GumService.Default.Draw();

        using SKImage withShaderImage = surface.Snapshot();
        using SKBitmap withShaderBitmap = SKBitmap.FromImage(withShaderImage);
        SKColor withShader = withShaderBitmap.GetPixel(19, 19);
        Math.Abs(withShader.Red - withShader.Green).ShouldBeLessThan(20);
        Math.Abs(withShader.Red - withShader.Blue).ShouldBeLessThan(20);
    }

    [Fact]
    public void Draw_SourceShaderFile_GraysTheCompositedPixels_WhenResolverRegistered()
    {
        string shaderPath = WriteTempShader(GrayscaleSksl);
        try
        {
            // Stand-in for a consumer's resolver: compile the referenced .sksl file's text.
            CustomSetPropertyOnRenderable.RenderTargetEffectResolver = path =>
                SKRuntimeEffect.CreateShader(File.ReadAllText(path), out _);

            using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
            GumService.Default.Initialize(surface.Canvas, 64, 64);

            ContainerRuntime renderTarget = BuildRedRenderTarget();
            GumService.Default.Root.Children.Add(renderTarget);

            // The .sksl reference resolves through the string property path into RenderTargetEffect.
            renderTarget.SourceShaderFile = shaderPath;
            GumService.Default.Draw();

            using SKImage image = surface.Snapshot();
            using SKBitmap bitmap = SKBitmap.FromImage(image);
            SKColor center = bitmap.GetPixel(19, 19);
            Math.Abs(center.Red - center.Green).ShouldBeLessThan(20);
            Math.Abs(center.Red - center.Blue).ShouldBeLessThan(20);
        }
        finally
        {
            CustomSetPropertyOnRenderable.RenderTargetEffectResolver = null;
            File.Delete(shaderPath);
        }
    }

    [Fact]
    public void Draw_SourceShaderFile_IsNoOp_WhenNoResolverRegistered()
    {
        // No resolver registered (clear any leakage from a prior test).
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = null;

        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);

        ContainerRuntime renderTarget = BuildRedRenderTarget();
        GumService.Default.Root.Children.Add(renderTarget);

        // With no resolver the assignment is a graceful no-op (no crash); the container renders
        // unshaded, so the composited pixel stays red.
        renderTarget.SourceShaderFile = "resources/DoesNotMatter.sksl";
        GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        SKColor center = bitmap.GetPixel(19, 19);
        center.Red.ShouldBeGreaterThan((byte)200);
        ((int)center.Red - center.Green).ShouldBeGreaterThan(80);
    }

    private static string WriteTempShader(string source)
    {
        string path = Path.Combine(Path.GetTempPath(), "GumSkiaShaderTest_" + Guid.NewGuid().ToString("N") + ".sksl");
        File.WriteAllText(path, source);
        return path;
    }
}
