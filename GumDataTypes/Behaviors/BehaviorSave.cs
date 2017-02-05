using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ToolsUtilities;

namespace Gum.DataTypes.Behaviors
{
    public class BehaviorSave : IStateContainer, IStateCategoryListContainer
    {
        public string Name { get; set; }

        [XmlIgnore]
        public bool IsSourceFileMissing
        {
            get;
            set;
        }

        static List<StateSave> EmptyList = new List<StateSave>();
        IList<StateSave> IStateContainer.UncategorizedStates => EmptyList;

        public StateSave RequiredVariables { get; set; } = new StateSave();

        IEnumerable<StateSaveCategory> IStateContainer.Categories => Categories;
        [XmlElement("Category")]
        public List<StateSaveCategory> Categories { get; set; } = new List<StateSaveCategory>();

        public IEnumerable<StateSave> AllStates
        {
            get
            {
                return Categories.SelectMany(item => item.States);
            }
        }

        public override string ToString()
        {
            return Name;
        }


        public void Save(string fileName)
        {
#if WINDOWS_8 || UWP

            throw new NotImplementedException();
#else
            FileManager.XmlSerialize(this.GetType(), this, fileName);
#endif
        }
    }
}
