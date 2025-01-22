using System.Collections.Generic;
using Gum.DataTypes.Variables;
using System.Xml.Serialization;
using ToolsUtilities;
using Gum.DataTypes.Behaviors;

namespace Gum.DataTypes
{

    public abstract class ElementSave : IStateContainer, IInstanceContainer
    {

        #region Properties
        public string Name
        {
            get;
            set;
        }

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

        public string BaseType
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
        public InstanceSave GetInstance(string name)
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

        public void Save(string fileName)
        {
            FileManager.XmlSerialize(this.GetType(), this, fileName);
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
