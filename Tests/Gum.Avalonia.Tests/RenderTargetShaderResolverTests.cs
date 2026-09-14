using EditorTabPlugin_XNA.Services;
using ShadowDusk.Core;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The shared render-target shader resolver both heads register: it compiles a .fx for the
/// target the head's device accepts (OpenGL here) and explains every failure.
/// </summary>
public class RenderTargetShaderResolverTests
{
    private const string GrayscaleFx = """
        #if OPENGL
            #define VS_SHADERMODEL vs_3_0
            #define PS_SHADERMODEL ps_3_0
        #else
            #define VS_SHADERMODEL vs_4_0_level_9_1
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
            float gray = dot(color.rgb, float3(0.299, 0.587, 0.114));
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
    public void Compile_ProducesOpenGlBytecode_ForTheAvaloniaHeadsDevice()
    {
        byte[] bytecode = RenderTargetShaderResolver.Compile(GrayscaleFx, PlatformTarget.OpenGL, "Grayscale.fx");

        bytecode.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Compile_NamesTheFileAndTheErrors_WhenTheShaderIsInvalid()
    {
        InvalidOperationException error = Should.Throw<InvalidOperationException>(
            () => RenderTargetShaderResolver.Compile("this is not a shader", PlatformTarget.OpenGL, "Broken.fx"));

        error.Message.ShouldContain("Broken.fx");
    }

    [Fact]
    public void Resolve_ExplainsAMissingDevice_InsteadOfReturningNull()
    {
        Func<string, object?> resolver = RenderTargetShaderResolver.For(PlatformTarget.OpenGL);

        Should.Throw<InvalidOperationException>(() => resolver("Grayscale.fx")).Message.ShouldContain("GraphicsDevice");
    }
}
