using System;
using System.IO;
using System.Linq;
using ShadowDusk.Compiler.Sksl;
using SkiaSharp;

namespace Gum;

/// <summary>
/// Converts an <c>.fx</c> or <c>.slang</c> shader file to SkSL with ShadowDusk and compiles it
/// into an <see cref="SKRuntimeEffect"/>.
/// </summary>
internal static class ShadowDuskEffectLoader
{
    // The texture name SkiaGum binds the baked container image to. It is the same name MonoGame
    // shaders use, so one .fx or .slang file runs on both runtimes.
    private const string TextureName = "SpriteTexture";

    public static bool CanLoad(string path)
    {
        string extension = Path.GetExtension(path);
        return string.Equals(extension, ".fx", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".slang", StringComparison.OrdinalIgnoreCase);
    }

    public static SKRuntimeEffect Load(string path)
    {
        string source = File.ReadAllText(path);
        SkslConvertOptions options = new SkslConvertOptions { SourceName = path };

        bool isSlang = string.Equals(Path.GetExtension(path), ".slang", StringComparison.OrdinalIgnoreCase);
        var result = isSlang
            ? SkslConverter.ConvertSlang(source, options)
            : SkslConverter.Convert(source, options);

        if (result.IsFailure)
        {
            string errors = string.Join("\n", result.Error.Select(
                error => $"{error.File}({error.Line},{error.Column}): {error.Code}: {error.Message}"));
            throw new InvalidOperationException($"ShadowDusk could not convert '{path}' to SkSL:\n{errors}");
        }

        SkslConversion conversion = result.Value;

        if (conversion.ChildShaders.Count != 1 || conversion.ChildShaders[0] != TextureName)
        {
            string found = conversion.ChildShaders.Count == 0 ? "none" : string.Join(", ", conversion.ChildShaders);
            throw new NotSupportedException(
                $"'{path}' must sample exactly one texture named {TextureName}, the container's image. Textures found: {found}.");
        }

        SKRuntimeEffect? effect = SKRuntimeEffect.CreateShader(conversion.SkslText, out string compileErrors);
        if (effect == null)
        {
            throw new InvalidOperationException($"The SkSL ShadowDusk produced for '{path}' did not compile:\n{compileErrors}");
        }

        return effect;
    }
}
