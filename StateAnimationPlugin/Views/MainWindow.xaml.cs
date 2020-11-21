using CommonFormsAndControls;
using Gum.ToolStates;
using StateAnimationPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using StateAnimationPlugin.Validation;
using Gum.Managers;
using StateAnimationPlugin.Managers;
using Gum;

namespace StateAnimationPlugin.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        #region Properties

        ElementAnimationsViewModel ViewModel => DataContext as ElementAnimationsViewModel;

        public GridLength FirstRowWidth
        {
            get => BottomGrid.ColumnDefinitions[0].Width;
            set => BottomGrid.ColumnDefinitions[0].Width = value;
        }

        public GridLength SecondRowWidth
        {
            get => BottomGrid.ColumnDefinitions[2].Width;
            set => BottomGrid.ColumnDefinitions[2].Width = value;
        }

        #endregion

        public event EventHandler AddStateKeyframeClicked;

        public event Action AnimationColumnsResized;

        public MainWindow()
        {
            InitializeComponent();

            InitializeTimer();
        }

        private void InitializeTimer()
        {
        }

        private void AddAnimationButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
            {
                throw new NullReferenceException("The ViewModel for this is invalid - set the DataContext on this view before showing it.");
            }

            string whyIsntValid = null;

            whyIsntValid = GetWhyAddingAnimationIsInvalid();

            if(!string.IsNullOrEmpty(whyIsntValid))
            {
                MessageBox.Show(whyIsntValid);
            }
            else
            {

                TextInputWindow tiw = new TextInputWindow();
                tiw.Message = "Enter new animation name:";

                var dialogResult = tiw.ShowDialog();

                if(dialogResult == System.Windows.Forms.DialogResult.OK)
                {
                    string whyInvalid;
                    if (!NameValidator.IsAnimationNameValid(tiw.Result, this.ViewModel.Animations, out whyInvalid))
                    {
                        MessageBox.Show(whyInvalid);
                    }
                    else
                    {
                        var newAnimation = new AnimationViewModel() { Name = tiw.Result };

                        this.ViewModel.Animations.Add(newAnimation);

                        this.ViewModel.SelectedAnimation = newAnimation;
                    }
                }
            }
        }

        private string GetWhyAddingAnimationIsInvalid()
        {
            string whyIsntValid = null;
            if (SelectedState.Self.SelectedScreen == null && SelectedState.Self.SelectedComponent == null)
            {
                whyIsntValid = "You must first select a Screen or Component";
            }

            return whyIsntValid;
        }

        private void AddStateKeyframeButton_Click(object sender, RoutedEventArgs e)
        {
            AddStateKeyframeClicked?.Invoke(this, null);
        }

        private void AddSubAnimationButton_Click(object sender, RoutedEventArgs e)
        {
            ////////////// Early Out//////////
            if(ViewModel.SelectedAnimation == null)
            {
                MessageBox.Show("You must first select an animation");
                return;
            }
            /////////// End Early Out/////////

            SubAnimationSelectionWindow window = new SubAnimationSelectionWindow();

            window.AnimationToExclude = this.ViewModel.SelectedAnimation;

            window.AnimationContainers = CreateAnimationContainers();

            var result = window.ShowDialog();

            if (result.HasValue && result.Value && window.SelectedAnimation != null)
            {
                var selectedAnimation = window.SelectedAnimation;

                AnimatedKeyframeViewModel newVm = new AnimatedKeyframeViewModel();
                if (selectedAnimation.ContainingInstance != null)
                {
                    newVm.AnimationName = selectedAnimation.ContainingInstance.Name + "." + selectedAnimation.Name;
                }
                else
                {
                    newVm.AnimationName = selectedAnimation.Name;
                }

                newVm.SubAnimationViewModel = selectedAnimation;

                newVm.HasValidState = true;

                if (ViewModel.SelectedAnimation.SelectedKeyframe != null)
                {
                    // put this after the current animation
                    newVm.Time = ViewModel.SelectedAnimation.SelectedKeyframe.Time + 1f;
                }
                else if (ViewModel.SelectedAnimation.Keyframes.Count != 0)
                {
                    newVm.Time = ViewModel.SelectedAnimation.Keyframes.Last().Time + 1f;
                }


                ViewModel.SelectedAnimation.Keyframes.Add(newVm);

                ViewModel.SelectedAnimation.Keyframes.BubbleSort();

                ViewModel.SelectedAnimation.SelectedKeyframe = newVm;
            }
        }

        private void AddNamedEventButton_Click(object sender, RoutedEventArgs e)
        {
            ////////////// Early Out//////////
            if (ViewModel.SelectedAnimation == null)
            {
                MessageBox.Show("You must first select an animation");
                return;
            }
            /////////// End Early Out/////////
            
            var textInputWindow = new TextInputWindow();
            textInputWindow.Message = "Enter new event name";
            var result = textInputWindow.ShowDialog();

            if(result == System.Windows.Forms.DialogResult.OK)
            {
                AnimatedKeyframeViewModel newVm = new AnimatedKeyframeViewModel();

                newVm.EventName = textInputWindow.Result;

                if (ViewModel.SelectedAnimation.SelectedKeyframe != null)
                {
                    // put this after the current animation
                    newVm.Time = ViewModel.SelectedAnimation.SelectedKeyframe.Time + 1f;
                }
                else if (ViewModel.SelectedAnimation.Keyframes.Count != 0)
                {
                    newVm.Time = ViewModel.SelectedAnimation.Keyframes.Last().Time + 1f;
                }


                ViewModel.SelectedAnimation.Keyframes.Add(newVm);

                ViewModel.SelectedAnimation.Keyframes.BubbleSort();

                ViewModel.SelectedAnimation.SelectedKeyframe = newVm;
            }
        }

        private List<AnimationContainerViewModel> CreateAnimationContainers()
        {

            var AnimationContainers = new List<AnimationContainerViewModel>();

            var acvm = new AnimationContainerViewModel(
                SelectedState.Self.SelectedElement, null
                );
            AnimationContainers.Add(acvm);

            foreach (var instance in SelectedState.Self.SelectedElement.Instances)
            {
                var instanceElement = ObjectFinder.Self.GetElementSave(instance);
                if (instanceElement != null)
                {
                    var animationSave = AnimationCollectionViewModelManager.Self.GetElementAnimationsSave(instanceElement);
                    if (animationSave != null && animationSave.Animations.Count != 0)
                    {
                        acvm = new AnimationContainerViewModel(SelectedState.Self.SelectedElement, instance);
                        AnimationContainers.Add(acvm);
                    }
                }
            }

            return AnimationContainers;
        }

        private void LoopToggleClick(object sender, RoutedEventArgs e)
        {
            var animation = ((Button)sender).DataContext as AnimationViewModel;
            if (animation != null)
            {
                animation.ToggleLoop();
            }
        }

        private void HandleAnimationListKeyPressed(object sender, KeyEventArgs e)
        {
            var alt = e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Alt);

            if (this.ViewModel.SelectedAnimation != null && alt)
            {
                if(e.SystemKey == Key.Up)
                {
                    var index = this.ViewModel.Animations.IndexOf(ViewModel.SelectedAnimation);

                    if(index > 0)
                    {
                        this.ViewModel.Animations.Move(index, index - 1);
                        e.Handled = true;
                    }
                }
                else if(e.SystemKey == Key.Down)
                {
                    var index = this.ViewModel.Animations.IndexOf(ViewModel.SelectedAnimation);

                    if(index < this.ViewModel.Animations.Count-1)
                    {
                        this.ViewModel.Animations.Move(index, index + 1);
                        e.Handled = true;
                    }
                }


            }

            if (e.Key == Key.Delete && this.ViewModel.SelectedAnimation != null)
            {
                e.Handled = true;
                var result = MessageBox.Show(
                    $"Delete animation {ViewModel.SelectedAnimation.Name}?", 
                    "Delete?", 
                    MessageBoxButton.YesNo);
                if(result == MessageBoxResult.Yes)
                {
                    this.ViewModel.Animations.Remove(this.ViewModel.SelectedAnimation);
                    this.ViewModel.SelectedAnimation = null;
                }
            }
        }

        private void HandleDeleteAnimatedStatePressed(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && this.ViewModel.SelectedAnimation != null && this.ViewModel.SelectedAnimation.SelectedKeyframe != null)
            {
                this.ViewModel.SelectedAnimation.Keyframes.Remove(this.ViewModel.SelectedAnimation.SelectedKeyframe);
                this.ViewModel.SelectedAnimation.SelectedKeyframe = null;
            }
        }

        private void HandlePlayStopClicked(object sender, RoutedEventArgs e)
        {
            if (this.ViewModel != null && SelectedState.Self.SelectedElement != null && this.ViewModel.SelectedAnimation != null)
            {
                this.ViewModel.SelectedAnimation.RefreshCombinedStates(SelectedState.Self.SelectedElement);
            }

            ViewModel.TogglePlayStop();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ViewModel?.Stop();
        }

        private void GridSplitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            AnimationColumnsResized?.Invoke();
        }
    }
}
