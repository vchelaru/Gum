using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ToolsUtilities;

namespace Gum.DataTypes
{
    public class InstanceSave
    {
        public string Name;
        public string BaseType;
        public bool DefinedByBase;

        public bool Locked
        {
            get;
            set;
        }
        public bool ShouldSerializeLocked()
        {
            return Locked == true;
        }

        [XmlIgnore]
        public ElementSave ParentContainer
        {
            get;
            set;
        }

        // Modify Clone if adding any XmlIgnored properties

        public InstanceSave Clone()
        {
            InstanceSave cloned = FileManager.CloneSaveObject(this);
            cloned.ParentContainer = this.ParentContainer;
            return cloned;

        }

        public override string ToString()
        {
            ElementSave parentContainer = ParentContainer;

            if (parentContainer == null)
            {
                return BaseType + " " + Name;
            }
            else
            {
                return BaseType + " " + Name + " in " + parentContainer;
            }
        }
    }
}
