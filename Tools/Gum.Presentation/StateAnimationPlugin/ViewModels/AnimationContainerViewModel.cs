using Gum.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StateAnimationPlugin.ViewModels;

public class AnimationContainerViewModel
{
    public string Name
    {
        get
        {
            if(InstanceSave == null)
            {
                return ElementSave.Name + " (container)";
            }
            else
            {
                return InstanceSave.Name + " (" + InstanceSave.BaseType + ")";
            }
        }
    }

    public ElementSave ElementSave { get; private set; }

    public InstanceSave? InstanceSave { get; private set; }

    public AnimationContainerViewModel(ElementSave element, InstanceSave? instance)
    {
        ElementSave = element;
        InstanceSave = instance;
    }
}
