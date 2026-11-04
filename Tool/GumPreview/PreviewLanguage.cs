using Gum.DataTypes;
using Gum.Localization;

namespace GumPreview;

/// <summary>
/// Makes the Preview translate with the language the tool has selected, which the project saves as
/// <see cref="GumProjectSave.CurrentLanguageIndex"/>. A game picks its own language in code, so the
/// runtime never applies that saved value; the Preview does, so it matches the tool.
/// </summary>
internal static class PreviewLanguage
{
    /// <summary>
    /// Applies the project's saved language to the service and returns its name for the window title,
    /// or null when the title should show none: no service, no localization files loaded, or the
    /// source language (index 0, the string IDs themselves), which has no name.
    /// </summary>
    public static string? Apply(GumProjectSave? project, ILocalizationService? service)
    {
        if (project == null || service == null || service.Languages.Count == 0)
        {
            return null;
        }

        int index = project.CurrentLanguageIndex;
        service.CurrentLanguage = index;
        return index > 0 && index <= service.Languages.Count ? service.Languages[index - 1] : null;
    }
}
