namespace Gum.ProjectServices;

/// <summary>
/// Maps stable error codes (e.g. <c>"GUM0001"</c>) to documentation locations.
/// The registry stores repo-relative documentation paths (with optional anchors)
/// and can resolve them to public help URLs.
/// </summary>
public interface IErrorDocsRegistry
{
    /// <summary>
    /// Returns the repo-relative docs path for the given code, including any
    /// anchor (e.g. <c>"gum-tool/gum-elements/behaviors#behavior-instance-requirements"</c>),
    /// or null if the code has no registered documentation.
    /// </summary>
    string? GetDocPath(string code);

    /// <summary>
    /// Returns the public help URL for the given code, or null if the code has
    /// no registered documentation.
    /// </summary>
    string? GetUrl(string code);

    /// <summary>
    /// All registered error codes. Primarily used by tests to verify that every
    /// registered code points at a real docs file and heading.
    /// </summary>
    System.Collections.Generic.IReadOnlyCollection<string> AllCodes { get; }
}
