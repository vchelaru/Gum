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

namespace StateAnimationPlugin.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        #region Properties

        ElementAnimationsViewModel ViewModel
        {
            get
            {
                return DataContext as ElementAnimationsViewModel;
            }
        }


        #endregion


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
                    var newAnimation = new AnimationViewModel() { Name = tiw.Result };

                    this.ViewModel.Animations.Add(newAnimation);

                    this.ViewModel.SelectedAnimation = newAnimation;

                }
            }
        }

        private void AddStateButton_Click(object sender, RoutedEventArgs e)
        {
            if(ViewModel == null)
            {
                throw new NullReferenceException("The ViewModel for this is invalid - set the DataContext on this view before showing it.");
            }

            string whyIsntValid = GetWhyAddingTimedStateIsInvalid();

            if(!string.IsNullOrEmpty(whyIsntValid))
            {
                MessageBox.Show(whyIsntValid);

            }
            else
            {
                ListBoxMessageBox lbmb = new ListBoxMessageBox();
                lbmb.RequiresSelection = true;
                lbmb.Message = "Select a state";

                foreach (var state in SelectedState.Self.SelectedElement.AllStates)
                {
                    lbmb.Items.Add(state.Name);
                }

                var dialogResult = lbmb.ShowDialog();

                if (dialogResult.HasValue && dialogResult.Value)
                {
                    var item = lbmb.SelectedItem;

                    var newVm = new AnimatedKeyframeViewModel() { StateName = (string)item, 
                        // User just selected the state, so it better be valid!
                        HasValidState = true,
                        InterpolationType = FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear,
                        Easing = FlatRedBall.Glue.StateInterpolation.Easing.Out
                    
                    };

                    if(ViewModel.SelectedAnimation.SelectedKeyframe != null)
                    {
                        // put this after the current animation
                        newVm.Time = ViewModel.SelectedAnimation.SelectedKeyframe.Time + 1f;
                    }
                    else if(ViewModel.SelectedAnimation.Keyframes.Count != 0)
                    {
                        newVm.Time = ViewModel.SelectedAnimation.Keyframes.Last().Time + 1f;
                    }

                    ViewModel.SelectedAnimation.Keyframes.Add(newVm);

                    ViewModel.SelectedAnimation.Keyframes.BubbleSort();
                }
            }
        }

        private void AddSubAnimationButton_Click(object sender, RoutedEventArgs e)
        {
            SubAnimationSelectionWindow window = new SubAnimationSelectionWindow();
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
            }
        }

        private void LoopToggleClick(object sender, RoutedEventArgs e)
        {
            var animation = ((Button)sender).DataContext as AnimationViewModel;
            if (animation != null)
            {
                animation.ToggleLoop();
            }
        }

        private string GetWhyAddingTimedStateIsInvalid()
        {
            string whyIsntValid = null;

            if (ViewModel.SelectedAnimation == null)
            {
                whyIsntValid = "You must first select an Animation";
            }

            if (SelectedState.Self.SelectedScreen == null && SelectedState.Self.SelectedComponent == null)
            {
                whyIsntValid = "You must first select a Screen or Component";
            }
            return whyIsntValid;
        }

        private void HandleDeleteAnimationPressed(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && this.ViewModel.SelectedAnimation != null)
            {
                this.ViewModel.Animations.Remove(this.ViewModel.SelectedAnimation);
                this.ViewModel.SelectedAnimation = null;
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
            ViewModel.Stop();
        }


    }
}
