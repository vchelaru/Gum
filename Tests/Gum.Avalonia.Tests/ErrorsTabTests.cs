using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Gum.Avalonia.Panels;
using Gum.Avalonia.Shell;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.Errors;
using Gum.Services;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>The Errors tab in the head's main panel.</summary>
public class ErrorsTabTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    [AvaloniaFact]
    public void Rows_ShowErrorsThatArrivedWhileAnotherTabWasSelected()
    {
        // The Errors plugin refreshes on selection and project load, usually while another bottom
        // tab is the one showing.
        AvaloniaTabManager tabs = ActivatorUtilities.CreateInstance<AvaloniaTabManager>(Services);
        AllErrorsViewModel viewModel = new AllErrorsViewModel(
            Services.GetRequiredService<IClipboardService>(),
            Services.GetRequiredService<IFileSystemRevealService>());
        AvaloniaPluginTab other = (AvaloniaPluginTab)tabs.AddControl(new TextBlock { Text = "Other" }, "Other", TabLocation.RightBottom);
        AvaloniaPluginTab errorsTab = (AvaloniaPluginTab)tabs.AddControl(viewModel, "Errors", TabLocation.RightBottom);
        Window window = new Window { Content = new MainPanelView(tabs), Width = 1200, Height = 800 };
        window.Show();
        TabControl region = window.GetVisualDescendants().OfType<TabControl>()
            .Single(tabControl => tabControl.Items.Contains(errorsTab));
        region.SelectedItem = other;
        window.UpdateLayout();

        viewModel.Errors.Add(new ErrorViewModel { Message = "First" });
        viewModel.Errors.Add(new ErrorViewModel { Message = "Second" });
        region.SelectedItem = errorsTab;
        window.UpdateLayout();

        ErrorsView errors = window.GetVisualDescendants().OfType<ErrorsView>().Single();
        errors.GetRealizedContainers().Count().ShouldBe(2);
        window.Close();
    }
}
