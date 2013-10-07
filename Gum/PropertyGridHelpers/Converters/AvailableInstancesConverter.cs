using System;
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


    }
}
