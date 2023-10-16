using Gum.DataTypes.Behaviors;
using System.Collections.Generic;
using ToolsUtilities;

namespace Gum.DataTypes
{
    public class ComponentSave : ElementSave
    {
        public List<ElementBehaviorReference> Behaviors { get; set; } = new List<ElementBehaviorReference>();


        public override string FileExtension
        {
            get { return GumProjectSave.ComponentExtension; }
        }


        public override string Subfolder
        {
            get { return ElementReference.ComponentSubfolder; }
        }

        public ComponentSave Clone()
        {
            var cloned = FileManager.CloneSaveObject(this);
            return cloned;

        }

    }
}
