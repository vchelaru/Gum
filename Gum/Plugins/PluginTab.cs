using System;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Mvvm;
using Gum.Plugins;

namespace Gum.Plugins
{
    public partial class PluginTab : ViewModel, IRecipient<TabSelectedMessage>
    {
        private readonly IMessenger _messenger;
        public event Action? TabShown;
        public event Action? TabHidden;
        public event Action? GotFocus;
        
        public string Title
        {
            get => Get<string>();
            set => Set(value);
        }

        public TabLocation Location
        {
            get => Get<TabLocation>();
            set => Set(value);
        }
        
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
                    }
                }
            }
        }
        
        public bool IsSelected
        {
            get => Get<bool>();
            set
            {
                if (Set(value) && value)
                {
                    _messenger.Send<TabSelectedMessage>(new(this));
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

        public PluginTab(FrameworkElement content, IMessenger messenger)
        {
            _messenger = messenger;
            _messenger.RegisterAll(this);
            
            CanClose = true;
            Location = TabLocation.RightBottom;
            Content = content;
            IsVisible = true;
        }

        [RelayCommand(CanExecute = nameof(CanClose))]
        public void Hide()
        {
            if (CanClose)
            {
                IsVisible = false;
            }
        }

        public void Show() => IsVisible = true;

        public void Receive(TabSelectedMessage message)
        {
            if (message.Tab != this && message.Tab.Location == Location)
            {
                IsSelected = false;
            }
        }
    }
}

public record TabSelectedMessage(PluginTab Tab);
