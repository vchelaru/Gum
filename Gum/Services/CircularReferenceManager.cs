using Gum.DataTypes;
using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Services;
public class CircularReferenceManager : ICircularReferenceManager
{
    private readonly IObjectFinder _objectFinder;

    public CircularReferenceManager(IObjectFinder objectFinder)
    {
        _objectFinder = objectFinder;
    }

    public bool CanTypeBeAddedToElement(ElementSave parent, string typeToAdd)
    {
        var typeElement = _objectFinder.GetElementSave(typeToAdd);
        if(typeElement != null)
        {
            var baseTypes = _objectFinder.GetBaseElements(typeElement);
            if(baseTypes.Contains(parent))
            {
                return false;
            }


            var referencedTypes = new HashSet<string>();
            GetAllReferencedTypesFor(typeElement, referencedTypes);
            if (referencedTypes.Contains(parent.Name))
            {
                return false;
            }
        }



        return true;
    }

    public void GetAllReferencedTypesFor(ElementSave element, HashSet<string> referencedTypes)
    {
        referencedTypes.Add(element.Name);

        foreach (var instance in element.Instances)
        {
            var instanceElement = _objectFinder.GetElementSave(instance);
            if (instanceElement != null && !referencedTypes.Contains(instanceElement.Name))
            {
                GetAllReferencedTypesFor(instanceElement, referencedTypes);
            }
        }

        if (!string.IsNullOrEmpty(element.BaseType))
        {
            var baseElement = _objectFinder.GetElementSave(element.BaseType);
            if (baseElement != null && !referencedTypes.Contains(baseElement.Name))
            {
                GetAllReferencedTypesFor(baseElement, referencedTypes);
            }
        }
    }


}
