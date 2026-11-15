namespace GumPreview;

/// <summary>
/// The Preview window's title. A deleted element, an empty selection, or a name the saved project
/// does not have yet (the tool selects a new name before its save lands) is not an error, so the
/// window then shows nothing under the plain title. The tool's language follows the title in
/// parentheses; a project with no localization has none, so its title is unchanged.
/// </summary>
internal static class PreviewWindowTitle
{
    public static string For(string elementName, bool isElementMissing, string? language = null)
    {
        string title = isElementMissing ? "Gum Preview" : $"Gum Preview - {elementName}";
        return string.IsNullOrEmpty(language) ? title : $"{title} ({language})";
    }

    /// <summary>
    /// The title of a window whose project could not be loaded. The window has nothing to draw, so the
    /// reason goes where it can be read without a dialog.
    /// </summary>
    public static string ForLoadFailure(string message) => $"Gum Preview - {message}";
}
