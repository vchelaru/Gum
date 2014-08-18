using Gum.DataTypes;
using Gum.DataTypes.Variables;
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

        AnimatedStateViewModel mSelectedState;

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
                if(States.Count == 0)
                {
                    return 0;
                }
                else
                {
                    var toReturn = States.Last().Time;
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

        public ObservableCollection<AnimatedStateViewModel> States { get; private set; }

        public AnimatedStateViewModel SelectedState 
        {
            get
            {
                return mSelectedState;
            }
            set
            {
                mSelectedState = value;

                OnPropertyChanged("SelectedState");
            }
        }

        public IEnumerable<double> MarkerTimes
        {
            get
            {
                foreach(var item in States)
                {
                    yield return (double)item.Time;
                }
            }
        }

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        public event PropertyChangedEventHandler AnyChange;

        #endregion

        #region Methods

        public AnimationViewModel()
        {
            States = new ObservableCollection<AnimatedStateViewModel>();
            States.CollectionChanged += HandleCollectionChanged;

            mLoopBitmap = BitmapLoader.Self.LoadImage("LoopIcon.png");

            mPlayOnceBitmap = BitmapLoader.Self.LoadImage("PlayOnceIcon.png");
        }

        public static AnimationViewModel FromSave(AnimationSave save, ElementSave element)
        {
            AnimationViewModel toReturn = new AnimationViewModel();
            toReturn.Name = save.Name;
            toReturn.mLoops = save.Loops;
            foreach(var stateSave in save.States)
            {
                var foundState = element.AllStates.FirstOrDefault(item => item.Name == stateSave.StateName);

                var newAnimatedStateViewModel = AnimatedStateViewModel.FromSave(stateSave);

                newAnimatedStateViewModel.HasValidState = foundState != null;

                toReturn.States.Add(newAnimatedStateViewModel);
            }

            return toReturn;
        }

        public AnimationSave ToSave()
        {
            AnimationSave toReturn = new AnimationSave();
            toReturn.Name = this.Name;
            toReturn.Loops = this.mLoops;

            foreach(var state in this.States)
            {
                toReturn.States.Add(state.ToSave());
            }

            return toReturn;
        }

        private void HandleCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && !mIsInMiddleOfSort)
            {
                foreach(var newAdd in e.NewItems)
                {
                    var asAnimatedState = newAdd as AnimatedStateViewModel;

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

            var oldSelected = this.SelectedState;

            this.States.BubbleSort();

            this.SelectedState = oldSelected;

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
        public void RefreshCombinedStates(ElementSave element)
        {
            StateSave previous = element.DefaultState;

            foreach(var animatedState in this.States)
            {
                var originalState = element.AllStates.FirstOrDefault(item => item.Name == animatedState.StateName);
                if (originalState != null)
                {
                    var combined = previous.Clone();
                    combined.MergeIntoThis(originalState);

                    animatedState.CachedCumulativeState = combined;

                    previous = combined;
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
}
