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

namespace StateAnimationPlugin
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : PluginBase
    {
        #region Fields

        ElementAnimationsViewModel mCurrentViewModel;

        StateAnimationPlugin.Views.MainWindow mMainWindow;

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

        public override void StartUp()
        {
            CreateMenuItems();

            AssignEvents();
        }

        public override bool ShutDown(Gum.Plugins.PluginShutDownReason shutDownReason)
        {
            return true;
        }

        private void CreateMenuItems()
        {
            var menuItem = AddMenuItem(new List<string> { "State Animation", "View Animations" });

            menuItem.Click += HandleViewAnimationsClick;
        }

        private void AssignEvents()
        {
            this.ElementSelected += delegate
            {
                RefreshViewModel();
            };

            this.InstanceSelected += delegate
            {
                RefreshViewModel();
            };

            this.InstanceRename += HandleInstanceRename;
            this.StateRename += HandleStateRename;
            this.CategoryRename += HandleCategoryRename;
            this.ElementRename += HandleElementRename;
            this.ElementDuplicate += HandleElementDuplicate;
        }

        #endregion

        private void HandleElementDuplicate(ElementSave oldElement, ElementSave newElement)
        {
            DuplicateManager.Self.HandleDuplicate(oldElement, newElement);
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
            if(mCurrentViewModel == null)
            {
                CreateViewModel();
            }

            if (SelectedState.Self.SelectedElement != null)
            {
                RenameManager.Self.HandleRename(stateSave, oldName, mCurrentViewModel);
            }
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


        private void HandleViewAnimationsClick(object sender, EventArgs e)
        {
            if(mMainWindow == null || mMainWindow.IsVisible == false)
            {
                mMainWindow = new StateAnimationPlugin.Views.MainWindow();
                // This fixes an issue where embedded wpf text boxes don't get input, as explained here:
                // http://stackoverflow.com/questions/835878/wpf-textbox-not-accepting-input-when-in-elementhost-in-window-forms
                //ElementHost.EnableModelessKeyboardInterop(mMainWindow);
                //mMainWindow.Show();
                //mMainWindow.Closed += (not, used) => Gum.ToolStates.SelectedState.Self.CustomCurrentStateSave = null;
                mMainWindow.AddStateKeyframeClicked += HandleAddStateKeyframe;
            }
                
            GumCommands.Self.GuiCommands.AddControl(mMainWindow, "Animations", 
                TabLocation.Right);

            GumCommands.Self.GuiCommands.ShowControl(mMainWindow);

            // forces a refresh:
            mCurrentViewModel = new ElementAnimationsViewModel();

            RefreshViewModel();
        }

        private void HandleAddStateKeyframe(object sender, EventArgs e)
        {
            string whyIsntValid = GetWhyAddingTimedStateIsInvalid();

            if (!string.IsNullOrEmpty(whyIsntValid))
            {
                MessageBox.Show(whyIsntValid);

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

                    var newVm = new AnimatedKeyframeViewModel()
                    {
                        StateName = (string)item,
                        // User just selected the state, so it better be valid!
                        HasValidState = true,
                        InterpolationType = FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear,
                        Easing = FlatRedBall.Glue.StateInterpolation.Easing.Out

                    };

                    newVm.AvailableStates = this.AvailableStates;
                    newVm.PropertyChanged += HandleAnimatedKeyframePropertyChanged;

                    if (mCurrentViewModel.SelectedAnimation.SelectedKeyframe != null)
                    {
                        // put this after the current animation
                        newVm.Time = mCurrentViewModel.SelectedAnimation.SelectedKeyframe.Time + 1f;
                    }
                    else if (mCurrentViewModel.SelectedAnimation.Keyframes.Count != 0)
                    {
                        newVm.Time = mCurrentViewModel.SelectedAnimation.Keyframes.Last().Time + 1f;
                    }

                    mCurrentViewModel.SelectedAnimation.Keyframes.Add(newVm);

                    mCurrentViewModel.SelectedAnimation.Keyframes.BubbleSort();
                }
            }
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

            AvailableStates = new ObservableCollection<string>();

            var element = SelectedState.Self.SelectedElement;

            if (element != null)
            {
                AvailableStates.AddRange(element.States.Select(item => item.Name));

                foreach (var category in element.Categories)
                {
                    AvailableStates.AddRange(category.States.Select(item => category.Name + "/" + item.Name));
                }

            }
        }

        private void CreateViewModel()
        {
            ElementSave currentlyReferencedElement = null;
            if (mCurrentViewModel != null)
            {
                currentlyReferencedElement = mCurrentViewModel.Element;
            }

            if (currentlyReferencedElement != SelectedState.Self.SelectedElement)
            {
                mCurrentViewModel = AnimationCollectionViewModelManager.Self.CurrentAnimationCollectionViewModel;

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
                    variableName == nameof(AnimationViewModel.SelectedKeyframe)) 
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
                AnimationCollectionViewModelManager.Self.Save(mCurrentViewModel);
            }
        }




    }
}
