using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Gum.DataTypes;
using Gum.ToolStates;

namespace Gum.PropertyGridHelpers.Converters
{
    public class AvailableBaseTypeConverter : TypeConverter
    {
        ElementSave element;
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
            this.element = element;
            this.instance = instance;
        }

        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<string> values = new List<string>();

            if(element is ScreenSave)
            {
                values.Add("");
                foreach(ScreenSave screenSave in ProjectManager.Self.GumProjectSave.Screens)
                {
                    if(element.IsOfType(screenSave.Name) == false)
                    {
                        values.Add(screenSave.Name);
                    }
                }
            }
            else
            {
                values.AddRange(Enum.GetNames(typeof(StandardElementTypes)));

                foreach (ComponentSave componentSave in ProjectManager.Self.GumProjectSave.Components)
                {
                    if (element == null || element.IsOfType(componentSave.Name) == false || element.Name == instance?.BaseType)
                    {
                        values.Add(componentSave.Name);
                    }
                }

            }

            return new StandardValuesCollection(values);
        }

    }
}
