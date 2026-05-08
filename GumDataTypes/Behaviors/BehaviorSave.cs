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

        /// <summary>
        /// Forms properties this behavior promotes to its implementing components. Each entry's
        /// <see cref="VariableSave.Name"/> must match a property on the corresponding
        /// <c>FrameworkElement</c> subclass (e.g. <c>ToolTip</c>, <c>IsEnabled</c>); the value
        /// is the design-time default. Surfaced in the Gum tool's variable grid and reflected
        /// onto the wrapped Forms control at runtime.
        /// </summary>
        [XmlElement("FormsProperty")]
        public List<VariableSave> FormsProperties { get; set; } = new List<VariableSave>();

        /// <summary>
        /// Variable reference assignments (e.g. <c>ButtonCategoryState = IsEnabled ? "Enabled" : "Disabled"</c>)
        /// that the Gum tool evaluates at design time to drive wireframe preview from <see cref="FormsProperties"/>.
        /// Unlike state-level <c>VariableReferences</c>, these are NEVER traversed by the runtime apply pass —
        /// at runtime the Forms control's own setter (e.g. <c>FrameworkElement.IsEnabled</c>) owns the visual
        /// state, so applying them again would double-write. Structural separation by name is the contract.
        /// </summary>
        [XmlElement("ToolOnlyVariableReference")]
        public List<string> ToolOnlyVariableReferences { get; set; } = new List<string>();

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


        public void Save(string fileName, bool useCompactFormat = false)
        {
            if (useCompactFormat)
            {
                var serializer = GumFileSerializer.GetCompactSerializer(this.GetType());
                FileManager.XmlSerialize(this, fileName, serializer);
            }
            else
            {
                FileManager.XmlSerialize(this.GetType(), this, fileName);
            }
        }
    }
}
