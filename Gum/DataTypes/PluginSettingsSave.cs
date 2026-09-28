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
        [UnconditionalSuppressMessage("AOT", "IL3050",
            Justification = "XmlSerializer falls back to reflection-only serialization when dynamic code is unsupported (Native AOT), so no code is generated at runtime; the IL2026 suppression above covers the trimming side.")]
        public static PluginSettingsSave Load(string fileName)
        {
            return FileManager.XmlDeserialize<PluginSettingsSave>(fileName);
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026",
            Justification = "Serializes this PluginSettingsSave instance, which GumCommon's ILLink.Descriptors.xml preserves in full (preserve=\"all\").")]
        [UnconditionalSuppressMessage("AOT", "IL3050",
            Justification = "XmlSerializer falls back to reflection-only serialization when dynamic code is unsupported (Native AOT), so no code is generated at runtime; the IL2026 suppression above covers the trimming side.")]
        public void Save(string fileName)
        {
            FileManager.XmlSerialize(this, fileName);
        }
    }
}
