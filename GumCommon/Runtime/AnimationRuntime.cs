using Gum.DataTypes;
using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gum.StateAnimation.Runtime;
public class AnimationRuntime
{
    public string Name { get; set; }
    public bool Loops { get; set; }

    public List<KeyframeRuntime> Keyframes { get; private set; } = new List<KeyframeRuntime>();

    public void RefreshCumulativeStates(ElementSave element, bool useDefaultAsStarting = true)
    {
        StateSave previous = null;

        if (useDefaultAsStarting)
        {
            previous = element.DefaultState;
        }

        foreach(var animatedState in this.Keyframes.Where(item=>!string.IsNullOrEmpty(item.StateName)))
        {
            var originalState = GetStateFromCategorizedName(animatedState.StateName, element);

            if (originalState != null)
            {
                if (previous == null)
                {
                    previous = originalState;
                    animatedState.CachedCumulativeState = originalState.Clone();
                }
                else
                {
                    var combined = previous.Clone();
                    combined.MergeIntoThis(originalState);
                    combined.Name = originalState.Name;
                    animatedState.CachedCumulativeState = combined;

                    previous = combined;
                }
            }
        }

        foreach(var subAnimation in this.Keyframes.Where(item=>!string.IsNullOrEmpty(item.AnimationName)))
        {
            InstanceSave instance = null;

            string name = subAnimation.AnimationName;

            if(name.Contains('.'))
            {
                int indexOfDot = name.IndexOf('.');

                string instanceName = name.Substring(0, indexOfDot);

                instance = element.Instances.FirstOrDefault(item => item.Name == instanceName);
            }
            if (instance == null)
            {
                // Null check in case the referenced instance was removed
                subAnimation.SubAnimation?.RefreshCumulativeStates(element, false);
            }
            else
            {
                var instanceElement = Gum.Managers.ObjectFinder.Self.GetElementSave(instance);
                if (instanceElement != null)
                {
                    subAnimation.SubAnimation.RefreshCumulativeStates(instanceElement, false);
                }
            }
        }
    }

    static StateSave GetStateFromCategorizedName(string categorizedName, ElementSave element)
    {
        if (categorizedName.Contains("/"))
        {
            var names = categorizedName.Split('/');

            string category = names[0];
            string stateName = names[1];

            return element
                .Categories.FirstOrDefault(item => item.Name == category)
                ?.States.FirstOrDefault(item => item.Name == stateName);
        }
        else
        {
            return element.States.FirstOrDefault(item => item.Name == categorizedName);
        }
    }


}
