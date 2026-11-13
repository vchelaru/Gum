using System;
using Gum.DataTypes.Variables;
using ToolsUtilities;

namespace Gum.Plugins.InternalPlugins.EditorTab;

/// <summary>
/// Decides whether an edit of a file variable names a file the content cache already holds. Kept apart from
/// <see cref="EditorTabPluginBase"/> so the decision never depends on what the canvas has built: the cached
/// textures are held by other cached content (an animation chain list's frames, other elements), so a pick of
/// a cached file rebuilds with a full reload instead of disposing one entry.
/// </summary>
internal static class EditedFileReload
{
    /// <summary>
    /// True when <paramref name="variable"/> is a file variable, <paramref name="value"/> is its path, and
    /// <paramref name="isCached"/> says the loader already caches that file. The path is standardized to the
    /// absolute form the loader uses as its cache key. A path that cannot be standardized is not a cached file.
    /// </summary>
    internal static bool IsCachedFileEdit(VariableSave? variable, object? value, Func<string, bool> isCached)
    {
        if (variable?.IsFile != true || value is not string path)
        {
            return false;
        }

        try
        {
            string key = FileManager.Standardize(path, preserveCase: true, makeAbsolute: true);
            key = FileManager.RemoveDotDotSlash(key);
            return isCached(key);
        }
        catch
        {
            // An invalid path in a variable cannot be a cached file.
            return false;
        }
    }
}
