using Gum.DataTypes;
using Gum.StateAnimation.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Gum.StateAnimation.Runtime;
public static class AnimationRuntimeExtensions
{
    public static AnimationRuntime ToRuntime(this AnimationSave animationSave, ElementSave element, 
        List<ElementAnimationsSave> allAnimationsSaves
        )
    {
        AnimationRuntime runtime = new AnimationRuntime();
        runtime.Name = animationSave.Name;
        runtime.Loops = animationSave.Loops;


        foreach (var eventSave in animationSave.Events)
        {
            runtime.Keyframes.Add(ToRuntime(eventSave));
        }

        foreach (var stateSave in animationSave.States)
        {
            runtime.Keyframes.Add(ToRuntime(stateSave));
        }

        foreach (var animationReferenceSave in animationSave.Animations)
        {
            runtime.Keyframes.Add(ToRuntime(animationReferenceSave, element, allAnimationsSaves));
        }


        return runtime;
    }

    static KeyframeRuntime ToRuntime(NamedEventSave namedEventSave)
    {
        KeyframeRuntime keyframeRuntime = new KeyframeRuntime();

        keyframeRuntime.EventName = namedEventSave.Name;
        keyframeRuntime.Time = namedEventSave.Time;

        return keyframeRuntime;

    }

    static KeyframeRuntime ToRuntime(AnimatedStateSave animatedStateSave)
    {
        KeyframeRuntime keyframeRuntime = new KeyframeRuntime();

        keyframeRuntime.StateName = animatedStateSave.StateName;
        keyframeRuntime.Time = animatedStateSave.Time;
        keyframeRuntime.InterpolationType = animatedStateSave.InterpolationType;
        keyframeRuntime.Easing = animatedStateSave.Easing;

        return keyframeRuntime;
    }

    private static KeyframeRuntime ToRuntime(AnimationReferenceSave animationReferenceSave, 
        ElementSave element, List<ElementAnimationsSave> elementAnimationsSaves)
    {
        AnimationSave animationSave = null;
        ElementSave subAnimationElement = null;
        ElementAnimationsSave subAnimationSiblings = null;

        if (string.IsNullOrEmpty(animationReferenceSave.SourceObject))
        {
            var elementAnimationsSave = elementAnimationsSaves.FirstOrDefault(item => item.ElementName == element.Name);

            animationSave = elementAnimationsSave.Animations.FirstOrDefault(item => item.Name == animationReferenceSave.RootName);
            subAnimationElement = element;
            subAnimationSiblings = elementAnimationsSave;
        }
        else
        {
            var instance = element.Instances.FirstOrDefault(item => item.Name == animationReferenceSave.SourceObject);

            if (instance != null)
            {
                ElementSave instanceElement = Gum.Managers.ObjectFinder.Self.GetElementSave(instance);
                subAnimationElement = instanceElement;

                if (instanceElement != null)
                {
                    var siblingAnimations = elementAnimationsSaves.FirstOrDefault(item => item.ElementName == instanceElement.Name);

                    animationSave = siblingAnimations.Animations.FirstOrDefault(item => item.Name == animationReferenceSave.RootName);
                    subAnimationElement = instanceElement;
                    subAnimationSiblings = siblingAnimations;
                }
            }
        }

        KeyframeRuntime keyframeRuntime = new KeyframeRuntime();
        keyframeRuntime.AnimationName = animationReferenceSave.Name;
        keyframeRuntime.Time = animationReferenceSave.Time;

        if (animationSave != null)
        {
            // can we track references and share them?
            // I suppose for now we duplicate...

            keyframeRuntime.SubAnimation = 
                ToRuntime(animationSave, subAnimationElement, elementAnimationsSaves);
        }

        return keyframeRuntime;
    }
}
