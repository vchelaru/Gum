using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Gum.DataTypes;
using Gum.Managers;
using Gum.ToolStates;

namespace Gum.PropertyGridHelpers.Converters
{
    public class AvailableStatesConverter : TypeConverter
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
            List<string> availableStates = GetAvailableStates(SelectedState.Self.SelectedInstance);

            return new StandardValuesCollection(availableStates);
        }

        public static List<string> GetAvailableStates(InstanceSave instanceSave)
        {
            List<string> toReturn = new List<string>();

            ElementSave elementType = ObjectFinder.Self.GetElementSave(instanceSave.BaseType);

            if (elementType != null)
            {
                foreach (var state in elementType.States)
                {
                    toReturn.Add(state.Name);
                }
            }


            return toReturn;
        }
    }
}
