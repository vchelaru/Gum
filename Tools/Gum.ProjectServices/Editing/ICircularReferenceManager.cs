using Gum.DataTypes;
using System.Collections.Generic;

namespace Gum.Services;

public interface ICircularReferenceManager
{
    bool CanTypeBeAddedToElement(ElementSave parent, string typeToAdd);
    void GetAllReferencedTypesFor(ElementSave element, HashSet<string> referencedTypes);
}
