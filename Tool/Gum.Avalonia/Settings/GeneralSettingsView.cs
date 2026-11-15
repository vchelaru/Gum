using Gum.SettingsWindow;

namespace Gum.Avalonia.Settings;

/// <summary>
/// The Application > General page of the Settings window: the rows of <see cref="GeneralRows"/> over the
/// <see cref="GeneralSettingsViewModel"/> that is its DataContext. The look is the shared
/// <see cref="SettingsRowsPageView"/>.
/// </summary>
public sealed class GeneralSettingsView : SettingsRowsPageView
{
    /// <inheritdoc/>
    protected override void HandleDataContextChanged() =>
        Rows = DataContext is GeneralSettingsViewModel viewModel ? new GeneralRows(viewModel) : null;
}
