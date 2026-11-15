using Avalonia.Controls;
using Avalonia.Styling;
using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Gum.Dialogs;
using Gum.Plugins;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.SettingsWindow;
using Gum.ToolStates;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Application > Plugins page of the Settings window in the real window and theme, one contact sheet
/// (zoom 9, 12, 16 and 24 on one PNG) per state and theme
/// (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter PluginsSettingsScreenshotTests</c>). Uses only API
/// that main already has, so the same file renders the "before" side.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class PluginsSettingsScreenshotTests
{
    [SkippableTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void RealPlugins(bool dark) => PrScreenshot.Run(() =>
    {
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        Save(viewModel, dark, $"settings-plugins-real-{(dark ? "dark" : "light")}");
    });

    // One of every kind of plugin: required, running, turned off, crashed, and found but never loaded.
    [SkippableTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void MixedStates(bool dark) => PrScreenshot.Run(() =>
    {
        IServiceProvider services = TestAppBuilder.Services;
        Mock<IPluginManager> pluginManager = new Mock<IPluginManager>();
        pluginManager.Setup(x => x.GetAllPluginSummaries()).Returns(new[]
        {
            new PluginSummary("Main Output", "Main Output", true, false, new object(), CanBeDisabled: false),
            new PluginSummary("Code Output", "Code Output", true, false, new object()),
            new PluginSummary("Skia Preview", "Skia Preview", false, false, new object()),
            new PluginSummary("My Exporter", "My Exporter(Failed in StartUp)", false, true, new object()),
        });
        pluginManager.Setup(x => x.GetPluginScanReport()).Returns(new PluginScanReport(@"C:\Gum\Plugins", FolderExists: true, new[]
        {
            new PluginFileScan("CodeOutputPlugin.dll", PluginFileOutcome.Loaded, CouldContainPlugins: true, null),
            new PluginFileScan("MyWpfPlugin.dll", PluginFileOutcome.NotHostable, CouldContainPlugins: true,
                "references PresentationFramework, which only exists on Windows; this plugin needs an Avalonia build"),
        }));
        SettingsDialogViewModel viewModel = new SettingsDialogViewModel(
            services.GetRequiredService<ISettingsPageRegistry>(),
            services.GetRequiredService<IProjectState>(),
            services.GetRequiredService<GeneralSettingsViewModel>(),
            services.GetRequiredService<ThemingDialogViewModel>(),
            new PluginsDialogViewModel(Mock.Of<IDialogService>(), pluginManager.Object, Mock.Of<IClipboardService>()));
        Save(viewModel, dark, $"settings-plugins-mixed-{(dark ? "dark" : "light")}");
    });

    [SkippableTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithSearch(bool dark) => PrScreenshot.Run(() =>
    {
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        Save(viewModel, dark, $"settings-plugins-search-{(dark ? "dark" : "light")}", search: "output");
    });

    private static void Save(SettingsDialogViewModel viewModel, bool dark, string name, string search = "")
    {
        using ScreenshotWindow window = PrScreenshot.ShowDialog(viewModel, dark ? ThemeVariant.Dark : ThemeVariant.Light);
        viewModel.SelectPage(SettingsPageIds.Plugins);
        // Tall enough that the whole page shows at the largest zoom.
        window.Window.SizeToContent = SizeToContent.Manual;
        window.Window.MaxHeight = double.PositiveInfinity;
        window.Window.Height = 1100;
        viewModel.SearchText = search;
        try
        {
            PrScreenshot.SaveContactSheet(window.Window, name);
        }
        finally
        {
            viewModel.SearchText = "";
        }
    }
}
