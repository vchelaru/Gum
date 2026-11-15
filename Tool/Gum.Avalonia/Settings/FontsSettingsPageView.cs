using Gum.Plugins.FontsPlugin;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.SettingsWindow;

namespace Gum.Avalonia.Settings;

/// <summary>
/// The Project > Fonts page of the Settings window: the rows of <see cref="FontsRows"/> over the
/// <see cref="FontsViewModel"/> that is its DataContext. The look is the shared <see cref="SettingsRowsPageView"/>.
/// </summary>
public sealed class FontsSettingsPageView : SettingsRowsPageView
{
    /// <summary>Builds the view; the per-font ranges list is its own row kind.</summary>
    public FontsSettingsPageView()
    {
        Grid.Displayers.Register(typeof(GumDisplayers.FontRanges), typeof(FontRangesDisplay));
    }

    /// <inheritdoc/>
    protected override void HandleDataContextChanged() =>
        Rows = DataContext is FontsViewModel viewModel ? new FontsRows(viewModel) : null;
}
