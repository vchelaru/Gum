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

    // A plugin built for the WPF tool dropped into the Plugins folder: found, but refused.
    [SkippableFact]
    public void ManagePlugins_WithARefusedPlugin() => PrScreenshot.Run(() =>
    {
        IPluginManager real = TestAppBuilder.Services.GetRequiredService<IPluginManager>();
        Mock<IPluginManager> pluginManager = new Mock<IPluginManager>();
        pluginManager.Setup(x => x.GetAllPluginSummaries()).Returns(real.GetAllPluginSummaries());
        pluginManager.Setup(x => x.GetPluginScanReport()).Returns(new PluginScanReport(@"C:\Gum\Plugins", FolderExists: true, new[]
        {
            new PluginFileScan("CodeOutputPlugin.dll", PluginFileOutcome.Loaded, CouldContainPlugins: true, null),
            new PluginFileScan("GumFormsPlugin.dll", PluginFileOutcome.Loaded, CouldContainPlugins: true, null),
            new PluginFileScan("MyWpfPlugin.dll", PluginFileOutcome.NotHostable, CouldContainPlugins: true,
                "references PresentationFramework, which only exists on Windows; this plugin needs an Avalonia build"),
        }));
        PluginsDialogViewModel viewModel = new PluginsDialogViewModel(
            Mock.Of<IDialogService>(), pluginManager.Object, Mock.Of<IClipboardService>());

        using ScreenshotWindow window = PrScreenshot.ShowDialog(viewModel);
        // The refused row sorts after the running plugins; the "before" side has no such row.
        CheckBox? refused = window.FindAll<CheckBox>().FirstOrDefault(row => RowText(row).StartsWith("MyWpfPlugin", StringComparison.Ordinal));
        CheckBox last = refused ?? window.FindAll<CheckBox>().Last();
        last.BringIntoView();
        if (refused != null)
        {
            window.HoverForToolTip(refused);
        }
        window.Save("manage-plugins-refused-plugin");
        window.Click(window.Find<TabItem>(tab => tab.Header as string == "Folder scan"));
        window.Save("manage-plugins-refused-plugin-folder-scan");
    });

    private static PluginsDialogViewModel CreateViewModel() => new PluginsDialogViewModel(
        Mock.Of<IDialogService>(),
        TestAppBuilder.Services.GetRequiredService<IPluginManager>(),
        Mock.Of<IClipboardService>());

    private static string RowText(CheckBox row) =>
        string.Concat(row.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text));
}
