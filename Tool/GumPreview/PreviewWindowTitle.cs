namespace GumPreview;

/// <summary>
/// The Preview window's title. A deleted element, an empty selection, or a name the saved project
/// does not have yet (the tool selects a new name before its save lands) is not an error, so the
/// window then shows nothing under the plain title.
/// </summary>
internal static class PreviewWindowTitle
{
    public static string For(string elementName, bool isElementMissing) =>
        isElementMissing ? "Gum Preview" : $"Gum Preview - {elementName}";
}
