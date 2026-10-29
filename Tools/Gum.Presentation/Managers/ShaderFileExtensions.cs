using System;
using System.Linq;

namespace Gum.Managers;

/// <summary>
/// The shader source extensions (lowercase, without the leading dot) a container's SourceShaderFile
/// offers to pick: raw HLSL <c>.fx</c> and <c>.slang</c>. The built-in shader copy reads this list. The file
/// watcher reloads a shader in any extension that an element references; it reads this list only to leave an
/// unreferenced <c>.slang</c>/<c>.fx</c> alone (#7034).
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
