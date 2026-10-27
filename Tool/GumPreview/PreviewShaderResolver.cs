using System;
using System.IO;
using System.Linq;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using ShadowDusk.Compiler;
using ShadowDusk.Compiler.Slang;
using ShadowDusk.Core;

namespace GumPreview;

/// <summary>
/// Resolves a container's <c>SourceShaderFile</c> (a <c>.fx</c> or <c>.slang</c> path) into an
/// <see cref="Effect"/> for the Preview window by compiling it at runtime with ShadowDusk for
/// OpenGL. Gum core ships no shader loader, so without this the Preview renders a shaded container
/// unshaded while the editor canvas shows the effect. Failures throw with the reason, which
/// <c>CustomSetPropertyOnRenderable</c> reports.
/// </summary>
internal static class PreviewShaderResolver
{
    public static void Register(GraphicsDevice graphicsDevice)
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = path => Resolve(graphicsDevice, path);
    }

    private static Effect Resolve(GraphicsDevice graphicsDevice, string absolutePath)
    {
        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException("Render-target shader file not found: " + absolutePath, absolutePath);
        }

        EffectCompiler compiler = new EffectCompiler();
        Result<CompiledShader, ShaderError[]> result = compiler.Compile(
            ReadFxSource(absolutePath), new CompilerOptions { Target = PlatformTarget.OpenGL });
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                "ShadowDusk could not compile the render-target shader '" + absolutePath + "':\n" +
                string.Join("\n", result.Error.Select(error => error.Message)));
        }
        return new Effect(graphicsDevice, result.Value.Data);
    }

    // Slang has no technique/pass concept, so a .slang file is converted to .fx text first.
    private static string ReadFxSource(string absolutePath)
    {
        string sourceText = File.ReadAllText(absolutePath);
        if (!string.Equals(Path.GetExtension(absolutePath), SlangFrontend.Extension, StringComparison.OrdinalIgnoreCase))
        {
            return sourceText;
        }

        Result<SlangFxConversion, ShaderError[]> conversion = SlangFrontend.ConvertToFx(
            sourceText, new SlangConvertOptions { SourceName = absolutePath });
        if (conversion.IsFailure)
        {
            throw new InvalidOperationException(
                "ShadowDusk could not convert the Slang render-target shader '" + absolutePath + "':\n" +
                string.Join("\n", conversion.Error.Select(error => error.Message)));
        }
        return conversion.Value.FxText;
    }
}
