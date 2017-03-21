using Gum.DataTypes.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public ElementSave Clone()
        {
            ElementSave cloned = FileManager.CloneSaveObject(this);
            return cloned;

        }

    }
}
