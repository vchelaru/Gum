using CodeOutputPlugin.Models;
using Gum.ToolStates;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace CodeOutputPlugin.Manager
{
    public static class CodeOutputProjectSettingsManager
    {
        public static void WriteSettingsForProject(CodeOutputProjectSettings settings)
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

        private static FilePath? GetProjectCodeSettingsFile()
        {
            FilePath folder = GumState.Self.ProjectState.ProjectDirectory;
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

        public static CodeOutputProjectSettings CreateOrLoadSettingsForProject()
        {
            CodeOutputProjectSettings toReturn;
            var fileName = GetProjectCodeSettingsFile();
            if(fileName?.Exists() == true)
            {
                var contents = System.IO.File.ReadAllText(fileName.FullPath);

                toReturn = JsonConvert.DeserializeObject<CodeOutputProjectSettings>(contents)!;
            }
            else
            {
                toReturn = new CodeOutputProjectSettings();

                toReturn.SetDefaults();
            }

            return toReturn;
        }
    }
}
