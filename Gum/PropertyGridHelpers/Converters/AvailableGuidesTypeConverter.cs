using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Gum.DataTypes;

namespace Gum.PropertyGridHelpers.Converters
{
    public class AvailableGuidesTypeConverter : TypeConverter
    {
        public const string NewGuideString = "<New Guide...>";
        public const string None = "<None>";

        public GumProjectSave GumProjectSave
        {
            get;
            set;
        }

        public bool ShowNewGuide { get; set; }
        public bool ShowNone { get; set; }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        public AvailableGuidesTypeConverter()
        {
            ShowNewGuide = false;
            ShowNone = true;
        }

        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<string> values = GetAvailableValues(GumProjectSave, ShowNewGuide, ShowNone);

            return new StandardValuesCollection(values);
        }


        public static List<string> GetAvailableValues(GumProjectSave gumProjectSave, bool includeNewValue, bool includeNone)
        {
            List<string> toReturn = new List<string>();
            foreach (NamedRectangle guide in gumProjectSave.Guides)
            {
                toReturn.Add(guide.Name);
            }



            if(includeNewValue)
            {
                toReturn.Add(NewGuideString);
            }

            if (includeNone)
            {
                toReturn.Add(None);
            }

            return toReturn;
        }

    }
}
