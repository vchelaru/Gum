using System;

namespace Gum.Localization;

/// <summary>
/// Keeps one <see cref="ILocalizationService.CurrentLanguageChanged"/> subscription pointed at the
/// active service as it is replaced. Each runtime's GumService feeds it from its dispatcher's
/// <c>CustomSetPropertyOnRenderable.LocalizationServiceChanged</c> event so a language switch
/// re-translates live text.
/// </summary>
internal sealed class LanguageChangeSubscription
{
    private readonly Action _onLanguageChanged;
    private ILocalizationService? _subscribedService;

    public LanguageChangeSubscription(Action onLanguageChanged)
    {
        _onLanguageChanged = onLanguageChanged;
    }

    /// <summary>
    /// Moves the subscription from the previously tracked service to <paramref name="current"/>.
    /// Matches the (previous, current) shape of <c>LocalizationServiceChanged</c>.
    /// </summary>
    public void Track(ILocalizationService? previous, ILocalizationService? current)
    {
        if (_subscribedService != null)
        {
            _subscribedService.CurrentLanguageChanged -= _onLanguageChanged;
        }
        _subscribedService = current;
        if (current != null)
        {
            current.CurrentLanguageChanged += _onLanguageChanged;
        }
    }
}
