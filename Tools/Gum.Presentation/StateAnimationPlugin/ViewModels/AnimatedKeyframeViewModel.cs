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
using Gum.StateAnimation.Runtime;

namespace StateAnimationPlugin.ViewModels;

public class AnimatedKeyframeViewModel : ViewModel, IComparable
{
    #region Fields

    AnimationViewModel? mSubAnimationViewModel;
    bool isUncategorized;

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

    public string? EventName
    {
        get => Get<string?>();
        set => Set(value);
    }

    public bool IsTimelineVisualHovered
    {
        get => Get<bool>();
        set => Set(value);
    }

    public AnimationViewModel? SubAnimationViewModel
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

    /// <summary>
    /// True while the editable state ComboBox should be shown (this keyframe references a state).
    /// The view turns this into <c>Visibility</c> via a stock bool-to-visibility converter (ADR-0004).
    /// </summary>
    public bool IsStateComboBoxVisible => !string.IsNullOrEmpty(StateName);

    /// <summary>
    /// True while this keyframe is uncategorized (its referenced state has no parent category).
    /// The view turns this into <c>Visibility</c> via a stock bool-to-visibility converter (ADR-0004).
    /// </summary>
    public bool IsUncategorized => isUncategorized;

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
            else if(EventName != null)
            {
                return EventName;
            }
            else
            {
                return "Unknown Animation";
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
        get => Get<bool>();
        set => Set(value);
    }

    /// <summary>
    /// True when this keyframe points at a state or animation whose reference is missing
    /// (<see cref="HasValidState"/> is false). Named events are never considered broken. Drives the
    /// broken-keyframe icon in the keyframe list (issue #3386); mirrors the broken-keyframe condition
    /// in <c>AnimationViewModel.GetErrors</c>.
    /// </summary>
    [DependsOn(nameof(HasValidState))]
    [DependsOn(nameof(StateName))]
    [DependsOn(nameof(AnimationName))]
    public bool IsMissingReference =>
        !HasValidState && (!string.IsNullOrEmpty(StateName) || !string.IsNullOrEmpty(AnimationName));

    /// <summary>
    /// The message shown when this keyframe's referenced state or animation is missing. Names the
    /// specific reference (and its category, for a categorized state) so the user can find the broken
    /// keyframe without hunting. Shown via <see cref="ShowInvalidStateWarning"/> (issue #3400).
    /// </summary>
    [DependsOn(nameof(StateName))]
    [DependsOn(nameof(AnimationName))]
    public string MissingReferenceMessage
    {
        get
        {
            if (!string.IsNullOrEmpty(StateName))
            {
                int slashIndex = StateName.IndexOf('/');
                if (slashIndex >= 0)
                {
                    string category = StateName.Substring(0, slashIndex);
                    string state = StateName.Substring(slashIndex + 1);
                    return $"Could not find state \"{state}\" in category \"{category}\"";
                }
                return $"Could not find state \"{StateName}\"";
            }
            else if (!string.IsNullOrEmpty(AnimationName))
            {
                return $"Could not find animation \"{AnimationName}\"";
            }
            return "Could not find state or animation";
        }
    }

    /// <summary>
    /// True while a missing-state/animation warning should be shown (this keyframe's reference is
    /// invalid and it isn't a named event, which is never considered broken). The view turns this
    /// into <c>Visibility</c> via a stock bool-to-visibility converter (ADR-0004).
    /// </summary>
    [DependsOn(nameof(HasValidState))]
    public bool ShowInvalidStateWarning => !HasValidState && string.IsNullOrEmpty(EventName);

    /// <summary>
    /// True while the interpolation type/easing editors should be shown (this keyframe references a
    /// state). The view turns this into <c>Visibility</c> via a stock bool-to-visibility converter
    /// (ADR-0004).
    /// </summary>
    public bool IsInterpolationElementVisible => !string.IsNullOrEmpty(StateName);

    #endregion

    #region Methods

    public AnimatedKeyframeViewModel Clone()
    {
        var newInstance = new AnimatedKeyframeViewModel();

        newInstance.StateName = StateName;
        newInstance.AnimationName = AnimationName;
        newInstance.EventName = EventName;
        newInstance.Time = Time;
        newInstance.InterpolationType = InterpolationType;
        newInstance.Easing = Easing;

        newInstance.AvailableStates = new ObservableCollection<string>(AvailableStates);

        newInstance.SubAnimationViewModel = SubAnimationViewModel;

        newInstance.HasValidState = HasValidState;

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
