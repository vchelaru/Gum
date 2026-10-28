using System;
using System.IO;
using Gum.GueDeriving;
using Gum.Wireframe;
using System.Linq;
using Raylib_cs;
using RaylibGum.Renderables;
using ShadowDusk.Compiler.Raylib;
using ShadowDusk.Core;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Pixel-readback tests for the raylib render-target post-process shader path (issue #3465). A
/// render-target container carrying a <see cref="ContainerRuntime.RenderTargetEffect"/> (or a
/// <see cref="ContainerRuntime.SourceShaderFile"/> that resolves into one) has that GLSL shader
/// bound for the single composite blit, so it post-processes the whole container. Each test nests
/// the shaded container inside a readable outer render target and samples the outer's baked texture
/// — the same deterministic-surface trick the sibling <see cref="RenderTargetTests"/> use — so the
/// shader's effect on the composite is observable without screen readback.
/// </summary>
public class RenderTargetShaderTests : BaseTestClass
{
    // A grayscale fragment shader in raylib's default-shader interface (texture0 / colDiffuse /
    // fragColor / fragTexCoord). Collapses the sampled RGB to luma so a solid-red composite reads
    // back as R ≈ G ≈ B, which a straight blit never would.
    private const string GrayscaleFragmentShader = @"#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
out vec4 finalColor;
uniform sampler2D texture0;
uniform vec4 colDiffuse;
void main()
{
    vec4 texel = texture(texture0, fragTexCoord);
    float gray = dot(texel.rgb, vec3(0.299, 0.587, 0.114));
    finalColor = vec4(gray, gray, gray, texel.a) * fragColor * colDiffuse;
}
";

    private static void DrawOnce()
    {
        BeginDrawing();
        GumService.Default.Draw();
        EndDrawing();
    }

    // Reads a pixel in top-left-origin draw space. A render texture is stored bottom-up in GL, so
    // the row index is flipped (same idiom as RenderTargetTests).
    private static Color ReadRenderTargetCenter(RenderTexture2D renderTexture)
    {
        Image image = LoadImageFromTexture(renderTexture.Texture);
        try
        {
            int x = renderTexture.Texture.Width / 2;
            int y = renderTexture.Texture.Height / 2;
            return GetImageColor(image, x, renderTexture.Texture.Height - 1 - y);
        }
        finally
        {
            UnloadImage(image);
        }
    }

    // Builds a solid-red inner render target nested inside a readable outer render target and
    // returns both. Sampling the outer's center reads whatever the inner composited into it.
    private static (ContainerRuntime outer, ContainerRuntime inner) BuildNestedRedRenderTarget()
    {
        ContainerRuntime outer = new();
        outer.X = 0;
        outer.Y = 0;
        outer.Width = 100;
        outer.Height = 100;
        outer.IsRenderTarget = true;

        ContainerRuntime inner = new();
        inner.Width = 100;
        inner.Height = 100;
        inner.IsRenderTarget = true;

#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete; simplest solid fill without a shape dependency.
        ColoredRectangleRuntime red = new();
#pragma warning restore CS0618
        red.Width = 100;
        red.Height = 100;
        red.Color = new Color((byte)255, (byte)0, (byte)0, (byte)255);
        inner.Children.Add(red);
        outer.Children.Add(inner);

        return (outer, inner);
    }

    [Fact]
    public void Draw_RenderTargetEffect_GraysTheCompositedPixels()
    {
        (ContainerRuntime outer, ContainerRuntime inner) = BuildNestedRedRenderTarget();

        GumService.Default.Root.Children.Add(outer);
        GumService.Default.Root.UpdateLayout();

        Shader grayscale = LoadShaderFromMemory(null, GrayscaleFragmentShader);
        try
        {
            // Control: no shader, the composited inner reads back clearly red.
            DrawOnce();
            Color withoutShader = ReadRenderTargetCenter(Renderer.Self.TryGetBakedRenderTargetFor(outer)!.Value);
            withoutShader.R.ShouldBeGreaterThan((byte)200);
            ((int)withoutShader.R - withoutShader.G).ShouldBeGreaterThan(80);

            // With the grayscale shader bound for the composite blit, the channels collapse.
            inner.RenderTargetEffect = grayscale;
            DrawOnce();
            Color withShader = ReadRenderTargetCenter(Renderer.Self.TryGetBakedRenderTargetFor(outer)!.Value);
            Math.Abs(withShader.R - withShader.G).ShouldBeLessThan(25);
            Math.Abs(withShader.R - withShader.B).ShouldBeLessThan(25);
        }
        finally
        {
            UnloadShader(grayscale);
            GumService.Default.Root.Children.Clear();
        }
    }

    // Two containers share one shader (as SourceShaderFile does through the cache) and set different
    // values for the same uniform, so each blit must apply its own container's value.
    [Fact]
    public void Draw_ShaderParameter_AppliesEachContainersOwnValue()
    {
        const string strengthShader = @"#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
out vec4 finalColor;
uniform sampler2D texture0;
uniform vec4 colDiffuse;
uniform float strength;
void main()
{
    vec4 texel = texture(texture0, fragTexCoord);
    finalColor = vec4(strength * texel.a, 0.0, 0.0, texel.a) * fragColor * colDiffuse;
}
";
        ContainerRuntime outer = new();
        outer.Width = 200;
        outer.Height = 100;
        outer.IsRenderTarget = true;

        ContainerRuntime dim = BuildRedInner(0);
        ContainerRuntime bright = BuildRedInner(100);
        outer.Children.Add(dim);
        outer.Children.Add(bright);
        GumService.Default.Root.Children.Add(outer);
        GumService.Default.Root.UpdateLayout();

        Shader shared = LoadShaderFromMemory(null, strengthShader);
        try
        {
            dim.RenderTargetEffect = shared;
            dim.SetShaderParameter("strength", 0.25f);
            bright.RenderTargetEffect = shared;
            bright.SetShaderParameter("strength", 0.75f);
            DrawOnce();

            RenderTexture2D baked = Renderer.Self.TryGetBakedRenderTargetFor(outer)!.Value;
            Math.Abs(ReadRenderTargetPixel(baked, 50, 50).R - 64).ShouldBeLessThan(12);
            Math.Abs(ReadRenderTargetPixel(baked, 150, 50).R - 191).ShouldBeLessThan(12);
        }
        finally
        {
            UnloadShader(shared);
            GumService.Default.Root.Children.Clear();
        }
    }

    // Runs the Outline.slang file the samples ship through ShadowDusk's raylib converter, so a break
    // in the shader or the conversion shows up here and not only by eye in a sample.
    [Fact]
    public void Draw_OutlineSampleSlang_DrawsTheColorJustOutsideTheContent_AndStopsAtThickness()
    {
        Result<RaylibShader, ShaderError[]> conversion = RaylibConverter.ConvertSlang(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Outline.slang")), new RaylibConvertOptions());
        conversion.IsSuccess.ShouldBeTrue(
            conversion.IsFailure ? string.Join("\n", conversion.Error.Select(e => e.Message)) : "");

        // A 24x24 red square in the middle of a 48x48 render target, outlined in blue.
        ContainerRuntime outer = new();
        outer.Width = 48;
        outer.Height = 48;
        outer.IsRenderTarget = true;

        ContainerRuntime inner = new();
        inner.Width = 48;
        inner.Height = 48;
        inner.IsRenderTarget = true;

#pragma warning disable CS0618
        ColoredRectangleRuntime square = new();
#pragma warning restore CS0618
        square.X = 12;
        square.Y = 12;
        square.Width = 24;
        square.Height = 24;
        square.Color = new Color((byte)255, (byte)0, (byte)0, (byte)255);
        inner.Children.Add(square);
        outer.Children.Add(inner);
        GumService.Default.Root.Children.Add(outer);
        GumService.Default.Root.UpdateLayout();

        Shader outline = LoadShaderFromMemory(null, conversion.Value.FragmentShader);
        try
        {
            inner.RenderTargetEffect = outline;
            inner.SetShaderParameter("Thickness", 2f);
            inner.SetShaderParameter("OutlineBlue", 255f);
            DrawOnce();

            RenderTexture2D baked = Renderer.Self.TryGetBakedRenderTargetFor(outer)!.Value;

            // Inside the square is still red.
            Color inside = ReadRenderTargetPixel(baked, 24, 24);
            inside.R.ShouldBeGreaterThan((byte)200);
            inside.B.ShouldBeLessThan((byte)60);

            // One and two pixels outside the left edge (the square starts at x = 12) are the blue
            // outline, three pixels out is not.
            Color oneOut = ReadRenderTargetPixel(baked, 11, 24);
            oneOut.B.ShouldBeGreaterThan((byte)200);
            oneOut.R.ShouldBeLessThan((byte)60);
            ReadRenderTargetPixel(baked, 10, 24).B.ShouldBeGreaterThan((byte)200);
            ReadRenderTargetPixel(baked, 9, 24).A.ShouldBe((byte)0);
        }
        finally
        {
            UnloadShader(outline);
            GumService.Default.Root.Children.Clear();
        }
    }

    // Runs the Outline.slang file the samples ship through ShadowDusk's raylib converter, so a break
    // in the shader or the conversion shows up here and not only by eye in a sample.
    [Fact]
    public void Draw_OutlineSampleSlang_WithNothingSet_DrawsAOnePixelBlackOutline()
    {
        Result<RaylibShader, ShaderError[]> conversion = RaylibConverter.ConvertSlang(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Outline.slang")), new RaylibConvertOptions());
        conversion.IsSuccess.ShouldBeTrue(
            conversion.IsFailure ? string.Join("\n", conversion.Error.Select(e => e.Message)) : "");

        // A 24x24 red square in the middle of a 48x48 render target, outlined in blue.
        ContainerRuntime outer = new();
        outer.Width = 48;
        outer.Height = 48;
        outer.IsRenderTarget = true;

        ContainerRuntime inner = new();
        inner.Width = 48;
        inner.Height = 48;
        inner.IsRenderTarget = true;

#pragma warning disable CS0618
        ColoredRectangleRuntime square = new();
#pragma warning restore CS0618
        square.X = 12;
        square.Y = 12;
        square.Width = 24;
        square.Height = 24;
        square.Color = new Color((byte)255, (byte)0, (byte)0, (byte)255);
        inner.Children.Add(square);
        outer.Children.Add(inner);
        Gum.GumService.Default.Root.Children.Add(outer);
        Gum.GumService.Default.Root.UpdateLayout();

        Shader outline = LoadShaderFromMemory(null, conversion.Value.FragmentShader);
        try
        {
            inner.RenderTargetEffect = outline;
            DrawOnce();

            RenderTexture2D baked = Renderer.Self.TryGetBakedRenderTargetFor(outer)!.Value;

            // Inside the square is still red.
            Color inside = ReadRenderTargetPixel(baked, 24, 24);
            inside.R.ShouldBeGreaterThan((byte)200);
            inside.B.ShouldBeLessThan((byte)60);

            // One pixel outside the left edge (the square starts at x = 12), diagonal corner included,
            // is black; two pixels out is empty.
            Color oneOut = ReadRenderTargetPixel(baked, 11, 24);
            oneOut.A.ShouldBeGreaterThan((byte)200);
            oneOut.B.ShouldBeLessThan((byte)30);
            oneOut.R.ShouldBeLessThan((byte)30);
            ReadRenderTargetPixel(baked, 11, 11).A.ShouldBeGreaterThan((byte)200);
            ReadRenderTargetPixel(baked, 10, 24).A.ShouldBe((byte)0);
        }
        finally
        {
            UnloadShader(outline);
            Gum.GumService.Default.Root.Children.Clear();
        }
    }

    // Bakes a 24x24 red square at 12,12 in a 48x48 render target outlined by the shipped shader and
    // hands the baked texture's pixel reader to the check, at the given camera zoom and Alpha.
    private static void WithOutlinedSquare(
        System.Action<ContainerRuntime> setParameters, float zoom, int alpha, System.Action<System.Func<int, int, Color>> check)
    {
        Result<RaylibShader, ShaderError[]> conversion = RaylibConverter.ConvertSlang(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Outline.slang")), new RaylibConvertOptions());
        conversion.IsSuccess.ShouldBeTrue(
            conversion.IsFailure ? string.Join(", ", conversion.Error.Select(e => e.Message)) : "");

        ContainerRuntime outer = new() { Width = 48, Height = 48, IsRenderTarget = true };
        ContainerRuntime inner = new() { Width = 48, Height = 48, IsRenderTarget = true, Alpha = alpha };
#pragma warning disable CS0618
        ColoredRectangleRuntime square = new();
#pragma warning restore CS0618
        square.X = 12;
        square.Y = 12;
        square.Width = 24;
        square.Height = 24;
        square.Color = new Color((byte)255, (byte)0, (byte)0, (byte)255);
        inner.Children.Add(square);
        outer.Children.Add(inner);
        Gum.GumService.Default.Root.Children.Add(outer);
        Gum.GumService.Default.Root.UpdateLayout();

        Shader outline = LoadShaderFromMemory(null, conversion.Value.FragmentShader);
        global::RenderingLibrary.SystemManagers.Default.Renderer.Camera.Zoom = zoom;
        try
        {
            inner.RenderTargetEffect = outline;
            setParameters(inner);
            DrawOnce();

            RenderTexture2D baked = Renderer.Self.TryGetBakedRenderTargetFor(outer)!.Value;
            check((x, y) => ReadRenderTargetPixel(baked, x, y));
        }
        finally
        {
            global::RenderingLibrary.SystemManagers.Default.Renderer.Camera.Zoom = 1f;
            UnloadShader(outline);
            Gum.GumService.Default.Root.Children.Clear();
        }
    }

    [Theory]
    [InlineData(1f, 1f, 1f)]
    [InlineData(2f, 1f, 1f)]
    [InlineData(0.5f, 1f, 1f)]
    [InlineData(1.5f, 1f, 1f)]
    [InlineData(2f, 2f, 1f)]
    [InlineData(2f, 1f, 3f)]
    public void Draw_OutlineSampleSlang_ScalesWithCameraZoom(float zoom, float thickness, float pixelSize)
    {
        WithOutlinedSquare(container =>
        {
            container.SetShaderParameter("Thickness", thickness);
            container.SetShaderParameter("PixelSize", pixelSize);
        }, zoom, 255, pixel =>
        {
            int row = (int)(24 * zoom);
            int squareLeft = (int)(12 * zoom);
            int outlineWidth = 0;
            for (int x = squareLeft - 1; x >= 0 && pixel(x, row).A > 127; x--)
            {
                outlineWidth++;
            }

            float expected = thickness * pixelSize * zoom;
            outlineWidth.ShouldBeInRange((int)System.Math.Floor(expected), (int)System.Math.Ceiling(expected));
        });
    }

    [Fact]
    public void Draw_OutlineSampleSlang_WithPixelSizeThree_OutlinesThreePixelsIncludingTheDiagonal()
    {
        WithOutlinedSquare(container => container.SetShaderParameter("PixelSize", 3f), 1f, 255, pixel =>
        {
            pixel(9, 24).A.ShouldBeGreaterThan((byte)200);
            pixel(8, 24).A.ShouldBe((byte)0);
            pixel(9, 9).A.ShouldBeGreaterThan((byte)200);
            pixel(8, 8).A.ShouldBe((byte)0);
        });
    }

    [Fact]
    public void Draw_OutlineSampleSlang_WithThicknessTwo_OutlinesTheDiagonalCorner()
    {
        WithOutlinedSquare(container => container.SetShaderParameter("Thickness", 2f), 1f, 255, pixel =>
        {
            pixel(10, 10).A.ShouldBeGreaterThan((byte)200);
            pixel(9, 9).A.ShouldBe((byte)0);
        });
    }

    [Fact]
    public void Draw_OutlineSampleSlang_WithThicknessSetToZero_DrawsNoOutline_AtAnyPixelSize()
    {
        WithOutlinedSquare(container =>
        {
            container.SetShaderParameter("Thickness", 0f);
            container.SetShaderParameter("PixelSize", 3f);
        }, 1f, 255, pixel =>
        {
            pixel(24, 24).R.ShouldBeGreaterThan((byte)200);
            pixel(11, 24).A.ShouldBe((byte)0);
            pixel(11, 11).A.ShouldBe((byte)0);
        });
    }

    [Fact]
    public void Draw_OutlineSampleSlang_WithHalfAlpha_FadesTheOutline()
    {
        WithOutlinedSquare(container => { }, 1f, 128, pixel =>
        {
            pixel(11, 24).A.ShouldBeInRange((byte)118, (byte)138);
        });
    }

    private static ContainerRuntime BuildRedInner(float x)
    {
        ContainerRuntime inner = new();
        inner.X = x;
        inner.Width = 100;
        inner.Height = 100;
        inner.IsRenderTarget = true;

#pragma warning disable CS0618
        ColoredRectangleRuntime red = new();
#pragma warning restore CS0618
        red.Width = 100;
        red.Height = 100;
        red.Color = new Color((byte)255, (byte)0, (byte)0, (byte)255);
        inner.Children.Add(red);
        return inner;
    }

    private static Color ReadRenderTargetPixel(RenderTexture2D renderTexture, int x, int y)
    {
        Image image = LoadImageFromTexture(renderTexture.Texture);
        try
        {
            return GetImageColor(image, x, renderTexture.Texture.Height - 1 - y);
        }
        finally
        {
            UnloadImage(image);
        }
    }

    [Fact]
    public void Draw_SourceShaderFile_GraysTheCompositedPixels_WhenResolverRegistered()
    {
        string shaderPath = WriteTempShader(GrayscaleFragmentShader);
        (ContainerRuntime outer, ContainerRuntime inner) = BuildNestedRedRenderTarget();

        try
        {
            // Stand-in for a consumer's resolver: raylib loads GLSL directly, no compiler needed.
            CustomSetPropertyOnRenderable.RenderTargetEffectResolver = path => LoadShader(null, path);

            GumService.Default.Root.Children.Add(outer);
            GumService.Default.Root.UpdateLayout();

            // The .fs reference resolves through the string property path into RenderTargetEffect.
            inner.SourceShaderFile = shaderPath;
            DrawOnce();

            Color center = ReadRenderTargetCenter(Renderer.Self.TryGetBakedRenderTargetFor(outer)!.Value);
            Math.Abs(center.R - center.G).ShouldBeLessThan(25);
            Math.Abs(center.R - center.B).ShouldBeLessThan(25);
        }
        finally
        {
            CustomSetPropertyOnRenderable.RenderTargetEffectResolver = null;
            GumService.Default.Root.Children.Clear();
            File.Delete(shaderPath);
        }
    }

    [Fact]
    public void Draw_SourceShaderFile_IsNoOp_WhenNoResolverRegistered()
    {
        // No resolver registered (clear any leakage from a prior test).
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = null;

        (ContainerRuntime outer, ContainerRuntime inner) = BuildNestedRedRenderTarget();

        GumService.Default.Root.Children.Add(outer);
        GumService.Default.Root.UpdateLayout();

        // With no resolver the assignment is a graceful no-op (no crash); the container renders
        // unshaded, so the composited pixel stays red.
        inner.SourceShaderFile = "resources/DoesNotMatter.fs";
        DrawOnce();

        Color center = ReadRenderTargetCenter(Renderer.Self.TryGetBakedRenderTargetFor(outer)!.Value);
        center.R.ShouldBeGreaterThan((byte)200);
        ((int)center.R - center.G).ShouldBeGreaterThan(80);

        GumService.Default.Root.Children.Clear();
    }

    private static string WriteTempShader(string source)
    {
        string path = Path.Combine(Path.GetTempPath(), "GumRaylibShaderTest_" + Guid.NewGuid().ToString("N") + ".fs");
        File.WriteAllText(path, source);
        return path;
    }
}
