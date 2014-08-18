using FlatRedBall.Glue.StateInterpolation;
using Gum.DataTypes.Variables;
using StateAnimationPlugin.SaveClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace StateAnimationPlugin.ViewModels
{
    public class AnimatedStateViewModel : INotifyPropertyChanged, IComparable
    {
        #region Fields

        string mStateName;
        float mTime;

        InterpolationType mInterpolationType;
        Easing mEasing;

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
                return StateName + " (" + Time.ToString("0.00") + ")";
            }
        }

        public bool HasValidState
        {
            get;
            set;
        }

        public SolidColorBrush LabelBrush
        {
            get
            {
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

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Methods

        public static AnimatedStateViewModel FromSave(AnimatedStateSave save)
        {
            AnimatedStateViewModel toReturn = new AnimatedStateViewModel();

            toReturn.StateName = save.StateName;
            toReturn.Time = save.Time;
            toReturn.InterpolationType = save.InterpolationType;
            toReturn.Easing = save.Easing;

            return toReturn;
        }

        public AnimatedStateSave ToSave()
        {
            AnimatedStateSave toReturn = new AnimatedStateSave();

            toReturn.StateName = this.StateName;
            toReturn.Time = this.Time;
            toReturn.InterpolationType = this.InterpolationType;
            toReturn.Easing = this.Easing;

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
            return StateName + " (" + Time.ToString("0.00") + ")";
        }


        public int CompareTo(object other)
        {
            if (other is AnimatedStateViewModel)
            {
                return this.Time.CompareTo((other as AnimatedStateViewModel).Time);
            }
            else
            {
                return 0;
            }
        }

        #endregion
    }
}
