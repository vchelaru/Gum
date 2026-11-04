using System;
using System.Collections.Generic;
using System.Linq;

namespace Gum;

/// <summary>
/// Remembers the files each shader read while compiling (its <c>#include</c>s, nested ones too), so
/// runtime hot reload can reload a shader when one of them changes. GumCommon cannot reference ShadowDusk,
/// so the opt-in ShadowDusk packages call <see cref="Record"/> after each compile (#7034).
/// </summary>
public static class ShaderIncludeTracker
{
    private static readonly object _lock = new object();
    private static readonly Dictionary<string, HashSet<string>> _includesByShader =
        new Dictionary<string, HashSet<string>>();

    /// <summary>
    /// Sets the files <paramref name="shaderPath"/> included in its latest compile, replacing any earlier set.
    /// Call it for a compile that failed too, so fixing the included file reloads the shader.
    /// </summary>
    public static void Record(string shaderPath, IEnumerable<string> includedFiles)
    {
        string shader = Standardize(shaderPath);
        HashSet<string> includes = new HashSet<string>(includedFiles.Select(Standardize));
        lock (_lock)
        {
            if (includes.Count == 0)
            {
                _includesByShader.Remove(shader);
            }
            else
            {
                _includesByShader[shader] = includes;
            }
        }
    }

    /// <summary>
    /// The standardized files <paramref name="shaderPath"/> read in its latest compile, or an empty list when none was recorded.
    /// </summary>
    public static IReadOnlyCollection<string> GetIncludedFiles(string shaderPath)
    {
        lock (_lock)
        {
            return _includesByShader.TryGetValue(Standardize(shaderPath), out HashSet<string>? includes)
                ? includes.ToList()
                : new List<string>();
        }
    }

    /// <summary>Forgets every recorded shader.</summary>
    public static void Clear()
    {
        lock (_lock)
        {
            _includesByShader.Clear();
        }
    }

    /// <summary>
    /// Whether <paramref name="shaderPath"/> read any file in <paramref name="changedFiles"/>, which must
    /// already be standardized.
    /// </summary>
    internal static bool Includes(string shaderPath, HashSet<string> changedFiles)
    {
        lock (_lock)
        {
            return _includesByShader.TryGetValue(Standardize(shaderPath), out HashSet<string>? includes)
                && includes.Overlaps(changedFiles);
        }
    }

    private static string Standardize(string path) =>
        ToolsUtilities.FileManager.Standardize(path, preserveCase: false, makeAbsolute: true);
}
