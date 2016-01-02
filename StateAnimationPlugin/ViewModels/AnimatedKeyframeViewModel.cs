using FlatRedBall.Glue.StateInterpolation;
using Gum.DataTypes.Variables;
using StateAnimationPlugin.Managers;
using StateAnimationPlugin.SaveClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace StateAnimationPlugin.ViewModels
{
    public class AnimatedKeyframeViewModel : INotifyPropertyChanged, IComparable
    {
        #region Fields

        string mStateName;
        string mAnimationName;
        string mEventName;
        AnimationViewModel mSubAnimationViewModel;

        float mTime;

        InterpolationType mInterpolationType;
        Easing mEasing;


        static BitmapFrame mStateBitmap;
        static BitmapFrame mAnimationBitmap;
        static BitmapFrame mEventBitmap;
        #endregion

        #region Properties

        public string StateName 
        {
            get { return mStateName; } 
            set
            {
                mStateName = value;
                OnPropertyChanged("StateName");
            }
        }

        public string AnimationName
        {
            get { return mAnimationName; }
            set
            {
                mAnimationName = value;
                OnPropertyChanged("AnimationName");
            }
        }

        public string EventName
        {
            get
            {
                return mEventName;
            }
            set
            {
                mEventName = value;
                OnPropertyChanged("mEventName");
            }
        }

        public AnimationViewModel SubAnimationViewModel
        {
            get { return mSubAnimationViewModel; }
            set { mSubAnimationViewModel = value; }
        }

        public string DisplayName
        {
            get
            {
                if(!string.IsNullOrEmpty(StateName))
                {
                    return StateName;
                }
                else if(!string.IsNullOrEmpty(AnimationName))
                {
                    return AnimationName;
                }
                else
                {
                    return EventName;
                }
            }
        }

        public float Time 
        {
            get { return mTime; }
            set 
            { 
                mTime = value;

                OnPropertyChanged("Time");
                OnPropertyChanged("DisplayString");
                
            }
        }

        public float Length
        {
            get
            {
                if(mSubAnimationViewModel == null)
                {
                    return 0;
                }
                else
                {
                    return mSubAnimationViewModel.Length;
                }
            }
        }

        public InterpolationType InterpolationType 
        {
            get { return mInterpolationType; }
            set
            {
                mInterpolationType = value;
                OnPropertyChanged("InterpolationType");
            }
        }
        
        public Easing Easing 
        {
            get { return mEasing; }
            set 
            {
                mEasing = value;
                OnPropertyChanged("Easing");
            }
        }

        public string DisplayString 
        {
            get
            {
                var timeAsString = Time.ToString("0.00");

                if (!string.IsNullOrEmpty(StateName))
                {
                    return StateName + " (" + timeAsString + ")";
                }
                else if(!string.IsNullOrEmpty(AnimationName))
                {
                    return AnimationName + " (" + timeAsString + ")";
                }
                else
                {
                    return $"{EventName} ({timeAsString})";
                }
            }
        }

        public bool HasValidState
        {
            get;
            set;
        }

        public System.Windows.Visibility HasInvalidStateVisibility
        {
            get
            {
                if(HasValidState || !string.IsNullOrEmpty(EventName))
                {
                    return System.Windows.Visibility.Collapsed;
                }
                else
                {
                    return System.Windows.Visibility.Visible;
                }
            }
        }

        public SolidColorBrush LabelBrush
        {
            get
            {
                if (!string.IsNullOrEmpty(EventName))
                {
                    return Brushes.DarkBlue;
                }
                if (HasValidState)
                {
                    return Brushes.Black;

                }
                else
                {

                    return Brushes.Red;
                }
            }
        }

        public StateSave CachedCumulativeState
        {
            get;
            set;
        }

        public System.Windows.Visibility InterpolationElementVisibility
        {
            get
            {
                if(!string.IsNullOrEmpty(StateName))
                {
                    return System.Windows.Visibility.Visible;
                }
                else
                {
                    return System.Windows.Visibility.Collapsed;
                }
            }
        }


        public BitmapFrame IconBitmapFrame
        {
            get
            {
                if(!string.IsNullOrEmpty(StateName))
                {
                    return mStateBitmap;
                }
                else if(!string.IsNullOrEmpty(AnimationName))
                {
                    return mAnimationBitmap;
                }
                else
                {
                    return mEventBitmap;
                }
            }
        }

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Methods

        static AnimatedKeyframeViewModel()
        {
            mStateBitmap = BitmapLoader.Self.LoadImage("StateAnimationIcon.png");
            mAnimationBitmap = BitmapLoader.Self.LoadImage("ReferencedAnimationIcon.png");
            mEventBitmap = BitmapLoader.Self.LoadImage("NamedEventIcon.png");

        }

        public static AnimatedKeyframeViewModel FromSave(AnimatedStateSave save)
        {
            AnimatedKeyframeViewModel toReturn = new AnimatedKeyframeViewModel();

            toReturn.StateName = save.StateName;
            toReturn.Time = save.Time;
            toReturn.InterpolationType = save.InterpolationType;
            toReturn.Easing = save.Easing;

            return toReturn;
        }

        public static AnimatedKeyframeViewModel FromSave(AnimationReferenceSave save)
        {
            AnimatedKeyframeViewModel toReturn = new AnimatedKeyframeViewModel();

            toReturn.AnimationName = save.Name;
            toReturn.Time = save.Time;
            // There's no easing/interpolation supported for animation references

            return toReturn;
        }

        public static AnimatedKeyframeViewModel FromSave(NamedEventSave save)
        {
            AnimatedKeyframeViewModel toReturn = new AnimatedKeyframeViewModel();
            toReturn.EventName = save.Name;
            toReturn.Time = save.Time;

            return toReturn;
        }

        public AnimatedStateSave ToAnimatedStateSave()
        {
            AnimatedStateSave toReturn = new AnimatedStateSave();

            if(string.IsNullOrEmpty(StateName))
            {
                throw new InvalidOperationException("Could not convert this to a AnimatedStateSave because it doesn't have a valid StateName");
            }

            toReturn.StateName = this.StateName;
            toReturn.Time = this.Time;
            toReturn.InterpolationType = this.InterpolationType;
            toReturn.Easing = this.Easing;

            return toReturn;

        }

        public AnimationReferenceSave ToAnimationReferenceSave()
        {
            AnimationReferenceSave toReturn = new AnimationReferenceSave();

            if(string.IsNullOrEmpty(AnimationName))
            {
                throw new InvalidOperationException("Could not convert this to an AnimationReference because it doesn't have a valid Animation name");
            }

            toReturn.Name = this.AnimationName;
            toReturn.Time = this.Time;

            return toReturn;
        }

        public NamedEventSave ToEventSave()
        {
            NamedEventSave toReturn = new NamedEventSave();

            if(string.IsNullOrEmpty(EventName))
            {
                throw new InvalidOperationException("Could not convert this to a NamedEventSave because it doesn't have a valid EventName");
            }

            toReturn.Name = this.EventName;
            toReturn.Time = this.Time;

            return toReturn;
        }


        private void OnPropertyChanged(string property)
        {
            if(PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }


        public override string ToString()
        {
            return DisplayString;
        }


        public int CompareTo(object other)
        {
            if (other is AnimatedKeyframeViewModel)
            {
                return this.Time.CompareTo((other as AnimatedKeyframeViewModel).Time);
            }
            else
            {
                return 0;
            }
        }

        #endregion
    }
}
