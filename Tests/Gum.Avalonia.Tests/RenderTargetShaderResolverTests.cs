using EditorTabPlugin_XNA.Services;
using Gum.Managers;
using System.IO;
using RenderingLibrary;
using ShadowDusk.Core;
using ToolsUtilities;
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

    private const string LumaHlsli = """
        static const float3 LumaWeights = float3(0.299, 0.587, 0.114);
        """;

    // GrayscaleFx with the luma weights moved into an #included file.
    private static string GrayscaleFxIncluding(string includeName) =>
        GrayscaleFx
            .Replace("float3(0.299, 0.587, 0.114)", "LumaWeights")
            .Replace("Texture2D SpriteTexture;", "#include \"" + includeName + "\"\n\nTexture2D SpriteTexture;");

    [Fact]
    public void Compile_ResolvesAnIncludeNextToTheShader()
    {
        string folder = Path.Combine(Path.GetTempPath(), "Gum7034i_" + Path.GetRandomFileName());
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(Path.Combine(folder, "Luma.hlsli"), LumaHlsli);
            string shaderPath = Path.Combine(folder, "Grayscale.fx");

            byte[] bytecode = RenderTargetShaderResolver.Compile(
                GrayscaleFxIncluding("Luma.hlsli"), PlatformTarget.OpenGL, shaderPath);

            bytecode.Length.ShouldBeGreaterThan(0);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void Compile_ReportsEveryIncludedFile_NestedOnesToo()
    {
        string folder = Path.Combine(Path.GetTempPath(), "Gum7034i_" + Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(folder, "lib"));
        try
        {
            // Luma.hlsli includes lib/Weights.hlsli, which is found next to Luma.hlsli rather than the shader.
            File.WriteAllText(Path.Combine(folder, "Luma.hlsli"), "#include \"lib/Weights.hlsli\"\n");
            File.WriteAllText(Path.Combine(folder, "lib", "Weights.hlsli"), LumaHlsli);
            List<string> included = new List<string>();

            RenderTargetShaderResolver.Compile(
                GrayscaleFxIncluding("Luma.hlsli"), PlatformTarget.OpenGL, Path.Combine(folder, "Grayscale.fx"), included.Add);

            included.Select(Path.GetFullPath).ShouldBe(
                new[] { Path.Combine(folder, "Luma.hlsli"), Path.Combine(folder, "lib", "Weights.hlsli") },
                ignoreOrder: true);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void CompileFile_RecordsTheIncludesInTheMap_AgainstTheShadersPath()
    {
        string folder = Path.Combine(Path.GetTempPath(), "Gum7034i_" + Path.GetRandomFileName());
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(Path.Combine(folder, "Luma.hlsli"), LumaHlsli);
            string shaderPath = Path.Combine(folder, "Grayscale.fx");
            File.WriteAllText(shaderPath, GrayscaleFxIncluding("Luma.hlsli"));
            ShaderIncludeMap includes = new ShaderIncludeMap();

            RenderTargetShaderResolver.CompileFile(shaderPath, PlatformTarget.OpenGL, includes);

            includes.GetShadersIncluding(new FilePath(Path.Combine(folder, "Luma.hlsli")))
                .ShouldBe(new[] { new FilePath(shaderPath) });
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void CompileFile_StillRecordsTheIncludes_WhenTheShaderDoesNotCompile()
    {
        string folder = Path.Combine(Path.GetTempPath(), "Gum7034i_" + Path.GetRandomFileName());
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(Path.Combine(folder, "Luma.hlsli"), "this is not hlsl");
            string shaderPath = Path.Combine(folder, "Grayscale.fx");
            File.WriteAllText(shaderPath, GrayscaleFxIncluding("Luma.hlsli"));
            ShaderIncludeMap includes = new ShaderIncludeMap();

            Should.Throw<InvalidOperationException>(
                () => RenderTargetShaderResolver.CompileFile(shaderPath, PlatformTarget.OpenGL, includes));

            // Fixing the include has to reload the shader that kept its last good version.
            includes.IsIncluded(new FilePath(Path.Combine(folder, "Luma.hlsli"))).ShouldBeTrue();
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
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
        // A canvas scenario earlier in the run leaves the editor's renderer, and its device, in place.
        SystemManagers systemManagers = SystemManagers.Default;
        SystemManagers.Default = null!;
        try
        {
            Should.Throw<InvalidOperationException>(() => resolver("Grayscale.fx")).Message.ShouldContain("GraphicsDevice");
        }
        finally
        {
            SystemManagers.Default = systemManagers;
        }
    }
}
