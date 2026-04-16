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

    internal static void MigrateIfNeeded(CodeOutputProjectSettings settings)
    {
        // Version 1: DefaultScreenBase was previously set to stale defaults
        // (e.g. "Gum.Wireframe.BindableGue", "Gum.Wireframe.GraphicalUiElement")
        // by project templates, but the value was never actually used in codegen
        // for MonoGameForms screens. Now that codegen respects it, clear it so
        // each OutputLibrary falls back to its own appropriate default.
        if (settings.Version < 1)
        {
            settings.DefaultScreenBase = "";
            settings.Version = 1;
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

        MigrateIfNeeded(toReturn);

        return toReturn;
    }
}
