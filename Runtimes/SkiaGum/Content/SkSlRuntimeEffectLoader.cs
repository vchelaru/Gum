using System;
using System.IO;
using SkiaSharp;

namespace SkiaGum.Content;

/// <summary>
/// Compiles a <c>.sksl</c> file into an <see cref="SKRuntimeEffect"/>. This is the default
/// <see cref="CustomSetPropertyOnRenderable.RenderTargetEffectResolver"/>, so a render-target
/// container's <c>SourceShaderFile</c> works with a hand-written SkSL file without any setup.
/// </summary>
public static class SkSlRuntimeEffectLoader
{
    /// <summary>
    /// Reads <paramref name="path"/> and compiles it as an SkSL shader.
    /// </summary>
    /// <exception cref="NotSupportedException">The file is not a <c>.sksl</c> file.</exception>
    /// <exception cref="InvalidOperationException">The SkSL does not compile. The message holds the compiler errors.</exception>
    public static SKRuntimeEffect Load(string path)
    {
        if (!string.Equals(Path.GetExtension(path), ".sksl", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                $"SkiaGum compiles .sksl shader files only, so '{path}' cannot be loaded. " +
                "Add the Gum.SkiaSharp.ShadowDusk package to load .fx and .slang files.");
        }

        SKRuntimeEffect? effect = SKRuntimeEffect.CreateShader(File.ReadAllText(path), out string errors);
        if (effect == null)
        {
            throw new InvalidOperationException($"The SkSL in '{path}' did not compile:\n{errors}");
        }

        return effect;
    }
}
