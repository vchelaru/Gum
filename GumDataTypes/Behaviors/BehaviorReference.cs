using System.IO;
using ToolsUtilities;

namespace Gum.DataTypes.Behaviors
{
    public class BehaviorReference
    {
        public const string Subfolder = "Behaviors";
        public const string Extension = "behx";

        public string Name;

        /// <summary>
        /// Optional path (relative to the project root, may traverse outside it via "../") to the
        /// backing .behx file, for a behavior linked from a shared location instead of physically
        /// copied into this project's own Behaviors folder. Null/empty falls back to the
        /// conventional <c>Behaviors/{Name}.behx</c> location under the project root.
        /// </summary>
        public string SourcePath;

        /// <summary>
        /// Optional per-project override for the backing behavior's <see cref="BehaviorSave.DefaultImplementation"/>,
        /// applied after loading. Lets a project link to a shared <see cref="SourcePath"/> behavior
        /// while still supplying its own default-visual path (e.g. each Forms theme's own
        /// default button component), without writing a theme-specific value into the shared file.
        /// </summary>
        public string DefaultImplementationOverride;

        public string GetRelativeFilePath()
        {
            return string.IsNullOrEmpty(SourcePath)
                ? Subfolder + "/" + Name + "." + Extension
                : SourcePath;
        }

        public BehaviorSave ToBehaviorSave(string projectRoot, int projectVersion = 1)
        {
            string fullName = projectRoot + GetRelativeFilePath();

            if (FileManager.FileExists(fullName))
            {
                BehaviorSave behaviorSave = DeserializeBehavior(fullName, projectVersion);

                if (!string.IsNullOrEmpty(DefaultImplementationOverride))
                {
                    behaviorSave.DefaultImplementation = DefaultImplementationOverride;
                }

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

        public static BehaviorSave DeserializeBehavior(string filePath, int projectVersion)
        {
            if (projectVersion >= (int)GumProjectSave.GumxVersions.AttributeVersion)
            {
                string content = FileManager.FromFileText(filePath);
                return GumFileSerializer.DeserializeBehaviorSave(content, projectVersion);
            }
            return FileManager.XmlDeserialize<BehaviorSave>(filePath);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
