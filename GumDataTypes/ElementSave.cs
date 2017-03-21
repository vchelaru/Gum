using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes.Variables;
using System.Xml.Serialization;
using ToolsUtilities;

namespace Gum.DataTypes
{

    public abstract class ElementSave : IStateContainer, IStateCategoryListContainer
    {

        #region Properties
        public string Name
        {
            get;
            set;
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

        IEnumerable<StateSaveCategory> IStateContainer.Categories => Categories;
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

        [XmlIgnore]
        public IEnumerable<StateSave> AllStates
        {
            get
            {
                foreach (var state in States)
                {
                    yield return state;
                }

                foreach (var category in Categories)
                {
                    foreach (var state in category.States)
                    {
                        yield return state;
                    }
                }
            }
        }

        #endregion

        public ElementSave()
        {
            States = new List<StateSave>();
            Instances = new List<InstanceSave>();
            Events = new List<EventSave>();
            Categories = new List<StateSaveCategory>();
        }

        public InstanceSave GetInstance(string name)
        {
            return Instances.FirstOrDefault(i => i.Name == name);
        }

        public void Save(string fileName)
        {
#if WINDOWS_8 || UWP
            throw new NotImplementedException();
#else
            FileManager.XmlSerialize(this.GetType(), this, fileName);
#endif
        }


        public override string ToString()
        {
            if (!string.IsNullOrEmpty(BaseType))
            {
                return Name;
            }
            else
            {
                return BaseType + " " + Name;
            }
        }
    }
}
