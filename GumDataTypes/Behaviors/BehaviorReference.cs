using System;
using System.IO;
using Gum.DataTypes.Serialization.Json;
using ToolsUtilities;

namespace Gum.DataTypes.Behaviors
{
    public class BehaviorReference
    {
        public const string Subfolder = "Behaviors";
        public const string Extension = "behx";

        /// <summary>JSON counterpart of <see cref="Extension"/>. See <see cref="GumJsonFileSerializer"/>.</summary>
        public const string JsonExtension = "behj";

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

        /// <summary>
        /// Returns the relative path to this behavior's backing file. When <see cref="SourcePath"/>
        /// is set it always wins (verbatim, whatever extension it carries); otherwise the conventional
        /// <c>Behaviors/{Name}</c> path is built using <paramref name="isJsonFormat"/> to pick
        /// <see cref="JsonExtension"/> or <see cref="Extension"/>.
        /// </summary>
        public string GetRelativeFilePath(bool isJsonFormat = false)
        {
            return string.IsNullOrEmpty(SourcePath)
                ? Subfolder + "/" + Name + "." + (isJsonFormat ? JsonExtension : Extension)
                : SourcePath;
        }

        public BehaviorSave ToBehaviorSave(string projectRoot, int projectVersion = 1, bool isJsonFormat = false)
        {
            string fullName = projectRoot + GetRelativeFilePath(isJsonFormat);

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
            // No content-sniffing between XML and JSON - the file's own extension determines the format.
            if (string.Equals(FileManager.GetExtension(filePath), JsonExtension, StringComparison.OrdinalIgnoreCase))
            {
                string jsonContent = FileManager.FromFileText(filePath);
                return GumJsonFileSerializer.DeserializeBehavior(jsonContent);
            }

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
