using System;
using System.IO;
using Gum;
using Gum.GueDeriving;
using Shouldly;
using SkiaGum.Content;
using SkiaSharp;

namespace SkiaGum.Tests.ShadowDusk;

/// <summary>
/// A parameter a container does not set reads the default its shader declares (#7042), not zero.
/// SkSL declares every uniform without a value, so SkiaGum gets the declared defaults from ShadowDusk.
/// </summary>
public class DeclaredDefaultsTests : IDisposable
{
    // Red is Strength, green is Tint.y, blue is Plain (declared without a default), alpha is 1.
    private const string Shader = @"
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

    private readonly string _path = Path.Combine(Path.GetTempPath(), "GumDeclaredDefaults7042_" + Guid.NewGuid().ToString("N") + ".slang");

    public DeclaredDefaultsTests()
    {
        File.WriteAllText(_path, Shader);
        Gum.Wireframe.GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        GumService.Default.UseShadowDusk();
    }

    public void Dispose()
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = SkSlRuntimeEffectLoader.Load;
        File.Delete(_path);
        foreach (string extra in _extraPaths) File.Delete(extra);
    }

    private SKColor DrawCenter(Action<ContainerRuntime>? setParameters) => DrawCenter(_path, setParameters);

    private string WriteShader(string source)
    {
        string path = Path.Combine(Path.GetTempPath(), "GumDeclaredDefaults7042_" + Guid.NewGuid().ToString("N") + ".slang");
        File.WriteAllText(path, source);
        _extraPaths.Add(path);
        return path;
    }

    private readonly System.Collections.Generic.List<string> _extraPaths = new();

    private SKColor DrawCenter(string shaderPath, Action<ContainerRuntime>? setParameters)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(32, 32));
        GumService.Default.Initialize(surface.Canvas, 32, 32);

        ContainerRuntime container = new() { Width = 32, Height = 32, IsRenderTarget = true };
        container.Children.Add(new RectangleRuntime
        {
            Width = 32,
            Height = 32,
            IsFilled = true,
            FillColor = SKColors.White,
        });
        GumService.Default.Root.Children.Add(container);
        setParameters?.Invoke(container);
        container.SourceShaderFile = shaderPath;
        GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        return bitmap.GetPixel(16, 16);
    }

    [Fact]
    public void Draw_UnsetParameters_ReadTheirDeclaredDefaults()
    {
        SKColor pixel = DrawCenter(null);

        Math.Abs(pixel.Red - 128).ShouldBeLessThan(3);   // Strength = 0.5
        Math.Abs(pixel.Green - 128).ShouldBeLessThan(3); // Tint.y * 0.5 = 0.5
    }

    [Fact]
    public void Draw_UnsetParameterWithoutADeclaredDefault_ReadsZero()
    {
        DrawCenter(null).Blue.ShouldBe((byte)0);
    }

    [Fact]
    public void Draw_SetParameter_OverridesItsDeclaredDefault()
    {
        SKColor pixel = DrawCenter(container => container.SetShaderParameter("Strength", 1f));

        pixel.Red.ShouldBeGreaterThan((byte)250);
        Math.Abs(pixel.Green - 128).ShouldBeLessThan(3); // the other declared default is still applied
    }

    // Red is Count / 6 (an int default), green is Weights[1] + Weights[2] (an array default).
    // SkSL has no uint or bool uniforms, so those never reach this route.
    private const string TypesShader = @"
Texture2D SpriteTexture;
SamplerState SpriteTextureSampler;
int Count = 3;
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
    return float4(float(Count) / 6.0, Weights[1] + Weights[2], 0.0, 1.0) * sprite.a;
}
";

    [Fact]
    public void Draw_UnsetIntAndArrayParameters_ReadTheirDeclaredDefaults()
    {
        SKColor pixel = DrawCenter(WriteShader(TypesShader), null);

        Math.Abs(pixel.Red - 128).ShouldBeLessThan(3);   // Count = 3
        Math.Abs(pixel.Green - 153).ShouldBeLessThan(3); // 0.2 + 0.4
    }

    [Fact]
    public void Draw_EffectWithNoRegisteredDefaults_UnsetParametersReadZero()
    {
        SKRuntimeEffect effect = SKRuntimeEffect.CreateShader(@"
uniform shader SpriteTexture;
uniform float Strength;
half4 main(float2 coord) {
    return half4(Strength, 1, 0, 1) * SpriteTexture.eval(coord).a;
}", out string errors)!;
        string.IsNullOrEmpty(errors).ShouldBeTrue(errors);
        SkiaRuntimeEffectDefaults.TryGet(effect).ShouldBeNull();

        using SKSurface surface = SKSurface.Create(new SKImageInfo(32, 32));
        GumService.Default.Initialize(surface.Canvas, 32, 32);
        ContainerRuntime container = new() { Width = 32, Height = 32, IsRenderTarget = true };
        container.Children.Add(new RectangleRuntime { Width = 32, Height = 32, IsFilled = true, FillColor = SKColors.White });
        GumService.Default.Root.Children.Add(container);
        container.RenderTargetEffect = effect;
        GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        SKColor pixel = bitmap.GetPixel(16, 16);
        pixel.Red.ShouldBe((byte)0);
        pixel.Green.ShouldBeGreaterThan((byte)250);
    }

    // The only way a Bool-kind (uint or bool) default could reach Skia is a uint or bool uniform, and
    // Skia's own compiler rejects those, so the Skia route never writes one.
    [Fact]
    public void Convert_UintUniform_IsRejectedBySkiasCompiler()
    {
        string source = TypesShader.Replace("int Count = 3;", "uint Count = 3;");
        var result = global::ShadowDusk.Compiler.Sksl.SkslConverter.ConvertSlang(source, new global::ShadowDusk.Compiler.Sksl.SkslConvertOptions());

        if (result.IsSuccess)
        {
            SKRuntimeEffect? effect = SKRuntimeEffect.CreateShader(result.Value.SkslText, out _);
            effect.ShouldBeNull();
        }
    }
}
