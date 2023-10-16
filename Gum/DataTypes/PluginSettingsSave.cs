using System.Collections.Generic;
using ToolsUtilities;

namespace Gum.DataTypes
{
    public class PluginSettingsSave
    {
        public List<string> DisabledPlugins
        {
            get;
            set;
        }




        public PluginSettingsSave()
        {
            DisabledPlugins = new List<string>();
        }

        public static PluginSettingsSave Load(string fileName)
        {
            return FileManager.XmlDeserialize<PluginSettingsSave>(fileName);
        }

        public void Save(string fileName)
        {
            FileManager.XmlSerialize(this, fileName);
        }
    }
}
