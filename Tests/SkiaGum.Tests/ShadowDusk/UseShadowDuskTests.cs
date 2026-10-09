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
/// End-to-end tests for <c>GumService.UseShadowDusk()</c> (issue #5659): a render-target
/// container's <c>SourceShaderFile</c> pointing at an <c>.fx</c> or <c>.slang</c> file is converted
/// to SkSL by ShadowDusk and post-processes the container's pixels.
/// </summary>
public class UseShadowDuskTests : IDisposable
{
    // The same shaders the MonoGame sample ships, so one file runs on both runtimes.
    private const string GrayscaleSlang = @"
Texture2D SpriteTexture;
SamplerState SpriteTextureSampler;

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

[shader(""fragment"")]
float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float4 color = SpriteTexture.Sample(SpriteTextureSampler, input.TextureCoordinates) * input.Color;
    float gray = dot(color.rgb, float3(0.299, 0.587, 0.114));
    return float4(gray, gray, gray, color.a);
}
";

    private const string GrayscaleFx = @"
Texture2D SpriteTexture;

sampler2D SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float4 color = tex2D(SpriteTextureSampler, input.TextureCoordinates) * input.Color;
    float gray = dot(color.rgb, float3(0.299, 0.587, 0.114));
    return float4(gray, gray, gray, color.a);
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile ps_3_0 MainPS();
    }
};
";

    private const string GrayscaleSksl = @"
uniform shader SpriteTexture;
half4 main(float2 coord) {
    half4 texel = SpriteTexture.eval(coord);
    half gray = dot(texel.rgb, half3(0.299, 0.587, 0.114));
    return half4(gray, gray, gray, texel.a);
}
";

    public UseShadowDuskTests()
    {
        Gum.Wireframe.GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        GumService.Default.UseShadowDusk();
    }

    public void Dispose()
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = SkSlRuntimeEffectLoader.Load;
    }

    private static string WriteTempFile(string extension, string text)
    {
        string path = Path.Combine(Path.GetTempPath(), "GumShadowDuskTest_" + Guid.NewGuid().ToString("N") + extension);
        File.WriteAllText(path, text);
        return path;
    }

    private static SKColor DrawRedContainerWithShader(string shaderPath)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);

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
        GumService.Default.Root.Children.Add(renderTarget);

        renderTarget.SourceShaderFile = shaderPath;
        GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        return bitmap.GetPixel(19, 19);
    }

    [Theory]
    [InlineData(".slang", GrayscaleSlang)]
    [InlineData(".fx", GrayscaleFx)]
    public void SourceShaderFile_FxOrSlang_GraysTheCompositedPixels(string extension, string source)
    {
        string path = WriteTempFile(extension, source);
        try
        {
            SKColor center = DrawRedContainerWithShader(path);

            Math.Abs(center.Red - center.Green).ShouldBeLessThan(20);
            Math.Abs(center.Red - center.Blue).ShouldBeLessThan(20);
            // Gray, not the black an unset ShadowDusk_Color would produce.
            center.Red.ShouldBeGreaterThan((byte)40);
            center.Alpha.ShouldBe((byte)255);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SourceShaderFile_Sksl_StillLoadsAfterUseShadowDusk()
    {
        string path = WriteTempFile(".sksl", GrayscaleSksl);
        try
        {
            SKColor center = DrawRedContainerWithShader(path);

            Math.Abs(center.Red - center.Green).ShouldBeLessThan(20);
            Math.Abs(center.Red - center.Blue).ShouldBeLessThan(20);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Resolver_SlangWithDifferentTextureName_ThrowsNamingTheExpectedName()
    {
        string path = WriteTempFile(".slang", GrayscaleSlang.Replace("SpriteTexture", "MyTexture"));
        try
        {
            NotSupportedException exception = Should.Throw<NotSupportedException>(
                () => CustomSetPropertyOnRenderable.RenderTargetEffectResolver!(path));

            exception.Message.ShouldContain("SpriteTexture");
            exception.Message.ShouldContain("MyTexture");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Resolver_InvalidSlang_ThrowsWithTheConverterErrors()
    {
        string path = WriteTempFile(".slang", "this is not a shader");
        try
        {
            InvalidOperationException exception = Should.Throw<InvalidOperationException>(
                () => CustomSetPropertyOnRenderable.RenderTargetEffectResolver!(path));

            exception.Message.ShouldContain(path);
            exception.Message.ShouldContain("SD");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
