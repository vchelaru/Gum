using System;
using System.IO;
using Gum;
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
    /// <exception cref="NotSupportedException">The file is not an SkSL file.</exception>
    /// <exception cref="InvalidOperationException">The SkSL does not compile. The message holds the compiler errors.</exception>
    public static SKRuntimeEffect Load(string path) =>
        Load(new ShaderSource(path, () => File.OpenRead(path)));

    /// <summary>
    /// Reads <paramref name="source"/> and compiles it as an SkSL shader.
    /// </summary>
    /// <exception cref="NotSupportedException">The source is not an SkSL file.</exception>
    /// <exception cref="InvalidOperationException">The SkSL does not compile. The message holds the compiler errors.</exception>
    public static SKRuntimeEffect Load(ShaderSource source)
    {
        if (!string.Equals(Path.GetExtension(source.Name), ".sksl", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                $"SkiaGum cannot load '{source.Name}' as a shader. " +
                "Add the Gum.SkiaSharp.ShadowDusk package to load other shader formats.");
        }

        SKRuntimeEffect? effect = SKRuntimeEffect.CreateShader(source.ReadAllText(), out string errors);
        if (effect == null)
        {
            throw new InvalidOperationException($"The SkSL in '{source.Name}' did not compile:\n{errors}");
        }

        return effect;
    }
}
