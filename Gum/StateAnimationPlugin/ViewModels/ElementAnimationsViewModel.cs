using Gum.DataTypes;
using Gum.ToolStates;
using StateAnimationPlugin.Managers;
using Gum.StateAnimation.SaveClasses;
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
using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using Gum.Mvvm;
using Gum.Wireframe;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.Plugins.Errors;

namespace StateAnimationPlugin.ViewModels;

public partial class ElementAnimationsViewModel : ViewModel
{
    #region Fields

    // 50 isn't smooth enough, we want more fps!
    //const int mTimerFrequencyInMs = 50;
    const int mTimerFrequencyInMs = 20;

    System.Windows.Threading.DispatcherTimer mPlayTimer;

    BitmapFrame mPlayBitmap;
    BitmapFrame mStopBitmap;

    private readonly ISelectedState _selectedState;
    private readonly INameVerifier _nameVerifier;
    private readonly IDialogService _dialogService;
    private readonly NameValidator _nameValidator;

    #endregion

    #region Properties

    public bool ClampInterpolationVisuals
    {
        get => Get<bool>();
        set => Set(value);
    }

    public ObservableCollection<AnimationViewModel> Animations { get; private set; }

    public ObservableCollection<MenuItem> AnimationRightClickItems
    {
        get;
        private set;
    } = new ObservableCollection<MenuItem>();

    public ObservableCollection<MenuItem> AnimationStateRightClickItems
    {
        get;
        private set;
    } = new ObservableCollection<MenuItem>();

    [DependsOn(nameof(SelectedAnimation))]
    public Visibility PlayButtonVisibility => 
        (SelectedAnimation != null).ToVisibility();

    public AnimationViewModel? SelectedAnimation
    {
        get => Get<AnimationViewModel?>();
        set
        {
            if (Set(value))
            {
                if (SelectedAnimation != null)
                {
                    var selectedElement = _selectedState.SelectedElement;
                    if(selectedElement == null)
                    {
                        return;
                    }
                    SelectedAnimation.RefreshCumulativeStates(selectedElement);

                    if(SelectedAnimation.SelectedKeyframe != null)
                    {
                        SelectedAnimation.TrySelectKeyframeReferencedStateSave();
                    }
                }

                RefreshAnimationsRightClickMenuItems();

            }
        }
    }

    public double DisplayedAnimationTime
    {
        get => Get<double>();
        set
        {
            var valueToSet = value;
            if (SelectedAnimation != null)
            {
                valueToSet = Math.Min(value, SelectedAnimation.Length);
            }



            Set(valueToSet);

        }
    }

    [DependsOn(nameof(SelectedAnimation))]
    public string OverLengthTime
    {
        get
        {
            if(SelectedAnimation == null || SelectedAnimation.Keyframes.Count == 0)
            {
                return string.Empty;
            }
            else
            {
                var maxLength = SelectedAnimation.Keyframes.Max(item => item.Time);
                return "/" + maxLength;

            }
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
        get => Get<ElementSave>();
        set => Set(value);
    }

    [DependsOn(nameof(Element))]
    public string AnimationColumnTitle => Element == null ? "No Element Selected" : $"{Element.Name} Animations";

    public ElementAnimationsSave? BackingData { get; private set; }

    public List<string> GameSpeedList { get; set; } =
        new List<string>
        {
            "4000%",
            "2000%",
            "1000%",
            "500%",
            "200%",
            "100%",
            "50%",
            "25%",
            "10%",
            "5%"
        };

    public string CurrentGameSpeed
    {
        get => Get<string>();
        set
        {
            if (Set(value))
            {
                AnimationSpeedMultiplier = 
                    int.Parse(CurrentGameSpeed.Substring(0, CurrentGameSpeed.Length - 1)) / 100.0;

            }
        }
    }

    double AnimationSpeedMultiplier = 1.0;

    #endregion

    #region Events


    public event PropertyChangedEventHandler? AnyChange;

    //public event EventHandler SelectedItemPropertyChanged;

    #endregion


    public ElementAnimationsViewModel(INameVerifier nameVerifier, IDialogService dialogService)
    {
        ClampInterpolationVisuals = true;
        CurrentGameSpeed = "100%";

        Animations = new ObservableCollection<AnimationViewModel>();
        Animations.CollectionChanged += HandleListChanged;

        this.PropertyChanged += (sender, args) => OnPropertyChanged(args.PropertyName);

        mPlayTimer = new DispatcherTimer();
        mPlayTimer.Interval = new TimeSpan(0, 0, 0, 0, mTimerFrequencyInMs);
        mPlayTimer.Tick += HandlePlayTimerTick;

        mPlayBitmap = BitmapLoader.Self.LoadImage("PlayIcon.png");

        mStopBitmap = BitmapLoader.Self.LoadImage("StopIcon.png");

        _selectedState = Locator.GetRequiredService<ISelectedState>();
        _nameVerifier = nameVerifier;
        _nameValidator = new NameValidator(_nameVerifier);
        _dialogService = dialogService;
    }

    public void LoadFromSave(ElementAnimationsSave save, Gum.DataTypes.ElementSave element)
    {
        BackingData = save;

        foreach (var animation in save.Animations)
        {
            var vm = AnimationViewModel.FromSave(animation, element);
            Animations.Add(vm);
        }
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

    private void OnPropertyChanged(string? propertyName)
    {
        OnAnyChange(this, propertyName);
    }

    private void RefreshAnimationsRightClickMenuItems()
    {
        AnimationRightClickItems.Clear();

        if(SelectedAnimation != null)
        {
            var menuItem = new MenuItem();
            menuItem.Header = "Rename Animation";
            menuItem.Click += HandleRenameAnimation;
            AnimationRightClickItems.Add(menuItem);

            var squashStretch = new MenuItem();
            squashStretch.Header = "Squash/Stretch Frame Times";
            squashStretch.Click += HandleSquashStretchTimes;
            AnimationRightClickItems.Add(squashStretch);

            var deleteAnimation = new MenuItem();
            deleteAnimation.Header = "Delete Animation";
            deleteAnimation.Click += HandleDeleteAnimation;
            AnimationRightClickItems.Add(deleteAnimation);

            var duplicateAnimation = new MenuItem();
            duplicateAnimation.Header = "Duplicate Animation";
            duplicateAnimation.Click += HandleDuplicateAnimation;
            AnimationRightClickItems.Add(duplicateAnimation);
        }
    }

    private void RefreshAnimationStatesRightClickMenuitems()
    {
        AnimationStateRightClickItems.Clear();

        if(this.SelectedAnimation?.SelectedKeyframe != null)
        {
            var deleteState = new MenuItem();
            deleteState.Header = "Delete Keyframe";
            deleteState.Click += HandleDeleteKeyframe;
            AnimationStateRightClickItems.Add(deleteState);
        }
    }

    private void HandleDeleteKeyframe(object sender, System.Windows.RoutedEventArgs e)
    {
        if(SelectedAnimation != null && SelectedAnimation.SelectedKeyframe != null)
        {
            SelectedAnimation.Keyframes.Remove(SelectedAnimation.SelectedKeyframe);
        }
    }

    private void HandleRenameAnimation(object sender, System.Windows.RoutedEventArgs e)
    {
        /////////////////Early Out/////////////////
        if(SelectedAnimation == null)
        {
            return;
        }
        ///////////////End Early Out///////////////
        string message = "Enter new animation name:";

        GetUserStringOptions options = new()
        {
            InitialValue = SelectedAnimation.Name,
            Validator = v =>
                _nameValidator.IsAnimationNameValid(v, Animations, out string? whyInvalid) ? null : whyInvalid,
        };

        if (_dialogService.GetUserString(message, null, options) is { } result)
        {
            var oldAnimationName = SelectedAnimation.Name;
            SelectedAnimation.Name = result;

            StateAnimationPlugin.Managers.RenameManager.Self.HandleRename(
                SelectedAnimation, 
                oldAnimationName, Animations, Element);
        }
    }

    private void HandleSquashStretchTimes(object sender, System.Windows.RoutedEventArgs e)
    {
        if(SelectedAnimation == null)
        {
            return;
        }

        string message = "Set desired animation length (in seconds):";
        GetUserStringOptions options = new()
        {
            InitialValue = SelectedAnimation.Length.ToString(CultureInfo.InvariantCulture),
            Validator = v =>
                float.TryParse(v, out float value) && value > 0
                    ? null
                    : "Please enter a valid number greater than 0"
        };

        if (_dialogService.GetUserString(message, null, options) is { } result)
        {
            // We should use decimals until the very last operation to avoid floating point errors:
            var value = decimal.Parse(result);
            var animationLengthBeforeChange = (decimal)SelectedAnimation.Length;

            if (animationLengthBeforeChange > 0)
            {
                var multiplier = value / animationLengthBeforeChange;

                foreach(var frame in this.SelectedAnimation.Keyframes.ToArray())
                {
                    var frameTime = (decimal)frame.Time;

                    var newTime = frameTime * multiplier;

                    frame.Time = (float)newTime;
                }
            }

        }
    }

    private void HandleDeleteAnimation(object sender, System.Windows.RoutedEventArgs e)
    {
        if(SelectedAnimation == null)
        {
            return;
        }
        if (_dialogService.ShowYesNoMessage("Delete animation " + SelectedAnimation.Name + "?", "Delete?"))
        {
            Animations.Remove(SelectedAnimation);
        }
    }

    private void HandleDuplicateAnimation(object sender, System.Windows.RoutedEventArgs e)
    {
        if(SelectedAnimation == null)
        {
            return;
        }

        var copyOfAnimation = SelectedAnimation.Clone();

        copyOfAnimation.Name = $"Copy of {copyOfAnimation.Name}";
        StateAnimationPlugin.Managers.RenameManager.Self.HandleRename(
            copyOfAnimation,
            SelectedAnimation.Name, Animations, Element);

        Animations.Add(copyOfAnimation);
    }

    private void OnAnyChange(object? sender, string? propertyName)
    {
        AnyChange?.Invoke(sender, new PropertyChangedEventArgs(propertyName));
    }


    private void HandleListChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs eventArgs)
    {
        if(eventArgs.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && eventArgs.NewItems != null)
        {
            foreach(AnimationViewModel item in eventArgs.NewItems)
            {
                if(item != null)
                {
                    item.PropertyChanged += HandleAnimationItemChange;
                    item.FramePropertyChanged += HandleFrameItemChanged;
                }
            }
        }
        NotifyPropertyChanged(nameof(OverLengthTime));

        OnAnyChange(this, "Animations");
    }

    private void HandleFrameItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnAnyChange(sender, e.PropertyName);
    }

    private void HandleAnimationItemChange(object? sender, PropertyChangedEventArgs e)
    {
        var shouldNotifyOfTimeChange = false;

        // todo - depending on the sender, raise the event here

        if(shouldNotifyOfTimeChange)
        {
            NotifyPropertyChanged(nameof(OverLengthTime));
        }

        if(e.PropertyName == nameof(AnimationViewModel.SelectedKeyframe))
        {
            RefreshAnimationStatesRightClickMenuitems();
        }

        OnAnyChange(sender, e.PropertyName);

    }

    double? lastPlayTimerTickTime;
    private void HandlePlayTimerTick(object? sender, EventArgs e)
    {
        var currentTime = Gum.Wireframe.TimeManager.Self.CurrentTime;


        var increaseInValue = AnimationSpeedMultiplier * (mTimerFrequencyInMs /1000.0);

        if(lastPlayTimerTickTime != null)
        {
            increaseInValue = AnimationSpeedMultiplier * (currentTime - lastPlayTimerTickTime.Value);
        }

        lastPlayTimerTickTime = currentTime;

        var newValue = DisplayedAnimationTime + increaseInValue;

        if (SelectedAnimation != null)
        {
            bool reachedTheEnd = newValue > this.SelectedAnimation.Length;
            if(reachedTheEnd)
            {
                if (this.SelectedAnimation.Loops)
                {
                    newValue = 0;
                }
                else
                {
                    IsPlaying = !IsPlaying;
                }
            }
        }

        DisplayedAnimationTime = newValue;
    }

    public bool IsPlaying
    {
        get => Get<bool>();
        set
        {
            if (Set(value))
            {
                lastPlayTimerTickTime = null;
                mPlayTimer.IsEnabled = value;
                if (value)
                {
                    DisplayedAnimationTime = 0;
                }
            }
        }
    }

    internal void DecreaseGameSpeed()
    {
        var index = GameSpeedList.IndexOf(CurrentGameSpeed);
        if (index < GameSpeedList.Count - 1)
        {
            CurrentGameSpeed = GameSpeedList[index + 1];
        }
    }

    internal void IncreaseGameSpeed()
    {
        var index = GameSpeedList.IndexOf(CurrentGameSpeed);
        if (index > 0)
        {
            CurrentGameSpeed = GameSpeedList[index - 1];
        }
    }

    public string? GetWhyAddingAnimationIsInvalid()
    {
        string? whyIsntValid = null;
        if (_selectedState.SelectedScreen == null && _selectedState.SelectedComponent == null)
        {
            whyIsntValid = "You must first select a Screen or Component";
        }

        return whyIsntValid;
    }

    public IEnumerable<ErrorViewModel> GetErrors()
    {
        List<ErrorViewModel> toReturn = new List<ErrorViewModel>();
        foreach(var animation in Animations)
        {
            var animationErrors = animation.GetErrors();
            toReturn.AddRange(animationErrors);
        }
        return toReturn;
    }


    [RelayCommand]
    private void ToggleInterpolationClamping()
    {
        ClampInterpolationVisuals = !ClampInterpolationVisuals;
    }

    internal void RefreshErrors(ElementSave element)
    {
        foreach(var animation in Animations)
        {
            animation.RefreshErrors(element);
        }
    }
}
