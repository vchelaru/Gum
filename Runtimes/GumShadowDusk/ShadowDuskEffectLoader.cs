using System;
using Microsoft.Xna.Framework.Graphics;
using ShadowDusk.Compiler;
using ShadowDusk.Core;
using ShadowDusk.SkSL;

namespace Gum;

/// <summary>
/// Compiles a shader ShadowDusk recognizes from its content (an <c>.fx</c> effect or Slang), or an
/// <c>.sksl</c> file (recognized by name, since SkSL text does not identify itself), into an
/// <see cref="Effect"/>.
/// </summary>
internal static class ShadowDuskEffectLoader
{
    private static bool IsSksl(string name) => name.EndsWith(".sksl", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Whether ShadowDusk reads <paramref name="text"/>. False means it is some other shader language, which a
    /// resolver further down the chain may handle.
    /// </summary>
    public static bool CanLoad(string text, string name)
    {
        if (IsSksl(name))
        {
            // ShadowDusk.SkSL registers its frontend when it loads; this also covers a trimmed app.
            SkSLFrontend.Register();
            return true;
        }
        return ShaderConverter.CanConvert(text, name);
    }

    /// <summary>
    /// Compiles <paramref name="text"/>, or returns null when ShadowDusk reports the input unsupported
    /// (nothing was attempted). A real compile error throws.
    /// </summary>
    public static Effect? TryLoad(GraphicsDevice graphicsDevice, string name, string text, PlatformTarget target)
    {
        // ShadowDusk.SkSL brings ShadowDusk.ShaderToy with it, which would also compile GLSL text. Gum does not
        // route GLSL here (it belongs to the next resolver, e.g. raylib's), so it is declined as before.
        if (ShaderFormatDetector.Detect(text, name).Format == ShaderFormat.Glsl)
        {
            return null;
        }

        // SourceFileName makes a relative #include resolve next to the shader.
        ReportingIncludeResolver includes = new ReportingIncludeResolver(name);
        Result<CompiledShader, ShaderError[]> result = ShaderConverter.Compile(
            new EffectCompiler(),
            text,
            new CompilerOptions { Target = target, SourceFileName = name, IncludeResolver = includes },
            name);
        // Also when the compile failed, so fixing an included file reloads the shader.
        includes.Record();
        if (result.IsFailure)
        {
            if (ShadowDuskInput.IsUnsupported(result.Error))
            {
                return null;
            }

            throw new InvalidOperationException(
                $"ShadowDusk could not compile '{name}':\n{ShadowDuskInput.FormatErrors(result.Error)}");
        }

        return new Effect(graphicsDevice, result.Value.Data);
    }
}
