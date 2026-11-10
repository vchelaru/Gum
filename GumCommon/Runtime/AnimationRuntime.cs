using FlatRedBall.Glue.StateInterpolation;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;

namespace Gum.StateAnimation.Runtime;
public class AnimationRuntime
{
    public string? Name { get; set; }
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
                // A loop rather than Max with a lambda: a looping animation reads this every tick, and
                // the enumerator and selector cost 64 bytes each time (#7873).
                float length = float.MinValue;
                for (int i = 0; i < Keyframes.Count; i++)
                {
                    float end = Keyframes[i].Time + Keyframes[i].Length;
                    if (end > length)
                    {
                        length = end;
                    }
                }
                return length;
            }
        }
    }

    public List<KeyframeRuntime> Keyframes { get; private set; } = new List<KeyframeRuntime>();

    public void RefreshCumulativeStates(ElementSave element, bool useDefaultAsStarting = true)
    {
        RefreshCumulativeStatesForStateKeyframes(element.Categories, useDefaultAsStarting);

        foreach (var subAnimation in this.Keyframes)
        {
            string? name = subAnimation.AnimationName;
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            InstanceSave? instance = null;

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
                    // Null when the instance's animation was removed after this reference was made.
                    subAnimation.SubAnimation?.RefreshCumulativeStates(instanceElement, false);
                }
            }
        }
    }

    public void RefreshCumulativeStates(GraphicalUiElement element, bool useDefaultAsStarting = true)
    {
        RefreshCumulativeStatesForStateKeyframes(element.Categories.Values, useDefaultAsStarting);

        foreach (var subAnimation in this.Keyframes)
        {
            string? name = subAnimation.AnimationName;
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            GraphicalUiElement? instance = null;

            if (name.Contains('.'))
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

        HashSet<string> allVariables = new();

        foreach (var keframeRuntime in this.Keyframes)
        {
            string? keyframeStateName = keframeRuntime.StateName;
            // This is going to be the case almost all the time:
            if(keyframeStateName?.Contains("/") == true)
            {
                var names = keyframeStateName.Split('/');

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

    static StateSave? GetStateFromCategorizedName(string? categorizedName, ElementSave element)
    {
        if (categorizedName == null)
        {
            return null;
        }
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
            return foundCategory == null ? null : foundCategory.States.FindByName(stateName);
        }
        else
        {
            return element.States.FindByName(categorizedName);
        }
    }


    static StateSave? GetStateFromCategorizedName(string? categorizedName, GraphicalUiElement graphicalUiElement)
    {
        if (categorizedName == null)
        {
            return null;
        }
        if (TrySplitCategorizedName(categorizedName, out string category, out string stateName))
        {
            if(graphicalUiElement.Categories.TryGetValue(category, out var foundCategory))
            {
                return foundCategory.States.FindByName(stateName);
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

    public StateSave GetStateToSet(double animationTime, ElementSave element, bool defaultIfNull = false)
    {
        return GetStateToSet(animationTime, element, null, defaultIfNull);
    }

    /// <summary>
    /// The state the animation sets at the time. The reference rows of the keyframe states are
    /// evaluated against <paramref name="liveRoot"/>, the live element, before interpolating; null
    /// leaves the stored values (the tool's preview, which re-evaluates references itself).
    /// </summary>
    internal StateSave GetStateToSet(double animationTime, ElementSave element, GraphicalUiElement? liveRoot, bool defaultIfNull = false)
    {
        return BuildStateToSet(animationTime, element, null, liveRoot, defaultIfNull, null);
    }

    public StateSave GetStateToSet(double animationTime, GraphicalUiElement graphicalUiElement, bool defaultIfNull = false)
    {
        return BuildStateToSet(animationTime, null, graphicalUiElement, null, defaultIfNull, null);
    }

    // With a scratch, the returned state is the scratch's and is rewritten by the next tick that uses it, so only
    // ApplyAtTimeTo passes one. The public overloads return a state the caller may keep, so they pass none.
    private StateSave BuildStateToSet(double animationTime, ElementSave? element, GraphicalUiElement? graphicalUiElement,
        GraphicalUiElement? liveRoot, bool defaultIfNull, TickScratch? scratch)
    {
        StateSave stateToSet = GetStateToSetFromStateKeyframes(animationTime, element, defaultIfNull, graphicalUiElement, liveRoot, scratch);
        CombineStateFromAnimations(animationTime, element, graphicalUiElement, stateToSet, scratch);
        return stateToSet;
    }

    // True when the state has a variable-reference list with at least one row. Compares names in place
    // because this runs for every keyframe state on every tick.
    private static bool HasReferenceRows(StateSave state)
    {
        List<VariableListSave> lists = state.VariableLists;
        for (int i = 0; i < lists.Count; i++)
        {
            string name = lists[i].Name;
            if ((name == "VariableReferences" || name.EndsWith(".VariableReferences", StringComparison.Ordinal))
                && lists[i].ValueAsIList.Count > 0)
            {
                return true;
            }
        }
        return false;
    }

    // A keyframe state's reference rows are evaluated against the live element, so the keyframe holds
    // what the element reads now (an OffsetX the game set) rather than a number stored when it was authored.
    // A state without rows, or no live element to read (the tool previews without one), comes back as is.
    private static StateSave? ResolveReferences(StateSave? state, ElementSave element, GraphicalUiElement? liveRoot)
    {
        if (state == null || liveRoot == null || !HasReferenceRows(state))
        {
            return state;
        }
        return liveRoot.ResolveKeyframeReferences(element, state);
    }

    private StateSave GetStateToSetFromStateKeyframes(double animationTime, ElementSave? element, bool defaultIfNull, GraphicalUiElement? graphicalUiElement, GraphicalUiElement? liveRoot, TickScratch? scratch)
    {
        //var stateKeyframes = this.Keyframes.Where(item => !string.IsNullOrEmpty(item.StateName) || item.CachedCumulativeState != null);

        if(this.Loops && this.Length > 0)
        {
            animationTime = animationTime % this.Length;
        }

        // A GraphicalUiElement, or an element with no states, has no default to start from.
        // The scratch has no default state to start from, so a default-based tick builds its own.
        if (defaultIfNull)
        {
            scratch = null;
        }
        StateSave stateToSet = scratch?.Result.Begin()
            ?? (defaultIfNull ? element?.DefaultState?.Clone() : null)
            ?? new StateSave();

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
                    stateForTrack = ResolveReferences(GetStateFromCategorizedName(keyframeAfter.StateName, element), element, liveRoot);
                }
                else if (keyframeBefore != null && keyframeAfter == null)
                {
                    stateForTrack = ResolveReferences(GetStateFromCategorizedName(keyframeBefore.StateName, element), element, liveRoot);
                }
                else if (keyframeBefore != null && keyframeAfter != null)
                {
                    double linearRatio = GetLinearRatio(animationTime, keyframeBefore, keyframeAfter);
                    var stateBefore = ResolveReferences(GetStateFromCategorizedName(keyframeBefore.StateName, element), element, liveRoot);
                    var stateAfter = ResolveReferences(GetStateFromCategorizedName(keyframeAfter.StateName, element), element, liveRoot);

                    if (stateBefore != null && stateAfter != null)
                    {
                        double processedRatio = ProcessRatio(keyframeBefore.InterpolationType, keyframeBefore.Easing, linearRatio);


                        stateForTrack = Interpolate(stateBefore, stateAfter, (float)processedRatio, scratch);
                    }
                }
            }
            else if (graphicalUiElement != null)
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


                        stateForTrack = Interpolate(stateBefore, stateAfter, (float)processedRatio, scratch);
                    }
                }
            }

            if (stateForTrack != null)
            {
                MergeInto(stateToSet, stateForTrack, 1, scratch?.Result);
            }
        }

        return stateToSet;
    }

    // before with after merged in at the ratio, without changing either.
    private static StateSave Interpolate(StateSave before, StateSave after, float ratio, TickScratch? scratch)
    {
        if (scratch == null)
        {
            StateSave combined = before.Clone();
            combined.MergeIntoThis(after, ratio);
            return combined;
        }
        StateSave reused = scratch.Combined.BeginCopyOf(before);
        reused.MergeIntoThis(after, ratio, scratch.Combined.Create);
        return reused;
    }

    private static void MergeInto(StateSave into, StateSave from, float ratio, ReusedState? reuse)
    {
        if (reuse == null)
        {
            into.MergeIntoThis(from, ratio);
        }
        else
        {
            into.MergeIntoThis(from, ratio, reuse.Create);
        }
    }



    private void CombineStateFromAnimations(double animationTime, ElementSave? element, GraphicalUiElement? graphicalUiElement, StateSave stateToSet, TickScratch? scratch)
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

                ElementSave? subAnimationElement = element;

                string? instanceName = null;
                string? animationName = keyframe.AnimationName;

                if (animationName?.Contains('.') == true)
                {
                    instanceName = animationName.Substring(0, animationName.IndexOf('.'));

                    InstanceSave? instance = element.Instances.FirstOrDefault(item => item.Name == instanceName);

                    if (instance != null)
                    {
                        subAnimationElement = Gum.Managers.ObjectFinder.Self.GetElementSave(instance);
                    }
                }

                if (subAnimationElement == null)
                {
                    // The instance's type is missing, so its animation's states can't be found.
                    continue;
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

                    MergeInto(stateToSet, stateFromAnimation, 1, scratch?.Result);
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

                string? instanceName = null;
                string? animationName = keyframe.AnimationName;

                if (animationName?.Contains('.') == true)
                {
                    instanceName = animationName.Substring(0, animationName.IndexOf('.'));

                    var instance = graphicalUiElement.FindByName(instanceName);

                    if (instance != null)
                    {
                        subAnimationGue = instance;
                    }
                }

                var relativeTime = animationTime - keyframe.Time;

                var stateFromAnimation = keyframe.SubAnimation.GetStateToSet(relativeTime, subAnimationGue, false);

                if (stateFromAnimation != null)
                {
                    if (subAnimationGue != graphicalUiElement)
                    {
                        foreach (var variable in stateFromAnimation.Variables)
                        {
                            variable.Name = instanceName + "." + variable.Name;
                        }
                    }

                    MergeInto(stateToSet, stateFromAnimation, 1, scratch?.Result);
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

        // The state is applied and dropped within this call, so it is built in states kept from the last tick
        // (#7873). The scratch is taken out of the field while in use: a tick of this animation that starts
        // while ApplyState runs (another element, or the same one from a handler) finds the field empty and
        // builds its own, so neither can overwrite the state the other is applying.
        TickScratch scratch = Interlocked.Exchange(ref _scratch, null) ?? new TickScratch();
        try
        {
            StateSave state = graphicalUiElement.ElementSave != null
                ? BuildStateToSet(secondsFromBeginning, graphicalUiElement.ElementSave, null, graphicalUiElement, shouldFirstKeyframeBeDefaultState, scratch)
                : BuildStateToSet(secondsFromBeginning, null, graphicalUiElement, null, shouldFirstKeyframeBeDefaultState, scratch);
            graphicalUiElement.ApplyState(state);
        }
        finally
        {
            _scratch = scratch;
        }
    }

    private TickScratch? _scratch;

    // A state that is rewritten in place, with the variable objects it hands out kept between rewrites. Only
    // what a tick reads or applies is copied onto a variable (name, exposed name, type, SetsValue, Value), not
    // every property of the VariableSave a clone would carry.
    private sealed class ReusedState
    {
        private readonly StateSave _state = new StateSave();
        private readonly List<VariableSave> _pool = new List<VariableSave>();
        private int _used;

        public ReusedState()
        {
            Create = Rent;
        }

        // Rent for MergeIntoThis, which adds the variable to the state itself.
        public Func<VariableSave, VariableSave> Create { get; }

        public StateSave Begin()
        {
            for (int i = 0; i < _used; i++)
            {
                _pool[i].Value = null;
            }
            _used = 0;
            _state.Variables.Clear();
            _state.Name = string.Empty;
            _state.ParentContainer = null;
            _state.Apply = null;
            return _state;
        }

        // The same as source.Clone() for the variables of a state.
        public StateSave BeginCopyOf(StateSave source)
        {
            StateSave state = Begin();
            state.Name = source.Name;
            state.ParentContainer = source.ParentContainer;
            state.Apply = source.Apply;
            List<VariableSave> variables = source.Variables;
            for (int i = 0; i < variables.Count; i++)
            {
                state.Variables.Add(Rent(variables[i]));
            }
            return state;
        }

        private VariableSave Rent(VariableSave source)
        {
            if (_used == _pool.Count)
            {
                _pool.Add(new VariableSave());
            }
            VariableSave variable = _pool[_used++];
            // Assigning a name splits it, which allocates for an instance variable, so only when it changed.
            if (variable.Name != source.Name)
            {
                variable.Name = source.Name;
            }
            variable.ExposedAsName = source.ExposedAsName;
            variable.Type = source.Type;
            variable.SetsValue = source.SetsValue;
            variable.Value = source.Value;
            return variable;
        }
    }

    // The two states one tick is built in: the interpolation of a pair of keyframes, and the result of all tracks.
    private sealed class TickScratch
    {
        public ReusedState Combined { get; } = new ReusedState();
        public ReusedState Result { get; } = new ReusedState();
    }

    public override string ToString()
    {
        return $"{Name} with {Keyframes.Count} keyframes";
    }
}
