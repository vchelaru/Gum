using Gum.DataTypes;
using Newtonsoft.Json;
using ToolsUtilities;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Loads and saves per-element code output settings (.codsj files alongside element XML).
/// </summary>
public class CodeOutputElementSettingsManager
{
    private readonly string? _projectDirectory;

    public CodeOutputElementSettingsManager(string? projectDirectory)
    {
        _projectDirectory = projectDirectory;
    }

    /// <summary>
    /// Writes element-level code settings to the .codsj file alongside the element's XML.
    /// </summary>
    public void WriteSettingsForElement(ElementSave element, CodeOutputElementSettings settings)
    {
        var fileName = GetCodeSettingsFileFor(element);
        if (fileName == null)
        {
            return;
        }
        var serialized = JsonConvert.SerializeObject(settings);
        System.IO.File.WriteAllText(fileName.FullPath, serialized);
    }

    private FilePath? GetCodeSettingsFileFor(ElementSave element)
    {
        FilePath? fileName = ElementFilePathHelper.GetFullPathXmlFile(element, _projectDirectory);
        if (fileName == null)
        {
            return null;
        }
        return fileName.RemoveExtension() + ".codsj";
    }

    /// <summary>
    /// Loads element-level code settings from disk, or creates defaults if the file does not exist.
    /// </summary>
    public CodeOutputElementSettings LoadOrCreateSettingsFor(ElementSave element)
    {
        CodeOutputElementSettings toReturn;
        var fileName = GetCodeSettingsFileFor(element);
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
