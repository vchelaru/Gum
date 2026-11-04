using Gum.Dialogs;

namespace Gum.Avalonia.Settings;

/// <summary>
/// The Application > Appearance page of the Settings window: the rows of <see cref="AppearanceRows"/> over
/// the <see cref="ThemingDialogViewModel"/> that is its DataContext. The look is the shared
/// <see cref="SettingsRowsPageView"/>.
/// </summary>
public sealed class AppearanceSettingsPageView : SettingsRowsPageView
{
    /// <inheritdoc/>
    protected override void HandleDataContextChanged() =>
        Rows = DataContext is ThemingDialogViewModel viewModel ? new AppearanceRows(viewModel) : null;
}
