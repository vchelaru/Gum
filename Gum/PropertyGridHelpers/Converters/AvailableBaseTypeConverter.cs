using System;
using System.Collections.Generic;
using System.ComponentModel;
using Gum.DataTypes;
using Gum.Managers;

namespace Gum.PropertyGridHelpers.Converters
{
    public class AvailableBaseTypeConverter : TypeConverter
    {
        ElementSave elementViewing;
        InstanceSave instance;
        private StandardValuesCollection standardValues;

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        public AvailableBaseTypeConverter(ElementSave instanceOwner, InstanceSave instance) : base()
        {
            this.elementViewing = instanceOwner;
            this.instance = instance;

            CacheStandardValuesCollection();

        }

        private void CacheStandardValuesCollection()
        {
            List<string> values = new List<string>();

            var gumProject = ProjectManager.Self.GumProjectSave;

            ElementSave? effectiveElement = elementViewing;
            if (instance != null)
            {
                effectiveElement = ObjectFinder.Self.GetElementSave(instance);
            }


            if (effectiveElement is ScreenSave && instance == null)
            {
                values.Add("");
                foreach (ScreenSave screenSave in gumProject.Screens)
                {
                    if (effectiveElement.IsOfType(screenSave.Name) == false)
                    {
                        values.Add(screenSave.Name);
                    }
                }
            }
            else
            {
                foreach (var standard in gumProject.StandardElements)
                {
                    // Component is a special base type but we don't actually want to inherit from component.
                    if (standard.Name != "Component")
                    {
                        values.Add(standard.Name);
                    }
                }

                foreach (ComponentSave componentSave in gumProject.Components)
                {
                    //var shouldShow = element == null || element.IsOfType(componentSave.Name) == false || element.Name == instance?.BaseType;
                    var shouldShow = effectiveElement == null || effectiveElement.Name != componentSave.Name || effectiveElement.Name == instance?.BaseType;

                    if (shouldShow)
                    {
                        values.Add(componentSave.Name);
                    }
                }

            }

            standardValues = new StandardValuesCollection(values);
        }

        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) => standardValues;

    }
}
