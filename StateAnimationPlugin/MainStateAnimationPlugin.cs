using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;
using StateAnimationPlugin.Views;
using StateAnimationPlugin.Managers;
using System.Windows.Forms.Integration;
using StateAnimationPlugin.ViewModels;
using Gum.ToolStates;
using System.Windows;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using FlatRedBall.Glue.StateInterpolation;
using Gum.DataTypes;
using Gum;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Gum.Responses;
using Gum.StateAnimation.SaveClasses;

using Gum.Plugins;
using System.Windows.Forms;
using Gum.Services;
using Gum.Commands;
using Gum.Managers;
using Gum.Services.Dialogs;


namespace StateAnimationPlugin;

[Export(typeof(PluginBase))]
public class MainStateAnimationPlugin : PluginBase
{
    private readonly ISelectedState _selectedState;
    private readonly INameVerifier _nameVerifier;
    private readonly Func<ElementAnimationsViewModel> _animationVmFactory;

    #region Fields
    private readonly DuplicateService _duplicateService;
    private readonly AnimationFilePathService _animationFilePathService;
    private readonly ElementDeleteService _elementDeleteService;
    private readonly RenameManager _renameManager;
    private readonly SettingsManager _settingsManager;
    private readonly ProjectState _projectState;
    private readonly AnimationCollectionViewModelManager _animationCollectionViewModelManager;
    ElementAnimationsViewModel _viewModel;

    StateAnimationPlugin.Views.MainWindow mMainWindow;
    private PluginTab pluginTab;
    private ToolStripMenuItem menuItem;

    #endregion

    #region Properties

    public override string FriendlyName
    {
        get { return "State Animation Plugin"; }
    }

    // 0.0.0.2: Renaming Gum file now renames its animations
    public override Version Version
    {
        get { return new Version(0, 0, 0, 2); }
    }

    ObservableCollection<string> AvailableStates { get; set; } = new ObservableCollection<string>();

    #endregion

    #region StartUp/ShutDown

    public MainStateAnimationPlugin()
    {
        _selectedState = Locator.GetRequiredService<ISelectedState>();
        _nameVerifier = Locator.GetRequiredService<INameVerifier>();
        _animationVmFactory = () => new ElementAnimationsViewModel(_nameVerifier, _dialogService);
        _duplicateService = new DuplicateService();
        _animationFilePathService = new AnimationFilePathService();
        _elementDeleteService = new ElementDeleteService(_animationFilePathService);
        _renameManager = RenameManager.Self;
        _settingsManager = SettingsManager.Self;
        _projectState = GumState.Self.ProjectState;
        _animationCollectionViewModelManager = AnimationCollectionViewModelManager.Self;
    }

    public override void StartUp()
    {
        CreateMenuItems();
        CreateAnimationWindow();
        AssignEvents();
    }


    private void CreateMenuItems()
    {
        menuItem = AddMenuItem("View", "View Animations");

        menuItem.Click += HandleToggleTabVisibility;
    }

    private void AssignEvents()
    {
        this.ElementSelected += HandleElementSelected;

        this.InstanceSelected += (_, _) => RefreshViewModel();

        this.InstanceRename += HandleInstanceRename;
        this.StateRename += HandleStateRename;

        this.StateAdd += HandleStateAdd;
        this.StateDelete += HandleStateDelete;

        this.VariableSet += HandleVariableSet;

        this.CategoryRename += HandleCategoryRename;

        this.ElementRename += HandleElementRename;
        this.ElementDuplicate += HandleElementDuplicate;

        this.GetDeleteStateResponse = HandleGetDeleteStateResponse;
        this.GetDeleteStateCategoryResponse = HandleGetDeleteStateCategoryResponse;

        this.DeleteOptionsWindowShow += _elementDeleteService.HandleDeleteOptionsWindowShow;
        this.DeleteConfirm += _elementDeleteService.HandleConfirmDelete;
    }

    private void HandleElementSelected(ElementSave? element)
    {
        RefreshViewModel();

        if (element != null && !pluginTab.IsVisible)
        {
            var fileName = _animationFilePathService.GetAbsoluteAnimationFileNameFor(element);

            if (fileName.Exists())
            {
                pluginTab.Show();
            }
        }
    }

    public override bool ShutDown(PluginShutDownReason shutDownReason)
    {
        return true;
    }

    private void HandleVariableSet(ElementSave element, InstanceSave save2, string arg3, object arg4)
    {
        // This maybe a little inefficient but it should address all issues:
        // eventually this could be more targeted
        var state = _selectedState.SelectedStateSave;
        if (_viewModel == null || state == null) return;

        var isDefault =
            state == _selectedState.SelectedElement?.DefaultState;

        var stateName = state.Name;
        if (_selectedState.SelectedStateCategorySave != null)
        {
            stateName = _selectedState.SelectedStateCategorySave.Name + "/" + stateName;
        }

        foreach (var animation in _viewModel.Animations)
        {
            var shouldRefresh = isDefault;
            if (!shouldRefresh)
            {
                // see if this state is referenced:
                foreach (var keyframe in animation.Keyframes)
                {
                    if (keyframe.StateName == stateName)
                    {
                        shouldRefresh = true;
                        break;
                    }
                }
            }

            if (shouldRefresh)
            {
                animation.RefreshCumulativeStates(element);
            }
        }
    }


    #endregion

    private void HandleElementDuplicate(ElementSave oldElement, ElementSave newElement)
    {
        _duplicateService.HandleDuplicate(oldElement, newElement);
    }

    private void HandleElementRename(ElementSave element, string oldName)
    {
        if (_viewModel == null)
        {
            CreateViewModel();
        }

        _renameManager.HandleRename(element, oldName, _viewModel);
    }

    private void HandleInstanceRename(ElementSave element, InstanceSave instanceSave, string oldName)
    {
        if (_viewModel == null)
        {
            CreateViewModel();
        }

        if (_selectedState.SelectedElement != null)
        {
            _renameManager.HandleRename(instanceSave, oldName, _viewModel);
        }
    }

    private void HandleStateRename(StateSave stateSave, string oldName)
    {
        RefreshViewModel();

        if (_selectedState.SelectedElement != null)
        {
            _renameManager.HandleRename(stateSave, oldName, _viewModel);
        }
    }

    private void HandleStateAdd(StateSave state)
    {
        RefreshViewModel();
    }

    private void HandleStateDelete(StateSave state)
    {
        RefreshViewModel();
    }

    private void HandleCategoryRename(StateSaveCategory category, string oldName)
    {
        if (_viewModel == null)
        {
            CreateViewModel();
        }

        // We only care about this if we have an element. Otherwise, it could be a behavior:
        if (_selectedState.SelectedElement != null)
        {
            _renameManager.HandleRename(category, oldName, _viewModel);
        }

    }

    private void HandleToggleTabVisibility(object sender, EventArgs e)
    {
        pluginTab.IsVisible = !pluginTab.IsVisible;
    }

    private void CreateAnimationWindow()
    {
        if (mMainWindow == null)
        {
            _settingsManager.LoadOrCreateSettings();

            mMainWindow = new StateAnimationPlugin.Views.MainWindow();

            var settings = _settingsManager.GlobalSettings;

            mMainWindow.FirstRowWidth = new GridLength((double)settings.FirstToSecondColumnRatio, GridUnitType.Star);
            mMainWindow.SecondRowWidth = new GridLength(1, GridUnitType.Star);
            mMainWindow.AddStateKeyframeClicked += HandleAddStateKeyframe;
            mMainWindow.AnimationKeyframeAdded += HandleAnimationKeyrameAdded;
            mMainWindow.AnimationColumnsResized += HandleAnimationColumnsResized;
            
            pluginTab = _tabManager.AddControl(mMainWindow, "Animations",
                TabLocation.RightBottom);

            pluginTab.TabShown += HandleTabShown;
            pluginTab.TabHidden += HandleTabHidden;
            pluginTab.CanClose = true;
            pluginTab.Hide();
        }

        // forces a refresh:
        _viewModel = new ElementAnimationsViewModel(_nameVerifier, _dialogService);

        RefreshViewModel();
    }

    private void HandleAnimationColumnsResized()
    {
        if (mMainWindow.SecondRowWidth.Value > 0)
        {
            var ratio = mMainWindow.FirstRowWidth.Value / mMainWindow.SecondRowWidth.Value;

            _settingsManager.GlobalSettings.FirstToSecondColumnRatio = (decimal)ratio;

            _settingsManager.SaveSettings();
        }
    }

    private void HandleAddStateKeyframe(object sender, EventArgs e)
    {
        string whyIsntValid = GetWhyAddingTimedStateIsInvalid();

        if (!string.IsNullOrEmpty(whyIsntValid))
        {
            _dialogService.ShowMessage(whyIsntValid);
            return;
        }

        AddStateKeyframeDialog dialog = new()
        {
            ElementSave = _selectedState.SelectedElement
        };

        _dialogService.Show(dialog);

        if (dialog.Result is { } newVm)
        {
            if (_viewModel.SelectedAnimation.SelectedKeyframe != null)
            {
                // put this after the current animation
                newVm.Time = _viewModel.SelectedAnimation.SelectedKeyframe.Time + 1f;
            }
            else if (_viewModel.SelectedAnimation.Keyframes.Count != 0)
            {
                newVm.Time = _viewModel.SelectedAnimation.Keyframes.Last().Time + 1f;
            }

            _viewModel.SelectedAnimation.Keyframes.BubbleSort();

            _viewModel.SelectedAnimation.Keyframes.Add(newVm);
            // Call this *before* setting SelectedKeyframe so the available
            // states are assigned. Otherwise
            // StateName will be nulled out.
            HandleAnimationKeyrameAdded(newVm);
            _viewModel.SelectedAnimation.SelectedKeyframe = newVm;
        }
    }

    private void HandleAnimationKeyrameAdded(AnimatedKeyframeViewModel newVm)
    {
        newVm.AvailableStates = this.AvailableStates;
        newVm.PropertyChanged += HandleAnimatedKeyframePropertyChanged;
    }

    private string GetWhyAddingTimedStateIsInvalid()
    {
        string whyIsntValid = null;

        if (_viewModel.SelectedAnimation == null)
        {
            whyIsntValid = "You must first select an Animation";
        }

        if (_selectedState.SelectedScreen == null && _selectedState.SelectedComponent == null)
        {
            whyIsntValid = "You must first select a Screen or Component";
        }
        return whyIsntValid;
    }

    private void HandleTabShown()
    {
        menuItem.Text = "Hide Animations";
    }

    private void HandleTabHidden()
    {
        menuItem.Text = "View Animations";
    }

    private void RefreshViewModel()
    {
        RefreshAvailableStates();

        CreateViewModel();

        if (mMainWindow != null)
        {
            mMainWindow.DataContext = _viewModel;
        }
    }

    private void RefreshAvailableStates()
    {
        // we always create the view model after refreshing states, so we can new up an observable collection:

        //AvailableStates = new ObservableCollection<string>();

        var states = new List<string>();

        var element = _selectedState.SelectedElement;

        if (element != null)
        {
            states.AddRange(element.States.Select(item => item.Name));

            foreach (var category in element.Categories)
            {
                states.AddRange(category.States.Select(item => category.Name + "/" + item.Name));
            }

        }

        AvailableStates.ReplaceWith(states);
    }

    private void CreateViewModel()
    {
        ElementSave currentlyReferencedElement = null;
        if (_viewModel != null)
        {
            currentlyReferencedElement = _viewModel.Element;
        }

        var element = _selectedState.SelectedElement;

        if (currentlyReferencedElement != element)
        {
            if (_projectState.GumProjectSave?.FullFileName == null)
            {
                _viewModel = null;
            }
            else
            {
                _viewModel = _animationCollectionViewModelManager.GetAnimationCollectionViewModel(element);
            }

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += HandlePropertyChanged;
                _viewModel.AnyChange += HandleDataChange;

                foreach (var item in _viewModel.Animations)
                {
                    foreach (var keyframe in item.Keyframes)
                    {
                        keyframe.AvailableStates = this.AvailableStates;
                        keyframe.PropertyChanged += HandleAnimatedKeyframePropertyChanged;

                    }
                }
            }
        }

        if (_viewModel == null)
        {
            _viewModel = new(_nameVerifier, _dialogService);
        }
    }

    private void HandleAnimatedKeyframePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(AnimatedKeyframeViewModel.StateName):
                // user may have changed a state that is currently being displayed so let's refresh it all!
                SetWireframeStateFromDisplayedAnimTime();
                break;
        }
    }

    private void HandlePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        var variableName = e.PropertyName;

        if (sender is ElementAnimationsViewModel)
        {
            if (variableName == "DisplayedAnimationTime")
            {
                SetWireframeStateFromDisplayedAnimTime();
            }
        }
    }

    private void SetWireframeStateFromDisplayedAnimTime()
    {
        //////////////////////// EARLY OUT
        if (_viewModel.SelectedAnimation == null)
        {
            return;
        }
        ////////////////////// END EARLY OUT

        var animationTime = _viewModel.DisplayedAnimationTime;

        var animation = _viewModel.SelectedAnimation;
        var element = _selectedState.SelectedElement;

        animation.SetStateAtTime(animationTime, element, defaultIfNull: true);
    }

    private void HandleDataChange(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        var variableName = e.PropertyName;

        bool shouldSave = true;

        if (sender is ElementAnimationsViewModel)
        {
            if (variableName == nameof(ElementAnimationsViewModel.SelectedAnimation) ||
                variableName == nameof(ElementAnimationsViewModel.OverLengthTime))
            {
                shouldSave = false;
            }
            else if (variableName == nameof(ElementAnimationsViewModel.DisplayedAnimationTime))
            {
                shouldSave = false;
            }
        }

        if (sender is AnimationViewModel)
        {
            // can this happen? I don't see anything on the view model
            if (variableName == "SelectedState" ||
                variableName == nameof(AnimationViewModel.SelectedKeyframe) ||
                variableName == nameof(AnimationViewModel.Length)
                )
            {
                shouldSave = false;
            }
        }

        if (sender is AnimatedKeyframeViewModel)
        {
            if (variableName == nameof(AnimatedKeyframeViewModel.DisplayString) ||
                variableName == nameof(AnimatedKeyframeViewModel.AvailableStates))
            {
                shouldSave = false;
            }
        }

        if (shouldSave)
        {
            try
            {
                _animationCollectionViewModelManager.Save(_viewModel);
            }
            catch (Exception exc)
            {
                _guiCommands.PrintOutput($"Could not save animations for {_viewModel?.Element}:\n{exc}");
            }
        }
    }

    private DeleteResponse HandleGetDeleteStateResponse(StateSave state, IStateContainer container)
    {
        var response = new DeleteResponse();
        response.ShouldDelete = true;

        List<AnimationSave> animatedStatesReferencingState = GetAnimationsReferencingState(state, container as ElementSave);

        if (animatedStatesReferencingState?.Count > 0)
        {
            string message = "Are you sure you want to delete this state? It is used by the following animations. Deleting this state may break the animation:\n\n";

            foreach (var animation in animatedStatesReferencingState)
            {
                message += animation.Name;
            }

            if (!_dialogService.ShowYesNoMessage(message, "Delete state?"))
            {
                response.ShouldDelete = false;
                response.Message = null;
                response.ShouldShowMessage = false; // user said 'no', no need to show a message...S
            }
        }

        return response;
    }

    private DeleteResponse HandleGetDeleteStateCategoryResponse(StateSaveCategory category, IStateContainer container)
    {
        var response = new DeleteResponse();
        response.ShouldDelete = true;

        var animatedStatesReferencingState = new HashSet<AnimationSave>();

        foreach (var state in category.States)
        {
            foreach (var toAdd in GetAnimationsReferencingState(state, container as ElementSave))
            {
                animatedStatesReferencingState.Add(toAdd);
            }
        }

        if (animatedStatesReferencingState?.Count > 0)
        {
            string message = "Are you sure you want to delete this category? It is used by the following animations. Deleting this category may break the animation:\n\n";

            foreach (var animation in animatedStatesReferencingState)
            {
                message += animation.Name;
            }

            if (!_dialogService.ShowYesNoMessage(message, "Delete category?"))
            {
                response.ShouldDelete = false;
                response.Message = null;
                response.ShouldShowMessage = false; // user said 'no', no need to show a message...S
            }
        }

        return response;
    }

    private List<AnimationSave> GetAnimationsReferencingState(StateSave state, ElementSave element)
    {
        List<AnimationSave> animatedStatesReferencingState = new List<AnimationSave>();
        if (element != null)
        {
            global::Gum.StateAnimation.SaveClasses.ElementAnimationsSave model;
            if (element == _viewModel?.Element)
            {
                model = _viewModel.BackingData;
            }
            else
            {
                model = _animationCollectionViewModelManager.GetElementAnimationsSave(element);
            }


            animatedStatesReferencingState = new List<AnimationSave>();

            var category = element.Categories.FirstOrDefault(item => item.States.Contains(state));

            var stateName = category != null
                ? $"{category.Name}/{state.Name}"
                : state.Name;

            if (model != null)
            {
                foreach (var animation in model.Animations)
                {
                    foreach (var animatedState in animation.States)
                    {
                        if (animatedState.StateName == stateName)
                        {
                            animatedStatesReferencingState.Add(animation);
                            break;
                        }
                    }
                }
            }
        }

        return animatedStatesReferencingState;
    }
}
