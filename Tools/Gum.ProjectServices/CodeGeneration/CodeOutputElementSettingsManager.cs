using Gum.DataTypes;
using Newtonsoft.Json;
using ToolsUtilities;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Loads and saves per-element code output settings (.codsj files alongside element XML).
/// </summary>
public class CodeOutputElementSettingsManager
{
    private readonly IProjectDirectoryProvider _projectDirectoryProvider;

    public CodeOutputElementSettingsManager(IProjectDirectoryProvider projectDirectoryProvider)
    {
        _projectDirectoryProvider = projectDirectoryProvider;
    }

    /// <summary>
    /// Writes element-level code settings to the .codsj file alongside the element's XML.
    /// </summary>
    public void WriteSettingsForElement(ElementSave element, CodeOutputElementSettings settings)
    {
        var fileName = GetCodeSettingsFilePath(element);
        if (fileName == null)
        {
            return;
        }
        var serialized = JsonConvert.SerializeObject(settings);
        System.IO.File.WriteAllText(fileName.FullPath, serialized);
    }

    /// <summary>
    /// Gives a duplicated element the source element's settings. The Generated File Name override is
    /// cleared, since keeping it would make both elements generate into the same file. Does nothing
    /// when the source has no settings file.
    /// </summary>
    /// <returns>Whether settings were written for the copy.</returns>
    public bool CopySettings(ElementSave source, ElementSave copy)
    {
        FilePath? sourceFile = GetCodeSettingsFilePath(source);
        FilePath? copyFile = GetCodeSettingsFilePath(copy);
        if (sourceFile == null || copyFile == null || !sourceFile.Exists())
        {
            return false;
        }

        CodeOutputElementSettings settings = LoadOrCreateSettingsFor(source);
        settings.GeneratedFileName = string.Empty;

        // A paste into a folder can land before the copy's own XML has created that folder.
        if (copyFile.GetDirectoryContainingThis() is { } directory)
        {
            System.IO.Directory.CreateDirectory(directory.FullPath);
        }
        WriteSettingsForElement(copy, settings);
        return true;
    }

    /// <summary>
    /// Gets the path to the element's .codsj settings file, which sits alongside the element's XML,
    /// or null when the element has no resolvable XML path. Public so delete/rename reconciliation
    /// outside this assembly can move or remove the file along with the element.
    /// Pass <paramref name="forcedElementName"/> to resolve the path the file had under a different
    /// element name, which rename uses to find the file still sitting at the old name.
    /// </summary>
    public FilePath? GetCodeSettingsFilePath(ElementSave? element, string? forcedElementName = null)
    {
        FilePath? fileName = ElementFilePathHelper.GetFullPathXmlFile(element,
            _projectDirectoryProvider.ProjectDirectory, forcedElementName);
        if (fileName == null)
        {
            return null;
        }
        return fileName.RemoveExtension() + ".codsj";
    }

    /// <summary>
    /// Loads element-level code settings from disk, or creates defaults if the file does not exist.
    /// </summary>
    public CodeOutputElementSettings LoadOrCreateSettingsFor(ElementSave? element)
    {
        CodeOutputElementSettings toReturn;
        var fileName = GetCodeSettingsFilePath(element);
        if (fileName != null && fileName.Exists())
        {
            var contents = System.IO.File.ReadAllText(fileName.FullPath);
            toReturn = JsonConvert.DeserializeObject<CodeOutputElementSettings>(contents)!;
        }
        else
        {
            toReturn = new CodeOutputElementSettings();
            // As of August 3, 2022 we now have basic refactoring support
            // in place (rename, change base type) so we can probably handle
            // regen on change:
            toReturn.AutoGenerateOnChange = true;
        }
        return toReturn;
    }
}
