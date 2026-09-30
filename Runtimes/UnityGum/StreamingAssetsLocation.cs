#nullable enable
using System;
using System.IO;

namespace Gum;

/// <summary>
/// Maps project-relative paths between Gum's <see cref="ToolsUtilities.FileManager"/> and Unity's
/// StreamingAssets. Where StreamingAssets is a URL rather than a folder (Android's
/// <c>jar:file://</c>, WebGL's <c>http(s)://</c>), FileManager can't handle the URL as a path, so Gum
/// is given paths under <see cref="VirtualRoot"/> instead and <see cref="GetUrl"/> turns them back
/// into URLs for the read. The Unity package's <c>GumStreamingAssets</c> uses this.
/// </summary>
public class StreamingAssetsLocation
{
    /// <summary>The rooted directory Gum sees in place of a URL StreamingAssets.</summary>
    public const string VirtualRoot = "/StreamingAssets";

    private readonly string _streamingAssetsPath;

    /// <summary>
    /// Creates a location for <paramref name="streamingAssetsPath"/>, which is Unity's
    /// <c>Application.streamingAssetsPath</c>.
    /// </summary>
    public StreamingAssetsLocation(string streamingAssetsPath)
    {
        _streamingAssetsPath = streamingAssetsPath;
        IsUrl = streamingAssetsPath.Contains("://");
    }

    /// <summary>True when StreamingAssets is a URL, so files must be read through a web request.</summary>
    public bool IsUrl { get; }

    /// <summary>
    /// Returns the path Gum should load for <paramref name="relativePath"/>, a path relative to
    /// StreamingAssets.
    /// </summary>
    public string GetGumPath(string relativePath)
    {
        if (!IsUrl)
        {
            return Path.Combine(_streamingAssetsPath, relativePath);
        }
        return VirtualRoot + "/" + relativePath.Replace('\\', '/').TrimStart('/');
    }

    /// <summary>
    /// Returns the StreamingAssets URL for <paramref name="gumPath"/>, a path Gum built from
    /// <see cref="GetGumPath"/>, or null when StreamingAssets is a folder or the path isn't under
    /// <see cref="VirtualRoot"/>.
    /// </summary>
    public string? GetUrl(string gumPath)
    {
        if (!IsUrl)
        {
            return null;
        }
        string path = gumPath.Replace('\\', '/');
        string prefix = VirtualRoot + "/";
        if (!path.StartsWith(prefix, StringComparison.Ordinal))
        {
            return null;
        }
        return _streamingAssetsPath.TrimEnd('/') + "/" + path.Substring(prefix.Length);
    }
}
