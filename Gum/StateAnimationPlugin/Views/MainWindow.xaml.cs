using Gum;
using Gum.Managers;
using Gum.ViewModels;
using StateAnimationPlugin.Managers;
using StateAnimationPlugin.ViewModels;
using System;
using System.Collections.Specialized;
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
        private ElementAnimationsViewModel? _subscribedViewModel;

        public event EventHandler? AddStateKeyframeClicked;
        public event Action<AnimatedKeyframeViewModel>? AnimationKeyframeAdded;

        public event Action? AnimationColumnsResized;

        public MainWindow()
        {
            InitializeComponent();
            InitializeTimer();

            _hotkeyManager = Locator.GetRequiredService<IHotkeyManager>();

            DataContextChanged += HandleDataContextChanged;
        }

        private void InitializeTimer()
        {
        }

        /// <summary>
        /// ElementAnimationsViewModel exposes its right-click menus as framework-neutral
        /// <see cref="ContextMenuItemViewModel"/> collections (ADR-0005, issue #3754) rather than WPF
        /// MenuItems, so the View is responsible for turning them into real MenuItems here -- mirroring
        /// EditingManager.RightClick.cs's ToMenuItem. A new ElementAnimationsViewModel is assigned to
        /// DataContext whenever the selected element changes, so the menus are rebuilt on every switch
        /// and whenever the current view model refreshes its menu contents.
        /// </summary>
        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_subscribedViewModel != null)
            {
                _subscribedViewModel.AnimationRightClickItems.CollectionChanged -= HandleAnimationRightClickItemsChanged;
                _subscribedViewModel.AnimationStateRightClickItems.CollectionChanged -= HandleAnimationStateRightClickItemsChanged;
            }

            _subscribedViewModel = e.NewValue as ElementAnimationsViewModel;

            if (_subscribedViewModel != null)
            {
                _subscribedViewModel.AnimationRightClickItems.CollectionChanged += HandleAnimationRightClickItemsChanged;
                _subscribedViewModel.AnimationStateRightClickItems.CollectionChanged += HandleAnimationStateRightClickItemsChanged;
            }

            RebuildAnimationContextMenu();
            RebuildAnimationStateContextMenu();
        }

        private void HandleAnimationRightClickItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
            RebuildAnimationContextMenu();

        private void HandleAnimationStateRightClickItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
            RebuildAnimationStateContextMenu();

        private void RebuildAnimationContextMenu()
        {
            AnimationContextMenu.Items.Clear();
            if (_subscribedViewModel != null)
            {
                foreach (var item in _subscribedViewModel.AnimationRightClickItems)
                {
                    AnimationContextMenu.Items.Add(ToMenuItem(item));
                }
            }
        }

        private void RebuildAnimationStateContextMenu()
        {
            AnimationStateContextMenu.Items.Clear();
            if (_subscribedViewModel != null)
            {
                foreach (var item in _subscribedViewModel.AnimationStateRightClickItems)
                {
                    AnimationStateContextMenu.Items.Add(ToMenuItem(item));
                }
            }
        }

        private Control ToMenuItem(ContextMenuItemViewModel item)
        {
            if (item.IsSeparator)
            {
                return new Separator();
            }

            var menuItem = new MenuItem { Header = item.Text };

            if (item.Action != null)
            {
                menuItem.Click += (_, _) => item.Action();
            }

            foreach (var child in item.Children)
            {
                menuItem.Items.Add(ToMenuItem(child));
            }

            return menuItem;
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
