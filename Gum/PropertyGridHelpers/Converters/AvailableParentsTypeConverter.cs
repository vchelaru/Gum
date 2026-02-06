using Gum.DataTypes;
using Gum.Managers;
using Gum.Services;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.ComponentModel.TypeConverter;

namespace Gum.PropertyGridHelpers.Converters;

public class AvailableParentsTypeConverter : TypeConverter
{
    private readonly ISelectedState _selectedState;

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

    public AvailableParentsTypeConverter()
    {
        ExcludeCurrentInstance = true;
        _selectedState = Locator.GetRequiredService<ISelectedState>();
    }



    public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
    {
        List<string> values = new();

        var element = _selectedState.SelectedElement;

        values.Add("<NONE>");

        if (element != null)
        {
            foreach(var instance in element.Instances)
            {
                if(instance == _selectedState.SelectedInstance)
                {
                    // This is the current instance, skip it
                    continue;
                }

                var instanceComponent = ObjectFinder.Self.GetElementSave(instance) as ComponentSave;

                var nameToAdd = instance.Name;

                // we could go many levels deep here, but lets just do one level for now for performance:
                if (instanceComponent != null)
                {
                    var defaultChildVariable = 
                        instanceComponent.DefaultState.Variables.Find(item => item.Name == "DefaultChildContainer");

                    if(defaultChildVariable?.Value is string asString && asString != string.Empty)
                    {
                        nameToAdd += "." + asString;
                    }
                }

                values.Add(nameToAdd);
            }
        }


        return new StandardValuesCollection(values);

    }
}
