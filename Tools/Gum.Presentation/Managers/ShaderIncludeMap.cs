using System;
using System.Collections.Generic;
using System.Linq;
using ToolsUtilities;

namespace Gum.Managers;

/// <summary>
/// Which shader files read which other files, through <c>#include</c>, filled each time the canvas compiles a
/// shader. An edit of such a file reloads the shaders that read it (#7034). Compiling lives in a plugin that
/// Gum.Presentation does not reference, so the plugin writes here and the file watch reads.
/// </summary>
public interface IShaderIncludeMap
{
    /// <summary>
    /// Records that <paramref name="shader"/> read exactly <paramref name="includes"/> (nested includes too) the last
    /// time it compiled, replacing what was recorded before.
    /// </summary>
    void SetIncludes(FilePath shader, IEnumerable<FilePath> includes);

    /// <summary>True when some shader read <paramref name="file"/> the last time it compiled.</summary>
    bool IsIncluded(FilePath file);

    /// <summary>The shaders that read <paramref name="file"/> the last time they compiled.</summary>
    IReadOnlyList<FilePath> GetShadersIncluding(FilePath file);
}

/// <inheritdoc cref="IShaderIncludeMap"/>
/// <remarks>
/// A shader is recorded until it compiles again, which replaces its entry; a shader the project no longer references
/// stays listed, and the caller asks the project whether a listed shader is still in use. Thread-safe: the canvas
/// compiles during rendering while the file watch reacts on the UI thread.
/// </remarks>
public sealed class ShaderIncludeMap : IShaderIncludeMap
{
    private readonly Dictionary<FilePath, HashSet<FilePath>> _includesByShader = new Dictionary<FilePath, HashSet<FilePath>>();

    public void SetIncludes(FilePath shader, IEnumerable<FilePath> includes)
    {
        HashSet<FilePath> set = new HashSet<FilePath>(includes);
        lock (_includesByShader)
        {
            if (set.Count == 0)
            {
                _includesByShader.Remove(shader);
            }
            else
            {
                _includesByShader[shader] = set;
            }
        }
    }

    public bool IsIncluded(FilePath file)
    {
        lock (_includesByShader)
        {
            return _includesByShader.Values.Any(includes => includes.Contains(file));
        }
    }

    public IReadOnlyList<FilePath> GetShadersIncluding(FilePath file)
    {
        lock (_includesByShader)
        {
            return _includesByShader
                .Where(pair => pair.Value.Contains(file))
                .Select(pair => pair.Key)
                .ToList();
        }
    }
}
