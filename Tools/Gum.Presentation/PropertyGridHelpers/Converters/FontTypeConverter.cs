using Gum.Services.Fonts;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Gum.PropertyGridHelpers.Converters;

/// <summary>The Font variable's drop-down: the installed font families, re-read at most every 10 seconds.</summary>
class FontTypeConverter : TypeConverter
{
    private readonly IInstalledFontProvider _fontProvider;

    public FontTypeConverter(IInstalledFontProvider fontProvider)
    {
        _fontProvider = fontProvider;
    }

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
            List<string> familyNames = new List<string>(_fontProvider.GetInstalledFontFamilyNames());

            cachedCollection = new StandardValuesCollection(familyNames);
        }


        return cachedCollection!;
    } 
}
