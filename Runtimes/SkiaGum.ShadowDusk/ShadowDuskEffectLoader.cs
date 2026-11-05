using System;
using System.IO;
using ShadowDusk.Compiler;
using RenderingLibrary.Graphics;
using ShadowDusk.Compiler.Sksl;
using SkiaGum.Content;
using SkiaSharp;

namespace Gum;

/// <summary>
/// Converts a shader ShadowDusk recognizes from its content (an <c>.fx</c> effect or Slang) to SkSL with ShadowDusk and compiles it
/// into an <see cref="SKRuntimeEffect"/>.
/// </summary>
internal static class ShadowDuskEffectLoader
{
    // The texture name SkiaGum binds the baked container image to. It is the same name MonoGame
    // shaders use, so one .fx or .slang file runs on both runtimes.
    private const string TextureName = "SpriteTexture";

    /// <summary>
    /// Converts <paramref name="source"/> to SkSL and compiles it, or returns null when ShadowDusk reports the
    /// input unsupported (nothing was attempted). A real conversion error throws.
    /// </summary>
    public static SKRuntimeEffect? TryLoad(ShaderSource source)
    {
        string text = source.ReadAllText();
        ReportingIncludeResolver includes = new ReportingIncludeResolver(source.Name);
        SkslConvertOptions options = new SkslConvertOptions { SourceName = source.Name, IncludeResolver = includes };

        var result = ShaderConverter.ConvertToSksl(text, options, source.Name);

        // Also when the conversion failed, so fixing an included file reloads the shader.
        includes.Record();

        if (result.IsFailure)
        {
            if (ShadowDuskInput.IsUnsupported(result.Error))
            {
                return null;
            }

            throw new InvalidOperationException(
                $"ShadowDusk could not convert '{source.Name}' to SkSL:\n{ShadowDuskInput.FormatErrors(result.Error)}");
        }

        SkslConversion conversion = result.Value;

        if (conversion.ChildShaders.Count != 1 || conversion.ChildShaders[0] != TextureName)
        {
            string found = conversion.ChildShaders.Count == 0 ? "none" : string.Join(", ", conversion.ChildShaders);
            throw new NotSupportedException(
                $"'{source.Name}' must sample exactly one texture named {TextureName}, the container's image. Textures found: {found}.");
        }

        SKRuntimeEffect? effect = SKRuntimeEffect.CreateShader(conversion.SkslText, out string compileErrors);
        if (effect == null)
        {
            throw new InvalidOperationException($"The SkSL ShadowDusk produced for '{source.Name}' did not compile:\n{compileErrors}");
        }

        RegisterDeclaredDefaults(effect, conversion);
        return effect;
    }

    // SkSL declares every uniform without a value, so the defaults the .fx or .slang declares are
    // handed to SkiaGum, which writes them for a parameter a container did not set.
    private static void RegisterDeclaredDefaults(SKRuntimeEffect effect, SkslConversion conversion)
    {
        ShaderUniformDefaults defaults = new ShaderUniformDefaults();
        foreach (SkslUniform uniform in conversion.Uniforms)
        {
            ShaderUniformDefault? declared = ShaderUniformDefault.FromTypeName(
                uniform.Name, uniform.SkslType, uniform.ArrayLength, uniform.DefaultValue);
            if (declared != null)
            {
                defaults.Add(declared);
            }
        }
        if (defaults.Count > 0)
        {
            SkiaRuntimeEffectDefaults.Set(effect, defaults);
        }
    }
}
