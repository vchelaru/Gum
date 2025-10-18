using System;
using System.Windows.Navigation;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Mvvm;
using Gum.Themes;

namespace Gum.Services;

public class UiSettingsService : ViewModel, IUiSettingsService
{
    private IMessenger Messenger { get; }

    Lazy<AppScale> _scale = new(() => (AppScale)System.Windows.Application.Current.Resources["Scale"]);

    public double BaseFontSize
    {
        get => _scale.Value.BaseFontSize;
        set
        {
            if (value is < 6 or > 24)
            {
                return;
            }
            AppScale scale = (AppScale)System.Windows.Application.Current.Resources["Scale"];
            scale.BaseFontSize = value;
            Messenger.Send(new UiBaseFontSizeChangedMessage(value));
        }
    }
    
    public UiSettingsService(IMessenger messenger)
    {
        Messenger = messenger;
    }
}

public record UiBaseFontSizeChangedMessage(double Size);