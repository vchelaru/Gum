using System;
using System.Collections.Generic;
using System.IO;
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
        // Policy mirrors the tool's FileCommands.LoadLocalizationFile and the runtime's
        // GumService auto-load:
        //   0 paths  -> no-op
        //   1 path   -> dispatch by extension (single-file overloads)
        //   2+ paths -> require all .resx; call the multi-file RESX overload.
        //               Mixed CSV+RESX or multi-CSV is rejected because the
        //               LocalizationService has no merge API for them.
        List<string>? projectFiles = project.LocalizationFiles;
        if (projectFiles == null || projectFiles.Count == 0)
        {
            return;
        }

        List<string> resolvedPaths = new List<string>();
        foreach (string relative in projectFiles)
        {
            if (!string.IsNullOrEmpty(relative))
            {
                resolvedPaths.Add(Path.Combine(projectDirectory, relative));
            }
        }

        if (resolvedPaths.Count == 0)
        {
            return;
        }

        try
        {
            if (resolvedPaths.Count == 1)
            {
                string fileName = resolvedPaths[0];

                if (!File.Exists(fileName))
                {
                    _logger.PrintError($"Localization: file not found, skipping: {fileName}");
                }
                else if (IsResx(fileName))
                {
                    localizationService.AddResxDatabase(fileName);
                }
                else
                {
                    using FileStream stream = File.OpenRead(fileName);
                    localizationService.AddCsvDatabase(stream);
                }
            }
            else
            {
                bool allResx = true;
                foreach (string path in resolvedPaths)
                {
                    if (!IsResx(path))
                    {
                        allResx = false;
                        break;
                    }
                }

                if (!allResx)
                {
                    _logger.PrintError(
                        "Localization: multiple files configured but not all are .resx. " +
                        "Mixed CSV/RESX and multi-CSV loading are not supported. Loading was skipped.");
                }
                else
                {
                    List<string> existingPaths = new List<string>();
                    foreach (string path in resolvedPaths)
                    {
                        if (File.Exists(path))
                        {
                            existingPaths.Add(path);
                        }
                        else
                        {
                            _logger.PrintError($"Localization: file not found, skipping: {path}");
                        }
                    }

                    if (existingPaths.Count > 0)
                    {
                        localizationService.AddResxDatabase(
                            existingPaths,
                            onWarning: message => _logger.PrintOutput("Localization warning: " + message));
                    }
                }
            }

            localizationService.CurrentLanguage = project.CurrentLanguageIndex;
        }
        catch (Exception e)
        {
            string joined = string.Join(", ", resolvedPaths);
            _logger.PrintError($"Error loading localization file(s) {joined}: {e.Message}");
        }
    }

    private static bool IsResx(string fileName)
    {
        return string.Equals(Path.GetExtension(fileName), ".resx", StringComparison.OrdinalIgnoreCase);
    }
}
