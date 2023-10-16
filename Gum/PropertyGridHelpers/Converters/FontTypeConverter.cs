using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace Gum.PropertyGridHelpers.Converters
{
    class FontTypeConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        static List<string> stringToReturn = new List<string>();
        public override StandardValuesCollection
                     GetStandardValues(ITypeDescriptorContext context)
        {
            stringToReturn.Clear();

            foreach (FontFamily font in System.Drawing.FontFamily.Families)
            {
                stringToReturn.Add(font.Name);
            }


            StandardValuesCollection svc = new StandardValuesCollection(stringToReturn);

            return svc;
        } 







    }
}
