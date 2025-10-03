using FlatRedBall.Glue.StateInterpolation;
using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Mvvm;
using Gum.ToolStates;
using Gum.Wireframe;
using StateAnimationPlugin.Managers;
using Gum.StateAnimation.SaveClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Gum.Services;

namespace StateAnimationPlugin.ViewModels;

public class AnimationViewModel : ViewModel
{
    #region Fields

    bool mIsInMiddleOfSort = false;

    BitmapFrame mLoopBitmap;
    BitmapFrame mPlayOnceBitmap;

    bool mLoops = false;
    
    private readonly ISelectedState _selectedState;

    #endregion

    #region Properties

    public string Name 
    { 
        get => Get<string>(); 
        set => Set(value); 
    }

    public float Length 
    { 
        get
        {
            if(Keyframes.Count == 0)
            {
                return 0;
            }
            else
            {
                return Keyframes.Max(item => item.Time + item.Length);
            }
        }
    }

    public bool Loops => mLoops;

    public BitmapFrame ButtonBitmapFrame
    {
        get
        {
            if (mLoops)
            {
                return mLoopBitmap;
            }
            else
            {
                return mPlayOnceBitmap;
            }

        }
    }

    public ObservableCollection<AnimatedKeyframeViewModel> Keyframes { get; private set; }

    public event PropertyChangedEventHandler FramePropertyChanged;

    public AnimatedKeyframeViewModel SelectedKeyframe 
    {
        get => Get<AnimatedKeyframeViewModel>();
        set 
        {
            if (Set(value))
            {
                TrySelectKeyframeReferencedStateSave();
            }
        }
    }

    public void TrySelectKeyframeReferencedStateSave()
    {
        var stateName = SelectedKeyframe?.StateName;
        if (stateName != null)
        {
            string categoryName = null;
            if (stateName.Contains("/"))
            {
                categoryName = stateName.Substring(0, stateName.IndexOf('/'));
                stateName = stateName.Substring(stateName.IndexOf("/") + 1);
            }

            var element = _selectedState.SelectedElement;
            if (string.IsNullOrEmpty(categoryName))
            {
                _selectedState.SelectedStateSave = element.GetStateSaveRecursively(stateName);
            }
            else
            {
                var category = element.GetStateSaveCategoryRecursively(categoryName);

                _selectedState.SelectedStateSave = category?.States.FirstOrDefault(item => item.Name == stateName);
            }
        }
    }

    public InstanceSave ContainingInstance
    {
        get;
        set;
    }

    #endregion

    #region Methods

    public AnimationViewModel()
    {
        Keyframes = new ObservableCollection<AnimatedKeyframeViewModel>();
        Keyframes.CollectionChanged += HandleCollectionChanged;

        mLoopBitmap = BitmapLoader.Self.LoadImage("LoopIcon.png");

        mPlayOnceBitmap = BitmapLoader.Self.LoadImage("PlayOnceIcon.png");
        _selectedState = Locator.GetRequiredService<ISelectedState>();
    }

    public AnimationViewModel Clone()
    {
        var clone = base.Clone<AnimationViewModel>();

        clone.Keyframes = new ObservableCollection<AnimatedKeyframeViewModel>();

        clone.FramePropertyChanged = null;
        clone.ContainingInstance = this.ContainingInstance;
        
        foreach(var item in this.Keyframes)
        {
            clone.Keyframes.Add(item.Clone());
        }

        return clone;
    }

    public static AnimationViewModel FromSave(AnimationSave save, ElementSave element, ElementAnimationsSave allAnimationSaves = null)
    {
        AnimationViewModel toReturn = new AnimationViewModel();
        toReturn.Name = save.Name;
        toReturn.mLoops = save.Loops;

        foreach(var eventSave in save.Events)
        {
            var newViewModel = AnimatedKeyframeViewModel.FromSave(eventSave, element);

            toReturn.Keyframes.Add(newViewModel);
        }

        foreach(var stateSave in save.States)
        {
            var foundState = GetStateFromCategorizedName(stateSave.StateName, element);

            var newAnimatedStateViewModel = AnimatedKeyframeViewModel.FromSave(stateSave, element);

            newAnimatedStateViewModel.HasValidState = foundState != null;

            toReturn.Keyframes.Add(newAnimatedStateViewModel);
        }

        foreach(var animationReference in save.Animations)
        {
            AnimationSave animationSave = null;
            ElementSave subAnimationElement= null;
            ElementAnimationsSave subAnimationSiblings = null;

            if (string.IsNullOrEmpty(animationReference.SourceObject))
            {
                if(allAnimationSaves == null)
                {
                    allAnimationSaves = AnimationCollectionViewModelManager.Self.GetElementAnimationsSave(element);
                }

                animationSave = allAnimationSaves.Animations.FirstOrDefault(item => item.Name == animationReference.RootName);
                subAnimationElement = element;
                subAnimationSiblings = allAnimationSaves;
            }
            else
            {
                var instance =  element.Instances.FirstOrDefault(item => item.Name == animationReference.SourceObject);

                if(instance != null)
                {
                    ElementSave instanceElement = Gum.Managers.ObjectFinder.Self.GetElementSave(instance);
                    subAnimationElement = instanceElement;

                    if(instanceElement != null)
                    {
                        var allAnimations = AnimationCollectionViewModelManager.Self.GetElementAnimationsSave(instanceElement);

                        animationSave = allAnimations.Animations.FirstOrDefault(item => item.Name == animationReference.RootName);
                        subAnimationElement = instanceElement;
                        subAnimationSiblings = allAnimations;
                    }
                }
            }
            var newVm = AnimatedKeyframeViewModel.FromSave(animationReference, element);

            if(animationSave != null)
            {
                newVm.SubAnimationViewModel = AnimationViewModel.FromSave(animationSave, subAnimationElement, subAnimationSiblings);
            }


            newVm.HasValidState = animationReference != null;

            toReturn.Keyframes.Add(newVm);

        }

        toReturn.SortList();

        return toReturn;
    }

    public AnimationSave ToSave()
    {
        AnimationSave toReturn = new AnimationSave();
        toReturn.Name = this.Name;
        toReturn.Loops = this.mLoops;

        foreach(var state in this.Keyframes)
        {
            if (!string.IsNullOrEmpty(state.StateName))
            {
                toReturn.States.Add(state.ToAnimatedStateSave());
            }
            else if(!string.IsNullOrEmpty(state.AnimationName))
            {
                toReturn.Animations.Add(state.ToAnimationReferenceSave());
            }
            else
            {
                toReturn.Events.Add(state.ToEventSave());
            }
        }

        return toReturn;
    }

    private void HandleCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && !mIsInMiddleOfSort)
        {
            foreach(var newAdd in e.NewItems)
            {
                var asAnimatedState = newAdd as AnimatedKeyframeViewModel;

                asAnimatedState.PropertyChanged += HandleAnimatedKeyframePropertyChange;
            }
        }

        NotifyPropertyChanged(nameof(Length));

        NotifyPropertyChanged(nameof(Keyframes));
        
    }

    private void HandleAnimatedKeyframePropertyChange(object sender, PropertyChangedEventArgs e)
    {

        if (e.PropertyName == nameof(AnimatedKeyframeViewModel.Time))
        {
            SortList();

            NotifyPropertyChanged(nameof(Length));

        }
        else if (e.PropertyName == nameof(AnimatedKeyframeViewModel.StateName))
        {
            if(_selectedState.SelectedElement != null)
            {
                RefreshCumulativeStates(_selectedState.SelectedElement);
            }
        }


        FramePropertyChanged?.Invoke(sender, e);
    }

    public override string ToString()
    {
        return Name + " (" + Length.ToString("0.00") + ")";
    }

    void SortList()
    {
        mIsInMiddleOfSort = true;

        var oldSelected = this.SelectedKeyframe;

        this.Keyframes.BubbleSort();

        this.SelectedKeyframe = oldSelected;

        mIsInMiddleOfSort = false;
    }


    static StateSave GetStateFromCategorizedName(string categorizedName, ElementSave element)
    {
        if(categorizedName.Contains("/"))
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

    /// <summary>
    /// Perofrms a sequential, cumulative combination of all states along an animation.
    /// As an animation plays each state (keyframe) combines with the previous
    /// to create a combined state.  As the animation continues, these combined states
    /// accumulate the changes of all states before them.  Therefore, we need to pre-combine
    /// the keyframes so that when animations are played they can properly interpolate.
    /// 
    /// </summary>
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
                subAnimation.SubAnimationViewModel?.RefreshCumulativeStates(element, false);
            }
            else
            {
                var instanceElement = Gum.Managers.ObjectFinder.Self.GetElementSave(instance);
                if (instanceElement != null)
                {
                    subAnimation.SubAnimationViewModel.RefreshCumulativeStates(instanceElement, false);
                }
            }
        }
    }

    public void ToggleLoop()
    {
        mLoops = !mLoops;

        NotifyPropertyChanged("ButtonBitmapFrame");

        NotifyPropertyChanged("Loops");
    }

    public void SetStateAtTime(double animationTime, ElementSave element, bool defaultIfNull)
    {
        StateSave stateToSet = GetStateToSet(animationTime, element, defaultIfNull);

        if(stateToSet != null)
        {
            WireframeObjectManager.Self.RootGue?.ApplyState(stateToSet);
        }
    }

    private StateSave GetStateToSet(double animationTime, ElementSave element, bool defaultIfNull)
    {
        StateSave stateToSet = null;

        GetStateToSetFromStateKeyframes(animationTime, element, ref stateToSet, defaultIfNull);

        CombineStateFromAnimations(animationTime, element, ref stateToSet);

        return stateToSet;
    }

    private void CombineStateFromAnimations(double animationTime, ElementSave element, ref StateSave stateToSet)
    {
        var animationKeyframes = this.Keyframes.Where(item => item.SubAnimationViewModel != null && item.Time <= animationTime);

        foreach(var keyframe in animationKeyframes)
        {
            var subAnimationElement = element;

            string instanceName = null;

            if(keyframe.AnimationName.Contains('.'))
            {
                instanceName = keyframe.AnimationName.Substring(0, keyframe.AnimationName.IndexOf('.'));

                InstanceSave instance = element.Instances.FirstOrDefault(item => item.Name == instanceName);

                if(instance != null)
                {
                    subAnimationElement = Gum.Managers.ObjectFinder.Self.GetElementSave(instance);
                }
            }

            var relativeTime = animationTime - keyframe.Time;

            var stateFromAnimation = keyframe.SubAnimationViewModel.GetStateToSet(relativeTime, subAnimationElement, false);

            if(stateFromAnimation != null)
            {
                if(subAnimationElement != element)
                {
                    foreach(var variable in stateFromAnimation.Variables)
                    {
                        variable.Name = instanceName + "." + variable.Name;
                    }
                }

                stateToSet.MergeIntoThis(stateFromAnimation, 1);
            }
        }

    }

    private StateSave GetStateToSetFromStateKeyframes(double animationTime, ElementSave element, ref StateSave stateToSet, bool defaultIfNull)
    {
        var stateKeyframes = this.Keyframes.Where(item => !string.IsNullOrEmpty(item.StateName));

        var stateVmBefore = stateKeyframes.LastOrDefault(item => item.Time <= animationTime);
        var stateVmAfter = stateKeyframes.FirstOrDefault(item => item.Time >= animationTime);

        if (stateVmBefore == null && stateVmAfter != null)
        {
            if (stateVmAfter.CachedCumulativeState == null)
            {
                if (element != null)
                {
                    RefreshCumulativeStates(element);
                }
            }

            SetCustomState(stateVmAfter.CachedCumulativeState);

            // The custom state can be null if the animation window references states which don't exist:
            stateToSet = _selectedState.CustomCurrentStateSave?.Clone();
        }
        else if (stateVmBefore != null && stateVmAfter == null)
        {
            if (stateVmBefore.CachedCumulativeState == null)
            {
                if (element != null)
                {
                    RefreshCumulativeStates(element);
                }
            }
            SetCustomState(stateVmBefore.CachedCumulativeState);

            stateToSet = _selectedState.CustomCurrentStateSave.Clone();
        }
        else if (stateVmBefore != null && stateVmAfter != null)
        {
            if (stateVmBefore.CachedCumulativeState == null ||
                stateVmAfter.CachedCumulativeState == null)
            {
                if (element != null)
                {
                    RefreshCumulativeStates(element);
                }
            }
            double linearRatio = GetLinearRatio(animationTime, stateVmBefore, stateVmAfter);
            var stateBefore = stateVmBefore.CachedCumulativeState;
            var stateAfter = stateVmAfter.CachedCumulativeState;

            if (stateBefore != null && stateAfter != null)
            {
                double processedRatio = ProcessRatio(stateVmBefore.InterpolationType, stateVmBefore.Easing, linearRatio);


                var combined = stateBefore.Clone();
                combined.MergeIntoThis(stateAfter, (float)processedRatio);

                SetCustomState(combined);

                // for performance we will only update wireframe:
                //_selectedState.UpdateToSelectedStateSave();
                //WireframeObjectManager.Self.RefreshAll(true);

                stateToSet = combined;
            }
        }

        if(stateToSet == null && defaultIfNull)
        {
            stateToSet = element?.DefaultState.Clone();
        }
        else if(stateToSet == null)
        {

            stateToSet = new StateSave();
        }
        return stateToSet;
    }

    private static void SetCustomState(StateSave combined)
    {
        ISelectedState selectedState = Locator.GetRequiredService<ISelectedState>();
        selectedState.CustomCurrentStateSave = combined;
        selectedState.SelectedStateSave = null;
    }

    private double ProcessRatio(FlatRedBall.Glue.StateInterpolation.InterpolationType interpolationType, FlatRedBall.Glue.StateInterpolation.Easing easing, double linearRatio)
    {
        var interpolationFunction = Tweener.GetInterpolationFunction(interpolationType, easing);

        return interpolationFunction.Invoke((float)linearRatio, 0, 1, 1);
    }

    private static double GetLinearRatio(double value, AnimatedKeyframeViewModel stateVmBefore, AnimatedKeyframeViewModel stateVmAfter)
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

    #endregion
}

#region ListExtension methods class

public static class ListExtension
{
    public static void BubbleSort(this IList o)
    {
        for (int i = o.Count - 1; i >= 0; i--)
        {
            for (int j = 1; j <= i; j++)
            {
                object o1 = o[j - 1];
                object o2 = o[j];
                if (((IComparable)o1).CompareTo(o2) > 0)
                {
                    o.Remove(o1);
                    o.Insert(j, o1);
                }
            }
        }
    }
}

#endregion

#region AnimationSave Extension Methods

public static class AnimationSaveExtensions
{
    public static float GetLength(this AnimationSave animation, ElementSave elementSave, ElementAnimationsSave allAnimationSaves)
    {
        float lastState = animation.States.Max(item => item.Time);

        float endOfLastSubAnimation = 0;
        if(animation.Animations != null)
        {
            foreach(var subAnimation in animation.Animations)
            {
                AnimationSave subAnimationSave = null;
                ElementSave subAnimationElement = null;
                ElementAnimationsSave subAnimationSiblings = null;

                if(subAnimation.SourceObject == null)
                {
                    subAnimationSave = allAnimationSaves.Animations.FirstOrDefault(item => item.Name == subAnimation.Name);
                    subAnimationElement = elementSave;
                    subAnimationSiblings = allAnimationSaves;
                }
                else
                {
                    var instance = elementSave.Instances.FirstOrDefault(item=>item.Name == subAnimation.SourceObject);
                    if(instance != null)
                    {
                        ElementSave instanceElement = Gum.Managers.ObjectFinder.Self.GetElementSave(instance);

                        if(instanceElement != null)
                        {
                            var instanceAnimations = AnimationCollectionViewModelManager.Self.GetElementAnimationsSave(instanceElement);

                            subAnimationSave = instanceAnimations.Animations.FirstOrDefault(item => item.Name == subAnimation.RootName);
                            subAnimationElement = instanceElement;
                            subAnimationSiblings = instanceAnimations;
                        }
                    }
                }

                if (subAnimationSave != null)
                {
                    endOfLastSubAnimation = 
                        System.Math.Max( endOfLastSubAnimation,
                        subAnimation.Time + subAnimationSave.GetLength(subAnimationElement, subAnimationSiblings));
                }
            }
        }

        return System.Math.Max(lastState, endOfLastSubAnimation);
    }
}

#endregion
