using CodeOutputPlugin.Models;
using Gum.DataTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace CodeOutputPlugin.Manager;

public class CodeOutputElementSettingsManager
{
    public static void WriteSettingsForElement(ElementSave element, CodeOutputElementSettings settings)
    {
        // save the file
        var fileName = GetCodeSettingsFileFor(element);
        var serialized = JsonConvert.SerializeObject(settings);
        System.IO.File.WriteAllText(fileName.FullPath, serialized);
    }

    private static FilePath GetCodeSettingsFileFor(ElementSave element)
    {
        FilePath fileName = element.GetFullPathXmlFile()!;
        fileName = fileName.RemoveExtension() + ".codsj";
        return fileName;
    }

    public static CodeOutputElementSettings LoadOrCreateSettingsFor(ElementSave element)
    {
        CodeOutputElementSettings toReturn;
        var fileName = GetCodeSettingsFileFor(element);
        if (fileName.Exists())
        {
            var contents = System.IO.File.ReadAllText(fileName.FullPath);
            toReturn = JsonConvert.DeserializeObject<Models.CodeOutputElementSettings>(contents)!;
        }
        else
        {
            toReturn = new Models.CodeOutputElementSettings();
            // As of August 3, 2022 we now have basic refactoring support
            // in place (rename, change base type) so we can probably handle
            // regen on change:
            toReturn.AutoGenerateOnChange = true;
        }
        return toReturn;
    }
}
