using Gum.Mvvm;
using Gum.Plugins.BaseClasses;

namespace Gum.Managers;

public class ErrorViewModel : ViewModel
{
    public PluginBase? OwnerPlugin { get; set; }

    public string Message
    {
        get; set;
    } = string.Empty;

    /// <summary>
    /// Stable error code (e.g. <c>"GUM0001"</c>) for searchability and doc linking.
    /// Null if the error has not been assigned a code.
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Public help URL for this error, resolved from <see cref="Code"/> via the
    /// docs registry. Null if no docs are registered for this code, or if the
    /// error has no code.
    /// </summary>
    public string? HelpUrl { get; set; }

    /// <summary>
    /// Visibility helper for UI binding — true when <see cref="HelpUrl"/> is set.
    /// </summary>
    public bool HasHelpUrl => !string.IsNullOrEmpty(HelpUrl);

    /// <summary>
    /// Visibility helper for UI binding — true when <see cref="Code"/> is set.
    /// </summary>
    public bool HasCode => !string.IsNullOrEmpty(Code);

    /// <summary>
    /// Visibility helper — true when a code is set but no help URL is available,
    /// so the code should render as plain text rather than a hyperlink.
    /// </summary>
    public bool HasCodeWithoutHelpUrl => HasCode && !HasHelpUrl;
}
