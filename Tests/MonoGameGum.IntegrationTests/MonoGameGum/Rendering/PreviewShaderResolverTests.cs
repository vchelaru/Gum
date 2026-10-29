using System.IO;
using Gum.GueDeriving;
using Gum.Wireframe;
using GumPreview;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// The Preview window (Tool/GumPreview) must register a SourceShaderFile resolver, or a project's
/// shaded container renders unshaded there while the editor canvas shows the effect.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class PreviewShaderResolverTests : BaseTestClass
{
    [Fact]
    public void SourceShaderFile_ResolvesToEffect_AfterPreviewRegistersItsResolver()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        try
        {
            PreviewShaderResolver.Register(game.GraphicsDevice);

            ContainerRuntime container = new();
            container.Width = 64;
            container.Height = 64;
            container.IsRenderTarget = true;
            container.AddToManagers(SystemManagers.Default, null);

            container.SourceShaderFile = Path.Combine(AppContext.BaseDirectory, "Outline.slang");

            Effect? effect = container.RenderTargetEffect;
            effect.ShouldNotBeNull();
            effect.Parameters["Thickness"].ShouldNotBeNull();
        }
        finally
        {
            CustomSetPropertyOnRenderable.RenderTargetEffectResolver = null;
        }
    }

    // #7034: a relative #include resolves against the shader's own folder.
    private const string GrayscaleFxIncludingLuma = """
        #include "Luma.hlsli"

        #if OPENGL
            #define PS_SHADERMODEL ps_3_0
        #else
            #define PS_SHADERMODEL ps_4_0_level_9_1
        #endif

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
            float gray = dot(color.rgb, LumaWeights);
            return float4(gray, gray, gray, color.a);
        }

        technique SpriteDrawing
        {
            pass P0
            {
                PixelShader = compile PS_SHADERMODEL MainPS();
            }
        };
        """;

    [Fact]
    public void SourceShaderFile_ResolvesAnIncludeNextToTheShader()
    {
        string folder = Path.Combine(Path.GetTempPath(), "Gum7034i_" + Path.GetRandomFileName());
        Directory.CreateDirectory(folder);
        using MinimalGame game = new();
        game.RunOneFrame();

        try
        {
            File.WriteAllText(Path.Combine(folder, "Luma.hlsli"), "static const float3 LumaWeights = float3(0.299, 0.587, 0.114);");
            string shaderPath = Path.Combine(folder, "Grayscale.fx");
            File.WriteAllText(shaderPath, GrayscaleFxIncludingLuma);
            PreviewShaderResolver.Register(game.GraphicsDevice);

            ContainerRuntime container = new();
            container.Width = 64;
            container.Height = 64;
            container.IsRenderTarget = true;
            container.AddToManagers(SystemManagers.Default, null);

            container.SourceShaderFile = shaderPath;

            container.RenderTargetEffect.ShouldNotBeNull();
        }
        finally
        {
            CustomSetPropertyOnRenderable.RenderTargetEffectResolver = null;
            try { Directory.Delete(folder, recursive: true); } catch { /* best-effort */ }
        }
    }

    private sealed class MinimalGame : SharedGameLease
    {
        public GumService GumService { get; } = new GumService();

        protected override void OnInitialize() => GumService.Initialize(Game, Gum.Forms.DefaultVisualsVersion.V3);

        protected override void OnDispose()
        {
            if (GumService.IsInitialized)
            {
                GumService.Uninitialize();
            }
        }
    }
}
