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


    public override bool GetStandardValuesSupported(ITypeDescriptorContext? context)
    {
        return true;
    }

    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context)
    {
        return true;
    }

    public AvailableParentsTypeConverter(ISelectedState selectedState)
    {
        ExcludeCurrentInstance = true;
        _selectedState = selectedState ?? throw new ArgumentNullException(nameof(selectedState));
    }



    public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)
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

                values.Add(instance.Name);

                // we could go many levels deep here, but lets just do one level for now for performance:
                if (instanceComponent != null)
                {
                    if (instanceComponent.DefaultState == null)
                    {
                        throw new InvalidOperationException($"Component '{instanceComponent.Name}' has a null DefaultState. All components must have a DefaultState.");
                    }

                    var defaultChildVariable =
                        instanceComponent.DefaultState.Variables.Find(item => item.Name == "DefaultChildContainer");

                    if(defaultChildVariable?.Value is string asString && asString != string.Empty)
                    {
                        values.Add(instance.Name + "." + asString);
                    }

                    // Also add slot instances
                    var allSlots = GetAllSlotInstancesRecursively(instanceComponent);
                    foreach (var slotInstance in allSlots)
                    {
                        values.Add(instance.Name + "." + slotInstance.Name);
                    }
                }
            }
        }


        return new StandardValuesCollection(values);

    }

    private List<InstanceSave> GetAllSlotInstancesRecursively(ElementSave element)
    {
        var slots = new List<InstanceSave>();

        if (element == null)
        {
            return slots;
        }

        // Get all instances from this element and its base elements
        var allInstances = GetAllInstancesFromElementAndBase(element);

        foreach (var instance in allInstances)
        {
            // Check if this instance is marked as IsSlot (checking recursively through base elements)
            if (IsInstanceMarkedAsSlot(instance, element))
            {
                slots.Add(instance);
            }
        }

        return slots;
    }

    private List<InstanceSave> GetAllInstancesFromElementAndBase(ElementSave element)
    {
        var instances = new List<InstanceSave>();

        if (element == null)
        {
            return instances;
        }

        // Add instances from this element
        instances.AddRange(element.Instances);

        // Recursively add instances from base elements
        if (!string.IsNullOrEmpty(element.BaseType))
        {
            var baseElement = ObjectFinder.Self.GetElementSave(element.BaseType);
            if (baseElement != null)
            {
                instances.AddRange(GetAllInstancesFromElementAndBase(baseElement));
            }
        }

        return instances;
    }

    private bool IsInstanceMarkedAsSlot(InstanceSave instance, ElementSave containerElement)
    {
        if (instance == null || containerElement == null)
        {
            return false;
        }

        // Check in the current element and all its base elements
        var currentElement = containerElement;
        while (currentElement != null)
        {
            // Find the instance in this element's instances
            var instanceInCurrentElement = currentElement.Instances.FirstOrDefault(i => i.Name == instance.Name);

            if (instanceInCurrentElement != null)
            {
                // Return the IsSlot value from the most derived class (don't continue searching)
                return instanceInCurrentElement.IsSlot;
            }

            // Move to base element
            if (!string.IsNullOrEmpty(currentElement.BaseType))
            {
                currentElement = ObjectFinder.Self.GetElementSave(currentElement.BaseType);
            }
            else
            {
                currentElement = null;
            }
        }

        return false;
    }
}
