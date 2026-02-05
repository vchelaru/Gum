using Gum.DataTypes;
using Gum.Managers;
using Gum.Services;
using Gum.ToolStates;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Gum.PropertyGridHelpers.Converters;

public class AvailableInstancesConverter : TypeConverter
{
    private readonly ISelectedState _selectedState;
    
    public bool ExcludeCurrentInstance
    {
        get;
        set;
    }

    public bool IncludeScreenBounds
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
        _selectedState = Locator.GetRequiredService<ISelectedState>();
    }



    public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
    {
        List<string> values;

        var element = _selectedState.SelectedElement;

        if (element == null)
        {
            values = new List<string>();
        }
        else
        {
            values = element.Instances
                .Where(item=>item != _selectedState.SelectedInstance)
                .Select(item => item.Name)
                .ToList<string>();
        }

        values.Insert(0, "<NONE>");

        // If the selected object is an instance which is part of a component, don't let the user attach that to screen bounds:
        var isInstanceInComponent = element is ComponentSave && _selectedState.SelectedInstance != null;
        if (IncludeScreenBounds && !isInstanceInComponent)
        {
            values.Insert(1, StandardElementsManager.ScreenBoundsName);
        }

        return new StandardValuesCollection(values);

    }
}
