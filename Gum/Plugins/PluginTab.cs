using System;
using System.ComponentModel;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using Gum.Commands;
using Gum.Mvvm;
using Gum.Services;
using Gum.ViewModels;

namespace Gum.Plugins
{
    public partial class PluginTab : ViewModel
    {
        public event Action? TabShown;
        public event Action? TabHidden;
        public event Action? GotFocus;
        
        public string Title
        {
            get => Get<string>();
            set => Set(value);
        }
        
        public TabLocation SuggestedLocation { get; set; } = TabLocation.RightBottom;
        
        public FrameworkElement Content
        {
            get => Get<FrameworkElement>();
            private set => Set(value);
        }

        public FrameworkElement? CustomHeaderContent
        {
            get => Get<FrameworkElement?>();
            set => Set(value);
        }

        public PluginTabContainerViewModel? ParentContainer
        {
            get => Get<PluginTabContainerViewModel>();
            set
            {
                PluginTabContainerViewModel? previousParent = ParentContainer;

                if (Set(value))
                {
                    if (previousParent is not null)
                    {
                        previousParent.PropertyChanged -= OnParentContainerChanged;
                        previousParent.Tabs.Remove(this);
                    }

                    if (value is {} newParent)
                    {
                        newParent.PropertyChanged += OnParentContainerChanged;
                        if (!newParent.Tabs.Contains(this))
                        {
                            newParent.Tabs.Add(this);
                        }
                    }
                }
            }
        }

        private void OnParentContainerChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PluginTabContainerViewModel.SelectedTab))
            {
                NotifyPropertyChanged(nameof(IsSelected));
            }
        }
        
        public bool IsVisible
        {
            get => Get<bool>();
            set
            {
                if (Set(value))
                {
                    if(value)
                    {
                        TabShown?.Invoke();
                    }
                    else
                    {
                        TabHidden?.Invoke();
                        IsSelected = false;
                    }
                }
            }
        }
        
        public bool IsSelected
        {
            get => ParentContainer?.SelectedTab == this;
            set
            {
                if (value && ParentContainer is { } parent)
                {
                    parent.SelectedTab = this;
                }
            }
        }

        public bool CanClose
        {
            get => Get<bool>();
            set
            {
                if (Set(value))
                {
                    HideCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public PluginTab(FrameworkElement content)
        {
            Content = content;
            IsVisible = true;
        }
        
        [RelayCommand(CanExecute = nameof(CanClose))]
        public void Hide() => IsVisible = false;

        public void Show(bool select = true)
        {
            IsVisible = true;
            IsSelected = select;
        }
    }
}
