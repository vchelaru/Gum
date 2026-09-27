using System;
using System.Linq;
using Gum.DataTypes;
using Gum.Localization;
using Gum.ProjectServices.CodeGeneration;

namespace Gum.ProjectServices;

/// <inheritdoc cref="IHeadlessLocalizationLoader"/>
public class HeadlessLocalizationLoader : IHeadlessLocalizationLoader
{
    private readonly ICodeGenLogger _logger;

    public HeadlessLocalizationLoader(ICodeGenLogger logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public void LoadLocalizationFiles(GumProjectSave project, string projectDirectory, ILocalizationService localizationService)
    {
        if (project.LocalizationFiles.All(string.IsNullOrEmpty))
        {
            return;
        }

        try
        {
            // The policy is shared with the tool and the runtime; skipped files are errors here and
            // string ID collisions are output.
            ProjectLocalizationLoader.Load(project, projectDirectory, localizationService, new ProjectLocalizationLoadOptions
            {
                OnSkipped = _logger.PrintError,
                OnWarning = _logger.PrintOutput,
            });

            localizationService.CurrentLanguage = project.CurrentLanguageIndex;
        }
        catch (Exception e)
        {
            string joined = string.Join(", ", project.LocalizationFiles);
            _logger.PrintError($"Error loading localization file(s) {joined}: {e.Message}");
        }
    }
}
