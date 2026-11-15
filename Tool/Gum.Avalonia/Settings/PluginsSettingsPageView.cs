using Gum.Services.Dialogs;
using Gum.SettingsWindow;

namespace Gum.Avalonia.Settings;

/// <summary>
/// The Application > Plugins page of the Settings window: the rows of <see cref="PluginsRows"/> over the
/// <see cref="PluginsDialogViewModel"/> that is its DataContext. The look is the shared
/// <see cref="SettingsRowsPageView"/>.
/// </summary>
public sealed class PluginsSettingsPageView : SettingsRowsPageView
{
    /// <inheritdoc/>
    protected override void HandleDataContextChanged() =>
        Rows = DataContext is PluginsDialogViewModel viewModel ? new PluginsRows(viewModel) : null;
}
