using System.Collections.Generic;
using System.ComponentModel;

namespace Gum.PropertyGridHelpers.Converters
{
    public class StringOptionsTypeConverter :  TypeConverter
    {
        public List<string> Options
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



        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(Options);
        }

    }
}
