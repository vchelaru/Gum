using Gum.DataTypes;
using Gum.Managers;
using Gum.Services;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Gum.PropertyGridHelpers.Converters
{
    public class AvailableContainedTypeConverter : TypeConverter
    {
        private readonly ISelectedState _selectedState;
        private readonly IProjectManager _projectManager;

        public AvailableContainedTypeConverter()
        {
            _selectedState = Locator.GetRequiredService<ISelectedState>();
            _projectManager = Locator.GetRequiredService<IProjectManager>();
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
            List<string> values = new List<string>();

            values.Add("");

            values.AddRange(Enum.GetNames(typeof(StandardElementTypes)));

            foreach (ComponentSave componentSave in _projectManager.GumProjectSave.Components)
            {
                // Currently we allow any type. We may want to make sure we don't include the current type....or do we?
                //if (element == null || element.IsOfType(componentSave.Name) == false || element.Name == instance?.BaseType)
                {
                    values.Add(componentSave.Name);
                }
            }

            var instance = _selectedState.SelectedInstance;
            if(instance != null)
            {
                var instanceName = instance.Name;
                var currentElement = _selectedState.SelectedElement;

                foreach(var otherInstance in currentElement.Instances)
                {
                    var parentVariableName = $"{otherInstance.Name}.Parent";

                    var variable = currentElement.DefaultState.Variables.Find(item => item.Name == parentVariableName);

                    if(variable != null && variable.Value is string asString && asString == instanceName)
                    {
                        // this instance is a child of the current instance, so let's get that type:
                        var otherInstanceElement = ObjectFinder.Self.GetElementSave(otherInstance);

                        if(otherInstanceElement != null && values.Contains(otherInstanceElement.Name))
                        {
                            values.Remove(otherInstanceElement.Name);
                            values.Insert(0, otherInstanceElement.Name);
                        }
                    }
                }
            }

            return new StandardValuesCollection(values);
        }
    }
}
