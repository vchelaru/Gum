using CommunityToolkit.Mvvm.Messaging;
using Gum.Mvvm;

namespace Gum.Services;

public class UiSettingsService : ViewModel, IUiSettingsService
{
    private IMessenger Messenger { get; }
    private IAppScaleProvider AppScaleProvider { get; }

    public double BaseFontSize
    {
        get => AppScaleProvider.BaseFontSize;
        set
        {
            if (value is < 6 or > 24)
            {
                return;
            }
            AppScaleProvider.BaseFontSize = value;
            Messenger.Send(new UiBaseFontSizeChangedMessage(value));
        }
    }

    public UiSettingsService(IMessenger messenger, IAppScaleProvider appScaleProvider)
    {
        Messenger = messenger;
        AppScaleProvider = appScaleProvider;
    }
}

public record UiBaseFontSizeChangedMessage(double Size);
