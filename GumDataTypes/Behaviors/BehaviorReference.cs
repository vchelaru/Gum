using System.IO;
using ToolsUtilities;

namespace Gum.DataTypes.Behaviors
{
    public class BehaviorReference
    {
        public const string Subfolder = "Behaviors";
        public const string Extension = "behx";

        public string Name;

        public BehaviorSave ToBehaviorSave(string projectRoot, int projectVersion = 1)
        {
            string fullName = projectRoot + Subfolder + "/" + Name + "." + Extension;

            if (FileManager.FileExists(fullName))
            {
                BehaviorSave behaviorSave = DeserializeBehavior(fullName, projectVersion);

                return behaviorSave;
            }
            else
            {
                // todo: eventually add this:
                //result.MissingFiles.Add(fullName);


                BehaviorSave behaviorSave = new BehaviorSave();

                behaviorSave.Name = Name;
                behaviorSave.IsSourceFileMissing = true;

                return behaviorSave;
            }
        }

        private static BehaviorSave DeserializeBehavior(string filePath, int projectVersion)
        {
            if (projectVersion >= (int)GumProjectSave.GumxVersions.AttributeVersion)
            {
                var (content, isCompact) = VariableSaveSerializer.ReadAndDetectFormat(filePath);
                if (isCompact)
                {
                    var compactSerializer = VariableSaveSerializer.GetCompactSerializer(typeof(BehaviorSave));
                    using var reader = new StringReader(content);
                    return (BehaviorSave)compactSerializer.Deserialize(reader);
                }
                else
                {
                    return FileManager.XmlDeserializeFromStream<BehaviorSave>(
                        new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)));
                }
            }
            return FileManager.XmlDeserialize<BehaviorSave>(filePath);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
