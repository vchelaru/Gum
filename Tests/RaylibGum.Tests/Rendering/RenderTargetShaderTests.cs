using System;
using System.IO;
using Gum.GueDeriving;
using Gum.Wireframe;
using Raylib_cs;
using RaylibGum.Renderables;
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
