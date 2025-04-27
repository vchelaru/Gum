using Gum.DataTypes;
using Gum.StateAnimation.SaveClasses;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Gum.StateAnimation.Runtime;
internal class AnimationRuntimeExtensions
{
    public static AnimationRuntime ToRuntime(AnimationSave animationSave)
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
            runtime.Keyframes.Add(ToRuntime(animationReferenceSave));
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

    private static KeyframeRuntime ToRuntime(AnimationReferenceSave animationReferenceSave)
    {
        //AnimationSave animationSave = null;
        //ElementSave subAnimationElement = null;
        //ElementAnimationsSave subAnimationSiblings = null;

        //if (string.IsNullOrEmpty(animationReferenceSave.SourceObject))
        //{
        //    if (allAnimationSaves == null)
        //    {
        //        allAnimationSaves = AnimationCollectionViewModelManager.Self.GetElementAnimationsSave(element);
        //    }

        //    animationSave = allAnimationSaves.Animations.FirstOrDefault(item => item.Name == animationReference.RootName);
        //    subAnimationElement = element;
        //    subAnimationSiblings = allAnimationSaves;
        //}
        //else
        //{
        //    var instance = element.Instances.FirstOrDefault(item => item.Name == animationReference.SourceObject);

        //    if (instance != null)
        //    {
        //        ElementSave instanceElement = Gum.Managers.ObjectFinder.Self.GetElementSave(instance);
        //        subAnimationElement = instanceElement;

        //        if (instanceElement != null)
        //        {
        //            var allAnimations = AnimationCollectionViewModelManager.Self.GetElementAnimationsSave(instanceElement);

        //            animationSave = allAnimations.Animations.FirstOrDefault(item => item.Name == animationReference.RootName);
        //            subAnimationElement = instanceElement;
        //            subAnimationSiblings = allAnimations;
        //        }
        //    }
        //}
        //var newVm = AnimatedKeyframeViewModel.FromSave(animationReference, element);

        //if (animationSave != null)
        //{
        //    newVm.SubAnimationViewModel = AnimationViewModel.FromSave(animationSave, subAnimationElement, subAnimationSiblings);
        //}


        //newVm.HasValidState = animationReference != null;

        //toReturn.Keyframes.Add(newVm);
    }


}
