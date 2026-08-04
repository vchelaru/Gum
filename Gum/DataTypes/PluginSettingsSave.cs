using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        [UnconditionalSuppressMessage("Trimming", "IL2026",
            Justification = "Deserializes PluginSettingsSave, which GumCommon's ILLink.Descriptors.xml preserves in full (preserve=\"all\").")]
        public static PluginSettingsSave Load(string fileName)
        {
            return FileManager.XmlDeserialize<PluginSettingsSave>(fileName);
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026",
            Justification = "Serializes this PluginSettingsSave instance, which GumCommon's ILLink.Descriptors.xml preserves in full (preserve=\"all\").")]
        public void Save(string fileName)
        {
            FileManager.XmlSerialize(this, fileName);
        }
    }
}
