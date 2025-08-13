using System;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Mvvm;

namespace Gum.Services;

public class UiSettingsService : ViewModel, IUiSettingsService
{
    private IMessenger Messenger { get; }
    
    public UiSettingsService(IMessenger messenger)
    {
        Messenger = messenger;
        Scale = 1;
    }

    private const double _minScale = 0.7;
    private const double _maxScale = 5;
    
    public double Scale
    {
        get => Get<double>();
        set
        {
            value = Math.Max(_minScale, value);
            value = Math.Min(_maxScale, value);
            if (Set(value))
            {
                Messenger.Send<UiScalingChangedMessage>(new(value));
            }
        }
    }
}

public record UiScalingChangedMessage(double Scale);