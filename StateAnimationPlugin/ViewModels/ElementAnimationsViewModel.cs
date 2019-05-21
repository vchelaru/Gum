using Gum.DataTypes;
using Gum.ToolStates;
using StateAnimationPlugin.Managers;
using StateAnimationPlugin.SaveClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows;
using CommonFormsAndControls;
using StateAnimationPlugin.Validation;
using Gum.Managers;
using ToolsUtilities;

namespace StateAnimationPlugin.ViewModels
{
    public class ElementAnimationsViewModel : INotifyPropertyChanged
    {
        #region Fields

        AnimationViewModel mSelectedAnimation;
        ObservableCollection<AnimationViewModel> mAnimations;

        double mDisplayedAnimationTime;
        double timeAnimationStarted;

        // 50 isn't smooth enough, we want more fps!
        //const int mTimerFrequencyInMs = 50;
        const int mTimerFrequencyInMs = 20;

        System.Windows.Threading.DispatcherTimer mPlayTimer;

        BitmapFrame mPlayBitmap;
        BitmapFrame mStopBitmap;



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

        public ObservableCollection<MenuItem> AnimationRightClickItems
        {
            get;
            private set;
        } = new ObservableCollection<MenuItem>();

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

        public BitmapFrame ButtonBitmapFrame
        {
            get
            {
                if(mPlayTimer.IsEnabled)
                {
                    return mStopBitmap;
                }
                else
                {
                    return mPlayBitmap;
                }
            }
        }

        public ElementSave Element
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

        public ElementAnimationsViewModel()
        {
            Animations = new ObservableCollection<AnimationViewModel>();


            mPlayTimer = new DispatcherTimer();
            mPlayTimer.Interval = new TimeSpan(0, 0, 0, 0, mTimerFrequencyInMs);
            mPlayTimer.Tick += HandlePlayTimerTick;

            mPlayBitmap = BitmapLoader.Self.LoadImage("PlayIcon.png");

            mStopBitmap = BitmapLoader.Self.LoadImage("StopIcon.png");
        }

        public static ElementAnimationsViewModel FromSave(ElementAnimationsSave save, Gum.DataTypes.ElementSave element)
        {
            ElementAnimationsViewModel toReturn = new ElementAnimationsViewModel();

            foreach(var animation in save.Animations)
            {
                var vm = AnimationViewModel.FromSave(animation, element);
                toReturn.Animations.Add(vm);
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
            
            if (propertyName == nameof(SelectedAnimation))
            {
                if(SelectedAnimation != null)
                {
                    var selectedElement = SelectedState.Self.SelectedElement;
                    SelectedAnimation.RefreshCombinedStates(selectedElement);
                }

                RefreshRightClickMenuItems();
            }

            if(PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }

            OnAnyChange(this, propertyName);
        }

        private void RefreshRightClickMenuItems()
        {
            AnimationRightClickItems.Clear();

            if(SelectedAnimation != null)
            {
                var menuItem = new MenuItem();
                menuItem.Header = "Rename Animation";
                menuItem.Click += HandleRenameAnimation;

                AnimationRightClickItems.Add(menuItem);
            }


        }

        private void HandleRenameAnimation(object sender, RoutedEventArgs e)
        {
            TextInputWindow tiw = new TextInputWindow();
            tiw.Message = "Enter new animation name:";
            tiw.Result = SelectedAnimation.Name;

            var dialogResult = tiw.ShowDialog();

            if (dialogResult == System.Windows.Forms.DialogResult.OK)
            {
                string whyInvalid;
                if (!NameValidator.IsAnimationNameValid(tiw.Result, Animations, out whyInvalid))
                {
                    MessageBox.Show(whyInvalid);
                }
                else
                {
                    var oldAnimationName = SelectedAnimation.Name;
                    SelectedAnimation.Name = tiw.Result;

                    StateAnimationPlugin.Managers.RenameManager.Self.HandleRename(
                        SelectedAnimation, 
                        oldAnimationName, Animations, Element);   
                }
            }
        }

        private void OnAnyChange(object sender, string propertyName)
        {
            AnyChange?.Invoke(sender, new PropertyChangedEventArgs(propertyName));
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


        private void HandlePlayTimerTick(object sender, EventArgs e)
        {
            var currentTime = Gum.Wireframe.TimeManager.Self.CurrentTime;
            var newValue = currentTime - timeAnimationStarted;

            if (SelectedAnimation != null)
            {
                bool reachedTheEnd = newValue > this.SelectedAnimation.Length;
                if(reachedTheEnd)
                {
                    if (this.SelectedAnimation.Loops)
                    {
                        if (this.SelectedAnimation.Length == 0)
                        {
                            newValue = 0;
                        }
                        else
                        {
                            newValue = newValue % this.SelectedAnimation.Length;
                            timeAnimationStarted = Gum.Wireframe.TimeManager.Self.CurrentTime - newValue;
                        }
                    }
                    else
                    {
                        TogglePlayStop();
                    }
                }
            }
            DisplayedAnimationTime = newValue;
        }



        internal void TogglePlayStop()
        {
            mPlayTimer.IsEnabled = !mPlayTimer.IsEnabled;

            if(mPlayTimer.IsEnabled)
            {
                timeAnimationStarted = Gum.Wireframe.TimeManager.Self.CurrentTime;
                DisplayedAnimationTime = 0;
            }

            OnPropertyChanged("ButtonBitmapFrame");
        }

        public void Stop()
        {
            if (mPlayTimer.IsEnabled)
            {
                mPlayTimer.Stop();
                OnPropertyChanged("ButtonBitmapFrame");

            }
        }

        #endregion

    }
}
