using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using ShadowDusk.Compiler;
using ShadowDusk.Compiler.Slang;
using ShadowDusk.Core;

namespace Gum;

/// <summary>
/// Compiles an <c>.fx</c> or <c>.slang</c> shader file with ShadowDusk into an <see cref="Effect"/>.
/// </summary>
internal static class ShadowDuskEffectLoader
{
    public static bool CanLoad(string path)
    {
        string extension = Path.GetExtension(path);
        return string.Equals(extension, ".fx", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, SlangFrontend.Extension, StringComparison.OrdinalIgnoreCase);
    }

    public static Effect Load(GraphicsDevice graphicsDevice, string path, PlatformTarget target)
    {
        string fxSource = ReadFxSource(path);

        EffectCompiler compiler = new EffectCompiler();
        Result<CompiledShader, ShaderError[]> result = compiler.Compile(
            fxSource, new CompilerOptions { Target = target });
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"ShadowDusk could not compile '{path}':\n{FormatErrors(result.Error)}");
        }

        return new Effect(graphicsDevice, result.Value.Data);
    }

    // Slang has no technique or pass concept, so a .slang file is converted to .fx text first; an .fx
    // file is already ordinary effect source.
    private static string ReadFxSource(string path)
    {
        string source = File.ReadAllText(path);
        if (!string.Equals(Path.GetExtension(path), SlangFrontend.Extension, StringComparison.OrdinalIgnoreCase))
        {
            return source;
        }

        Result<SlangFxConversion, ShaderError[]> conversion = SlangFrontend.ConvertToFx(
            source, new SlangConvertOptions { SourceName = path });
        if (conversion.IsFailure)
        {
            throw new InvalidOperationException(
                $"ShadowDusk could not convert the Slang shader '{path}':\n{FormatErrors(conversion.Error)}");
        }

        return conversion.Value.FxText;
    }

    private static string FormatErrors(ShaderError[] errors) =>
        string.Join("\n", errors.Select(
            error => $"{error.File}({error.Line},{error.Column}): {error.Code}: {error.Message}"));
}
