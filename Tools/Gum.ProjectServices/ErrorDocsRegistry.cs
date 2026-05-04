using System.Collections.Generic;

namespace Gum.ProjectServices;

/// <inheritdoc/>
public class ErrorDocsRegistry : IErrorDocsRegistry
{
    private const string UrlPrefix = "https://docs.flatredball.com/gum/";

    private readonly Dictionary<string, string> _docPaths;

    public ErrorDocsRegistry()
    {
        _docPaths = new Dictionary<string, string>
        {
            ["GUM0001"] = "gum-tool/gum-elements/behaviors#behavior-instance-requirements",
        };
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<string> AllCodes => _docPaths.Keys;

    /// <inheritdoc/>
    public string? GetDocPath(string code)
    {
        return _docPaths.TryGetValue(code, out string? path) ? path : null;
    }

    /// <inheritdoc/>
    public string? GetUrl(string code)
    {
        string? path = GetDocPath(code);
        if (path == null)
        {
            return null;
        }
        return UrlPrefix + path;
    }
}
