using Gum.DataTypes;
using System;
using System.Threading.Tasks;

namespace Gum.Plugins.ImportPlugin.Services;

/// <summary>
/// Loads a Gum project (and the element/asset files it references) from either a local
/// .gumx path or a remote URL, so the import pipeline can treat both sources uniformly.
/// </summary>
public interface IGumxSourceService
{
    /// <summary>
    /// Loads a <see cref="GumProjectSave"/> from a local file path or a URL. For local paths,
    /// uses <see cref="GumProjectSave.Load(string, out bool)"/> directly. For URLs, fetches the
    /// .gumx and all referenced element files over HTTP.
    /// </summary>
    Task<GumProjectSave?> LoadProjectAsync(string pathOrUrl, IProgress<(int loaded, int total)>? progress = null);

    /// <summary>
    /// Fetches the text of a single element file (.gucx, .gusx, .behx, .gutx) relative to
    /// <paramref name="sourceBase"/>, which may be a local directory path or a base URL.
    /// </summary>
    Task<string?> FetchElementTextAsync(string relativeElementPath, string sourceBase);

    /// <summary>
    /// Converts a GitHub blob URL to its raw.githubusercontent.com equivalent, e.g.
    /// <c>https://github.com/user/repo/blob/branch/path/file.gumx</c> to
    /// <c>https://raw.githubusercontent.com/user/repo/branch/path/file.gumx</c>.
    /// </summary>
    string NormalizeGitHubUrl(string url);

    /// <summary>
    /// Returns the base path or URL for resolving element files relative to the .gumx source.
    /// For local paths, returns the directory containing the .gumx file. For URLs, returns the
    /// directory portion of the URL.
    /// </summary>
    string GetSourceBase(string pathOrUrl);

    /// <summary>
    /// Fetches the raw bytes of an asset file (e.g. a .png) relative to <paramref name="sourceBase"/>.
    /// Returns null if the file cannot be found or downloaded.
    /// </summary>
    Task<byte[]?> FetchBinaryAsync(string relativePath, string sourceBase);
}
