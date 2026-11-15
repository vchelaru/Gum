using System;
using Microsoft.Xna.Framework.Graphics;
using ShadowDusk.Compiler;
using ShadowDusk.Core;

namespace Gum;

/// <summary>
/// Compiles a shader ShadowDusk recognizes from its content (an <c>.fx</c> effect or Slang) into an
/// <see cref="Effect"/>. The file name plays no part in telling the formats apart.
/// </summary>
internal static class ShadowDuskEffectLoader
{
    /// <summary>
    /// Whether ShadowDusk reads <paramref name="text"/>. False means it is some other shader language, which a
    /// resolver further down the chain may handle.
    /// </summary>
    public static bool CanLoad(string text, string name) => ShaderConverter.CanConvert(text, name);

    /// <summary>
    /// Compiles <paramref name="text"/>, or returns null when ShadowDusk reports the input unsupported
    /// (nothing was attempted). A real compile error throws.
    /// </summary>
    public static Effect? TryLoad(GraphicsDevice graphicsDevice, string name, string text, PlatformTarget target)
    {
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
