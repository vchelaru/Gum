using FlatRedBall.Glue.StateInterpolation;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Mvvm;
using StateAnimationPlugin.Managers;
using Gum.StateAnimation.SaveClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Gum.StateAnimation.Runtime;

namespace StateAnimationPlugin.ViewModels;

public class AnimatedKeyframeViewModel : ViewModel, IComparable
{
    #region Fields

    AnimationViewModel mSubAnimationViewModel;

    static BitmapFrame mStateBitmap;
    static BitmapFrame mAnimationBitmap;
    static BitmapFrame mEventBitmap;
    #endregion

    #region Properties

    public string StateName 
    {
        get => Get<string>(); 
        set => Set(value); 
    }

    public string AnimationName
    {
        get => Get<string>();
        set => Set(value); 
    }

    public ObservableCollection<string> AvailableStates
    {
        get => Get<ObservableCollection<string>>();
        set => Set(value);
    }

    public string EventName
    {
        get => Get<string>(); 
        set => Set(value); 
    }

    public bool IsTimelineVisualHovered
    {
        get => Get<bool>();
        set => Set(value);
    }

    public AnimationViewModel SubAnimationViewModel
    {
        get { return mSubAnimationViewModel; }
        set { mSubAnimationViewModel = value; }
    }


    public float Time 
    {
        get { return Get<float>(); }
        set { Set(value); }
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
        get => Get<InterpolationType>();
        set => Set(value);
    }
    
    public Easing Easing 
    {
        get => Get<Easing>();
        set => Set(value); 
    }

    public Visibility StateComboBoxVisibility =>
        !string.IsNullOrEmpty(StateName) ? Visibility.Visible : Visibility.Collapsed;

    public Visibility DisplayNameLabelVisibility =>
        string.IsNullOrEmpty(StateName) ? Visibility.Visible : Visibility.Collapsed;

    bool isUncategorized;
    public Visibility UncategorizedIconVisibility => isUncategorized.ToVisibility();

    [DependsOn(nameof(StateName))]
    [DependsOn(nameof(AnimationName))]
    [DependsOn(nameof(EventName))]
    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrEmpty(StateName))
            {
                return StateName;
            }
            else if (!string.IsNullOrEmpty(AnimationName))
            {
                return AnimationName;
            }
            else
            {
                return EventName;
            }
        }
    }

    [DependsOn("Time")]
    [DependsOn(nameof(StateName))]
    [DependsOn(nameof(AnimationName))]
    [DependsOn(nameof(EventName))]
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

    public KeyframeRuntime KeyframeRuntime { get; set; }

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


    [DependsOn(nameof(StateName))]
    [DependsOn(nameof(AnimationName))]
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


    static BitmapFrame mExclamationIcon;
    public BitmapFrame ExclamationIcon => mExclamationIcon;

    #endregion

    #region Methods

    static AnimatedKeyframeViewModel()
    {
        mStateBitmap = BitmapLoader.Self.LoadImage("StateAnimationIcon.png");
        mAnimationBitmap = BitmapLoader.Self.LoadImage("ReferencedAnimationIcon.png");
        mEventBitmap = BitmapLoader.Self.LoadImage("NamedEventIcon.png");
        mExclamationIcon = BitmapLoader.Self.LoadImage("redExclamation.png");

    }

    public AnimatedKeyframeViewModel Clone()
    {
        var newInstance = new AnimatedKeyframeViewModel();

        newInstance.StateName = StateName;
        newInstance.AnimationName = AnimationName;
        newInstance.EventName = EventName;
        newInstance.Time = Time;
        newInstance.InterpolationType = InterpolationType;
        newInstance.Easing = Easing;

        newInstance.AvailableStates = new ObservableCollection<string>();
        newInstance.AvailableStates.AddRange(AvailableStates);

        newInstance.SubAnimationViewModel = SubAnimationViewModel;

        newInstance.HasValidState = HasValidState;

        // do we assign this?
        newInstance.KeyframeRuntime = new KeyframeRuntime();
    
        return newInstance;
    }

    public static AnimatedKeyframeViewModel FromSave(AnimatedStateSave save, ElementSave elementSave)
    {
        AnimatedKeyframeViewModel toReturn = new AnimatedKeyframeViewModel();

        toReturn.StateName = save.StateName;
        toReturn.Time = save.Time;
        toReturn.InterpolationType = save.InterpolationType;
        toReturn.Easing = save.Easing;

        toReturn.isUncategorized = elementSave.States.Any(item => item.Name == save.StateName);

        return toReturn;
    }

    public static AnimatedKeyframeViewModel FromSave(AnimationReferenceSave save, ElementSave elementSave)
    {
        AnimatedKeyframeViewModel toReturn = new AnimatedKeyframeViewModel();

        toReturn.AnimationName = save.Name;
        toReturn.Time = save.Time;
        // There's no easing/interpolation supported for animation references

        return toReturn;
    }

    public static AnimatedKeyframeViewModel FromSave(NamedEventSave save, ElementSave elementSave)
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


    public override string ToString()
    {
        return DisplayString;
    }


    public int CompareTo(object? other)
    {
        if (other is AnimatedKeyframeViewModel animatedKeyframeViewModel)
        {
            return this.Time.CompareTo(animatedKeyframeViewModel.Time);
        }
        else
        {
            return 0;
        }
    }

    internal KeyframeRuntime ToKeyframeRuntime()
    {
        KeyframeRuntime runtime = new KeyframeRuntime();
        runtime.StateName = this.StateName;
        runtime.AnimationName = this.AnimationName;
        runtime.EventName = this.EventName;
        runtime.Time = this.Time;
        runtime.InterpolationType = this.InterpolationType;
        runtime.Easing = this.Easing;
        runtime.SubAnimation = this.SubAnimationViewModel?.ToAnimationRuntime();
        return runtime;
    }

    #endregion
}
