using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Gum.PropertyGridHelpers.Converters
{
    public class RestrictiveEnumConverter : EnumConverter
    {
        public List<object> ValuesToExclude = new List<object>();

        public RestrictiveEnumConverter(Type type)
            : base(type)
        {

        }

        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<object> valuesToReturn = new List<object>();

            Array values = Enum.GetValues(EnumType);

            foreach (object value in values)
            {
                if (!ValuesToExclude.Contains(value))
                {
                    valuesToReturn.Add(value);
                }
            }

            return new StandardValuesCollection(valuesToReturn);
        }
    }
}
