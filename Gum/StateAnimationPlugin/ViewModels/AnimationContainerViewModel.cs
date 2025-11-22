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
            if(mInstanceSave == null)
            {
                return mElementSave.Name + " (container)";
            }
            else
            {
                return mInstanceSave.Name + " (" + InstanceSave.BaseType + ")";
            }
        }
    }

    public ElementSave ElementSave
    {
        get
        {
            return mElementSave;
        }
    }

    public InstanceSave InstanceSave
    {
        get
        {
            return mInstanceSave;
        }
    }


    ElementSave mElementSave;
    InstanceSave mInstanceSave;

    public AnimationContainerViewModel(ElementSave element, InstanceSave instance)
    {
        mElementSave = element;
        mInstanceSave = instance;
    }
}
