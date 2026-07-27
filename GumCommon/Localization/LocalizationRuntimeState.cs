namespace Gum.Localization;

/// <summary>
/// Holds the process-wide active <see cref="ILocalizationService"/> instance. Lives in
/// GumCommon (below every platform runtime project) so GumCommon-layer consumers - such as
/// Gum.Expressions' variable-reference evaluator - can read the current language without
/// depending on any platform runtime assembly. <c>CustomSetPropertyOnRenderable.LocalizationService</c>,
/// which is compiled per-platform-runtime, forwards to this static so its existing public API
/// (assignment, the <c>LocalizationServiceChanged</c> event) is unchanged.
/// </summary>
public static class LocalizationRuntimeState
{
    public static ILocalizationService? Current { get; set; }
}
