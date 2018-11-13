using Gum.DataTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.PropertyGridHelpers.Converters
{
    public class AvailableContainedTypeConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<string> values = new List<string>();

            values.Add("");

            values.AddRange(Enum.GetNames(typeof(StandardElementTypes)));

            foreach (ComponentSave componentSave in ProjectManager.Self.GumProjectSave.Components)
            {
                // Currently we allow any type. We may want to make sure we don't include the current type....or do we?
                //if (element == null || element.IsOfType(componentSave.Name) == false || element.Name == instance?.BaseType)
                {
                    values.Add(componentSave.Name);
                }
            }

            return new StandardValuesCollection(values);
        }
    }
}
