using Gum.ToolStates;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Gum.PropertyGridHelpers.Converters
{
    public class AvailableInstancesConverter : TypeConverter
    {
        public bool ExcludeCurrentInstance
        {
            get;
            set;
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        public AvailableInstancesConverter()
        {
            ExcludeCurrentInstance = true;
        }



        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<string> values;

            var element = SelectedState.Self.SelectedElement;

            if (element == null)
            {
                values = new List<string>();
            }
            else
            {
                values = SelectedState.Self.SelectedElement.Instances
                    .Where(item=>item != SelectedState.Self.SelectedInstance)
                    .Select(item => item.Name)
                    .ToList<string>();
            }

            values.Insert(0, "<NONE>");


            return new StandardValuesCollection(values);

        }
    }
}
