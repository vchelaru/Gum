using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Gum.DataTypes;
using Gum.Managers;
using Gum.ToolStates;

namespace Gum.PropertyGridHelpers.Converters
{
    public class AvailableStatesConverter : TypeConverter
    {

        string mCategory;

        public AvailableStatesConverter(string category)
        {
            mCategory = category;
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
            List<string> availableStates = new List<string>();
            if (SelectedState.Self.SelectedInstance != null)
            {
                availableStates = GetAvailableStates(SelectedState.Self.SelectedInstance, mCategory);
            }
            else
            {
                availableStates = GetAvailableStates(SelectedState.Self.SelectedElement, mCategory);
            }

            return new StandardValuesCollection(availableStates);
        }

        public static List<string> GetAvailableStates(InstanceSave instanceSave, string categoryName)
        {

            ElementSave elementSave = ObjectFinder.Self.GetElementSave(instanceSave.BaseType);

            List<string> toReturn = GetAvailableStates(elementSave, categoryName);

            return toReturn;
        }

        private static List<string> GetAvailableStates(ElementSave elementSave, string categoryName)
        {
            List<string> toReturn = new List<string>();


            if (elementSave != null)
            {
                if (string.IsNullOrEmpty(categoryName))
                {
                    toReturn = elementSave.States.Select(item => item.Name).ToList();
                }
                else
                {
                    var category = elementSave.Categories.FirstOrDefault(item => item.Name == categoryName);
                    if (category != null)
                    {
                        toReturn = category.States.Select(item => item.Name).ToList();
                    }
                }
            }
            return toReturn;
        }
    }
}
