using System.Collections.Generic;
using System.ComponentModel;
using Gum.DataTypes;

namespace Gum.PropertyGridHelpers.Converters
{
    public class AvailableBaseTypeConverter : TypeConverter
    {
        ElementSave elementViewing;
        InstanceSave instance;

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        public AvailableBaseTypeConverter(ElementSave element, InstanceSave instance) : base()
        {
            this.elementViewing = element;
            this.instance = instance;
        }

        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<string> values = new List<string>();

            var gumProject = ProjectManager.Self.GumProjectSave;

            if (elementViewing is ScreenSave)
            {
                values.Add("");
                foreach(ScreenSave screenSave in gumProject.Screens)
                {
                    if(elementViewing.IsOfType(screenSave.Name) == false)
                    {
                        values.Add(screenSave.Name);
                    }
                }
            }
            else
            {
                // We used to use this enum, but why not just use the standard elements becuase those 
                // can be modified by plugins:
                //values.AddRange(Enum.GetNames(typeof(StandardElementTypes)));
                foreach(var standard in gumProject.StandardElements)
                {
                    // Component is a special base type but we don't actually want to inherit from component.
                    // The closest match would actually be "Container" so let's use that...
                    if(standard.Name != "Component")
                    {
                        values.Add(standard.Name);
                    }
                }

                foreach (ComponentSave componentSave in gumProject.Components)
                {
                    //var shouldShow = element == null || element.IsOfType(componentSave.Name) == false || element.Name == instance?.BaseType;
                    var shouldShow = elementViewing == null || elementViewing.Name != componentSave.Name || elementViewing.Name == instance?.BaseType;

                    if(shouldShow)
                    {
                        values.Add(componentSave.Name);
                    }
                }

            }

            return new StandardValuesCollection(values);
        }

    }
}
