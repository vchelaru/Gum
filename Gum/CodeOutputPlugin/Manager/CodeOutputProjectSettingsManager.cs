using CodeOutputPlugin.Models;
using Gum.Managers;
using Gum.Services;
using Gum.ToolStates;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace CodeOutputPlugin.Manager;

public class CodeOutputProjectSettingsManager
{
    private readonly IOutputManager _outputManager;

    public CodeOutputProjectSettingsManager(IOutputManager outputManager)
    {
        _outputManager = outputManager;
    }

    public void WriteSettingsForProject(CodeOutputProjectSettings settings)
    {
        var fileName = GetProjectCodeSettingsFile();
        if(fileName != null)
        {
            var serialized = JsonConvert.SerializeObject(settings,
                // This makes debugging a little easier:
                Formatting.Indented);
            System.IO.File.WriteAllText(fileName.FullPath, serialized);
        }
    }

    private FilePath? GetProjectCodeSettingsFile()
    {
        var projectState = Locator.GetRequiredService<IProjectState>();
        if(projectState.ProjectDirectory == null)
        {
            return null;
        }
        FilePath folder = projectState.ProjectDirectory;
        if(folder == null)
        {
            return null;
        }
        else
        {
            var fileName = folder + "ProjectCodeSettings.codsj";
            return fileName;
        }
    }

    public CodeOutputProjectSettings CreateOrLoadSettingsForProject()
    {
        CodeOutputProjectSettings? toReturn = null;
        var fileName = GetProjectCodeSettingsFile();
        try
        {
            if(fileName?.Exists() == true)
            {
                var contents = System.IO.File.ReadAllText(fileName.FullPath);

                toReturn = JsonConvert.DeserializeObject<CodeOutputProjectSettings>(contents)!;
            }
        }
        catch(Exception e)
        {
            _outputManager.AddError($"Error loading project code settings from {fileName}: {e.Message}");
        }

        if(toReturn == null)
        {
            toReturn = new CodeOutputProjectSettings();

            toReturn.SetDefaults();
        }

        return toReturn;
    }
}
