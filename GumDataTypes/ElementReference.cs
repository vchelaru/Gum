using System;
using System.IO;
using ToolsUtilities;

namespace Gum.DataTypes
{
    public enum ElementType
    {
        Screen,
        Component,
        Standard
    }

    public enum LinkType
    {
        ReferenceOriginal,
        CopyLocally
    }

    public class ElementReference
    {
        public const string ScreenSubfolder = "Screens";
        public const string ComponentSubfolder = "Components";
        public const string StandardSubfolder = "Standards";

        public ElementType ElementType
        {
            get;
            set;
        }

        public LinkType LinkType { get; set; }

        public string Extension
        {
            get
            {
                switch (ElementType)
                {
                    case DataTypes.ElementType.Standard:
                        return GumProjectSave.StandardExtension;
                    case DataTypes.ElementType.Component:
                        return GumProjectSave.ComponentExtension;
                    case DataTypes.ElementType.Screen:
                        return GumProjectSave.ScreenExtension;
                }
                throw new InvalidOperationException();
            }
        }

        public string Subfolder
        {
            get
            {
                switch (ElementType)
                {
                    case DataTypes.ElementType.Standard:
                        return StandardSubfolder;
                    case DataTypes.ElementType.Component:
                        return ComponentSubfolder;
                    case DataTypes.ElementType.Screen:
                        return ScreenSubfolder;
                }
                throw new InvalidOperationException();
            }
        }

        public string Name;

        /// <summary>
        /// The location of the file relative to the project if it differs from the Name. By default
        /// this will be empty, so the Name will be used to load/save the element. However, if this is not null,
        /// then this value is used instead to load the referenced element.
        /// </summary>
        public string Link { get; set; }

        //public ElementSave ToElementSave(string projectroot, string extension)
        //{
        //    string fullName = projectroot + Subfolder + "/" + Name + "." + extension;

        //    ElementSave elementSave = FileManager.XmlDeserialize<ElementSave>(fullName);

        //    return elementSave;
        //}

        public T ToElementSave<T>(string projectroot, string extension, GumLoadResult result, LinkLoadingPreference linkLoadingPreference = LinkLoadingPreference.PreferLinked, int projectVersion = 1) where T : ElementSave, new()
        {
            FilePath linkedName = null;
            FilePath containedReferenceName = null;

            if (!string.IsNullOrWhiteSpace(this.Link))
            {
                linkedName = projectroot + this.Link;

            }
            containedReferenceName = projectroot + Subfolder + "/" + Name + "." + extension;

            if (linkedName != null && ToolsUtilities.FileManager.IsRelative(linkedName.Original))
            {
                linkedName = ToolsUtilities.FileManager.RelativeDirectory + linkedName.Original;
            }
            if (ToolsUtilities.FileManager.IsRelative(containedReferenceName.Original))
            {
                containedReferenceName = ToolsUtilities.FileManager.RelativeDirectory + containedReferenceName.Original;
            }

            var usesTitleContainer = false;
#if ANDROID || IOS
            usesTitleContainer = true;
#elif NET6_0_OR_GREATER
            usesTitleContainer = System.OperatingSystem.IsAndroid() ||
                                 System.OperatingSystem.IsIOS() ||
                                 System.OperatingSystem.IsBrowser() ||
                                 FileManager.CustomGetStreamFromFile != null;
#endif



            if (linkedName?.Exists() == true)
            {
                T elementSave = DeserializeElement<T>(linkedName.FullPath, projectVersion);
                return elementSave;
            }
#if ANDROID || IOS
            else if (containedReferenceName != null && (linkedName == null || linkLoadingPreference == LinkLoadingPreference.PreferLinked))
#else
            else if (  ((usesTitleContainer && containedReferenceName != null) ||  containedReferenceName.Exists()) && (linkedName == null || linkLoadingPreference == LinkLoadingPreference.PreferLinked))
#endif
            {
                T elementSave = DeserializeElement<T>(
#if ANDROID || IOS
                    containedReferenceName.StandardizedCaseSensitive,
#else
                    containedReferenceName.FullPath,
#endif
                    projectVersion);

                if (Name != elementSave.Name)
                {
                    // The file name doesn't match the name of the element.  This can cause errors
                    // at runtime so let's tell the user:
                    result.ErrorMessage += "\nThe project references an element named " + Name + ", but the XML for this element has its name set to " + elementSave.Name + "\n";
                }

                return elementSave;
            }
            else
            {
                // I don't think we want to consider this an error anymore
                // because Gum can handle it - it doesn't allow saving that 
                // individual element and it shows a red ! next to the element.
                // We should just tolerate this and let the user deal with it.
                // If we do treat this as an error, then Gum goes into a state 
                // where it can't save anything.
                //errors += "\nCould not find the file name " + fullName;
                // Update Feb 20, 2015
                // But we can record it:
                result.MissingFiles.Add(containedReferenceName.FullPath);


                T elementSave = new T();

                elementSave.Name = Name;
                elementSave.IsSourceFileMissing = true;

                return elementSave;
            }
        }

        public static T DeserializeElement<T>(string filePath, int projectVersion) where T : ElementSave, new()
        {
            if (projectVersion >= (int)GumProjectSave.GumxVersions.AttributeVersion)
            {
                var (content, isCompact) = VariableSaveSerializer.ReadAndDetectFormat(filePath);
                if (isCompact)
                {
                    // Transitional files saved before instance compaction have compact variables
                    // but instances still as child elements — use the legacy-instances serializer.
                    bool hasLegacyInstances = content.Contains("<Instance>");
                    var serializer = hasLegacyInstances
                        ? VariableSaveSerializer.GetLegacyInstancesCompactSerializer(typeof(T))
                        : VariableSaveSerializer.GetCompactSerializer(typeof(T));
                    using var reader = new StringReader(content);
                    return (T)serializer.Deserialize(reader);
                }
                else
                {
                    using var reader = new StringReader(content);
                    return FileManager.XmlDeserializeFromStream<T>(
                        new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)));
                }
            }
            return FileManager.XmlDeserialize<T>(filePath);
        }

        public override string ToString()
        {
            return Name;
        }

    }



}
