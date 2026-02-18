using Gum;
using Gum.Managers;
using StateAnimationPlugin.Managers;
using StateAnimationPlugin.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Services;

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

        private readonly IHotkeyManager _hotkeyManager;

        public event EventHandler? AddStateKeyframeClicked;
        public event Action<AnimatedKeyframeViewModel>? AnimationKeyframeAdded;

        public event Action? AnimationColumnsResized;

        public MainWindow()
        {
            InitializeComponent();
            InitializeTimer();

            _hotkeyManager = Locator.GetRequiredService<IHotkeyManager>();
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
            if (ViewModel.SelectedAnimation != null && _hotkeyManager.ReorderUp.IsPressed(e))
            {
                if (ViewModel.MoveSelectedAnimationUp())
                {
                    e.Handled = true;
                }
            }
            else if (ViewModel.SelectedAnimation != null && _hotkeyManager.ReorderDown.IsPressed(e))
            {
                if (ViewModel.MoveSelectedAnimationDown())
                {
                    e.Handled = true;
                }
            }
            else if (ViewModel.SelectedAnimation != null && _hotkeyManager.Delete.IsPressed(e))
            {
                e.Handled = true;
                ViewModel.DeleteSelectedAnimation();
            }
            else if (_hotkeyManager.Copy.IsPressed(e))
            {
                var objectToCopy = ViewModel.SelectedAnimation;
                if (objectToCopy != null)
                {
                    AnimationCopyPasteManager.Copy(objectToCopy);
                }
            }
            else if (_hotkeyManager.Paste.IsPressed(e))
            {
                AnimationCopyPasteManager.Paste(ViewModel);
            }
        }


        private void HandleAnimationKeyframeListBoxKey(object? sender, KeyEventArgs e)
        {
            if (this.ViewModel.SelectedAnimation != null && this.ViewModel.SelectedAnimation.SelectedKeyframe != null)
            {
                if (_hotkeyManager.Delete.IsPressed(e))
                {
                    ViewModel.DeleteSelectedKeyframe();
                }
                else if (_hotkeyManager.Copy.IsPressed(e))
                {
                    ViewModel.CopySelectedKeyframe();
                }
                else if (_hotkeyManager.Paste.IsPressed(e))
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
