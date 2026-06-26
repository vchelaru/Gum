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
            ["GUM0002"] = "gum-tool/gum-elements/states/categories#gum0002-variable-reference-conflicts-with-explicit-set",
            ["GUM0003"] = "gum-tool/gum-elements/states/categories#gum0003-category-state-sets-its-own-category-selector",
            ["GUM0004"] = "gum-tool/project-files#missing-source-files-gum0004",
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
