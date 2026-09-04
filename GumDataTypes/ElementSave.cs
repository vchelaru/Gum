using System.Collections.Generic;
using Gum.DataTypes.Variables;
using System;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Xml.Serialization;
using ToolsUtilities;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Serialization.Json;

namespace Gum.DataTypes
{

    public abstract class ElementSave : IStateContainer, IInstanceContainer
    {

        #region Properties
        public string Name
        {
            get;
            set;
        } = "";

        public string StrippedName
        {
            get
            {
                if(Name.Contains("/"))
                {
                    return Name.Substring(Name.LastIndexOf("/") + 1);
                }
                return Name;                        
            }
        }

        public string? BaseType
        {
            get;
            set;
        }

        [XmlIgnore]
        public string FileName
        {
            get;
            set;
        }

        IList<StateSave> IStateContainer.UncategorizedStates => States;
        [XmlElement("State")]
        public List<StateSave> States
        {
            get;
            set;
        }

        IList<StateSaveCategory> IStateContainer.Categories => Categories;
        [XmlElement("Category")]
        public List<StateSaveCategory> Categories
        {
            get;
            set;
        }


        [XmlElement("Instance")]
        public List<InstanceSave> Instances
        {
            get;
            set;
        }

        [XmlIgnore]
        IEnumerable<InstanceSave> IInstanceContainer.Instances => Instances;

        [XmlElement("Event")]
        public List<EventSave> Events
        {
            get;
            set;
        }

        public abstract string Subfolder
        {
            get;
        }

        public abstract string FileExtension
        {
            get;
        }

        /// <summary>
        /// <see cref="FileExtension"/> (XML) or its JSON counterpart, matching the project's actual
        /// format. Every JSON element extension is its XML counterpart with the trailing "x" swapped
        /// for "j" (gusx-&gt;gusj, gucx-&gt;gucj, gutx-&gt;gutj), the same convention used by
        /// <see cref="ElementReference.GetExtension(bool)"/>.
        /// <para>
        /// Any code composing an element's on-disk path must use this rather than
        /// <see cref="FileExtension"/> directly - writing to the XML path inside a .gumj project
        /// saves content the project never loads back (issue #4595).
        /// </para>
        /// </summary>
        public string GetFileExtension(bool isJsonFormat)
        {
            string extension = FileExtension;
            return isJsonFormat ? extension.Substring(0, extension.Length - 1) + "j" : extension;
        }

        [XmlIgnore]
        public StateSave DefaultState
        {
            get
            {
                if (States == null || States.Count == 0)
                {
                    return null;
                }
                else
                {
                    // This may change if the user can redefine the default state as Justin asked.
                    return States[0];
                }
            }
        }

        [XmlIgnore]
        public bool IsSourceFileMissing
        {
            get;
            set;
        }

        /// <summary>
        /// Returns all states in the element including categorized states. For uncategorized states, see
        /// the States property.
        /// </summary>
        [XmlIgnore]
        public IEnumerable<StateSave> AllStates
        {
            get
            {
                if(States != null)
                {
                    foreach (var state in States)
                    {
                        yield return state;
                    }

                }
                if(Categories != null)
                {
                    foreach (var category in Categories)
                    {
                        foreach (var state in category.States)
                        {
                            yield return state;
                        }
                    }
                }
            }
        }

        public List<ElementBehaviorReference> Behaviors { get; set; } = new List<ElementBehaviorReference>();

        /// <summary>
        /// Variable names listed here are hidden in the Variables tab when editing an instance of this element,
        /// unless the variable has been explicitly set on that instance in the current state.
        /// Checked recursively up the inheritance chain via <see cref="IObjectFinder.IsVariableHiddenRecursively"/>.
        /// </summary>
        public List<string> VariablesHiddenFromInstances { get; set; } = new List<string>();

        /// <summary>
        /// Controls XML serialization — suppresses the property when the list is empty to avoid bloating saved files.
        /// </summary>
        public bool ShouldSerializeVariablesHiddenFromInstances() => VariablesHiddenFromInstances?.Count > 0;



        #endregion

        public ElementSave()
        {
            States = new List<StateSave>();
            Instances = new List<InstanceSave>();
            Events = new List<EventSave>();
            Categories = new List<StateSaveCategory>();
        }

        /// <summary>
        /// Returns the instance by name owned by this element.
        /// </summary>
        /// <remarks>
        /// This only searches the top-level for instances, but inheritance will result in DefinedByBase being set to true, so
        /// a true recursive search isn't needed.
        /// </remarks>
        /// <param name="name">The case-sensitive name of the instance.</param>
        /// <returns>The found instance, or null if no matches are found.</returns>
        public InstanceSave? GetInstance(string? name)
        {
            if(string.IsNullOrEmpty(name))
            {
                return null;
            }
            for(int i = Instances.Count-1; i > -1; i--)
            {
                if(Instances[i].Name == name)
                {
                    return Instances[i];
                }
            }
            return null;
        }

        // Gated because this file also compiles into GumDataTypesNet6.csproj (netstandard2.0),
        // which has no trim attributes.
#if NET5_0_OR_GREATER
        [UnconditionalSuppressMessage("Trimming", "IL2026",
            Justification = "this.GetType() is always an ElementSave subtype, which GumCommon's ILLink.Descriptors.xml preserves in full (preserve=\"all\").")]
#endif
        public void Save(string fileName, bool useCompactFormat = false)
        {
            // No content-sniffing between XML and JSON - the target file's own extension decides
            // the format, symmetric with ElementReference.DeserializeElement.
            string jsonExtension = GetFileExtension(isJsonFormat: true);
            bool isJsonFormat = string.Equals(FileManager.GetExtension(fileName), jsonExtension, StringComparison.OrdinalIgnoreCase);
            if (isJsonFormat)
            {
                GumJsonFileSerializer.WriteToFile(fileName, GumJsonFileSerializer.SerializeElement(this));
            }
            else if (useCompactFormat)
            {
                var serializer = GumFileSerializer.GetCompactSerializer(this.GetType());
                FileManager.XmlSerialize(this, fileName, serializer);
            }
            else
            {
                FileManager.XmlSerialize(this.GetType(), this, fileName);
            }
        }


        public override string ToString()
        {
            if (string.IsNullOrEmpty(BaseType))
            {
                return Name;
            }
            else
            {
                return $"{Name} ({BaseType})";
            }
        }
    }
}
