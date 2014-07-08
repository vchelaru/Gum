using Gum.ToolStates;
using StateAnimationPlugin.SaveClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StateAnimationPlugin.ViewModels
{
    public class ElementAnimationsViewModel : INotifyPropertyChanged
    {
        #region Fields

        AnimationViewModel mSelectedAnimation;
        ObservableCollection<AnimationViewModel> mAnimations;

        double mDisplayedAnimationTime;

        double mTimelineValue;

        #endregion

        #region Properties

        public ObservableCollection<AnimationViewModel> Animations
        {
            get { return mAnimations; }
            set
            {
                if(mAnimations != null)
                {
                    mAnimations.CollectionChanged -= HandleListChanged;
                }
                mAnimations = value;

                if (mAnimations != null)
                {
                    mAnimations.CollectionChanged += HandleListChanged;
                }
            }
        }
                
        public AnimationViewModel SelectedAnimation
        {
            get { return mSelectedAnimation; }
            set
            {
                mSelectedAnimation = value;
                OnPropertyChanged("SelectedAnimation");
            }
        }

        public double DisplayedAnimationTime
        {
            get { return mDisplayedAnimationTime; }
            set
            {
                mDisplayedAnimationTime = value;

                if (SelectedAnimation != null)
                {
                    mDisplayedAnimationTime = Math.Min(value, SelectedAnimation.Length);
                }

                OnPropertyChanged("DisplayedAnimationTime");
            }
        }
        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        public event PropertyChangedEventHandler AnyChange;

        #endregion

        #region Methods

        public ElementAnimationsViewModel()
        {
            Animations = new ObservableCollection<AnimationViewModel>();
        }

        public static ElementAnimationsViewModel FromSave(ElementAnimationsSave save)
        {
            ElementAnimationsViewModel toReturn = new ElementAnimationsViewModel();

            foreach(var animation in save.Animations)
            {
                toReturn.Animations.Add(AnimationViewModel.FromSave(animation));
            }

            return toReturn;
        }

        public ElementAnimationsSave ToSave()
        {
            ElementAnimationsSave toReturn = new ElementAnimationsSave();

            foreach(var animation in this.Animations)
            {
                toReturn.Animations.Add(animation.ToSave());
            }

            
            return toReturn;
        }

        private void OnPropertyChanged(string propertyName)
        {
            
            if (propertyName == "SelectedAnimation")
            {
                if(SelectedAnimation != null)
                {
                    SelectedAnimation.RefreshCombinedStates( SelectedState.Self.SelectedElement );
                }
            }

            if(PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }

            OnAnyChange(this, propertyName);
        }

        private void OnAnyChange(object sender, string propertyName)
        {
            if(AnyChange != null)
            {
                AnyChange(sender, new PropertyChangedEventArgs(propertyName));
            }
        }


        private void HandleListChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs eventArgs)
        {
            if(eventArgs.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                foreach(AnimationViewModel item in eventArgs.NewItems)
                {
                    item.PropertyChanged += HandleAnimationItemChange;
                    item.AnyChange += HandleAnimationItemChange;
                }
            }

            OnAnyChange(this, "Animations");
        }

        private void HandleAnimationItemChange(object sender, PropertyChangedEventArgs e)
        {
            OnAnyChange(sender, e.PropertyName);
        }

        #endregion

    }
}
