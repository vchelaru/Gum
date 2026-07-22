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
        RefreshCumulativeStatesForStateKeyframes(element.Categories, useDefaultAsStarting);

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

    public void RefreshCumulativeStates(GraphicalUiElement element, bool useDefaultAsStarting = true)
    {
        RefreshCumulativeStatesForStateKeyframes(element.Categories.Values, useDefaultAsStarting);

        foreach (var subAnimation in this.Keyframes.Where(item => !string.IsNullOrEmpty(item.AnimationName)))
        {
            GraphicalUiElement? instance = null;

            string name = subAnimation.AnimationName;

            if (name?.Contains('.') == true)
            {
                int indexOfDot = name.IndexOf('.');

                string instanceName = name.Substring(0, indexOfDot);

                instance = element.FindByName(instanceName);
            }
            if (instance == null)
            {
                // Null check in case the referenced instance was removed
                subAnimation.SubAnimation?.RefreshCumulativeStates(element, false);
            }
            else
            {
                subAnimation.SubAnimation?.RefreshCumulativeStates(instance, false);
            }
        }
    }


    private void RefreshCumulativeStatesForStateKeyframes(IEnumerable<StateSaveCategory> categories, bool useDefaultAsStarting)
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
                var state = categories
                    .FirstOrDefault(item => item.Name == categoryName)
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

    static StateSave? GetStateFromCategorizedName(string categorizedName, ElementSave element)
    {
        if (TrySplitCategorizedName(categorizedName, out string category, out string stateName))
        {
            StateSaveCategory? foundCategory = null;
            for (int i = 0; i < element.Categories.Count; i++)
            {
                if (element.Categories[i].Name == category)
                {
                    foundCategory = element.Categories[i];
                    break;
                }
            }
            return foundCategory == null ? null : FindStateByName(foundCategory.States, stateName);
        }
        else
        {
            return FindStateByName(element.States, categorizedName);
        }
    }


    static StateSave? GetStateFromCategorizedName(string categorizedName, GraphicalUiElement graphicalUiElement)
    {
        if (TrySplitCategorizedName(categorizedName, out string category, out string stateName))
        {
            if(graphicalUiElement.Categories.TryGetValue(category, out var foundCategory))
            {
                return FindStateByName(foundCategory.States, stateName);
            }
        }
        else
        {
            // we can only assume default state:
            if(graphicalUiElement.States.TryGetValue(categorizedName, out var foundState))
            {
                return foundState;
            }
        }
        return null;
    }

    // Splits "Category/State" into its two parts without allocating the string[] a Split('/') would.
    // Matches the prior behavior: category is the text before the first '/', state is the text
    // between the first and second '/' (or to the end when there is no second '/').
    private static bool TrySplitCategorizedName(string categorizedName, out string category, out string stateName)
    {
        int firstSlash = categorizedName.IndexOf('/');
        if (firstSlash < 0)
        {
            category = "";
            stateName = "";
            return false;
        }

        category = categorizedName.Substring(0, firstSlash);
        int secondSlash = categorizedName.IndexOf('/', firstSlash + 1);
        stateName = secondSlash >= 0
            ? categorizedName.Substring(firstSlash + 1, secondSlash - firstSlash - 1)
            : categorizedName.Substring(firstSlash + 1);
        return true;
    }

    // FirstOrDefault(item => item.Name == name) over a state list without the closure allocation.
    private static StateSave? FindStateByName(List<StateSave> states, string name)
    {
        for (int i = 0; i < states.Count; i++)
        {
            if (states[i].Name == name)
            {
                return states[i];
            }
        }
        return null;
    }

    public StateSave GetStateToSet(double animationTime, ElementSave element, bool defaultIfNull = false)
    {
        StateSave stateToSet = null;

        GetStateToSetFromStateKeyframes(animationTime, element, ref stateToSet, defaultIfNull);

        CombineStateFromAnimations(animationTime, element, null, ref stateToSet);

        return stateToSet;
    }

    public StateSave GetStateToSet(double animationTime, GraphicalUiElement graphicalUiElement, bool defaultIfNull = false)
    {
        StateSave stateToSet = null;
        GetStateToSetFromStateKeyframes(animationTime, null, ref stateToSet, defaultIfNull, graphicalUiElement);
        CombineStateFromAnimations(animationTime, null, graphicalUiElement, ref stateToSet);
        return stateToSet;
    }

    private StateSave GetStateToSetFromStateKeyframes(double animationTime, ElementSave? element, ref StateSave stateToSet, bool defaultIfNull, GraphicalUiElement? graphicalUiElement = null)
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
            if (element != null)
            {
                RefreshCumulativeStates(element);
            }
            else if(graphicalUiElement != null)
            {
                RefreshCumulativeStates(graphicalUiElement);
            }
        }

        foreach (var track in _tracks)
        {
            StateSave? stateForTrack = null;
            // Plain loops rather than LINQ LastOrDefault/FirstOrDefault: this runs every frame for
            // every playing animation, and the capturing lambdas would allocate a closure + delegate
            // per call (issue #1934).
            KeyframeRuntime? keyframeBefore = GetLastKeyframeAtOrBefore(track.Value, animationTime);
            KeyframeRuntime? keyframeAfter = GetFirstKeyframeAtOrAfter(track.Value, animationTime);

            if(element != null)
            {
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
            }
            else
            {
                if (keyframeBefore == null && keyframeAfter != null)
                {
                    stateForTrack = GetStateFromCategorizedName(keyframeAfter.StateName, graphicalUiElement);
                }
                else if (keyframeBefore != null && keyframeAfter == null)
                {
                    stateForTrack = GetStateFromCategorizedName(keyframeBefore.StateName, graphicalUiElement);
                }
                else if (keyframeBefore != null && keyframeAfter != null)
                {
                    double linearRatio = GetLinearRatio(animationTime, keyframeBefore, keyframeAfter);
                    var stateBefore = GetStateFromCategorizedName(keyframeBefore.StateName, graphicalUiElement);
                    var stateAfter = GetStateFromCategorizedName(keyframeAfter.StateName, graphicalUiElement);

                    if (stateBefore != null && stateAfter != null)
                    {
                        double processedRatio = ProcessRatio(keyframeBefore.InterpolationType, keyframeBefore.Easing, linearRatio);


                        var combined = stateBefore.Clone();
                        combined.MergeIntoThis(stateAfter, (float)processedRatio);
                        stateForTrack = combined;
                    }
                }
            }

            if (stateForTrack != null)
            {
                stateToSet.MergeIntoThis(stateForTrack, 1);
            }
        }

        return stateToSet;
    }



    private void CombineStateFromAnimations(double animationTime, ElementSave? element, GraphicalUiElement? graphicalUiElement, ref StateSave stateToSet)
    {
        // Iterate the keyframe list directly and guard inline rather than using a LINQ Where: this
        // runs every frame per playing animation, and the capturing lambda would allocate a closure,
        // delegate, and iterator even when no keyframe carries a sub-animation (issue #1934).
        if(element != null)
        {
            foreach (var keyframe in this.Keyframes)
            {
                if (keyframe.SubAnimation == null || keyframe.Time > animationTime)
                {
                    continue;
                }

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

                var stateFromAnimation = keyframe.SubAnimation!.GetStateToSet(relativeTime, subAnimationElement, false);

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
        else if(graphicalUiElement != null)
        {
            foreach (var keyframe in this.Keyframes)
            {
                if (keyframe.SubAnimation == null || keyframe.Time > animationTime)
                {
                    continue;
                }

                var subAnimationGue = graphicalUiElement;

                string instanceName = null;

                if (keyframe.AnimationName.Contains('.'))
                {
                    instanceName = keyframe.AnimationName.Substring(0, keyframe.AnimationName.IndexOf('.'));

                    var instance = graphicalUiElement.FindByName(instanceName);

                    if (instance != null)
                    {
                        subAnimationGue = instance;
                    }
                }

                var relativeTime = animationTime - keyframe.Time;

                var stateFromAnimation = keyframe.SubAnimation!.GetStateToSet(relativeTime, subAnimationGue, false);

                if (stateFromAnimation != null)
                {
                    if (subAnimationGue != graphicalUiElement)
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

    }

    // LastOrDefault(item => item.Time <= animationTime) without the closure allocation.
    private static KeyframeRuntime? GetLastKeyframeAtOrBefore(List<KeyframeRuntime> keyframes, double animationTime)
    {
        for (int i = keyframes.Count - 1; i >= 0; i--)
        {
            if (keyframes[i].Time <= animationTime)
            {
                return keyframes[i];
            }
        }
        return null;
    }

    // FirstOrDefault(item => item.Time >= animationTime) without the closure allocation.
    private static KeyframeRuntime? GetFirstKeyframeAtOrAfter(List<KeyframeRuntime> keyframes, double animationTime)
    {
        for (int i = 0; i < keyframes.Count; i++)
        {
            if (keyframes[i].Time >= animationTime)
            {
                return keyframes[i];
            }
        }
        return null;
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
        if(graphicalUiElement.ElementSave != null)
        {
            var state = GetStateToSet(secondsFromBeginning, graphicalUiElement.ElementSave, shouldFirstKeyframeBeDefaultState);
            graphicalUiElement.ApplyState(state);
        }
        else
        {
            var state = GetStateToSet(secondsFromBeginning, graphicalUiElement, shouldFirstKeyframeBeDefaultState);
            graphicalUiElement.ApplyState(state);
        }
    }

    public override string ToString()
    {
        return $"{Name} with {Keyframes.Count} keyframes";
    }
}
