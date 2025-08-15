using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace Gum.PropertyGridHelpers.Converters;

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

    DateTime lastFontGet = DateTime.MinValue;
    StandardValuesCollection cachedCollection;

    public override StandardValuesCollection
                 GetStandardValues(ITypeDescriptorContext context)
    {
        // getting fonts is slow, but we don't want fonts to 
        // display missing font values if the user has just installed
        // a new font. By making this happen on a timer we avoid constantly
        // getting the font families when dragging an object.
        if(cachedCollection == null || (DateTime.Now - lastFontGet) > TimeSpan.FromSeconds(10))
        {
            lastFontGet = DateTime.Now;
            var fontFamilies = System.Drawing.FontFamily.Families;
            var familyNames = new List<string>();

            foreach (FontFamily font in fontFamilies)
            {
                familyNames.Add(font.Name);
            }

            cachedCollection = new StandardValuesCollection(familyNames);
        }


        return cachedCollection!;
    } 
}
