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
using StateAnimationPlugin.Managers;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace StateAnimationPlugin.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        #region Properties

        ElementAnimationsViewModel ViewModel => (ElementAnimationsViewModel)DataContext;

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

        public event EventHandler? AddStateKeyframeClicked;
        public event Action<AnimatedKeyframeViewModel>? AnimationKeyframeAdded;

        public event Action? AnimationColumnsResized;

        public MainWindow()
        {
            InitializeComponent();
            InitializeTimer();
        }

        private void InitializeTimer()
        {
        }

        private void AddAnimationButton_Click(object? sender, RoutedEventArgs e)
        {
            ViewModel.AddAnimation();
        }



        private void AddStateKeyframeButton_Click(object? sender, RoutedEventArgs e)
        {
            AddStateKeyframeClicked?.Invoke(this, EventArgs.Empty);
        }

        private void AddSubAnimationButton_Click(object? sender, RoutedEventArgs e)
        {
            ViewModel.AddSubAnimation();
        }

        private void AddNamedEventButton_Click(object? sender, RoutedEventArgs e)
        {
            ViewModel.AddNamedEvent();
        }

        private void LoopToggleClick(object? sender, RoutedEventArgs e)
        {
            var animation = ((Button)sender!).DataContext as AnimationViewModel;
            if (animation != null)
            {
                animation.ToggleLoop();
            }
        }

        private void HandleAnimationListKeyPressed(object? sender, KeyEventArgs e)
        {
            // todo - this should use the hotkey manager
            var alt = e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Alt);

            if (this.ViewModel.SelectedAnimation != null && alt)
            {
                if (e.SystemKey == Key.Up)
                {
                    if (ViewModel.MoveSelectedAnimationUp())
                    {
                        e.Handled = true;
                    }
                }
                else if (e.SystemKey == Key.Down)
                {
                    if (ViewModel.MoveSelectedAnimationDown())
                    {
                        e.Handled = true;
                    }
                }
            }

            if (e.Key == Key.Delete && this.ViewModel.SelectedAnimation != null)
            {
                e.Handled = true;
                ViewModel.DeleteSelectedAnimation();
            }

            var isCtrlDown = Keyboard.IsKeyDown(Key.LeftCtrl);

            if (isCtrlDown)
            {
                if (e.Key == Key.C)
                {
                    var objectToCopy = ViewModel.SelectedAnimation;
                    if (objectToCopy != null)
                    {
                        AnimationCopyPasteManager.Copy(objectToCopy);
                    }
                }
                else if (e.Key == Key.V)
                {
                    AnimationCopyPasteManager.Paste(ViewModel);
                }
            }
        }


        private void HandleAnimationKeyframeListBoxKey(object? sender, KeyEventArgs e)
        {
            if (this.ViewModel.SelectedAnimation != null && this.ViewModel.SelectedAnimation.SelectedKeyframe != null)
            {
                var isCtrlDown = e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control);
                if (e.Key == Key.Delete)
                {
                    ViewModel.DeleteSelectedKeyframe();
                }
                else if (isCtrlDown && e.Key == Key.C)
                {
                    ViewModel.CopySelectedKeyframe();
                }
                else if (isCtrlDown && e.Key == Key.V)
                {
                    var source = ViewModel.PasteKeyframe();
                    if (source != null)
                    {
                        AnimationKeyframeAdded?.Invoke(source);
                    }
                }
            }
        }

        private void GridSplitter_DragCompleted(object? sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            AnimationColumnsResized?.Invoke();
        }

        private void SpeedDecreaseClicked(object? sender, RoutedEventArgs args)
        {
            ViewModel.DecreaseGameSpeed();
        }

        private void SpeedIncreaseClicked(object? sender, RoutedEventArgs args)
        {
            ViewModel.IncreaseGameSpeed();
        }

        private void OnKeyframeMouseEnter(object? sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: AnimatedKeyframeViewModel frame })
            {
                frame.IsTimelineVisualHovered = true;
            }
        }

        private void OnKeyframeMouseLeave(object? sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: AnimatedKeyframeViewModel frame })
            {
                frame.IsTimelineVisualHovered = false;
            }
        }

        private void MenuItem_OnClick(object? sender, RoutedEventArgs e)
        {
            ViewModel.ToggleInterpolationClampingCommand.Execute(null);
        }
    }
}
