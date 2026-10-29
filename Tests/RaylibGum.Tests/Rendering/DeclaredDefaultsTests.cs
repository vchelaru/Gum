using System;
using System.IO;
using System.Linq;
using Gum.GueDeriving;
using Gum.Wireframe;
using Raylib_cs;
using RenderingLibrary.Graphics;
using ShadowDusk.Compiler.Raylib;
using ShadowDusk.Core;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// A parameter a container does not set reads the default its shader declares (#7042), not zero.
/// A GLSL uniform has no value in the shader text, so the default comes from ShadowDusk's conversion
/// result and is registered with <see cref="RaylibShaderDefaults"/>.
/// </summary>
public class DeclaredDefaultsTests : BaseTestClass
{
    // Red is Strength, green is Tint.y / 2, blue is Plain (declared without a default), alpha is 1.
    private const string Slang = @"
Texture2D SpriteTexture;
SamplerState SpriteTextureSampler;
float Strength = 0.5;
float4 Tint = float4(0.25, 1.0, 0.75, 1.0);
float Plain;

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

[shader(""fragment"")]
float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float4 sprite = SpriteTexture.Sample(SpriteTextureSampler, input.TextureCoordinates);
    return float4(Strength, Tint.y * 0.5, Plain, 1.0) * sprite.a;
}
";

    private static void DrawOnce()
    {
        BeginDrawing();
        Gum.GumService.Default.Draw();
        EndDrawing();
    }

    private static Shader LoadConvertedShader(bool registerDefaults)
    {
        Result<RaylibShader, ShaderError[]> conversion = RaylibConverter.ConvertSlang(
            Slang, new RaylibConvertOptions(), TestContext.Current.CancellationToken);
        conversion.IsSuccess.ShouldBeTrue(
            conversion.IsFailure ? string.Join("\n", conversion.Error.Select(e => e.Message)) : "");

        Shader shader = LoadShaderFromMemory(null, conversion.Value.FragmentShader);
        if (registerDefaults)
        {
            ShaderUniformDefaults defaults = new ShaderUniformDefaults();
            foreach (RaylibUniform uniform in conversion.Value.Uniforms)
            {
                ShaderUniformDefault? declared = ShaderUniformDefault.FromTypeName(
                    uniform.Name, uniform.GlslType, uniform.ArrayLength, uniform.DefaultValue);
                if (declared != null)
                {
                    defaults.Add(declared);
                }
            }
            RaylibShaderDefaults.Set(shader, defaults);
        }
        return shader;
    }

    private static ContainerRuntime BuildInner(float x)
    {
        ContainerRuntime inner = new();
        inner.X = x;
        inner.Width = 32;
        inner.Height = 32;
        inner.IsRenderTarget = true;
#pragma warning disable CS0618
        ColoredRectangleRuntime white = new();
#pragma warning restore CS0618
        white.Width = 32;
        white.Height = 32;
        white.Color = new Color((byte)255, (byte)255, (byte)255, (byte)255);
        inner.Children.Add(white);
        return inner;
    }

    private static Color ReadPixel(RenderTexture2D renderTexture, int x, int y)
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

    // Draws the inner containers (each at x = 32 * index) in order with the given setup, and returns
    // the center pixel of each.
    private static Color[] Draw(Shader shader, Action<ContainerRuntime[]> setParameters)
    {
        ContainerRuntime outer = new();
        outer.Width = 64;
        outer.Height = 32;
        outer.IsRenderTarget = true;
        ContainerRuntime[] inners = { BuildInner(0), BuildInner(32) };
        foreach (ContainerRuntime inner in inners)
        {
            inner.RenderTargetEffect = shader;
            outer.Children.Add(inner);
        }
        Gum.GumService.Default.Root.Children.Add(outer);
        Gum.GumService.Default.Root.UpdateLayout();
        try
        {
            setParameters(inners);
            DrawOnce();
            RenderTexture2D baked = Renderer.Self.TryGetBakedRenderTargetFor(outer)!.Value;
            return new[] { ReadPixel(baked, 16, 16), ReadPixel(baked, 48, 16) };
        }
        finally
        {
            Gum.GumService.Default.Root.Children.Clear();
        }
    }

    [Fact]
    public void Draw_UnsetParameters_ReadTheirDeclaredDefaults()
    {
        Shader shader = LoadConvertedShader(registerDefaults: true);
        try
        {
            Color[] pixels = Draw(shader, _ => { });

            foreach (Color pixel in pixels)
            {
                Math.Abs(pixel.R - 128).ShouldBeLessThan(3);   // Strength = 0.5
                Math.Abs(pixel.G - 128).ShouldBeLessThan(3);   // Tint.y * 0.5 = 0.5
            }
        }
        finally
        {
            RaylibShaderDefaults.Remove(shader);
            UnloadShader(shader);
        }
    }

    [Fact]
    public void Draw_UnsetParameterWithoutADeclaredDefault_ReadsZero()
    {
        Shader shader = LoadConvertedShader(registerDefaults: true);
        try
        {
            Draw(shader, _ => { })[0].B.ShouldBe((byte)0);
        }
        finally
        {
            RaylibShaderDefaults.Remove(shader);
            UnloadShader(shader);
        }
    }

    [Fact]
    public void Draw_SetParameter_OverridesItsDeclaredDefault_AndTheNextContainerGoesBackToTheDefault()
    {
        Shader shader = LoadConvertedShader(registerDefaults: true);
        try
        {
            Color[] pixels = Draw(shader, inners => inners[0].SetShaderParameter("Strength", 1f));

            pixels[0].R.ShouldBeGreaterThan((byte)250);
            Math.Abs(pixels[0].G - 128).ShouldBeLessThan(3);
            // The second container shares the shader and did not set Strength.
            Math.Abs(pixels[1].R - 128).ShouldBeLessThan(3);
        }
        finally
        {
            RaylibShaderDefaults.Remove(shader);
            UnloadShader(shader);
        }
    }

    [Fact]
    public void Draw_NoDefaultsRegistered_UnsetParametersStillReadZero()
    {
        Shader shader = LoadConvertedShader(registerDefaults: false);
        try
        {
            Color[] pixels = Draw(shader, inners => inners[0].SetShaderParameter("Strength", 1f));

            // The container that set Strength is unaffected, and the shared shader's next container falls
            // back to zero when nothing declares otherwise.
            pixels[0].R.ShouldBeGreaterThan((byte)250);
            pixels[1].R.ShouldBeLessThan((byte)3);
        }
        finally
        {
            UnloadShader(shader);
        }
    }
}
