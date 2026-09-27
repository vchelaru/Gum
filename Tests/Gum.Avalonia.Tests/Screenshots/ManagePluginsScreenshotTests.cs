using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Gum.Avalonia.Tests.Harness;
using Gum.Plugins;
using Gum.Services;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// Manage Plugins with the head's real plugins, for a PR's before/after table
/// (<c>Tools/pr-screenshots.ps1 -Filter ManagePluginsScreenshotTests</c>). Uses only API that
/// main already has, so the same file renders the "before" side.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class ManagePluginsScreenshotTests
{
    [SkippableFact]
    public void ManagePlugins() => PrScreenshot.Run(() =>
    {
        foreach ((ThemeVariant theme, string suffix) in new[] { (ThemeVariant.Light, "light"), (ThemeVariant.Dark, "dark") })
        {
            using ScreenshotWindow window = PrScreenshot.ShowDialog(CreateViewModel(), theme);
            window.Save($"manage-plugins-{suffix}");
        }
    });

    [SkippableFact]
    public void ManagePlugins_HoveringACorePlugin() => PrScreenshot.Run(() =>
    {
        using ScreenshotWindow window = PrScreenshot.ShowDialog(CreateViewModel());
        List<CheckBox> rows = window.FindAll<CheckBox>();
        CheckBox output = rows.FirstOrDefault(row => RowText(row) == "MainOutputPlugin")
            ?? throw new InvalidOperationException($"No MainOutputPlugin row; rows are [{string.Join(", ", rows.Select(RowText))}].");
        window.HoverForToolTip(output);
        window.Save("manage-plugins-core-plugin-hover");
    });

    private static PluginsDialogViewModel CreateViewModel() => new PluginsDialogViewModel(
        Mock.Of<IDialogService>(),
        TestAppBuilder.Services.GetRequiredService<IPluginManager>(),
        Mock.Of<IClipboardService>());

    private static string RowText(CheckBox row) =>
        string.Concat(row.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text));
}
