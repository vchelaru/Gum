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

    private static Shader LoadConvertedShader(bool registerDefaults, string slang = Slang)
    {
        Result<RaylibShader, ShaderError[]> conversion = RaylibConverter.ConvertSlang(
            slang, new RaylibConvertOptions(), TestContext.Current.CancellationToken);
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

    // Red is Count / 6 (int), green is Weights[1] + Weights[2] (array), blue is Mask / 10 when On
    // (uint and bool, which raylib uploads as unsigned).
    private const string TypesSlang = @"
Texture2D SpriteTexture;
SamplerState SpriteTextureSampler;
int Count = 3;
uint Mask = 5;
bool On = true;
float Weights[3] = { 0.1, 0.2, 0.4 };

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
    return float4(float(Count) / 6.0, Weights[1] + Weights[2], On ? float(Mask) / 10.0 : 0.0, 1.0) * sprite.a;
}
";

    [Fact]
    public void Draw_UnsetIntUintBoolAndArrayParameters_ReadTheirDeclaredDefaults()
    {
        Shader shader = LoadConvertedShader(registerDefaults: true, TypesSlang);
        try
        {
            Color pixel = Draw(shader, _ => { })[0];

            Math.Abs(pixel.R - 128).ShouldBeLessThan(3);   // Count = 3
            Math.Abs(pixel.G - 153).ShouldBeLessThan(3);   // 0.2 + 0.4
            Math.Abs(pixel.B - 128).ShouldBeLessThan(3);   // On, Mask = 5
        }
        finally
        {
            RaylibShaderDefaults.Remove(shader);
            UnloadShader(shader);
        }
    }

    // ShadowDusk refuses matrix uniforms on the raylib route, and SetShaderValueV with a count of 1 does
    // not upload a matrix, so a matrix default is skipped rather than written into the wrong uniform.
    [Fact]
    public void Draw_MatrixDefault_IsNotWritten()
    {
        Shader shader = LoadConvertedShader(registerDefaults: false);
        try
        {
            ShaderUniformDefaults defaults = new ShaderUniformDefaults();
            defaults.Add(ShaderUniformDefault.FromTypeName("Strength", "mat2", 0, new float[] { 0.5f, 0.5f, 0.5f, 0.5f })!);
            RaylibShaderDefaults.Set(shader, defaults);

            Draw(shader, _ => { })[0].R.ShouldBe((byte)0);
        }
        finally
        {
            RaylibShaderDefaults.Remove(shader);
            UnloadShader(shader);
        }
    }

    [Theory]
    [InlineData("float", ShaderParameterKind.Float, 1, false)]
    [InlineData("vec3", ShaderParameterKind.Float, 3, false)]
    [InlineData("float4", ShaderParameterKind.Float, 4, false)]
    [InlineData("half2", ShaderParameterKind.Float, 2, false)]
    [InlineData("int", ShaderParameterKind.Int, 1, false)]
    [InlineData("ivec2", ShaderParameterKind.Int, 2, false)]
    [InlineData("uint", ShaderParameterKind.Bool, 1, false)]
    [InlineData("uvec4", ShaderParameterKind.Bool, 4, false)]
    [InlineData("bool", ShaderParameterKind.Bool, 1, false)]
    [InlineData("mat4", ShaderParameterKind.Float, 1, true)]
    [InlineData("float2x2", ShaderParameterKind.Float, 1, true)]
    public void FromTypeName_MapsTheTypeName(string typeName, ShaderParameterKind kind, int components, bool isMatrix)
    {
        ShaderUniformDefault declared = ShaderUniformDefault.FromTypeName("U", typeName, 0, new float[] { 1f })!;

        declared.Kind.ShouldBe(kind);
        declared.ComponentCount.ShouldBe(components);
        declared.IsMatrix.ShouldBe(isMatrix);
    }
}
