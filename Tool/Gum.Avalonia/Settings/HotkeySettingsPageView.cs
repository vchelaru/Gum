using Gum.Plugins.InternalPlugins.Hotkey.ViewModels;
using Gum.SettingsWindow;

namespace Gum.Avalonia.Settings;

/// <summary>
/// The Application > Keyboard shortcuts page of the Settings window: the rows of <see cref="HotkeyRows"/> over the
/// <see cref="HotkeyViewModel"/> that is its DataContext. The look is the shared <see cref="SettingsRowsPageView"/>.
/// </summary>
public sealed class HotkeyView : SettingsRowsPageView
{
    /// <inheritdoc/>
    protected override void HandleDataContextChanged() =>
        Rows = DataContext is HotkeyViewModel viewModel ? new HotkeyRows(viewModel) : null;
}
