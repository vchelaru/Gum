using Gum.Managers;
using Newtonsoft.Json;
using StateAnimationPlugin.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace StateAnimationPlugin.Managers
{
    public class SettingsManager : Singleton<SettingsManager>
    {
        public AnimationPluginSettings GlobalSettings { get; private set; }

        FilePath GlobalSettingsFilePath
        {
            get
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + 
                    @"\Gum\AnimationPlugin\GlobalAnimationSettings.json";
            }
        }

        public void LoadOrCreateSettings()
        {
            if (GlobalSettingsFilePath.Exists())
            {
                var text = System.IO.File.ReadAllText(GlobalSettingsFilePath.FullPath);

                GlobalSettings = JsonConvert.DeserializeObject<AnimationPluginSettings>(text);
            }
            else
            {
                GlobalSettings = new AnimationPluginSettings();

            }
        }

        public void SaveSettings()
        {
            var text = JsonConvert.SerializeObject(GlobalSettings);

            var directory = GlobalSettingsFilePath.GetDirectoryContainingThis();

            System.IO.Directory.CreateDirectory(directory.FullPath);

            System.IO.File.WriteAllText(GlobalSettingsFilePath.FullPath, text);
        }


    }
}
