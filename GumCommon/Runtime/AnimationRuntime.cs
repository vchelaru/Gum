﻿using FlatRedBall.Glue.StateInterpolation;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gum.StateAnimation.Runtime;
public class AnimationRuntime
{
    public string Name { get; set; }
    public bool Loops { get; set; }

    public float Length
    {
        get
        {
            if (Keyframes.Count == 0)
            {
                return 0;
            }
            else
            {
                return Keyframes.Max(item => item.Time + item.Length);
            }
        }
    }

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

    public StateSave GetStateToSet(double animationTime, ElementSave element, bool defaultIfNull)
    {
        StateSave stateToSet = null;

        GetStateToSetFromStateKeyframes(animationTime, element, ref stateToSet, defaultIfNull);

        CombineStateFromAnimations(animationTime, element, ref stateToSet);

        return stateToSet;
    }

    private StateSave GetStateToSetFromStateKeyframes(double animationTime, ElementSave element, ref StateSave stateToSet, bool defaultIfNull)
    {
        var stateKeyframes = this.Keyframes.Where(item => !string.IsNullOrEmpty(item.StateName));

        if(this.Loops)
        {
            animationTime = animationTime % this.Length;
        }

        var keyframeBefore = stateKeyframes.LastOrDefault(item => item.Time <= animationTime);
        var keyframeAfter = stateKeyframes.FirstOrDefault(item => item.Time >= animationTime);

        if (keyframeBefore == null && keyframeAfter != null)
        {
            // The custom state can be null if the animation window references states which don't exist:
            if (keyframeAfter.CachedCumulativeState == null)
            {
                if (element != null)
                {
                    RefreshCumulativeStates(element);
                }
                else
                {
                    throw new InvalidOperationException("The animation has not had its RefreshCumulativeStates called, " +
                        "and GetStateToSetFromStateKeyframes is being called without a valid element. One or the other is required");
                }
            }

            stateToSet = keyframeAfter.CachedCumulativeState!.Clone();
        }
        else if (keyframeBefore != null && keyframeAfter == null)
        {
            if (keyframeBefore.CachedCumulativeState == null)
            {
                if (element != null)
                {
                    RefreshCumulativeStates(element);
                }
                else
                {
                    throw new InvalidOperationException("The animation has not had its RefreshCumulativeStates called, " +
                        "and GetStateToSetFromStateKeyframes is being called without a valid element. One or the other is required");
                }
            }

            stateToSet = keyframeBefore.CachedCumulativeState!.Clone();
        }
        else if (keyframeBefore != null && keyframeAfter != null)
        {
            if (keyframeAfter.CachedCumulativeState == null ||
                keyframeAfter.CachedCumulativeState == null)
            {
                if (element != null)
                {
                    RefreshCumulativeStates(element);
                }
                else
                {
                    throw new InvalidOperationException("The animation has not had its RefreshCumulativeStates called, " +
                        "and GetStateToSetFromStateKeyframes is being called without a valid element. One or the other is required");
                }

            }
            double linearRatio = GetLinearRatio(animationTime, keyframeBefore, keyframeAfter);
            var stateBefore = keyframeBefore.CachedCumulativeState;
            var stateAfter = keyframeAfter.CachedCumulativeState;

            if (stateBefore != null && stateAfter != null)
            {
                double processedRatio = ProcessRatio(keyframeBefore.InterpolationType, keyframeBefore.Easing, linearRatio);


                var combined = stateBefore.Clone();
                combined.MergeIntoThis(stateAfter, (float)processedRatio);
                stateToSet = combined;
            }
        }

        if (stateToSet == null && defaultIfNull)
        {
            stateToSet = element?.DefaultState.Clone();
        }
        else if (stateToSet == null)
        {
            stateToSet = new StateSave();
        }
        return stateToSet;
    }



    private void CombineStateFromAnimations(double animationTime, ElementSave element, ref StateSave stateToSet)
    {
        var animationKeyframes = this.Keyframes.Where(item => item.SubAnimation != null && item.Time <= animationTime);

        foreach (var keyframe in animationKeyframes)
        {
            var subAnimationElement = element;

            string instanceName = null;

            if (keyframe.AnimationName.Contains('.'))
            {
                instanceName = keyframe.AnimationName.Substring(0, keyframe.AnimationName.IndexOf('.'));

                InstanceSave instance = element.Instances.FirstOrDefault(item => item.Name == instanceName);

                if (instance != null)
                {
                    subAnimationElement = Gum.Managers.ObjectFinder.Self.GetElementSave(instance);
                }
            }

            var relativeTime = animationTime - keyframe.Time;

            var stateFromAnimation = keyframe.SubAnimation.GetStateToSet(relativeTime, subAnimationElement, false);

            if (stateFromAnimation != null)
            {
                if (subAnimationElement != element)
                {
                    foreach (var variable in stateFromAnimation.Variables)
                    {
                        variable.Name = instanceName + "." + variable.Name;
                    }
                }

                stateToSet.MergeIntoThis(stateFromAnimation, 1);
            }
        }

    }

    private static double GetLinearRatio(double value, KeyframeRuntime stateVmBefore, KeyframeRuntime stateVmAfter)
    {
        double valueBefore = stateVmBefore.Time;
        double valueAfter = stateVmAfter.Time;

        double range = valueAfter - valueBefore;
        double timeIn = value - valueBefore;

        double ratio = 0;

        if (valueAfter != valueBefore)
        {
            ratio = timeIn / range;
        }
        return ratio;
    }

    private double ProcessRatio(FlatRedBall.Glue.StateInterpolation.InterpolationType interpolationType, FlatRedBall.Glue.StateInterpolation.Easing easing, double linearRatio)
    {
        var interpolationFunction = Tweener.GetInterpolationFunction(interpolationType, easing);

        return interpolationFunction.Invoke((float)linearRatio, 0, 1, 1);
    }

    public void ApplyAtTimeTo(double secondsFromBeginning, GraphicalUiElement graphicalUiElement)
    {
        var state = GetStateToSet(secondsFromBeginning, graphicalUiElement.ElementSave, true);
        graphicalUiElement.ApplyState(state);
    }

    public override string ToString()
    {
        return $"{Name} with {Keyframes.Count} keyframes";
    }
}
