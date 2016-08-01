using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

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
        IEnumerable<StateSave> IStateContainer.UncategorizedStates => EmptyList;


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

    }
}
