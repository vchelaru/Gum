using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Gum.DataTypes;
using Gum.Managers;
using Gum.ToolStates;

namespace Gum.PropertyGridHelpers.Converters
{
    public class AvailableStatesConverter : TypeConverter
    {
        private readonly ISelectedState _selectedState;
        
        InstanceSave mOverridingInstanceSave;
        ElementSave mOverridingElementSave;

        bool mUsesOverrides;

        public string CategoryName { get; private set; }

        public InstanceSave InstanceSave 
        { 
            get
            {
                if (mUsesOverrides)
                {
                    return mOverridingInstanceSave;

                }
                else
                {
                    return _selectedState.SelectedInstance;
                }
            }
            set
            {
                mOverridingInstanceSave = value;
                mUsesOverrides = true;
            }
        }
        public ElementSave ElementSave 
        { 
            get
            {
                if (mUsesOverrides)
                {
                    return mOverridingElementSave;
                }
                else
                {
                    return _selectedState.SelectedElement;

                }
            }
            set
            {
                mOverridingElementSave = value;
                mUsesOverrides = true;
            }
        }

        public AvailableStatesConverter(string category, ISelectedState selectedState)
        {
            CategoryName = category;
            _selectedState = selectedState;
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
            if (InstanceSave != null)
            {
                availableStates = GetAvailableStates(InstanceSave, CategoryName);
            }
            else
            {
                availableStates = GetAvailableStates(ElementSave, CategoryName);
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
                    // This could be defined in a base:
                    var category = elementSave.GetStateSaveCategoryRecursively(categoryName);

                    toReturn = category?.States.Select(item => item.Name).ToList();
                }
            }
            return toReturn;
        }
    }
}
