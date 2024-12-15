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
using StateAnimationPlugin.SaveClasses;

using Gum.Plugins;
using System.Windows.Forms;


namespace StateAnimationPlugin
{
    [Export(typeof(PluginBase))]
    public class MainStateAnimationPlugin : PluginBase
    {
        #region Fields
        private readonly DuplicateService _duplicateService;
        private readonly AnimationFilePathService _animationFilePathService;
        private readonly ElementDeleteService _elementDeleteService;
        ElementAnimationsViewModel mCurrentViewModel;

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
            _duplicateService = new DuplicateService();
            _animationFilePathService = new AnimationFilePathService();
            _elementDeleteService = new ElementDeleteService(_animationFilePathService);
        }

        public override void StartUp()
        {

            CreateMenuItems();
            CreateAnimationWindow();
            AssignEvents();
        }

        public override bool ShutDown(Gum.Plugins.PluginShutDownReason shutDownReason)
        {
            return true;
        }

        private void CreateMenuItems()
        {
            menuItem = AddMenuItem(new List<string> { "State Animation", "View Animations" });

            menuItem.Click += HandleToggleTabVisibility;
        }

        private void AssignEvents()
        {
            this.ElementSelected += (_) => RefreshViewModel();

            this.InstanceSelected += (_, _) => RefreshViewModel();

            this.InstanceRename += HandleInstanceRename;
            this.StateRename += HandleStateRename;

            this.StateAdd += HandleStateAdd;
            this.StateDelete += HandleStateDelete;

            this.CategoryRename += HandleCategoryRename;
            this.ElementRename += HandleElementRename;
            this.ElementDuplicate += HandleElementDuplicate;

            this.GetDeleteStateResponse = HandleGetDeleteStateResponse;
            this.GetDeleteStateCategoryResponse = HandleGetDeleteStateCategoryResponse;

            this.DeleteOptionsWindowShow += _elementDeleteService.HandleDeleteOptionsWindowShow;
            this.DeleteConfirm += _elementDeleteService.HandleConfirmDelete;
        }


        #endregion

        private void HandleElementDuplicate(ElementSave oldElement, ElementSave newElement)
        {
            _duplicateService.HandleDuplicate(oldElement, newElement);
        }

        private void HandleElementRename(ElementSave element, string oldName)
        {
            if (mCurrentViewModel == null)
            {
                CreateViewModel();
            }

            RenameManager.Self.HandleRename(element, oldName, mCurrentViewModel);
        }

        private void HandleInstanceRename(ElementSave element, InstanceSave instanceSave, string oldName)
        {
            if (mCurrentViewModel == null)
            {
                CreateViewModel();
            }

            if (SelectedState.Self.SelectedElement != null)
            {
                RenameManager.Self.HandleRename(instanceSave, oldName, mCurrentViewModel);
            }
        }

        private void HandleStateRename(StateSave stateSave, string oldName)
        {
            RefreshViewModel();

            if (SelectedState.Self.SelectedElement != null)
            {
                RenameManager.Self.HandleRename(stateSave, oldName, mCurrentViewModel);
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
            if (mCurrentViewModel == null)
            {
                CreateViewModel();
            }

            // We only care about this if we have an element. Otherwise, it could be a behavior:
            if(SelectedState.Self.SelectedElement != null)
            {
                RenameManager.Self.HandleRename(category, oldName, mCurrentViewModel);
            }

        }

        private void HandleToggleTabVisibility(object sender, EventArgs e)
        {
            if (!GumCommands.Self.GuiCommands.IsTabVisible(pluginTab))
            {
                pluginTab.Show();
            }
            else
            {
                pluginTab.Hide();
            }
        }

        private void CreateAnimationWindow()
        {
            var shouldCreate = mMainWindow == null;
            if (mMainWindow == null)
            {
                SettingsManager.Self.LoadOrCreateSettings();

                mMainWindow = new StateAnimationPlugin.Views.MainWindow();

                var settings = SettingsManager.Self.GlobalSettings;

                mMainWindow.FirstRowWidth = new GridLength((double)settings.FirstToSecondColumnRatio, GridUnitType.Star);
                mMainWindow.SecondRowWidth = new GridLength(1, GridUnitType.Star);
                mMainWindow.AddStateKeyframeClicked += HandleAddStateKeyframe;
                mMainWindow.AnimationKeyframeAdded += HandleAnimationKeyrameAdded;
                mMainWindow.AnimationColumnsResized += HandleAnimationColumnsResized;
            }

            var wasShown = GumCommands.Self.GuiCommands.ShowTabForControl(mMainWindow);

            if(!wasShown)
            {
                pluginTab = GumCommands.Self.GuiCommands.AddControl(mMainWindow, "Animations", 
                    TabLocation.RightBottom);

                pluginTab.TabShown += HandleTabShown;
                pluginTab.TabHidden += HandleTabHidden;
                pluginTab.CanClose = true;
                pluginTab.Hide();
            }

            // forces a refresh:
            mCurrentViewModel = new ElementAnimationsViewModel();

            RefreshViewModel();
        }

        private void HandleAnimationColumnsResized()
        {
            if(mMainWindow.SecondRowWidth.Value > 0)
            {
                var ratio = mMainWindow.FirstRowWidth.Value / mMainWindow.SecondRowWidth.Value;

                SettingsManager.Self.GlobalSettings.FirstToSecondColumnRatio = (decimal)ratio;

                SettingsManager.Self.SaveSettings();
            }
        }

        private void HandleAddStateKeyframe(object sender, EventArgs e)
        {
            string whyIsntValid = GetWhyAddingTimedStateIsInvalid();

            if (!string.IsNullOrEmpty(whyIsntValid))
            {
                System.Windows.MessageBox.Show(whyIsntValid);

            }
            else
            {
                ListBoxMessageBox lbmb = new ListBoxMessageBox();
                lbmb.RequiresSelection = true;
                lbmb.Message = "Select a state";

                var element = SelectedState.Self.SelectedElement;

                foreach (var state in element.States)
                {
                    lbmb.Items.Add(state.Name);
                }

                foreach (var category in element.Categories)
                {
                    foreach (var state in category.States)
                    {
                        lbmb.Items.Add(category.Name + "/" + state.Name);
                    }
                }


                var dialogResult = lbmb.ShowDialog();

                if (dialogResult.HasValue && dialogResult.Value)
                {
                    var item = lbmb.SelectedItem;

                    var newVm = new AnimatedKeyframeViewModel();


                    newVm.StateName = (string)item;
                    // User just selected the state, so it better be valid!
                    newVm.HasValidState = true;
                    newVm.InterpolationType = FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear;
                    newVm.Easing = FlatRedBall.Glue.StateInterpolation.Easing.Out;


                    if (mCurrentViewModel.SelectedAnimation.SelectedKeyframe != null)
                    {
                        // put this after the current animation
                        newVm.Time = mCurrentViewModel.SelectedAnimation.SelectedKeyframe.Time + 1f;
                    }
                    else if (mCurrentViewModel.SelectedAnimation.Keyframes.Count != 0)
                    {
                        newVm.Time = mCurrentViewModel.SelectedAnimation.Keyframes.Last().Time + 1f;
                    }


                    mCurrentViewModel.SelectedAnimation.Keyframes.BubbleSort();

                    mCurrentViewModel.SelectedAnimation.Keyframes.Add(newVm);
                    // Call this *before* setting SelectedKeyframe so the available states are assigned. Otherwise
                    // StateName will be nulled out.
                    HandleAnimationKeyrameAdded(newVm);
                    mCurrentViewModel.SelectedAnimation.SelectedKeyframe = newVm;


                }
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

            if (mCurrentViewModel.SelectedAnimation == null)
            {
                whyIsntValid = "You must first select an Animation";
            }

            if (SelectedState.Self.SelectedScreen == null && SelectedState.Self.SelectedComponent == null)
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
                mMainWindow.DataContext = mCurrentViewModel;
            }
        }

        private void RefreshAvailableStates()
        {
            // we always create the view model after refreshing states, so we can new up an observable collection:

            //AvailableStates = new ObservableCollection<string>();

            var states = new List<string>();

            var element = SelectedState.Self.SelectedElement;

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
            if (mCurrentViewModel != null)
            {
                currentlyReferencedElement = mCurrentViewModel.Element;
            }

            var element = SelectedState.Self.SelectedElement;

            if (currentlyReferencedElement != element)
            {
                if(GumState.Self.ProjectState.GumProjectSave?.FullFileName == null)
                {
                    mCurrentViewModel = null;
                }
                else
                {
                    mCurrentViewModel = AnimationCollectionViewModelManager.Self.GetAnimationCollectionViewModel(element);
                }

                if (mCurrentViewModel != null)
                {
                    mCurrentViewModel.PropertyChanged += HandlePropertyChanged;
                    mCurrentViewModel.AnyChange += HandleDataChange;

                    foreach(var item in mCurrentViewModel.Animations)
                    {
                        foreach(var keyframe in item.Keyframes)
                        {
                            keyframe.AvailableStates = this.AvailableStates;
                            keyframe.PropertyChanged += HandleAnimatedKeyframePropertyChanged;

                        }
                    }
                }
            }

            if(mCurrentViewModel == null)
            {
                mCurrentViewModel = new ElementAnimationsViewModel();
            }
        }

        private void HandleAnimatedKeyframePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
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
                if(variableName == "DisplayedAnimationTime")
                {
                    SetWireframeStateFromDisplayedAnimTime();
                }
            }
        }

        private void SetWireframeStateFromDisplayedAnimTime()
        {
            //////////////////////// EARLY OUT
            if(mCurrentViewModel.SelectedAnimation == null)
            {
                return;
            }
            ////////////////////// END EARLY OUT

            var animationTime = mCurrentViewModel.DisplayedAnimationTime;

            var animation = mCurrentViewModel.SelectedAnimation;
            var element = SelectedState.Self.SelectedElement;

            animation.SetStateAtTime(animationTime, element, defaultIfNull:true);
        }


        private void HandleDataChange(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var variableName = e.PropertyName;

            bool shouldSave = true;

            if(sender is  ElementAnimationsViewModel)
            {
                if(variableName == nameof(ElementAnimationsViewModel.SelectedAnimation) ||
                    variableName == nameof(ElementAnimationsViewModel.OverLengthTime))
                {
                    shouldSave = false;
                }
                else if(variableName == nameof(ElementAnimationsViewModel.DisplayedAnimationTime))
                {
                    shouldSave = false;
                }
            }

            if (sender is AnimationViewModel)
            {
                // can this happen? I don't see anything on the view model
                if(variableName == "SelectedState" || 
                    variableName == nameof(AnimationViewModel.SelectedKeyframe) ||
                    variableName == nameof(AnimationViewModel.Length) 
                    )
                {
                    shouldSave = false;
                }
            }

            if( sender is AnimatedKeyframeViewModel)
            {
                if(variableName == nameof(AnimatedKeyframeViewModel.DisplayString) || 
                    variableName == nameof(AnimatedKeyframeViewModel.AvailableStates))
                {
                    shouldSave = false;
                }
            }

            if (shouldSave)
            {
                try
                {
                    AnimationCollectionViewModelManager.Self.Save(mCurrentViewModel);
                }
                catch(Exception exc)
                {
                    GumCommands.Self.GuiCommands.PrintOutput($"Could not save animations for {mCurrentViewModel?.Element}:\n{exc}");
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

                var result = System.Windows.MessageBox.Show(message, "Delete state?", MessageBoxButton.YesNo);

                if (result != MessageBoxResult.Yes)
                {
                    response.ShouldDelete = false;
                    response.Message = null;
                    response.ShouldShowMessage = false; // user said 'no', no need to show a message...S
                }
            }

            return response;
        }

        private DeleteResponse HandleGetDeleteStateCategoryResponse(StateSaveCategory category, IStateCategoryListContainer container)
        {
            var response = new DeleteResponse();
            response.ShouldDelete = true;

            var animatedStatesReferencingState = new HashSet<AnimationSave>();
            
            foreach(var state in category.States)
            {
                foreach(var toAdd in GetAnimationsReferencingState(state, container as ElementSave))
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

                var result = System.Windows.MessageBox.Show(message, "Delete category?", MessageBoxButton.YesNo);

                if (result != MessageBoxResult.Yes)
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
                SaveClasses.ElementAnimationsSave model;
                if (element == mCurrentViewModel?.Element)
                {
                    model = mCurrentViewModel.BackingData;
                }
                else
                {
                    model = AnimationCollectionViewModelManager.Self.GetElementAnimationsSave(element);
                }


                animatedStatesReferencingState = new List<AnimationSave>();

                var category = element.Categories.FirstOrDefault(item => item.States.Contains(state));

                var stateName = category != null
                    ? $"{category.Name}/{state.Name}"
                    : state.Name;

                if(model != null)
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
}
