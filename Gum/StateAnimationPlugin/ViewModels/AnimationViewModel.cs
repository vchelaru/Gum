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
using CommunityToolkit.Mvvm.Input;
using Gum.Services;
using Gum.StateAnimation.Runtime;
using Gum.Plugins.Errors;

namespace StateAnimationPlugin.ViewModels;

public partial class AnimationViewModel : ViewModel
{
    #region Fields

    bool mIsInMiddleOfSort = false;

    BitmapFrame mLoopBitmap;
    BitmapFrame mPlayOnceBitmap;
    
    private readonly ISelectedState _selectedState;
    private readonly WireframeObjectManager _wireframeObjectManager;
    AnimationRuntime? _cachedAnimationRuntime;

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

    public bool Loops
    {
        get => Get<bool>();
        set => Set(value);
    }

    public ObservableCollection<AnimatedKeyframeViewModel> Keyframes { get; private set; }

    public event PropertyChangedEventHandler FramePropertyChanged;

    public AnimatedKeyframeViewModel? SelectedKeyframe 
    {
        get => Get<AnimatedKeyframeViewModel?>();
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
        Keyframes.CollectionChanged += HandleKeyframeCollectionChanged;

        mLoopBitmap = BitmapLoader.Self.LoadImage("LoopIcon.png");

        mPlayOnceBitmap = BitmapLoader.Self.LoadImage("PlayOnceIcon.png");
        _selectedState = Locator.GetRequiredService<ISelectedState>();
        _wireframeObjectManager = Locator.GetRequiredService<WireframeObjectManager>();
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
        toReturn.Loops = save.Loops;

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
            AnimationSave? animationSave = null;
            ElementSave? subAnimationElement= null;
            ElementAnimationsSave? subAnimationSiblings = null;

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
        toReturn.Loops = this.Loops;

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

    private void HandleKeyframeCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && !mIsInMiddleOfSort && e.NewItems != null)
        {
            foreach(var newAdd in e.NewItems)
            {
                var asAnimatedState = newAdd as AnimatedKeyframeViewModel;
                if(asAnimatedState != null)
                {
                    asAnimatedState.PropertyChanged += HandleAnimatedKeyframePropertyChange;
                }
            }
        }

        NotifyPropertyChanged(nameof(Length));

        NotifyPropertyChanged(nameof(Keyframes));

        if (_selectedState.SelectedElement != null)
        {
            RefreshCumulativeStates(_selectedState.SelectedElement);
        }

    }

    private void HandleAnimatedKeyframePropertyChange(object? sender, PropertyChangedEventArgs e)
    {
        switch(e.PropertyName)
        {
            case nameof(AnimatedKeyframeViewModel.Time):

                SortList();

                NotifyPropertyChanged(nameof(Length));

                if (_selectedState.SelectedElement != null)
                {
                    RefreshCumulativeStates(_selectedState.SelectedElement);
                }
                break;
            case nameof(AnimatedKeyframeViewModel.StateName):
            case nameof(AnimatedKeyframeViewModel.InterpolationType):
            case nameof(AnimatedKeyframeViewModel.Easing):
                if(_selectedState.SelectedElement != null)
                {
                    RefreshCumulativeStates(_selectedState.SelectedElement);
                }
                break;

            default:
                return;
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


    static StateSave? GetStateFromCategorizedName(string categorizedName, ElementSave element)
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
        _cachedAnimationRuntime = this.ToAnimationRuntime();

        _cachedAnimationRuntime.RefreshCumulativeStates(element, useDefaultAsStarting);

        System.Diagnostics.Debug.WriteLine("Updated cumulative states for animation " + this.Name + " at " + DateTime.Now);
    }

    internal AnimationRuntime ToAnimationRuntime()
    {
        AnimationRuntime animationRuntime = new AnimationRuntime();
        animationRuntime.Name = this.Name;
        animationRuntime.Loops = this.Loops;
        foreach(var keyframe in this.Keyframes)
        {
            animationRuntime.Keyframes.Add(keyframe.ToKeyframeRuntime());
        }
        return animationRuntime;
    }

    [RelayCommand]
    public void ToggleLoop()
    {
        Loops = !Loops;
    }

    public void SetStateAtTime(double animationTime, ElementSave element, bool defaultIfNull)
    {
        var stateToSet = _cachedAnimationRuntime?.GetStateToSet(animationTime, element, defaultIfNull);

        if(stateToSet != null)
        {
            _selectedState.CustomCurrentStateSave = stateToSet;
            _selectedState.SelectedStateSave = null;
            _wireframeObjectManager.RootGue?.ApplyState(stateToSet);
        }
    }

    internal IEnumerable<ErrorViewModel> GetErrors()
    {
        foreach(var keyframe in this.Keyframes)
        {
            if(!keyframe.HasValidState)
            {
                if (!string.IsNullOrEmpty(keyframe.StateName))
                {
                    yield return new ErrorViewModel()
                    {
                        Message = $"{this.Name} Keyframe at time {keyframe.Time} references a state {keyframe.StateName} which does not exist."
                    };
                }
                else if (!string.IsNullOrEmpty(keyframe.AnimationName))
                {
                    yield return new ErrorViewModel()
                    {
                        Message = $"{this.Name} Keyframe at time {keyframe.Time} references an animation {keyframe.AnimationName} which does not exist."
                    };
                }
            }
        }
    }

    internal void RefreshErrors(ElementSave elementSave)
    {
        foreach (var keyframe in this.Keyframes)
        {
            if(!string.IsNullOrEmpty(keyframe.StateName))
            {
                keyframe.HasValidState = GetStateFromCategorizedName(keyframe.StateName, elementSave) != null;
            }
            else if(!string.IsNullOrEmpty(keyframe.AnimationName))
            {
                keyframe.HasValidState = keyframe.SubAnimationViewModel != null;
            }
        }
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
                AnimationSave? subAnimationSave = null;
                ElementSave? subAnimationElement = null;
                ElementAnimationsSave? subAnimationSiblings = null;

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
                        ElementSave? instanceElement = Gum.Managers.ObjectFinder.Self.GetElementSave(instance);

                        if(instanceElement != null)
                        {
                            var instanceAnimations = AnimationCollectionViewModelManager.Self.GetElementAnimationsSave(instanceElement);

                            subAnimationSave = instanceAnimations.Animations.FirstOrDefault(item => item.Name == subAnimation.RootName);
                            subAnimationElement = instanceElement;
                            subAnimationSiblings = instanceAnimations;
                        }
                    }
                }

                if (subAnimationSave != null && subAnimationElement != null && subAnimationSiblings != null)
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
