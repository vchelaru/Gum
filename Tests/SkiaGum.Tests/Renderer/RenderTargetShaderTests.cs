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

    // Regression for #4001: the real SilkNetGum sample's RT Shader screen bakes a SpriteRuntime
    // (a real texture) into a render target, not a solid-fill RectangleRuntime. The existing
    // BuildRedRenderTarget tests above position the container near the origin (X=4, Y=4) with a
    // uniform-colored fill, which happened to mask a coordinate-mapping bug: CompositeRenderTarget
    // binds the baked image as the effect's "inputImage" child via a plain `image.ToShader()` (no
    // local matrix), so the shader samples using raw absolute canvas coordinates instead of
    // translating into the baked image's own local (0,0) space. At a small offset the sampled
    // pixel merely landed a few pixels off but still inside the same solid-colored fill, so the
    // test still passed. A two-color, fully-opaque texture at a realistic offset exposes it: a
    // pixel that should read back as the texture's left half instead reads the clamped right edge.
    [Fact]
    public void Draw_RenderTargetEffect_WithSpriteTexture_SamplesBakedImageAtCorrectOffset()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(256, 256));
        GumService.Default.Initialize(surface.Canvas, 256, 256);

        using SKBitmap texture = BuildTwoColorTexture();

        ContainerRuntime renderTarget = new()
        {
            X = 120,
            Y = 90,
            Width = 40,
            Height = 40,
            IsRenderTarget = true,
        };
        renderTarget.Children.Add(new SpriteRuntime
        {
            Texture = texture,
            WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            Width = 40,
            Height = 40,
        });
        GumService.Default.Root.Children.Add(renderTarget);

        using SKRuntimeEffect effect = CompileGrayscaleEffect();
        renderTarget.RenderTargetEffect = effect;
        GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);

        // Absolute (130, 100) is local (10, 10) inside the render target -- solidly within the
        // texture's red left half. Correctly mapped, this reads back as red's grayscale luma
        // (~76); a coordinate-mapping bug instead reads the clamped-to-edge blue half (~29).
        SKColor pixel = bitmap.GetPixel(130, 100);
        Math.Abs(pixel.Red - pixel.Green).ShouldBeLessThan(20);
        Math.Abs(pixel.Red - pixel.Blue).ShouldBeLessThan(20);
        pixel.Red.ShouldBeGreaterThan((byte)60);
    }

    // Regression for #4001: the real SilkNetGum sample loads a .gumx project, so
    // FileManager.RelativeDirectory ends up pointing at the project's own directory (e.g.
    // ".../Content/GumProject/") rather than the app's working directory. A bare SourceShaderFile
    // path ("resources/Grayscale.sksl") that resolves fine relative to the working directory does
    // NOT resolve under that project directory, so AssignSourceShaderFileOnContainer's
    // RelativeDirectory-prefixed candidate misses -- and it never fell back to the original path,
    // so the resolver reported "not found" and the container silently rendered unshaded (the same
    // symptom as no resolver being registered at all). The existing
    // Draw_SourceShaderFile_GraysTheCompositedPixels_WhenResolverRegistered test uses an absolute
    // temp-file path, which bypasses the RelativeDirectory-prefixing branch entirely and so never
    // exercised this gap.
    [Fact]
    public void Draw_SourceShaderFile_FallsBackToOriginalPath_WhenRelativeDirectoryPrefixDoesNotExist()
    {
        string workingDirectory = Directory.GetCurrentDirectory();
        string shaderSubfolder = "ShaderFallbackTest_" + Guid.NewGuid().ToString("N");
        string shaderAbsolutePath = Path.Combine(workingDirectory, shaderSubfolder, "Grayscale.sksl");
        string bareRelativeValue = shaderSubfolder + "/Grayscale.sksl";

        // A project directory that does NOT contain the shader -- mirrors RelativeDirectory
        // pointing at the loaded .gumx's folder, distinct from the working directory the bare
        // path actually resolves against.
        string wrongRelativeDirectory = Path.Combine(workingDirectory, "ProjectTest_" + Guid.NewGuid().ToString("N"), "GumProject")
            + Path.DirectorySeparatorChar;

        string previousRelativeDirectory = ToolsUtilities.FileManager.RelativeDirectory;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(shaderAbsolutePath)!);
            File.WriteAllText(shaderAbsolutePath, GrayscaleSksl);

            ToolsUtilities.FileManager.RelativeDirectory = wrongRelativeDirectory;
            CustomSetPropertyOnRenderable.RenderTargetEffectResolver =
                path => File.Exists(path) ? SKRuntimeEffect.CreateShader(File.ReadAllText(path), out _) : null;

            using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
            GumService.Default.Initialize(surface.Canvas, 64, 64);

            ContainerRuntime renderTarget = BuildRedRenderTarget();
            GumService.Default.Root.Children.Add(renderTarget);

            renderTarget.SourceShaderFile = bareRelativeValue;
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
            ToolsUtilities.FileManager.RelativeDirectory = previousRelativeDirectory;
            File.Delete(shaderAbsolutePath);
            Directory.Delete(Path.GetDirectoryName(shaderAbsolutePath)!);
        }
    }

    private static SKBitmap BuildTwoColorTexture()
    {
        SKBitmap bitmap = new(40, 40);
        using (SKCanvas canvas = new(bitmap))
        {
            canvas.Clear(SKColors.Red);
            canvas.DrawRect(new SKRect(20, 0, 40, 40), new SKPaint { Color = SKColors.Blue });
        }
        return bitmap;
    }

    [Fact]
    public void SourceShaderFile_CompilesOnce_WhenReferencedByMultipleContainers()
    {
        string shaderPath = WriteTempShader(GrayscaleSksl);
        int invocationCount = 0;
        try
        {
            CustomSetPropertyOnRenderable.RenderTargetEffectResolver = path =>
            {
                invocationCount++;
                return SKRuntimeEffect.CreateShader(File.ReadAllText(path), out _);
            };

            ContainerRuntime first = new() { IsRenderTarget = true };
            first.SourceShaderFile = shaderPath;

            ContainerRuntime second = new() { IsRenderTarget = true };
            second.SourceShaderFile = shaderPath;

            // The second container referencing the same .sksl must hit the LoaderManager cache.
            invocationCount.ShouldBe(1);
        }
        finally
        {
            CustomSetPropertyOnRenderable.RenderTargetEffectResolver = null;
            File.Delete(shaderPath);
        }
    }

    [Fact]
    public void SourceShaderFile_RaisesPropertyAssignmentError_WhenResolverFailsAndConsumingSilently()
    {
        MissingFileBehavior previousBehavior = GraphicalUiElement.MissingFileBehavior;
        string? reportedError = null;
        Action<string> handler = message => reportedError = message;
        try
        {
            GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ConsumeSilently;
            CustomSetPropertyOnRenderable.PropertyAssignmentError += handler;
            // Resolver registered but unable to produce an effect (missing .sksl / compile failure).
            CustomSetPropertyOnRenderable.RenderTargetEffectResolver = _ => null;

            ContainerRuntime container = new() { IsRenderTarget = true };

            // ConsumeSilently must not throw; it reports through PropertyAssignmentError instead.
            container.SourceShaderFile = "resources/Missing.sksl";

            reportedError.ShouldNotBeNull();
        }
        finally
        {
            CustomSetPropertyOnRenderable.PropertyAssignmentError -= handler;
            CustomSetPropertyOnRenderable.RenderTargetEffectResolver = null;
            GraphicalUiElement.MissingFileBehavior = previousBehavior;
        }
    }

    [Fact]
    public void SourceShaderFile_Throws_WhenResolverFailsAndMissingFileBehaviorIsThrow()
    {
        MissingFileBehavior previousBehavior = GraphicalUiElement.MissingFileBehavior;
        try
        {
            GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ThrowException;
            CustomSetPropertyOnRenderable.RenderTargetEffectResolver = _ => null;

            ContainerRuntime container = new() { IsRenderTarget = true };

            Should.Throw<FileNotFoundException>(() => container.SourceShaderFile = "resources/Missing.sksl");
        }
        finally
        {
            CustomSetPropertyOnRenderable.RenderTargetEffectResolver = null;
            GraphicalUiElement.MissingFileBehavior = previousBehavior;
        }
    }

    private static string WriteTempShader(string source)
    {
        string path = Path.Combine(Path.GetTempPath(), "GumSkiaShaderTest_" + Guid.NewGuid().ToString("N") + ".sksl");
        File.WriteAllText(path, source);
        return path;
    }
}
