using FlatRedBall.Glue.StateInterpolation;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;

namespace Gum.StateAnimation.Runtime;
public class AnimationRuntime
{
    public string Name { get; set; }
    public bool Loops { get; set; }

    Dictionary<string, List<KeyframeRuntime>> _tracks = new();


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
        RefreshCumulativeStatesForStateKeyframes(element, useDefaultAsStarting);

        foreach (var subAnimation in this.Keyframes.Where(item => !string.IsNullOrEmpty(item.AnimationName)))
        {
            InstanceSave? instance = null;

            string name = subAnimation.AnimationName;

            if (name.Contains('.'))
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


    private void RefreshCumulativeStatesForStateKeyframes(ElementSave element, bool useDefaultAsStarting)
    {
        // This allocates a bit for simplicity and debugging. If it's a problem, can do some work to reuse (ThreadLocal<T>?)
        Dictionary<string, HashSet<string>> categoryVariableAssignments = new ();

        var keyframesWithStates = this.Keyframes.Where(item => !string.IsNullOrEmpty(item.StateName)).ToList();
        HashSet<string> allVariables = new();

        foreach (var keframeRuntime in keyframesWithStates)
        {
            // This is going to be the case almost all the time:
            if(keframeRuntime.StateName.Contains("/"))
            {
                var names = keframeRuntime.StateName.Split('/');

                string categoryName = names[0];
                string stateName = names[1];

                if (!categoryVariableAssignments.ContainsKey(categoryName))
                {
                    categoryVariableAssignments[categoryName] = new HashSet<string>();
                }
                var state = element
                    .Categories.FirstOrDefault(item => item.Name == categoryName)
                    ?.States.FirstOrDefault(item => item.Name == stateName);
                if(state != null)
                {
                    foreach(var variable in state.Variables)
                    {
                        categoryVariableAssignments[categoryName].Add(variable.Name);
                        allVariables.Add(variable.Name);
                    }
                }

                if (!_tracks.ContainsKey(categoryName))
                {
                    _tracks[categoryName] = new List<KeyframeRuntime>();
                }
                _tracks[categoryName].Add(keframeRuntime);
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
        //var stateKeyframes = this.Keyframes.Where(item => !string.IsNullOrEmpty(item.StateName) || item.CachedCumulativeState != null);

        if(this.Loops)
        {
            animationTime = animationTime % this.Length;
        }

        if (stateToSet == null && defaultIfNull)
        {
            stateToSet = element?.DefaultState.Clone();
        }
        else if (stateToSet == null)
        {
            stateToSet = new StateSave();
        }

        if(_tracks.Count == 0)
        {
            RefreshCumulativeStates(element);
        }

        foreach (var track in _tracks)
        {
            StateSave? stateForTrack = null;
            var keyframeBefore = track.Value.LastOrDefault(item => item.Time <= animationTime);
            var keyframeAfter = track.Value.FirstOrDefault(item => item.Time >= animationTime);

            if (keyframeBefore == null && keyframeAfter != null)
            {
                stateForTrack = GetStateFromCategorizedName(keyframeAfter.StateName, element);
            }
            else if (keyframeBefore != null && keyframeAfter == null)
            {
                stateForTrack = GetStateFromCategorizedName(keyframeBefore.StateName, element);
            }
            else if (keyframeBefore != null && keyframeAfter != null)
            {
                double linearRatio = GetLinearRatio(animationTime, keyframeBefore, keyframeAfter);
                var stateBefore = GetStateFromCategorizedName(keyframeBefore.StateName, element);
                var stateAfter = GetStateFromCategorizedName(keyframeAfter.StateName, element);

                if (stateBefore != null && stateAfter != null)
                {
                    double processedRatio = ProcessRatio(keyframeBefore.InterpolationType, keyframeBefore.Easing, linearRatio);


                    var combined = stateBefore.Clone();
                    combined.MergeIntoThis(stateAfter, (float)processedRatio);
                    stateForTrack = combined;
                }
            }

            if(stateForTrack != null)
            {
                stateToSet.MergeIntoThis(stateForTrack, 1);
            }
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
        // November 23, 2025
        // This should be false,
        // because if it's true, then
        // multiple animations cannot play
        // at the same time since each would
        // be applying default, potentially wiping
        // the state set by other animations.
        bool shouldFirstKeyframeBeDefaultState = false;
        var state = GetStateToSet(secondsFromBeginning, graphicalUiElement.ElementSave, shouldFirstKeyframeBeDefaultState);
        graphicalUiElement.ApplyState(state);
    }

    public override string ToString()
    {
        return $"{Name} with {Keyframes.Count} keyframes";
    }
}
