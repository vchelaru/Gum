using System;
using System.Collections.Generic;
using Gum.DataTypes;
using ToolsUtilities;

namespace Gum.Localization;

/// <summary>
/// Every translation of a Text value across all languages of a project's localization data, for the
/// font scan (#7398): any language may be selected at runtime, so each one's BBCode tags need their fonts.
/// </summary>
public static class ProjectTranslations
{
    /// <summary>
    /// Loads <paramref name="project"/>'s localization files through <see cref="ProjectLocalizationLoader"/>
    /// and returns a lookup from a Text value to its translation in each language, or null when the
    /// project has no usable localization data (none configured, files missing, or a file that won't parse).
    /// </summary>
    public static Func<string, IEnumerable<string>>? CreateLookup(GumProjectSave project)
    {
        if (project.LocalizationFiles.Count == 0 || string.IsNullOrEmpty(project.FullFileName))
        {
            return null;
        }

        LocalizationService service = new LocalizationService();
        try
        {
            ProjectLocalizationLoader.Load(project, FileManager.GetDirectory(project.FullFileName), service,
                new ProjectLocalizationLoadOptions());
        }
        catch (Exception)
        {
            // Reported where the project's localization is loaded for real; a font scan just skips it.
            return null;
        }

        int languageCount = service.Languages.Count;
        if (languageCount == 0)
        {
            return null;
        }

        return key =>
        {
            List<string> translations = new List<string>();
            for (int language = 1; language <= languageCount; language++)
            {
                string translated = service.TranslateForLanguage(key, language);
                if (translated != key)
                {
                    translations.Add(translated);
                }
            }
            return translations;
        };
    }
}
