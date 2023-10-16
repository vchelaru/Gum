using System.Collections.Generic;
using System.Xml.Serialization;

namespace Gum.DataTypes.Behaviors
{
    // We use the InstanceSave name so this is compatible with old .behx files
    [XmlRootAttribute(nameof(InstanceSave))]
    public class BehaviorInstanceSave : InstanceSave
    {
        public List<BehaviorReference> Behaviors { get; set; } = new List<BehaviorReference>(); 
    }
}
