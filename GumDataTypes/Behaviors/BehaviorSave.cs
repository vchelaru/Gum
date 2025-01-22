using Gum.DataTypes.Variables;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using ToolsUtilities;

namespace Gum.DataTypes.Behaviors
{
    public class BehaviorSave : IStateContainer, IInstanceContainer
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

        IList<StateSaveCategory> IStateContainer.Categories => Categories;
        [XmlElement("Category")]
        public List<StateSaveCategory> Categories { get; set; } = new List<StateSaveCategory>();

        public IEnumerable<StateSave> AllStates
        {
            get
            {
                return Categories.SelectMany(item => item.States);
            }
        }

        [XmlArray("RequiredInstances")]
        [XmlArrayItem(ElementName = "InstanceSave")]
        public List<BehaviorInstanceSave> RequiredInstances { get; set; } = new List<BehaviorInstanceSave>();

        [XmlIgnore]
        IEnumerable<InstanceSave> IInstanceContainer.Instances => RequiredInstances.ToList<InstanceSave>();

        // Normally we reference the model type, but animations are in a plugin, so we can't do that here.
        // I did try moving the animation classes (just the models) from the plugin into the GumDataTypes, but
        // that required also bringing over interpolation so I didn't.
        public List<string> RequiredAnimations { get; set; } = new List<string>();

        public string DefaultImplementation { get; set; }

        public override string ToString()
        {
            return Name;
        }


        public void Save(string fileName)
        {
            FileManager.XmlSerialize(this.GetType(), this, fileName);
        }
    }
}
