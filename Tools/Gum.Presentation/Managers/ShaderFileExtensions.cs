using System;
using System.Linq;

namespace Gum.Managers;

/// <summary>
/// The shader source extensions (lowercase, without the leading dot) a container's SourceShaderFile
/// accepts: raw HLSL <c>.fx</c> and <c>.slang</c>. The Shader File drop-down, the built-in shader copy and
/// the file watcher all read this list so they stay in agreement.
/// </summary>
public static class ShaderFileExtensions
{
    private static readonly string[] All = { "slang", "fx" };

    /// <summary>
    /// Whether <paramref name="extension"/> (without the leading dot, any case) is a shader source extension.
    /// </summary>
    public static bool Contains(string extension) =>
        All.Contains(extension, StringComparer.OrdinalIgnoreCase);
}
