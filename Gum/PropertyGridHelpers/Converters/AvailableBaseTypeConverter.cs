using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Gum.DataTypes;
using Gum.ToolStates;

namespace Gum.PropertyGridHelpers.Converters
{
    public class AvailableBaseTypeConverter : TypeConverter
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


            values.AddRange(Enum.GetNames(typeof(StandardElementTypes)));


            foreach (ComponentSave componentSave in ProjectManager.Self.GumProjectSave.Components)
            {
                if (SelectedState.Self.SelectedComponent == null || SelectedState.Self.SelectedComponent.IsOfType(componentSave.Name) == false)
                {
                    values.Add(componentSave.Name);
                }
            }

            return new StandardValuesCollection(values);
        }

    }
}
