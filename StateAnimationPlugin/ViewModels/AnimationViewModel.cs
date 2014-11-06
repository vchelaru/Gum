using FlatRedBall.Glue.StateInterpolation;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using StateAnimationPlugin.Managers;
using StateAnimationPlugin.SaveClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace StateAnimationPlugin.ViewModels
{
    public class AnimationViewModel : INotifyPropertyChanged
    {
        #region Fields

        string mName;

        AnimatedKeyframeViewModel mSelectedState;

        bool mIsInMiddleOfSort = false;

        BitmapFrame mLoopBitmap;
        BitmapFrame mPlayOnceBitmap;

        bool mLoops = false;

        #endregion

        #region Properties

        public string Name 
        { 
            get { return mName; }
            set 
            { 
                mName = value;
                OnPropertyChanged("Name");
            }
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
                    var toReturn = Keyframes.Max(item => item.Time + item.Length);

                    return toReturn;
                }
            }
        }

        public bool Loops
        {
            get
            {
                return mLoops;
            }
        }

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

        public AnimatedKeyframeViewModel SelectedKeyframe 
        {
            get
            {
                return mSelectedState;
            }
            set
            {
                mSelectedState = value;

                OnPropertyChanged("SelectedKeyframe");
                OnPropertyChanged("HasSelectedKeyframeVisibility");
            }
        }

        public InstanceSave ContainingInstance
        {
            get;
            set;
        }

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        public event PropertyChangedEventHandler AnyChange;

        #endregion

        #region Methods

        public AnimationViewModel()
        {
            Keyframes = new ObservableCollection<AnimatedKeyframeViewModel>();
            Keyframes.CollectionChanged += HandleCollectionChanged;

            mLoopBitmap = BitmapLoader.Self.LoadImage("LoopIcon.png");

            mPlayOnceBitmap = BitmapLoader.Self.LoadImage("PlayOnceIcon.png");
        }

        public static AnimationViewModel FromSave(AnimationSave save, ElementSave element, ElementAnimationsSave allAnimationSaves = null)
        {
            AnimationViewModel toReturn = new AnimationViewModel();
            toReturn.Name = save.Name;
            toReturn.mLoops = save.Loops;
            foreach(var stateSave in save.States)
            {
                var foundState = element.AllStates.FirstOrDefault(item => item.Name == stateSave.StateName);

                var newAnimatedStateViewModel = AnimatedKeyframeViewModel.FromSave(stateSave);

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
                    var instance = element.Instances.FirstOrDefault(item => item.Name == animationReference.SourceObject);

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
                var newVm = AnimatedKeyframeViewModel.FromSave(animationReference);

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
                else
                {
                    toReturn.Animations.Add(state.ToAnimationReferenceSave());
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

                    asAnimatedState.PropertyChanged += HandleAnimatedStatePropertyChange;
                }
            }

            OnPropertyChanged("Length");
            OnPropertyChanged("MarkerTimes");

            OnAnyChange(this, "States");
            
        }

        private void OnAnyChange(object sender, string property)
        {
            if (AnyChange != null)
            {
                AnyChange(sender, new PropertyChangedEventArgs(property));
            }

        }

        private void HandleAnimatedStatePropertyChange(object sender, PropertyChangedEventArgs e)
        {

            if (e.PropertyName == "Time")
            {
                SortList();

                OnPropertyChanged("Length");
                OnPropertyChanged("MarkerTimes");

            }

            OnAnyChange(sender, e.PropertyName);
        }

        public override string ToString()
        {
            return Name + " (" + Length.ToString("0.00") + ")";
        }

        private void OnPropertyChanged(string propertyName)
        {


            if(PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }



            OnAnyChange(this, propertyName);

        }

        void SortList()
        {
            mIsInMiddleOfSort = true;

            var oldSelected = this.SelectedKeyframe;

            this.Keyframes.BubbleSort();

            this.SelectedKeyframe = oldSelected;

            mIsInMiddleOfSort = false;
        }

        /// <summary>
        /// Perofrms a sequential, cumulative combination of all states along an animation.
        /// As an animation plays each state (keyframe) combines with the previous
        /// to create a combined state.  As the animation continues, these combined states
        /// accumulate the changes of all states before them.  Therefore, we need to pre-combine
        /// the keyframes so that when animations are played they can properly interpolate.
        /// 
        /// </summary>
        public void RefreshCombinedStates(ElementSave element, bool useDefaultAsStarting = true)
        {
            StateSave previous = null;

            if (useDefaultAsStarting)
            {
                previous = element.DefaultState;
            }

            foreach(var animatedState in this.Keyframes.Where(item=>!string.IsNullOrEmpty(item.StateName)))
            {
                var originalState = element.AllStates.FirstOrDefault(item => item.Name == animatedState.StateName);

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
                    subAnimation.SubAnimationViewModel.RefreshCombinedStates(element, false);
                }
                else
                {
                    var instanceElement = Gum.Managers.ObjectFinder.Self.GetElementSave(instance);
                    if (instanceElement != null)
                    {
                        subAnimation.SubAnimationViewModel.RefreshCombinedStates(instanceElement, false);
                    }
                }
            }
        }

        public void ToggleLoop()
        {
            mLoops = !mLoops;

            OnPropertyChanged("ButtonBitmapFrame");

            if(AnyChange != null)
            {
                AnyChange(this, new PropertyChangedEventArgs("Loops"));
            }
        }

        public void SetStateAtTime(double animationTime, ElementSave element, bool defaultIfNull)
        {
            StateSave stateToSet = GetStateToSet(animationTime, element, defaultIfNull);

            if(stateToSet != null)
            {
                WireframeObjectManager.Self.RootGue.ApplyState(stateToSet);
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

                var stateFromAnimation = keyframe.SubAnimationViewModel.GetStateToSet(relativeTime, element, false);

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
                        RefreshCombinedStates(element);
                    }
                }

                Gum.ToolStates.SelectedState.Self.CustomCurrentStateSave = stateVmAfter.CachedCumulativeState;
                stateToSet = Gum.ToolStates.SelectedState.Self.CustomCurrentStateSave.Clone();
            }
            else if (stateVmBefore != null && stateVmAfter == null)
            {
                if (stateVmBefore.CachedCumulativeState == null)
                {
                    if (element != null)
                    {
                        RefreshCombinedStates(element);
                    }
                }
                Gum.ToolStates.SelectedState.Self.CustomCurrentStateSave = stateVmBefore.CachedCumulativeState;
                stateToSet = Gum.ToolStates.SelectedState.Self.CustomCurrentStateSave.Clone();
            }
            else if (stateVmBefore != null && stateVmAfter != null)
            {
                if (stateVmAfter.CachedCumulativeState == null ||
                    stateVmAfter.CachedCumulativeState == null)
                {
                    if (element != null)
                    {
                        RefreshCombinedStates(element);
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

                    Gum.ToolStates.SelectedState.Self.CustomCurrentStateSave = combined;

                    // for performance we will only update wireframe:
                    //SelectedState.Self.UpdateToSelectedStateSave();
                    //WireframeObjectManager.Self.RefreshAll(true);

                    stateToSet = combined;
                }
            }

            if(stateToSet == null && defaultIfNull)
            {
                stateToSet = element.DefaultState.Clone();
            }
            else if(stateToSet == null)
            {

                stateToSet = new StateSave();
            }
            return stateToSet;
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
}
