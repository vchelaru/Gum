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
using SkiaSharp;
using System.ComponentModel;
using SkiaSharp.Views.WPF;
using Gum.Logic;
using ToolsUtilities;
using Gum.Services;
using Gum.Services.Dialogs;

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

        private readonly ISelectedState _selectedState;
        private readonly INameVerifier _nameVerifier;
        private readonly NameValidator _nameValidator;
        private readonly IDialogService _dialogService;

        public event EventHandler AddStateKeyframeClicked;
        public event Action<AnimatedKeyframeViewModel> AnimationKeyframeAdded;

        public event Action AnimationColumnsResized;

        public MainWindow()
        {
            InitializeComponent();

            InitializeTimer();

            DataContextChanged += HandleDataContext;
            _selectedState = Locator.GetRequiredService<ISelectedState>();
            _nameVerifier = Locator.GetRequiredService<INameVerifier>();
            _nameValidator = new NameValidator(_nameVerifier);
            _dialogService = Locator.GetRequiredService<IDialogService>();
        }

        private void HandleDataContext(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(e.OldValue is ElementAnimationsViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= HandleViewModelPropertyChanged;
            }
            if(e.NewValue is ElementAnimationsViewModel newViewModel)
            {
                newViewModel.PropertyChanged += HandleViewModelPropertyChanged;

            }

            SkiaElement.InvalidateVisual();
            LeftSkiaElement.InvalidateVisual();
        }

        AnimationViewModel animationViewModel;

        private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(ViewModel.SelectedAnimation):

                    if(animationViewModel != null)
                    {
                        animationViewModel.PropertyChanged -= HandleAnimationViewModelPropertyChanged;
                    }

                    animationViewModel = ViewModel.SelectedAnimation;

                    if (animationViewModel != null)
                    {
                        animationViewModel.PropertyChanged += HandleAnimationViewModelPropertyChanged;
                    }

                    SkiaElement.InvalidateVisual();
                    LeftSkiaElement.InvalidateVisual();
                    break;
                case nameof(ViewModel.DisplayedAnimationTime):
                    SkiaElement.InvalidateVisual();
                    break;
                case nameof(ViewModel.Animations):
                    SkiaElement.InvalidateVisual();
                    LeftSkiaElement.InvalidateVisual();
                    break;
            }
        }

        private void HandleAnimationViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(AnimationViewModel.SelectedKeyframe):
                    SkiaElement.InvalidateVisual();
                    break;
                case nameof(AnimationViewModel.Length):
                    SkiaElement.InvalidateVisual();
                    break;
                case nameof(AnimationViewModel.Keyframes):
                    SkiaElement.InvalidateVisual();
                    LeftSkiaElement.InvalidateVisual();
                    break;
            }
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

            whyIsntValid = this.ViewModel.GetWhyAddingAnimationIsInvalid();

            if(!string.IsNullOrEmpty(whyIsntValid))
            {
                _dialogService.ShowMessage(whyIsntValid);
            }
            else
            {

                GetUserStringOptions options = new()
                {
                    Validator = x =>
                        _nameValidator.IsAnimationNameValid(x, ViewModel.Animations, out string whyInvalid)
                            ? null
                            : whyInvalid,
                };

                if(_dialogService.GetUserString(
                       message: "Enter new animation name:",
                       title: "New animation",
                       options: options) is {} result)
                {

                    var newAnimation = new AnimationViewModel() { Name = result };

                    this.ViewModel.Animations.Add(newAnimation);

                    this.ViewModel.SelectedAnimation = newAnimation;
                }
            }
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
                _dialogService.ShowMessage("You must first select an animation");
                return;
            }
            /////////// End Early Out/////////

            SubAnimationSelectionDialogViewModel window = new();

            window.AnimationToExclude = this.ViewModel.SelectedAnimation;

            window.AnimationContainers = CreateAnimationContainers();

            if (_dialogService.Show(window) && window.SelectedAnimation is { } selectedAnimation)
            {
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
                _dialogService.ShowMessage("You must first select an animation");
                return;
            }
            /////////// End Early Out/////////
            
            if(_dialogService.GetUserString("Enter new event name", "New event") is { } result)
            {
                AnimatedKeyframeViewModel newVm = new AnimatedKeyframeViewModel();

                newVm.EventName = result;

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
                _selectedState.SelectedElement, null
                );
            AnimationContainers.Add(acvm);

            foreach (var instance in _selectedState.SelectedElement.Instances)
            {
                var instanceElement = ObjectFinder.Self.GetElementSave(instance);
                if (instanceElement != null)
                {
                    var animationSave = AnimationCollectionViewModelManager.Self.GetElementAnimationsSave(instanceElement);
                    if (animationSave != null && animationSave.Animations.Count != 0)
                    {
                        acvm = new AnimationContainerViewModel(_selectedState.SelectedElement, instance);
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
            // todo - this should use the hotkey manager
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
                if(_dialogService.ShowYesNoMessage($"Delete animation {ViewModel.SelectedAnimation.Name}?", "Delete?"))
                {
                    this.ViewModel.Animations.Remove(this.ViewModel.SelectedAnimation);
                    this.ViewModel.SelectedAnimation = null;
                }
            }

            var isCtrlDown =
                Keyboard.IsKeyDown(Key.LeftCtrl);

            if (isCtrlDown )
            {
                if( e.Key == Key.C)
                {
                    var objectToCopy = ViewModel.SelectedAnimation;

                    if(objectToCopy != null)
                    {
                        AnimationCopyPasteManager.Copy(objectToCopy);
                    }
                }
                else if(e.Key == Key.V)
                {
                    AnimationCopyPasteManager.Paste(ViewModel);
                }
                else if(e.Key == Key.X)
                {

                }
            }
            

            //if ((e. & Keys.Control) == Keys.Control)
            //{
            //    // copy, ctrl c, ctrl + c
            //    if (e.KeyCode == Keys.C)
            //    {
            //        e.Handled = true;
            //        e.SuppressKeyPress = true;
            //    }
            //    // paste, ctrl v, ctrl + v
            //    else if (e.KeyCode == Keys.V)
            //    {
            //        e.Handled = true;
            //        e.SuppressKeyPress = true;
            //    }
            //    // cut, ctrl x, ctrl + x
            //    else if (e.KeyCode == Keys.X)
            //    {
            //        e.Handled = true;
            //        e.SuppressKeyPress = true;
            //    }
            //}
        }

                AnimatedKeyframeViewModel copiedFrame;

        private void HandleAnimationKeyframeListBoxKey(object sender, KeyEventArgs e)
        {
            if(this.ViewModel.SelectedAnimation != null && this.ViewModel.SelectedAnimation.SelectedKeyframe != null)
            {
                var isCtrlDown = e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control);
                if (e.Key == Key.Delete)
                {
                    // delete the selected keyframe
                    this.ViewModel.SelectedAnimation.Keyframes.Remove(this.ViewModel.SelectedAnimation.SelectedKeyframe);
                    this.ViewModel.SelectedAnimation.SelectedKeyframe = null;
                }
                // check if the ctrl key is held down and the C key is pressed
                else if (isCtrlDown && e.Key == Key.C)
                {
                    // copy the selected keyframe
                    copiedFrame = this.ViewModel.SelectedAnimation.SelectedKeyframe.Clone();
                }
                else if (isCtrlDown && e.Key == Key.V && copiedFrame != null)
                {
                    // paste the selected keyframe
                    var copiedKeyframe = copiedFrame.Clone();
                    copiedKeyframe.Time += .1f;
                    this.ViewModel.SelectedAnimation.Keyframes.Add(copiedKeyframe);
                    this.ViewModel.SelectedAnimation.Keyframes.BubbleSort();
                    this.ViewModel.SelectedAnimation.SelectedKeyframe = copiedKeyframe;

                    AnimationKeyframeAdded?.Invoke(copiedFrame);
                }
            }
        }

        private void HandlePlayStopClicked(object sender, RoutedEventArgs e)
        {
            if (this.ViewModel != null && _selectedState.SelectedElement != null && this.ViewModel.SelectedAnimation != null)
            {
                this.ViewModel.SelectedAnimation.RefreshCumulativeStates(_selectedState.SelectedElement);
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

        private void SpeedDecreaseClicked(object sender, RoutedEventArgs args)
        {
            ViewModel.DecreaseGameSpeed();
        }

        private void SpeedIncreaseClicked(object sender, RoutedEventArgs args)
        {
            ViewModel.IncreaseGameSpeed();
        }

        private void SKElement_PaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
        {
            TimelineRenderer.DrawTimeline(ViewModel, e.Surface, e.Info);
        }

        private void SkiaElement_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            /////////////Early Out/////////////////
            if (ViewModel.SelectedAnimation == null || e.LeftButton != MouseButtonState.Pressed) return;
            /////////////End Early Out///////////////////

            UpdateTimeToMousePosition(sender, e);
        }

        private void SkiaElement_MouseMove(object sender, MouseEventArgs e)
        {
            /////////////Early Out/////////////////
            if (ViewModel.SelectedAnimation == null || e.LeftButton != MouseButtonState.Pressed) return;
            /////////////End Early Out///////////////////
            UpdateTimeToMousePosition(sender, e);
        }

        private void UpdateTimeToMousePosition(object sender, MouseEventArgs e)
        {
            var element = sender as SKElement;
            Point mousePos = e.GetPosition(element);
            //SKPoint skMousePos = new SKPoint((float)(mousePos.X * dpiScale), (float)(mousePos.Y * dpiScale));
            var time = TimelineRenderer.XToTime((float)mousePos.X, ViewModel.SelectedAnimation.Length);
            ViewModel.DisplayedAnimationTime = time;
        }

        private void LeftSkiaElement_PaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
        {
            TimelineRenderer.DrawLeftSide(ViewModel, e.Surface, e.Info);
        }
    }
}
