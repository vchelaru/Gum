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
            List<string> availableStates = new List<string>();
            if (SelectedState.Self.SelectedInstance != null)
            {
                availableStates = GetAvailableStates(SelectedState.Self.SelectedInstance);
            }
            else
            {
                availableStates = GetAvailableStates(SelectedState.Self.SelectedElement);
            }

            return new StandardValuesCollection(availableStates);
        }

        public static List<string> GetAvailableStates(InstanceSave instanceSave)
        {

            ElementSave elementSave = ObjectFinder.Self.GetElementSave(instanceSave.BaseType);

            List<string> toReturn = GetAvailableStates(elementSave);

            return toReturn;
        }

        private static List<string> GetAvailableStates(ElementSave elementSave)
        {
            List<string> toReturn = new List<string>();


            if (elementSave != null)
            {
                foreach (var state in elementSave.States)
                {
                    toReturn.Add(state.Name);
                }
            }
            return toReturn;
        }
    }
}
