// Nullable-oblivious to match the MonoGame/Raylib localization holders this mirrors (their static
// service fields are un-annotated); the shared GumService consumes these members through its own
// nullable-enable context with the appropriate '!' / 'string?' handling.
#nullable disable

using Gum.Localization;
using Gum.Wireframe;
using System;
using System.Runtime.CompilerServices;

namespace SkiaGum;

// SilkNet-only partial extension of SkiaGum's CustomSetPropertyOnRenderable. The shared game-host
// GumService (issue #3608) wires runtime localization through this type's LocalizationService /
// LocalizationServiceChanged / TryGetLocalizationKey members, which SkiaGum's render-dispatch copy
// (unlike its MonoGame/Raylib siblings) never grew. Supply that holder here so the shared GumService
// compiles under SILK. It is a state holder only: SkiaGum's SetPropertyOnRenderable does not consult
// it, so no auto-translation happens (matching the prior standalone SilkNet service) -- closing that
// Skia localization parity gap is a separate effort. Kept SilkNet-local (not in SkiaGum's shared
// file) so SkiaGum / SkiaGum.Wpf / SkiaGum.Maui / SkiaGum.Standalone are unaffected.
public partial class CustomSetPropertyOnRenderable
{
    private static ILocalizationService _localizationService;

    /// <summary>
    /// The active localization service used by the runtime. Assigning a new instance fires
    /// <see cref="LocalizationServiceChanged"/> so consumers (e.g. <c>GumService</c>) can re-wire
    /// <see cref="ILocalizationService.CurrentLanguageChanged"/> subscriptions for language switching.
    /// </summary>
    public static ILocalizationService LocalizationService
    {
        get => _localizationService;
        set
        {
            if (ReferenceEquals(_localizationService, value))
            {
                return;
            }
            ILocalizationService previous = _localizationService;
            _localizationService = value;
            LocalizationServiceChanged?.Invoke(previous, value);
        }
    }

    /// <summary>
    /// Raised when <see cref="LocalizationService"/> is replaced. Arguments are
    /// (previousService, newService) — either may be null.
    /// </summary>
    public static event Action<ILocalizationService, ILocalizationService> LocalizationServiceChanged;

    private static readonly ConditionalWeakTable<GraphicalUiElement, string> _localizationKeys = new();

    /// <summary>
    /// Returns the original (pre-translation) string assigned via the localization path on the given
    /// element, or null if no localizable text has been assigned. Always null on SILK today (Skia's
    /// SetPropertyOnRenderable never populates the table), so localization refresh is a no-op.
    /// </summary>
    public static string TryGetLocalizationKey(GraphicalUiElement element)
    {
        return _localizationKeys.TryGetValue(element, out string key) ? key : null;
    }
}
