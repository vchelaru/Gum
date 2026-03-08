using Newtonsoft.Json;
using System;
using ToolsUtilities;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Loads and saves project-level code output settings (ProjectCodeSettings.codsj).
/// </summary>
public class CodeOutputProjectSettingsManager
{
    private readonly ICodeGenLogger _logger;
    private readonly string? _projectDirectory;

    public CodeOutputProjectSettingsManager(ICodeGenLogger logger, string? projectDirectory)
    {
        _logger = logger;
        _projectDirectory = projectDirectory;
    }

    /// <summary>
    /// Writes the project settings to the ProjectCodeSettings.codsj file.
    /// </summary>
    public void WriteSettingsForProject(CodeOutputProjectSettings settings)
    {
        var fileName = GetProjectCodeSettingsFile();
        if (fileName != null)
        {
            var serialized = JsonConvert.SerializeObject(settings,
                // This makes debugging a little easier:
                Formatting.Indented);
            System.IO.File.WriteAllText(fileName.FullPath, serialized);
        }
    }

    private FilePath? GetProjectCodeSettingsFile()
    {
        if (_projectDirectory == null)
        {
            return null;
        }
        FilePath folder = _projectDirectory;
        var fileName = folder + "ProjectCodeSettings.codsj";
        return fileName;
    }

    /// <summary>
    /// Loads the project code settings from disk, or creates defaults if not found.
    /// </summary>
    public CodeOutputProjectSettings CreateOrLoadSettingsForProject()
    {
        CodeOutputProjectSettings? toReturn = null;
        var fileName = GetProjectCodeSettingsFile();
        try
        {
            if (fileName?.Exists() == true)
            {
                var contents = System.IO.File.ReadAllText(fileName.FullPath);

                toReturn = JsonConvert.DeserializeObject<CodeOutputProjectSettings>(contents)!;
            }
        }
        catch (Exception e)
        {
            _logger.PrintError($"Error loading project code settings from {fileName}: {e.Message}");
        }

        if (toReturn == null)
        {
            toReturn = new CodeOutputProjectSettings();

            toReturn.SetDefaults();
        }

        return toReturn;
    }
}
